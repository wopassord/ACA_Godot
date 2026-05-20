using System.Collections.Generic;
// This code mocks the original tts logic to offer the BML something to work with until we have the final version from Josiane
namespace TextToSpeech
{
    public class Engine
    {
        public int VoiceCount { get { return 1; } }
        public int VoiceID(string name) { return 0; }
    }

    public class Channel
    {
        public Channel(Engine e, int voiceId) { }

        public SpeechData Speak(string text, bool useSSML)
        {
            SpeechData sdata = new SpeechData();
            
            // we calculate 0.1 seconds per letter
            int length = text.Length > 0 ? text.Length : 1;
            sdata.Phonemes = new SpeechData.Phoneme[length + 1];
            
            float currentTime = 0f;
            for (int i = 0; i < text.Length; i++)
            {
                sdata.Phonemes[i] = new SpeechData.Phoneme
                {
                    Name = text[i].ToString(), // We use the letter as an auxiliary viseme
                    Type = SpeechData.PhonemeType.PHONEME,
                    Start = currentTime,
                    End = currentTime + 0.1f,
                    Stress = 0
                };
                currentTime += 0.1f;
            }

            // Final timemarker required by the code
            sdata.Phonemes[length] = new SpeechData.Phoneme
            {
                Name = "end",
                Type = SpeechData.PhonemeType.TIMEMARKER,
                Start = currentTime,
                End = currentTime,
                Stress = 0
            };

            // Void audio to not break the IAnimationEngine
            sdata.Audiobuf = new short[0]; 
            sdata.Audiorate = 44100;

            return sdata;
        }
    }

    public class SpeechData
    {
        public enum PhonemeType { PHONEME, TIMEMARKER }
        public struct Phoneme
        {
            public string Name;
            public PhonemeType Type;
            public float Start;
            public float End;
            public float Stress;
        }

        public Phoneme[] Phonemes;
        public short[] Audiobuf;
        public int Audiorate;
    }
}