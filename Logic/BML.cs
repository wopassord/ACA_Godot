//----------------------------------------------------------------------------
// Copyright (C) 2019-2021, Elisabetta BEVACQUA, Fabrice HARROUET (ENIB)
//
// Permission to use, copy, modify, distribute and sell this software
// and its documentation for any purpose is hereby granted without fee,
// provided that the above copyright notice appear in all copies and
// that both that copyright notice and this permission notice appear
// in supporting documentation.
// The authors make no representations about the suitability of this
// software for any purpose.
// It is provided "as is" without express or implied warranty.
//----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bml
{
    public enum Composition { APPEND, MERGE, REPLACE };

    public interface Bml
    {
        string Id { get; }
        double StartTime { get; }
        IEnumerable<Signal> Signals { get; }
        Signal FindSignal(string signal_id);
    }

    public interface Signal
    {
        string Id { get; }
        string Modality { get; }
        bool Shift { get; }
        Bml Bml { get; }
        IEnumerable<Synchro> Synchros { get; }
        Synchro FindSynchro(string synchro_id);
    }

    public interface Synchro
    {
        string Id { get; }
        string Expr { get; }
        double Time { get; }
    }

    public interface Event
    {
        Bml Bml { get; }
        Signal Signal { get; }
        Synchro Synchro { get; }
    }

    public delegate void Logger(string msg);

    public delegate void EventAction(Event evt);

    public delegate bool SynchroSpecifier(Signal signal);

    public class Scheduler
    {
        public Scheduler(double init_time, string end_synchro,
            string speech_modality, double speech_stress_influence,
            Logger logger=null)
        {
            end_synchro_ = end_synchro;
            speech_modality_ = speech_modality;
            speech_stress_influence_ = speech_stress_influence;
            bml_list_ = new List<BmlImpl>();
            shift_signals_ = new Dictionary<string, SignalImpl>();
            schedule_ = new List<EventImpl>();
            now_ = init_time;
            in_step_ = false;
            in_specifier_ = false;
            must_recompute_ = false;
            bml_queue_ = new Queue<BmlImpl>();
            logger_=logger;
        }

        public void Log(string msg)
        {
            if (logger_!=null)
            {
                logger_(msg);
            }
        }

        public void Log(string format, params Object[] args)
        {
            Log(String.Format(format, args));
        }

        public bool HasBml(string bml_id)
        {
            return bml_list_.Exists(x => x.id_ == bml_id);
        }

        public bool HasSignal(string bml_id, string signal_id)
        {
            var bml = bml_list_.Find(x => x.id_ == bml_id);
            return
                bml != null &&
                bml.signals_.Exists(x => x.id_ == signal_id);
        }

        public static bool ValidTime(double time)
        {
            return time >= 0.0;
        }

        public static bool InvalidTime(double time)
        {
            return !ValidTime(time);
        }

        public bool Empty { get { return bml_list_.Count == 0; } }

        public double Now { get { return now_; } }

        public Bml AddBml(string id, Composition composition,
            IEnumerable<Signal> signals)
        {
            var new_bml = new BmlImpl(id, composition, signals);
            bml_queue_.Enqueue(new_bml);
            return new_bml;
        }

        public Signal NewSignal(string id, string modality, bool shift,
            string speech_stress_synchro,
            EventAction action, SynchroSpecifier specifier,
            IEnumerable<Synchro> synchros)
        {
            return new SignalImpl(id, modality, shift, speech_stress_synchro,
                action, specifier, synchros);
        }

        public Synchro NewSynchro(string id, string expr, bool stress = false)
        {
            return new SynchroImpl(id, expr, stress);
        }

        public Event FindEvent(string reference)
        {
            return FindEvent_(ParseReference_(reference));
        }

        public void ChangeSynchro(Synchro synchro, string expr)
        {
            if (in_specifier_)
            {
                throw new Exception(
                    "unexpected usage of 'ChangeSynchro()'");
            }
            var synchro_impl=(SynchroImpl)synchro;
            synchro_impl.expr_ = expr;
            must_recompute_ = true;
        }

        public void SpecifySynchro(Signal signal, Synchro synchro, double time)
        {
            if (!in_specifier_)
            {
                throw new Exception(
                    "unexpected usage of 'SpecifySynchro()'");
            }
            var synchro_impl=(SynchroImpl)synchro;
            synchro_impl.time_ = UNRESOLVED_TIME;
            synchro_impl.offset_ = time - signal.Bml.StartTime;
            synchro_impl.expr_ = synchro_impl.offset_.ToString(ci_);
            synchro_impl.ref_id_ = new RefId();
        }

        public void Step(double dt)
        {
            if (in_step_)
            {
                throw new Exception(
                    "recursive invocation of 'Step()'");
            }
            in_step_ = true;
            now_ += dt;
            while(bml_queue_.Count != 0)
            {
                DequeueBml_();
            }
            if (must_recompute_)
            {
                ComputeFirstAppendStartTime_();
                var not_done = CollectNotDone_();
                ComputeSynchroTimes_(not_done);
                GenerateSchedule_(not_done);
                PurgeUnusedBml_();
                //DisplayBml_();
                must_recompute_ = false;
            }
            while (schedule_.Count != 0)
            {
                var next = schedule_[schedule_.Count - 1];
                if (next.synchro_.time_ > now_)
                {
                    break;
                }
                schedule_.RemoveAt(schedule_.Count - 1);
                var bml = next.bml_;
                var signal = next.signal_;
                var synchro = next.synchro_;
                synchro.done_ = true;
                signal.current_ = synchro;
                if (signal.shift_)
                {
                    SignalImpl prev = null;
                    shift_signals_.TryGetValue(signal.modality_, out prev);
                    if (prev != signal)
                    {
                        if (prev != null)
                        {
                            Log("ABORT {0}:{1}:{2}",
                                prev.bml_.id_, prev.id_, prev.current_.id_);
                            prev.action_(new EventImpl(prev.bml_, prev, null));
                            prev.done_ = true;
                            ++prev.bml_.done_count_;
                        }
                        shift_signals_[signal.modality_] = signal;
                    }
                }
                signal.action_(new EventImpl(bml, signal, synchro));
                if (signal.current_.id_ == end_synchro_)
                {
                    if (signal.shift_)
                    {
                        shift_signals_[signal.modality_] = null;
                    }
                    signal.done_ = true;
                    ++bml.done_count_;
                    if (NextAppendCanStart_(bml))
                    {
                        must_recompute_ = true;
                    }
                }
            }
            in_step_ = false;
        }

        private void DequeueBml_()
        {
            var new_bml = bml_queue_.Dequeue();
            if (new_bml.composition_ == Composition.REPLACE)
            {
                foreach (var bml in bml_list_)
                {
                    foreach (var signal in bml.signals_)
                    {
                        if (signal.current_ != null && !signal.done_)
                        {
                            Log("ABORT {0}:{1}:{2}",
                                bml.id_, signal.id_, signal.current_.id_);
                            signal.action_(new EventImpl(bml, signal, null));
                        }
                    }
                }
                bml_list_.Clear();
                foreach (var key in shift_signals_.Keys)
                {
                    shift_signals_[key] = null;
                }
            }
            new_bml.start_time_ = new_bml.composition_ == Composition.APPEND
                ? UNKNOWN_TIME
                : now_;
            bml_list_.Add(new_bml);
            must_recompute_ = true;
        }

        private bool Done_(BmlImpl bml)
        {
            return bml.done_count_ == bml.signals_.Count;
        }

        private bool NextAppendCanStart_(BmlImpl bml)
        {
            if (Done_(bml))
            {
                return true;
            }
            foreach (var signal in bml.signals_)
            {
                if (!signal.shift_ && !signal.done_)
                {
                    return false;
                }
            }
            return true;
        }

        private void ComputeFirstAppendStartTime_()
        {
            BmlImpl first_append = null;
            foreach (var bml in bml_list_)
            {
                if (InvalidTime(bml.start_time_))
                {
                    if (first_append == null)
                    {
                        first_append = bml;
                        // remove break if next MERGEs must also be waited for
                        break;
                    }
                }
                else
                {
                    if (bml.start_time_ < now_ && !NextAppendCanStart_(bml))
                    {
                        first_append = null;
                        break;
                    }
                }
            }
            if (first_append != null)
            {
                first_append.start_time_ = now_;
            }
        }

        private List<EventImpl> CollectNotDone_()
        {
            var not_done = new List<EventImpl>();
            foreach (var bml in bml_list_)
            {
                bml.ref_count_ = 0;
                if (Done_(bml))
                {
                    // do not recompute what has already been triggered
                    continue;
                }
                foreach (var signal in bml.signals_)
                {
                    if (signal.done_)
                    {
                        // do not recompute what has already been triggered
                        continue;
                    }
                    foreach (var synchro in signal.synchros_)
                    {
                        if (synchro.done_)
                        {
                            // do not recompute what has already been triggered
                            continue;
                        }
                        not_done.Add(new EventImpl(bml, signal, synchro));
                        synchro.ref_id_ = new RefId();
                        synchro.offset_ = 0.0;
                        synchro.time_ = UNRESOLVED_TIME;
                        var expr = synchro.expr_;
                        if (!Double.TryParse(expr, ns_, ci_,
                            out synchro.offset_))
                        {
                            // expr is not a simple real value
                            var sign_position =
                                expr.LastIndexOfAny(new char[] { '+', '-' });
                            if (sign_position >= 0)
                            {
                                var offset_str =
                                    expr.Substring(sign_position)
                                        .Replace(" ", "");
                                expr = expr.Substring(0, sign_position);
                                synchro.offset_ =
                                    Double.Parse(offset_str, ns_, ci_);
                            }
                            synchro.ref_id_ =
                                ParseReference_(expr, bml.id_, signal.id_);
                        }
                    }
                }
            }
            return not_done;
        }

        private void ComputeSynchroTimes_(List<EventImpl> not_done)
        {
            // two passes might be necessary if some
            // synchro points were left unspecified
            for (;;)
            {
                var referenced = new List<BmlImpl>();
                var unspecified = new List<SignalImpl>();
                foreach (var evt in not_done)
                {
                    var stack = new Stack<EventImpl>();
                    stack.Push(evt);
                    while (true)
                    {
                        var top = stack.Peek();
                        var bml = top.bml_;
                        var signal = top.signal_;
                        var synchro = top.synchro_;
                        if (InvalidTime(bml.start_time_))
                        {
                            synchro.time_ = UNKNOWN_TIME;
                        }
                        else if (String.IsNullOrEmpty(synchro.ref_id_.synchro_))
                        {
                            // no dependency
                            synchro.time_ = bml.start_time_ +
                                Math.Max(synchro.offset_, 0.0);
                        }
                        else if (synchro.ref_id_.synchro_ == "???")
                        {
                            // external event
                            synchro.time_ = UNKNOWN_TIME;
                        }
                        else if (synchro.ref_id_.synchro_ == "!!!")
                        {
                            // unspecified synchro
                            synchro.time_ = UNSPECIFIED_TIME;
                            if (!unspecified.Contains(signal))
                            {
                                unspecified.Add(signal);
                            }
                        }
                        var time = synchro.time_;
                        if (time != UNRESOLVED_TIME)
                        {
                            // already solved, then propagate
                            if(time == UNSPECIFIED_TIME)
                            {
                                // previous events depend on unspecified
                                time = UNKNOWN_TIME;
                            }
                            stack.Pop();
                            while (stack.Count != 0)
                            {
                                var prev = stack.Pop();
                                if (ValidTime(time))
                                {
                                    time = Math.Max(
                                        prev.bml_.start_time_,
                                        time + prev.synchro_.offset_);
                                }
                                prev.synchro_.time_ = time;
                            }
                            break; // done with this synchro point
                        }
                        // need to solve the reference first
                        var ref_evt = FindEvent_(synchro.ref_id_);
                        if (ref_evt == null)
                        {
                            throw new Exception(String.Format(
                                "cannot find '{0}'",
                                synchro.ref_id_));
                        }
                        if (stack.Contains(ref_evt))
                        {
                            throw new Exception(String.Format(
                                "loop detected for '{0}'",
                                synchro.ref_id_));
                        }
                        referenced.Add(ref_evt.bml_);
                        stack.Push(ref_evt);
                    }
                }
                var just_specified = false;
                foreach (var signal in unspecified)
                {
                    if (SpecifySynchros_(signal))
                    {
                        just_specified = true;
                    }
                }
                if (just_specified)
                {
                    // recompute everything in order to
                    // determine correct reference count
                    foreach (var evt in not_done)
                    {
                        evt.synchro_.time_ = UNRESOLVED_TIME;
                    }
                }
                else
                {
                    // nothing was left unspecified
                    // then consider it's done
                    foreach (var bml in referenced)
                    {
                        ++bml.ref_count_;
                    }
                    break;
                }
            }
            SpeechSynchronize_(not_done);
        }

        private void GenerateSchedule_(List<EventImpl> not_done)
        {
            schedule_.Clear();
            foreach (var evt in not_done)
            {
                if (ValidTime(evt.synchro_.time_))
                {
                    schedule_.Add(evt);
                }
            }
            schedule_.Sort(delegate (EventImpl evt1, EventImpl evt2)
            {
                return evt2.synchro_.time_.CompareTo(evt1.synchro_.time_);
            });
        }

        private void PurgeUnusedBml_()
        {
            var used = new List<BmlImpl>();
            foreach (var bml in bml_list_)
            {
                if (bml.ref_count_ != 0 || !Done_(bml))
                {
                    used.Add(bml);
                }
            }
            bml_list_ = used;
        }

        private static RefId ParseReference_(string reference,
            string from_bml = "", string from_signal = "")
        {
            var ref_list = reference.Replace(" ", "").Split(':');
            switch (ref_list.Length)
            {
                case 1:
                {
                    return new RefId(from_bml, from_signal, ref_list[0]);
                }
                case 2:
                {
                    return new RefId(from_bml, ref_list[0], ref_list[1]);
                }
                case 3:
                {
                    return new RefId(ref_list[0], ref_list[1], ref_list[2]);
                }
                default:
                {
                    throw new Exception(String.Format(
                        "incorrect reference '{0}'",
                        reference));
                }
            }
        }

        private EventImpl FindEvent_(RefId ref_id)
        {
            var bml =
                bml_list_.Find(x => x.Id == ref_id.bml_);
            if (bml != null)
            {
                var signal =
                    bml.signals_.Find(x => x.Id == ref_id.signal_);
                if (signal != null)
                {
                    var synchro =
                        signal.synchros_.Find(x => x.Id == ref_id.synchro_);
                    if (synchro != null)
                    {
                        return new EventImpl(bml, signal, synchro);
                    }
                }
            }
            return null;
        }

        private bool SpecifySynchros_(SignalImpl signal)
        {
            var invalid = 0;
            var unspecified = 0;
            foreach (var synchro in signal.synchros_)
            {
                if (InvalidTime(synchro.time_))
                {
                    ++invalid;
                    if (synchro.time_ == UNSPECIFIED_TIME)
                    {
                        ++unspecified;
                    }
                }
            }
            var result = false;
            if (unspecified != 0 && // some unspecified times
                invalid == unspecified ) // but no other invalid times
            {
                in_specifier_ = true;
                result = signal.specifier_(signal);
                in_specifier_ = false;
            }
            return result;
        }

        private void SpeechSynchronize_(List<EventImpl> not_done)
        {
            if (String.IsNullOrEmpty(speech_modality_) ||
                InvalidTime(speech_stress_influence_))
            {
                return;
            }
            // detect all signals and speech stress times
            var signals = new List<SignalImpl>();
            var stress_times = new List<double>();
            foreach (var evt in not_done)
            {
                var signal = evt.signal_;
                if (!signals.Contains(signal))
                {
                    signals.Add(signal);
                    if (signal.modality_ == speech_modality_)
                    {
                        foreach (var synchro in signal.synchros_)
                        {
                            if (synchro.stress_)
                            {
                                var time = synchro.time_;
                                if (ValidTime(time))
                                {
                                    stress_times.Add(time);
                                }
                            }
                        }
                    }
                }
            }
            // adjust each signal to best speech stress time
            foreach (var signal in signals)
            {
                var ref_name = signal.speech_stress_synchro_;
                var ref_synchro = String.IsNullOrEmpty(ref_name) ? null :
                    signal.synchros_.Find(x => x.id_ == ref_name);
                var ref_time =
                    ref_synchro == null ? UNKNOWN_TIME : ref_synchro.time_;
                if (ValidTime(ref_time))
                {
                    var best_delta = Double.MaxValue;
                    var best_stress_time = UNKNOWN_TIME;
                    foreach (var stress_time in stress_times)
                    {
                        var delta = Math.Abs(ref_time - stress_time);
                        if (delta <= speech_stress_influence_ &&
                            delta < best_delta)
                        {
                            best_delta = delta;
                            best_stress_time = stress_time;
                        }
                    }
                    if (ValidTime(best_stress_time))
                    {
                        var offset = best_stress_time - ref_time;
#if true
                        foreach (var synchro in signal.synchros_)
                        {
                            var time = synchro.time_;
                            if (ValidTime(time))
                            {
                                synchro.time_ =
                                    Math.Max(now_, time + offset);
                                if (signal.Modality == "posture")
                                {
                                  Log("POSTURE {0}:{1}:{2}  {3} --> {4}",
                                    signal.Bml.Id, signal.Id, synchro.Id,
                                    time, synchro.time_);
                                }
                            }
                        }
#else
                        var first_time = ref_time;
                        foreach (var synchro in signal.synchros_)
                        {
                            var time = synchro.time_;
                            if (ValidTime(time))
                            {
                                first_time = Math.Min(first_time, time);
                            }
                        }
                        // if the offset would send the first synchro before
                        // now_, then an affine transformation will compress
                        // the synchros in order to keep the ref_time at
                        // best_stress_time and the first synchro at now_.
                        var new_first_time =
                            Math.Max(now_, first_time + offset);
                        var new_ref_time =
                            Math.Max(now_, ref_time + offset);
                        var a_coeff = Math.Abs(first_time - ref_time) < 1e-4
                            ? 1.0
                            : (new_first_time - new_ref_time) /
                              (first_time - ref_time);
                        var b_coeff = new_first_time - a_coeff * first_time;
                        foreach (var synchro in signal.synchros_)
                        {
                            var time = synchro.time_;
                            if (ValidTime(time))
                            {
                                synchro.time_ = a_coeff * time + b_coeff;
                            }
                        }
#endif
                    }
                }
            }
        }

        private void DisplayBml_()
        {
            Log("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~" +
                "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            foreach (var bml in bml_list_)
            {
                var time_str = InvalidTime(bml.start_time_)
                    ? "???"
                    : String.Format("{0:0.###}", bml.start_time_);
                Log("BML: id={0}, composition={1}, ref_count={2}, " +
                    "done_count={3}, start_time={4}",
                    bml.id_, bml.composition_, bml.ref_count_,
                    bml.done_count_, time_str);
                foreach (var signal in bml.signals_)
                {
                    var current_id = signal.current_ != null
                        ? signal.current_.id_
                        : "";
                    Log("  SIGNAL: {0}modality={1}, id={2}, current={3}",
                        signal.shift_ ? "shift_" : "",
                        signal.modality_, signal.id_, current_id);
                    foreach (var synchro in signal.synchros_)
                    {
                        Log("  {0} {1} = {2} {3} --> {4}",
                            synchro == signal.current_ ? '#' : '.',
                            synchro.id_, synchro.ref_id_,
                            OffsetStr_(synchro.offset_),
                            TimeStr_(synchro.time_));
                    }
                }
            }
            Log("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~" +
                "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
        }

        private static string OffsetStr_(double offset)
        {
            return offset < 0.0
                ? String.Format("- {0:0.###}", -offset)
                : String.Format("+ {0:0.###}", offset);
        }

        private static string TimeStr_(double time)
        {
            return time == UNKNOWN_TIME
                ? "???"
                : time == UNRESOLVED_TIME
                    ? "UNRESOLVED"
                    : time == UNSPECIFIED_TIME
                        ? "!!!"
                        : String.Format("{0:0.###}", time);
        }

        private class BmlImpl : Bml
        {
            public string Id { get { return id_; } }
            public double StartTime { get { return start_time_; } }
            public IEnumerable<Signal> Signals { get { return signals_; } }
            public Signal FindSignal(string signal_id)
            {
                return signals_.Find(x => x.id_ == signal_id);
            }

            public BmlImpl(string id, Composition composition,
                IEnumerable<Signal> signals)
            {
                id_ = id;
                composition_ = composition;
                ref_count_ = 0;
                done_count_ = 0;
                start_time_ = UNRESOLVED_TIME;
                signals_ = new List<SignalImpl>();
                foreach (var s in signals)
                {
                    var signal = (SignalImpl)s;
                    signal.bml_ = this;
                    signals_.Add(signal);
                }
            }

            public string id_;
            public Composition composition_;
            public int ref_count_;
            public int done_count_;
            public double start_time_;
            public List<SignalImpl> signals_;
        }

        private class SignalImpl : Signal
        {
            public string Id { get { return id_; } }
            public string Modality { get { return modality_; } }
            public bool Shift { get { return shift_; } }
            public Bml Bml { get { return bml_; } }
            public IEnumerable<Synchro> Synchros { get { return synchros_; } }
            public Synchro FindSynchro(string synchro_id)
            {
                return synchros_.Find(x => x.id_ == synchro_id);
            }

            public SignalImpl(string id, string modality, bool shift,
                string speech_stress_synchro,
                EventAction action, SynchroSpecifier specifier,
                IEnumerable<Synchro> synchros)
            {
                id_ = id;
                modality_ = modality;
                shift_ = shift;
                speech_stress_synchro_ = speech_stress_synchro;
                action_ = action;
                specifier_ = specifier;
                current_ = null;
                done_ = false;
                bml_ = null;
                synchros_ = new List<SynchroImpl>();
                foreach (var s in synchros)
                {
                    synchros_.Add((SynchroImpl)s);
                }
            }

            public string id_;
            public string modality_;
            public bool shift_;
            public string speech_stress_synchro_;
            public EventAction action_;
            public SynchroSpecifier specifier_;
            public SynchroImpl current_;
            public bool done_;
            public BmlImpl bml_;
            public List<SynchroImpl> synchros_;
        }

        private class SynchroImpl : Synchro
        {
            public string Id { get { return id_; } }
            public string Expr { get { return expr_; } }
            public double Time { get { return time_; } }

            public SynchroImpl(string id, string expr, bool stress)
            {
                id_ = id;
                expr_ = expr;
                stress_ = stress;
                ref_id_ = new RefId();
                offset_ = 0.0;
                time_ = UNRESOLVED_TIME;
                done_ = false;
            }

            public string id_;
            public string expr_;
            public bool stress_;
            public RefId ref_id_;
            public double offset_;
            public double time_;
            public bool done_;
        }

        private class EventImpl : Event
        {
            public Bml Bml { get { return bml_; } }
            public Signal Signal { get { return signal_; } }
            public Synchro Synchro { get { return synchro_; } }

            public override string ToString()
            {
                return String.Format(
                    "{0}:{1}:{2}",
                    bml_ != null ? bml_.id_ : "",
                    signal_ != null ? signal_.id_ : "",
                    synchro_ != null ? synchro_.id_ : "");
            }

            public EventImpl(BmlImpl bml, SignalImpl signal,
                SynchroImpl synchro)
            {
                bml_ = bml;
                signal_ = signal;
                synchro_ = synchro;
            }

            public BmlImpl bml_;
            public SignalImpl signal_;
            public SynchroImpl synchro_;
        }

        private struct RefId
        {
            public RefId(string bml = "", string signal = "",
                string synchro = "")
            {
                bml_ = bml;
                signal_ = signal;
                synchro_ = synchro;
            }

            public override string ToString()
            {
                return String.Format("{0}:{1}:{2}", bml_, signal_, synchro_);
            }

            public string bml_;
            public string signal_;
            public string synchro_;
        }

        private static System.Globalization.NumberStyles ns_ =
            System.Globalization.NumberStyles.Float;
        private static System.Globalization.CultureInfo ci_ =
            new System.Globalization.CultureInfo("en-US");

        private const double UNSPECIFIED_TIME = -3.0;
        private const double UNRESOLVED_TIME = -2.0;
        private const double UNKNOWN_TIME = -1.0;

        private string end_synchro_;
        private string speech_modality_;
        private double speech_stress_influence_;
        private List<BmlImpl> bml_list_;
        private Dictionary<string, SignalImpl> shift_signals_;
        private List<EventImpl> schedule_;
        private double now_;
        private bool in_step_;
        private bool in_specifier_;
        private bool must_recompute_;
        private Queue<BmlImpl> bml_queue_;
        private Logger logger_;
    }
} // namespace Bml

//----------------------------------------------------------------------------
