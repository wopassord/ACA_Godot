//----------------------------------------------------------------------------

/*
  Relies on cerevoice_sdk_6.1.0_unity_x86_64_60b24ce_academic

  On a workstation use:
  • cerevoice_eng/other/cerevoice_eng.cs
  • examples/CereProcUnityDemo/Assets/Plugins/
      x86_64/csharplib_linux/libcerevoice_eng.so
      x86_64/csharplib64_windows/cerevoice_eng.dll

  With HoloLens 2 use:
  • cerevoice_eng/uwp/cerevoice_eng.cs
  • examples/CereProcUnityDemo/Assets/Plugins/
      Plugins/WSA/ARM/cerevoice_eng.dll    ???
      Plugins/WSA/ARM64/cerevoice_eng.dll  ???
*/

// FIXME: using a callback seems to be the only way to obtain
//        stress information.
//        Not using a callback prevents from obtaining stress
//        information after the first few sentences!
#define USE_SPEAK_CB

using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

using Lib = cerevoice_engPINVOKE;

namespace TextToSpeech {

public class SpeechData
{
  public enum PhonemeType { PHONEME, TIMEMARKER };

  public struct Phoneme 
  {
    public PhonemeType Type;
    public string Name;
    public int Stress;
    public float Start;
    public float End;
  }

  public short[] Audiobuf
  {
    get { return audiobuf_; }
  }

  public int Audiorate
  {
    get { return audiorate_; }
  }

  public Phoneme[] Phonemes
  {
    get { return phonemes_; }
  }

  public void SaveRaw(string filename)
  {
    if (File.Exists(filename))
    {
      File.Delete(filename);
    }
    using(var bw = new BinaryWriter(new FileStream(
      filename, FileMode.OpenOrCreate, FileAccess.Write)))
    {
      foreach (var sample in audiobuf_)
      {
        bw.Write(sample);
      }
    }
  }

  public void SaveWav(string filename)
  {
    if (File.Exists(filename))
    {
      File.Delete(filename);
    }
    using(var bw = new BinaryWriter(new FileStream(
      filename, FileMode.OpenOrCreate, FileAccess.Write)))
    {
      var channels = 1;
      var bits_per_sample = 16;
      bw.Write((Int32)0x46464952); // 'RIFF'
      bw.Write((Int32)(36 + 2 * audiobuf_.Length)); // chunksize
      bw.Write((Int32)0x45564157); // 'WAVE'
      bw.Write((Int32)0x20746D66); // 'fmt '
      bw.Write((Int32)16); // subchunk1size
      bw.Write((Int16)1); // pcm
      bw.Write((Int16)channels);
      bw.Write((Int32)audiorate_); // sample-rate
      bw.Write((Int32)(audiorate_ * channels * bits_per_sample / 8));
      bw.Write((Int16)4); // block-align
      bw.Write((Int16)bits_per_sample);
      bw.Write((Int32)0x61746164); // 'data'
      bw.Write((Int32)(2 * audiobuf_.Length)); // subchunk2size
      foreach (var sample in audiobuf_)
      {
        bw.Write(sample);
      }
    }
  }

  internal short[] audiobuf_;
  internal int audiorate_;
  internal Phoneme[] phonemes_;
}

public class Channel
{
  public Channel(Engine engine, int voice_index)
  {
    engine_ = engine;
    voice_index_ = voice_index;
    channel_handle_ = Lib.CPRCEN_engine_open_channel(
      href_(engine_.engine_ptr_),
      engine_.voices_[voice_index_].language,
      engine_.voices_[voice_index_].country,
      engine_.voices_[voice_index_].name,
      engine_.voices_[voice_index_].rate.ToString());
    if (channel_handle_ == 0)
    {
      throw new ApplicationException(
        "CPRCEN_engine_open_channel() failure");
    }
  }

  ~Channel()
  {
    if (engine_.engine_ptr_ != IntPtr.Zero)
    {
      if (Lib.CPRCEN_engine_channel_close(
        href_(engine_.engine_ptr_), channel_handle_) == 0)
      {
        throw new ApplicationException(
          "CPRCEN_engine_channel_close() failure");
      }
    }
  }

  public Engine Engine
  {
    get { return engine_; }
  }

  public int VoiceIndex
  {
    get { return voice_index_; }
  }

  public SpeechData Speak(string txt, bool get_phonemes)
  {
    var sd = new SpeechData();
    sd.audiobuf_ = new short[0];
    if (get_phonemes)
    {
      sd.phonemes_ = new SpeechData.Phoneme[0];
    }
    // FIXME: the C# wrapper introduces a string conversion assuming that
    //        the input string uses the default encoding (not UTF8).
    //        Note that the native API directly accepts UTF8.
    byte[] utf8_bytes = Encoding.UTF8.GetBytes(txt);
    var local_encoding = Encoding.GetEncoding(0);
    if (local_encoding != Encoding.UTF8)
    {
      txt = local_encoding.GetString(utf8_bytes);
    }
    txt += new string(' ', utf8_bytes.Length - txt.Length);
#if USE_SPEAK_CB
    var sd_handle = GCHandle.Alloc(sd);
    var cb = Marshal.GetFunctionPointerForDelegate(new speak_cb_(speak_));
    if (Lib.CPRCEN_engine_set_callback(
      href_(engine_.engine_ptr_), channel_handle_,
      href_(GCHandle.ToIntPtr(sd_handle)), href_(cb)) == 0)
    {
      throw new ApplicationException(
        "CPRCEN_engine_set_callback() failure");
    }
    Lib.CPRCEN_engine_channel_speak(
      href_(engine_.engine_ptr_), channel_handle_, txt, txt.Length, 1);
    sd_handle.Free();
#else // not USE_SPEAK_CB
    var abuf_ptr = Lib.CPRCEN_engine_channel_speak(
      href_(engine_.engine_ptr_), channel_handle_, txt, txt.Length, 1);
    if (abuf_ptr == IntPtr.Zero)
    {
      throw new ApplicationException(
        "CPRCEN_engine_channel_speak() failure");
    }
    sd.audiorate_ = Lib.CPRC_abuf_wav_srate(href_(abuf_ptr));
    var wav_sz = Lib.CPRC_abuf_wav_sz(href_(abuf_ptr));
    var wav_data = Lib.CPRC_abuf_wav_data(href_(abuf_ptr));
    Array.Resize(ref sd.audiobuf_, wav_sz);
    Marshal.Copy(wav_data, sd.audiobuf_, 0, sd.audiobuf_.Length);
    if (sd.phonemes_ != null)
    {
      var size = Lib.CPRC_abuf_trans_sz(href_(abuf_ptr));
      Array.Resize(ref sd.phonemes_, size);
      var ph_count = 0;
      for (var i = 0; i < size; ++i)
      {
        var trans_ptr = Lib.CPRC_abuf_get_trans(href_(abuf_ptr), i);
        if (trans_ptr == IntPtr.Zero)
        {
          throw new ApplicationException(
            "CPRC_abuf_get_trans() failure");
        }
        sd.phonemes_[ph_count].Start = // Start time in seconds
          Lib.CPRC_abuf_trans_start(href_(trans_ptr));
        sd.phonemes_[ph_count].End =// End time in seconds
          Lib.CPRC_abuf_trans_end(href_(trans_ptr));
        sd.phonemes_[ph_count].Name = // Label, type dependent
          Lib.CPRC_abuf_trans_name(href_(trans_ptr));
        sd.phonemes_[ph_count].Stress =
          Lib.CPRC_abuf_trans_phone_stress(href_(trans_ptr));
        var type = Lib.CPRC_abuf_trans_type(href_(trans_ptr));
        if (type == (int)CPRC_ABUF_TRANS_TYPE.CPRC_ABUF_TRANS_PHONE)
        {
          sd.phonemes_[ph_count++].Type = SpeechData.PhonemeType.PHONEME;
        }
        else if (type == (int)CPRC_ABUF_TRANS_TYPE.CPRC_ABUF_TRANS_MARK)
        {
          sd.phonemes_[ph_count++].Type = SpeechData.PhonemeType.TIMEMARKER;
        }
      }
      Array.Resize(ref sd.phonemes_, ph_count);
    }
    // clearing callback prevents from repeating the same text next time
    Lib.CPRCEN_engine_clear_callback(
      href_(engine_.engine_ptr_), channel_handle_);
#endif // not USE_SPEAK_CB
    return sd;
  }

#if USE_SPEAK_CB
  internal delegate void speak_cb_(IntPtr abuf_ptr, IntPtr userdata);
#if ENABLE_IL2CPP
  // Unity uses IL2CPP when targetting UWP (see also UNITY_WSA)
  // IL2CPP requires callbacks to be static and have this annotation
  [AOT.MonoPInvokeCallback(typeof(speak_cb_))]
#endif
  internal static
  void
  speak_(IntPtr abuf_ptr, IntPtr userdata)
  {
    var sd = (SpeechData)GCHandle.FromIntPtr(userdata).Target;
    sd.audiorate_ = Lib.CPRC_abuf_wav_srate(href_(abuf_ptr));
    var time_factor = 1.0f / sd.audiorate_;
    var wav_first = Lib.CPRC_abuf_wav_mk(href_(abuf_ptr));
    var wav_last = Lib.CPRC_abuf_wav_done(href_(abuf_ptr));
    var wav_sz = wav_last+1-wav_first;
    var wav_data = Lib.CPRC_abuf_wav_data(href_(abuf_ptr));
    var wav_offset = sd.audiobuf_.Length;
    Array.Resize(ref sd.audiobuf_, wav_offset + wav_sz);
    Marshal.Copy(wav_data + sizeof(short) * wav_first,
                 sd.audiobuf_, wav_offset, wav_sz);
    if (sd.phonemes_ != null)
    {
      var ph_count = sd.phonemes_.Length;
      var first = Lib.CPRC_abuf_trans_mk(href_(abuf_ptr));
      var last = Lib.CPRC_abuf_trans_done(href_(abuf_ptr));
      Array.Resize(ref sd.phonemes_, ph_count + last + 1 - first);
      for (var i = first; i <= last; ++i)
      {
        var trans_ptr = Lib.CPRC_abuf_get_trans(href_(abuf_ptr), i);
        if (trans_ptr == IntPtr.Zero)
        {
          throw new ApplicationException(
            "CPRC_abuf_get_trans() failure");
        }
        var first_sample = Lib.CPRC_abuf_trans_start_sample(href_(trans_ptr));
        var last_sample = Lib.CPRC_abuf_trans_end_sample(href_(trans_ptr));
        sd.phonemes_[ph_count].Start = // Start time in seconds
          time_factor * (wav_offset - wav_first + first_sample);
        sd.phonemes_[ph_count].End = // End time in seconds
          time_factor * (wav_offset - wav_first + last_sample);
        sd.phonemes_[ph_count].Name = // Label, type dependent
          Lib.CPRC_abuf_trans_name(href_(trans_ptr));
        sd.phonemes_[ph_count].Stress =
          Lib.CPRC_abuf_trans_phone_stress(href_(trans_ptr));
        var type = Lib.CPRC_abuf_trans_type(href_(trans_ptr));
        if (type == (int)CPRC_ABUF_TRANS_TYPE.CPRC_ABUF_TRANS_PHONE)
        {
          sd.phonemes_[ph_count++].Type = SpeechData.PhonemeType.PHONEME;
        }
        else if (type == (int)CPRC_ABUF_TRANS_TYPE.CPRC_ABUF_TRANS_MARK)
        {
          sd.phonemes_[ph_count++].Type=SpeechData.PhonemeType.TIMEMARKER;
        }
      }
      Array.Resize(ref sd.phonemes_, ph_count);
    }
  }
#endif

  internal static HandleRef href_(IntPtr ptr)
  {
    return new HandleRef(null, ptr);
  }

  internal Engine engine_;
  internal int voice_index_;
  internal int channel_handle_;
}

public class Engine
{
  public delegate void Logger(string msg);

  public Engine(string voice_path, Logger logger = null)
  {
    logger_=logger;
    engine_ptr_ = Lib.CPRCEN_engine_new();
    if (engine_ptr_ == IntPtr.Zero)
    {
      throw new ApplicationException(
        "CPRCEN_engine_new() failure");
    }
    var voices = Directory.GetFiles(voice_path, "*.voice");
    Array.Sort(voices);
    foreach (var v in voices)
    {
      string license=null;
      string cert=null;
      string key=null;
      string root_cert=null;
      var filename=Path.GetFileName(v);
      var sp=filename.Split('_');
      if ((sp.Length >= 3) && (sp[0] == "cerevoice"))
      {
        var pattern = "CereVoice " + sp[1] + " *";
        foreach (var d in Directory.GetDirectories(voice_path, pattern))
        {
          foreach (var s in Directory.GetFiles(d))
          {
            if (s.EndsWith("license.lic"))
            {
              license=s;
            }
            else if (s.EndsWith("client.crt"))
            {
              cert=s;
            }
            else if (s.EndsWith("client.key"))
            {
              key=s;
            }
            else if (s.EndsWith("root_certificate.pem"))
            {
              root_cert=s;
            }
          }
        }
      }
      if ((license != null) && (cert != null) &&
          (key != null) && (root_cert != null))
      {
        if (Lib.CPRCEN_engine_load_voice(href_(engine_ptr_),
          v, null, (int)CPRC_VOICE_LOAD_TYPE.CPRC_VOICE_LOAD_EMB_AUDIO,
          license, root_cert, cert, key) == 0)
        {
          Log("Cannot load voice file: {0}", v);
        }
      }
      else
      {
        Log("Missing licence files for voice: {0}", v);
      }
    }
    var voice_count = Lib.CPRCEN_engine_get_voice_count(href_(engine_ptr_));
    voices_ = new Voice_[voice_count];
    for (var i=0; i<voice_count; ++i)
    {
      var name = Lib.CPRCEN_engine_get_voice_info(
        href_(engine_ptr_), i, "VOICE_NAME");
      var language = Lib.CPRCEN_engine_get_voice_info(
        href_(engine_ptr_), i, "LANGUAGE_CODE_ISO");
      var country = Lib.CPRCEN_engine_get_voice_info(
        href_(engine_ptr_), i, "COUNTRY_CODE_ISO");
      var rate = Lib.CPRCEN_engine_get_voice_info(
        href_(engine_ptr_), i, "SAMPLE_RATE");
      if (String.IsNullOrEmpty(name) || String.IsNullOrEmpty(language) ||
          String.IsNullOrEmpty(country) || String.IsNullOrEmpty(rate))
      {
        throw new ApplicationException(
          "CPRCEN_engine_get_voice_info() failure");
      }
      voices_[i].name = name;
      voices_[i].language = language;
      voices_[i].country = country;
      voices_[i].rate = Int32.Parse(rate);
    }
  }

  ~Engine()
  {
    Lib.CPRCEN_engine_delete(href_(engine_ptr_));
    engine_ptr_ = IntPtr.Zero;
  }

  public int VoiceCount
  {
    get { return voices_.Length; }
  }

  public string VoiceName(int i)
  {
    return voices_[i].name;
  }

  public string VoiceLanguage(int i)
  {
    return voices_[i].language;
  }

  public string VoiceCountry(int i)
  {
    return voices_[i].country;
  }

  public int VoiceRate(int i)
  {
    return voices_[i].rate;
  }

  public int VoiceID(string name)
  {
    for (var i = 0; i < voices_.Length; ++i)
    {
      if (voices_[i].name.ToLower() == name.ToLower())
      {
        return i;
      }
    }
    return -1;
  }

  public void Log(string msg)
  {
    if (logger_ != null)
    {
      logger_(msg);
    }
  }

  public void Log(string format, params Object[] args)
  {
    Log(String.Format(format, args));
  }

  internal static HandleRef href_(IntPtr ptr)
  {
    return new HandleRef(null, ptr);
  }

  internal struct Voice_
  {
    internal string name;
    internal string language;
    internal string country;
    internal int rate;
  }

  internal Logger logger_;
  internal IntPtr engine_ptr_;
  internal Voice_[] voices_;
}

} // namespace TextToSpeech

//----------------------------------------------------------------------------
