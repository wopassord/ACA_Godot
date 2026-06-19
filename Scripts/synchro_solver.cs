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

namespace Bml
{
    class SynchroSolver
    {
        public static bool Solve2(Scheduler sched, Signal signal,
            double default_duration)
        {
            var s = signal.FindSynchro("start");
            var e = signal.FindSynchro("end");
            var s_time = s.Time;
            var e_time = e.Time;
            var bml_start_time = signal.Bml.StartTime;
            var result = false;
            if (AdjustTime(ref s_time, bml_start_time, -1.0,
                TimeOffset(e_time, -default_duration),
                bml_start_time))
            {
                sched.SpecifySynchro(signal, s, s_time);
                result = true;
            }
            if (AdjustTime(ref e_time, s_time, -1.0,
                s_time + default_duration))
            {
                sched.SpecifySynchro(signal, e, e_time);
                result = true;
            }
            return result;
        }

        public static bool Solve4(Scheduler sched, Signal signal,
            double default_duration, double default_offset)
        {
            var s = signal.FindSynchro("start");
            var rd = signal.FindSynchro("ready");
            var rl = signal.FindSynchro("relax");
            var e = signal.FindSynchro("end");
            var s_time = s.Time;
            var rd_time = rd.Time;
            var rl_time = rl.Time;
            var e_time = e.Time;
            var bml_start_time = signal.Bml.StartTime;
            var result = false;
            if (AdjustTime(ref s_time, bml_start_time, -1.0,
                TimeOffset(rd_time, -default_offset),
                TimeOffset(rl_time, default_offset - default_duration),
                TimeOffset(e_time, -default_duration),
                bml_start_time))
            {
                sched.SpecifySynchro(signal, s, s_time);
                result = true;
            }
            if (AdjustTime(ref e_time, s_time, -1.0,
                TimeOffset(rl_time, default_offset),
                TimeOffset(rd_time, default_duration - default_offset),
                s_time + default_duration))
            {
                sched.SpecifySynchro(signal, e, e_time);
                result = true;
            }
            var offset = Math.Min(default_offset, 0.5 * (e_time - s_time));
            if (AdjustTime(ref rd_time, s_time, e_time,
                s_time + offset))
            {
                sched.SpecifySynchro(signal, rd, rd_time);
                result = true;
            }
            if (AdjustTime(ref rl_time, rd_time, e_time,
                e_time - offset))
            {
                sched.SpecifySynchro(signal, rl, rl_time);
                result = true;
            }
            return result;
        }

        public static bool Solve7(Scheduler sched, Signal signal,
            double default_duration, double rd_prop, double ss_prop,
            double str_prop, double se_prop, double rl_prop)
        {
            var s = signal.FindSynchro("start");
            var rd = signal.FindSynchro("ready");
            var ss = signal.FindSynchro("stroke_start");
            var str = signal.FindSynchro("stroke");
            var se = signal.FindSynchro("stroke_end");
            var rl = signal.FindSynchro("relax");
            var e = signal.FindSynchro("end");
            var s_time = s.Time;
            var rd_time = rd.Time;
            var ss_time = ss.Time;
            var str_time = str.Time;
            var se_time = se.Time;
            var rl_time = rl.Time;
            var e_time = e.Time;
            var bml_start_time = signal.Bml.StartTime;
            var result = false;
            if (AdjustTime(ref s_time, bml_start_time, -1.0,
                TimeOffset(str_time, -default_duration * str_prop),
                TimeOffset(ss_time, -default_duration * ss_prop),
                TimeOffset(se_time, -default_duration * se_prop),
                TimeOffset(rd_time, -default_duration * rd_prop),
                TimeOffset(rl_time, -default_duration * rl_prop),
                TimeOffset(e_time, -default_duration),
                bml_start_time))
            {
                sched.SpecifySynchro(signal, s, s_time);
                result = true;
            }
            if (AdjustTime(ref e_time, s_time, -1.0,
                TimeOffset(str_time, default_duration * (1.0 - str_prop)),
                TimeOffset(se_time, default_duration * (1.0 - se_prop)),
                TimeOffset(ss_time, default_duration * (1.0 - ss_prop)),
                TimeOffset(rl_time, default_duration * (1.0 - rl_prop)),
                TimeOffset(rd_time, default_duration * (1.0 - rd_prop)),
                s_time + default_duration))
            {
                sched.SpecifySynchro(signal, e, e_time);
                result = true;
            }
            var duration = e_time - s_time;
            if (AdjustTime(ref str_time, s_time, e_time,
                s_time + duration * str_prop))
            {
                sched.SpecifySynchro(signal, str, str_time);
                result = true;
            }
            var actual_str_prop = duration == 0.0 ? 1.0 :
                (str_time - s_time) / duration;
            var actual_factor = str_prop == 0.0 ? 1.0 :
                actual_str_prop / str_prop;
            var actual_ss_prop = ss_prop * actual_factor;
            var actual_rd_prop = rd_prop * actual_factor;
            if (AdjustTime(ref ss_time, s_time, str_time,
                s_time +  duration * actual_ss_prop))
            {
                sched.SpecifySynchro(signal, ss, ss_time);
                result = true;
            }
            if (AdjustTime(ref rd_time, s_time, ss_time,
                s_time + actual_rd_prop * duration))
            {
                sched.SpecifySynchro(signal, rd, rd_time);
                result = true;
            }
            var actual_comp_factor = str_prop == 1.0 ? 1.0 :
                (1.0 - actual_str_prop) / (1.0 - str_prop);
            var actual_comp_se_prop = (1.0 - se_prop) * actual_comp_factor;
            var actual_comp_rl_prop = (1.0 - rl_prop) * actual_comp_factor;
            if (AdjustTime(ref se_time, str_time, e_time,
                e_time - duration * actual_comp_se_prop))
            {
                sched.SpecifySynchro(signal, se, se_time);
                result = true;
            }
            if (AdjustTime(ref rl_time, se_time, e_time,
                e_time - duration * actual_comp_rl_prop))
            {
                sched.SpecifySynchro(signal, rl, rl_time);
                result = true;
            }
            return result;
        }

        static bool ValidTime(double time)
        {
            return Scheduler.ValidTime(time);
        }

        static bool InvalidTime(double time)
        {
            return Scheduler.InvalidTime(time);
        }

        static double TimeOffset(double time, double offset)
        {
            return ValidTime(time) ? time + offset : time;
        }

        static bool AdjustTime(ref double time, double low_bound,
            double high_bound, params double[] candidates)
        {
            if (ValidTime(time))
            {
                if (ValidTime(low_bound) && time < low_bound)
                {
                    time = low_bound;
                    return true; // adjusted
                }
                if (ValidTime(high_bound) && time > high_bound)
                {
                    time = high_bound;
                    return true; // adjusted
                }
                return false; // not adjusted
            }
            foreach (var candidate in candidates)
            {
                if (ValidTime(candidate))
                {
                    var t = candidate;
                    if(ValidTime(low_bound) && t < low_bound)
                    {
                        t = low_bound;
                    }
                    if(ValidTime(high_bound) && t > high_bound)
                    {
                        t = high_bound;
                    }
                    time = t;
                    return true; // adjusted
                }
            }
            throw new Exception(
                "no suitable time found for synchro adjustment");
        }
    }
} // namespace Bml

//----------------------------------------------------------------------------
