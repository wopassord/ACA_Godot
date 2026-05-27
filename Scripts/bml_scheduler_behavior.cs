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
using Bml;
using Godot;

// #if USING_UNITY3D
// using UnityEngine;
// #else
//     public class MonoBehaviour {}
//     public class Time
//     {
//         public static double time { get { return now_ += 0.1; } }
//         private static double now_ = 0.0;
//     }
//     public class Debug
//     {
//         public static void Log(string msg) { Console.WriteLine(msg); }
//     }
//     public class Shape
//     {
//         public class Phase
//         {
//             public Phase(string n, double t) { name = n; time = t; }
//             public string name;
//             public double time;
//         }
//         public double min_duration_ = 1.0;
//         public double max_duration_ = 5.0;
//         public double duration = 3.0;
//         public List<Phase> phases = new List<Phase>{
//             new Phase("ready", 0.5),
//             new Phase("stroke_start", 1.0),
//             new Phase("stroke", 1.5),
//             new Phase("stroke_end", 2.0),
//             new Phase("relax", 2.5)};
//     }
// #endif

public class BmlSchedulerBehaviour
{

    private Scheduler sched_;
    public Scheduler Sched { get { return sched_; } }
    public BmlSchedulerBehaviour()
    {
    }

    public void Init(float absoluteTime)
    {
        sched_ = new Scheduler(absoluteTime, "end", "speech", 0.0,
            delegate (string msg)
            {
                GD.Print("[BML] " + msg);
            });
    }

    public void Step(float absoluteTime)
    {
        if (sched_ != null)
        {
            sched_.Step(absoluteTime - sched_.Now);
        }

    }


}

public class SignalBuilder
{
    static double last_time = -1.0;
    static System.Random random_ = new System.Random();

    static string Prefix(Scheduler sched, Bml.Event evt)
    {
        var pfx = "";
        if (sched.Now != last_time)
        {
            last_time = sched.Now;
            pfx += String.Format("~~~~ now={0:0.0##} ~~~~\n", last_time);
        }
        pfx += "  ";
        if (evt.Synchro != null)
        {
            pfx += String.Format("@{0:0.0##} ", evt.Synchro.Time);
        }
        var signal = evt.Signal;
        pfx += String.Format("{0}\n    {1}{2}",
            evt,
            signal.Shift ? "Shift_" : "",
            char.ToUpper(signal.Modality[0]) + signal.Modality.Substring(1));
        return pfx;
    }

    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    public static Bml.Signal Speech(Scheduler sched, VirtualAgent.FaceEngine fe, string id,
        TextToSpeech.SpeechData sd,
        IEnumerable<Synchro> synchros)
    {
        var modality = "speech";
        var shift = false;
        string speech_stress_synchro = null;
        EventAction action = delegate (Bml.Event evt)
        {
            //sched.Log("{0}()", Prefix(sched, evt));
            fe.PlaySpeech(sched, evt, sd);
        };
        SynchroSpecifier specifier = null;
        return sched.NewSignal(
            id, modality, shift, speech_stress_synchro,
            action, specifier, synchros);
    }

    public static Bml.Signal Gaze(Scheduler sched, string id, VirtualAgent.FaceEngine fe, bool shift,
        string target,
        IEnumerable<Synchro> synchros)
    {
        var modality = "gaze";
        string speech_stress_synchro = "ready";
        EventAction action = delegate (Bml.Event evt)
        {
            //sched.Log("{0}(target={1})", Prefix(sched, evt), target);
            fe.PlayGaze(sched, evt, target);
        };
        var default_duration = 1.0 + random_.NextDouble() * 3.0;
        var default_offset = 0.25;
        SynchroSpecifier specifier = delegate (Bml.Signal signal)
        {
            return SynchroSolver.Solve4(
                sched, signal, default_duration,
                default_offset);
        };
        return sched.NewSignal(
            id, modality, shift, speech_stress_synchro,
            action, specifier, synchros);
    }

    public static Bml.Signal Face(Scheduler sched, string id,
        string lexeme, double amount, bool shift, List<VirtualAgent.ActionUnit> aus, VirtualAgent.FaceEngine fe,
        IEnumerable<Synchro> synchros)
    {
        var modality = "face";
        var speech_stress_synchro = "ready";
        EventAction action = delegate (Bml.Event evt)
        {
            //sched.Log("{0}(lexeme={1}, amount={2})",Prefix(sched, evt), lexeme, amount);
            fe.PlayFace(sched, evt, lexeme, amount, aus);
        };
        var default_duration = 1.0 + random_.NextDouble() * 3.0;
        var default_offset = 0.25;
        SynchroSpecifier specifier = delegate (Bml.Signal signal)
        {
            return SynchroSolver.Solve4(
                sched, signal, default_duration,
                default_offset);
        };
        return sched.NewSignal(
            id, modality, shift, speech_stress_synchro,
            action, specifier, synchros);
    }

    public static Bml.Signal Head(Scheduler sched, string id, VirtualAgent.FaceEngine fe_, VirtualAgent.HeadShape headShape_, bool shift,
        string lexeme, string target, double amount, int repetition,
        IEnumerable<Synchro> synchros)
    {
        var modality = "head";
        string speech_stress_synchro = "ready"; // "stroke";
        EventAction action = delegate (Bml.Event evt)
        {
            //sched.Log("{0}(lexeme={1}, target={2}, amount={3})",Prefix(sched, evt), lexeme, target, amount);
            fe_.PlayHead(sched, evt, headShape_, repetition, amount, target);
        };
        var default_duration = headShape_.min_duration_ +
            random_.NextDouble() *
            (headShape_.max_duration_ - headShape_.min_duration_);
        var rd_prop = 1.0 / 6.0;
        var ss_prop = 2.0 / 6.0;
        var str_prop = 3.0 / 6.0;
        var se_prop = 4.0 / 6.0;
        var rl_prop = 5.0 / 6.0;
        SynchroSpecifier specifier = delegate (Bml.Signal signal)
        {
            return SynchroSolver.Solve7(
                sched, signal, default_duration,
                rd_prop, ss_prop, str_prop, se_prop, rl_prop);
        };
        return sched.NewSignal(
            id, modality, shift, speech_stress_synchro,
            action, specifier, synchros);
    }

    public static Bml.Signal Gesture(Scheduler sched, string id,
        VirtualAgent.GestureEngine ge,
        string lexeme, string mode, VirtualAgent.GestureDescription gestureShape,
        IEnumerable<Synchro> synchros, string target = "", VirtualAgent.MocapDescription mocap = null, string modality = "gesture")
    {
        var shift = false;
        var speech_stress_synchro = "stroke";
        EventAction action = delegate (Bml.Event evt)
        {

            /* var arg_value = lexeme;
             //MGoffset
             if (modality == "gesture" && target!="")
             {
                 arg_value = lexeme+':'+target;
             }
             if (modality == "pointing")
             {
                 arg_value = target;
             }*/
            //sched.Log("{0}({1}, mode={2})",Prefix(sched, evt), arg_value, mode);
            ge.PlayGesture(sched, evt, /*arg_value*/ lexeme, modality, mode, gestureShape, target, mocap);
        };

        var default_duration =
            (modality != "pointing") && gestureShape != null
            ? gestureShape.duration
            : 1.0 + random_.NextDouble() * 2.0;
        var rd_p = gestureShape == null ? null :
            gestureShape.phases.Find(x => x.name == "ready");
        var ss_p = gestureShape == null ? null :
            gestureShape.phases.Find(x => x.name == "stroke_start");
        var str_p = gestureShape == null ? null :
            gestureShape.phases.Find(x => x.name == "stroke");
        var se_p = gestureShape == null ? null :
            gestureShape.phases.Find(x => x.name == "stroke_end");
        var rl_p = gestureShape == null ? null :
            gestureShape.phases.Find(x => x.name == "relax");
        var rd_prop = rd_p == null ? -1.0 :
            rd_p.time / gestureShape.duration;
        var ss_prop = ss_p == null ? -1.0 :
            ss_p.time / gestureShape.duration;
        var str_prop = str_p == null ? -1.0 :
            str_p.time / gestureShape.duration;
        var se_prop = se_p == null ? -1.0 :
            se_p.time / gestureShape.duration;
        var rl_prop = rl_p == null ? -1.0 :
            rl_p.time / gestureShape.duration;
        rd_prop =
            rd_prop >= 0.0 ? rd_prop :
            ss_prop >= 0.0 ? ss_prop :
            str_prop >= 0.0 ? str_prop :
            se_prop >= 0.0 ? se_prop :
            rl_prop >= 0.0 ? rl_prop :
            -1.0;
        if (rd_prop >= 0.0)
        {
            ss_prop = Math.Max(ss_prop, rd_prop);
            str_prop = Math.Max(str_prop, ss_prop);
            se_prop = Math.Max(se_prop, str_prop);
            rl_prop = Math.Max(rl_prop, se_prop);
        }
        else
        {
            rd_prop = 1.0 / 6.0;
            ss_prop = 2.0 / 6.0;
            str_prop = 3.0 / 6.0;
            se_prop = 4.0 / 6.0;
            rl_prop = 5.0 / 6.0;
        }
        SynchroSpecifier specifier = delegate (Bml.Signal signal)
        {
            if (modality == "gesture" && mocap != null)
            {
                return false;
            }
            return SynchroSolver.Solve7(
                sched, signal, default_duration,
                rd_prop, ss_prop, str_prop, se_prop, rl_prop);
        };
        return sched.NewSignal(
            id, modality, shift, speech_stress_synchro,
            action, specifier, synchros);
    }

    public static Bml.Signal Pointing(Scheduler sched, VirtualAgent.GestureEngine ge, string id,
        string target, string mode, VirtualAgent.GestureDescription gestureShape,
        IEnumerable<Synchro> synchros)
    {
        return Gesture(sched, id, ge, "", mode, gestureShape, synchros, target, null, "pointing");
    }

    public static Bml.Signal Torso(Scheduler sched, string id, bool shift,
        string lexeme, VirtualAgent.TorsoEngine te, VirtualAgent.TorsoShape torsoShape, float amount,
        IEnumerable<Synchro> synchros)
    {
        var modality = "torso";
        var speech_stress_synchro = "ready";
        EventAction action = delegate (Bml.Event evt)
        {
            //sched.Log("{0}(lexeme={1})",Prefix(sched, evt), lexeme);
            te.PlayTorso(sched, evt, torsoShape, lexeme, amount);
        };
        var default_duration = 1.0 + random_.NextDouble() * 3.0;
        var default_offset = 0.25;
        SynchroSpecifier specifier = delegate (Bml.Signal signal)
        {
            return SynchroSolver.Solve4(
                sched, signal, default_duration,
                default_offset);
        };
        return sched.NewSignal(
            id, modality, shift, speech_stress_synchro,
            action, specifier, synchros);
    }

    public static Bml.Signal Locomotion(Scheduler sched, string id,
        string target, string manner,
        IEnumerable<Synchro> synchros)
    {
        var modality = "locomotion";
        var shift = false;
        string speech_stress_synchro = null;
        EventAction action = delegate (Bml.Event evt)
        {
            sched.Log("{0}(target={1}, manner={2})", Prefix(sched, evt), target, manner);
        };
        SynchroSpecifier specifier = null;
        return sched.NewSignal(
            id, modality, shift, speech_stress_synchro,
            action, specifier, synchros);
    }

    public static Bml.Signal Posture(Scheduler sched, VirtualAgent.GestureEngine pe, string id, bool shift,
        string stance, string category,
        string target, string facing,
        IEnumerable<Synchro> synchros, VirtualAgent.PostureDescription pod, VirtualAgent.MocapDescription mocap = null)
    {
        var modality = "posture";
        var speech_stress_synchro = "ready";
        EventAction action = delegate (Bml.Event evt)
        {
            //sched.Log("{0}(target={1})",Prefix(sched, evt), target);
            pe.ChangePosture(sched, evt, stance, category, target, facing, pod, mocap);
        };
        var default_duration = 1.0 + random_.NextDouble() * 3.0;
        var default_offset = 0.25;
        SynchroSpecifier specifier = delegate (Bml.Signal signal)
        {
            return SynchroSolver.Solve4(
                sched, signal, default_duration,
                default_offset);
        };
        return sched.NewSignal(
            id, modality, shift, speech_stress_synchro,
            action, specifier, synchros);
    }

    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    /*static void Initialise(Scheduler sched)
    {
        // puisque shift==true si un APPEND arrive après il démarre
        // même si le shift n'est pas fini
        // . un shift continue jusqu'à ce qu'un autre de la même
        //   modalité le remplace ou qu'il ait un signal de fin
        // même modalité sans shift --> empiler
        // même modalité avec shift --> remplacer
        //   --> envoyer le signal end du précédent et start du suivant
        //       au même moment
        sched.AddBml("bml1", Composition.APPEND, new List<Bml.Signal>(){
            Gaze(sched, "g1", true, "User",
                new List<Synchro>(){
                    sched.NewSynchro("start", "0"),
                    sched.NewSynchro("ready", "start+0.2"),
                    sched.NewSynchro("relax", "start+1.8"),
                    // sched.NewSynchro("end",   "start+2"),
                    }),
            Face(sched, "f1", false, "RAISE_BROWS", "0.8",
                new List<Synchro>(){
                    sched.NewSynchro("start",       "s1:start"),
                    sched.NewSynchro("end",         "s1:tm2"),
                    sched.NewSynchro("attack_peak", "start+0.2"),
                    sched.NewSynchro("relax",       "end-0.2"),
                    }),
            Gesture(sched, "ge1", "hello-waving", "RIGHT_HAND",
                new List<Synchro>(){
                    sched.NewSynchro("start",        "stroke-1"),
                    sched.NewSynchro("ready",        "start+0.2"),
                    sched.NewSynchro("stroke_start", "stroke-0.2"),
                    sched.NewSynchro("stroke",       "s1:tm1"),
                    sched.NewSynchro("stroke_end",   "stroke+0.2"),
                    sched.NewSynchro("relax",        "stroke+0.4"),
                    sched.NewSynchro("end",          "!!!"), //"stroke+0.8"),
                    }),
            Locomotion(sched, "l1", "PATIENT", "WALK",
                new List<Synchro>(){
                    sched.NewSynchro("start", "ge1:end"),
                    sched.NewSynchro("end",   "???"),
                    }),
            Gesture(sched, "ge2", "manipulate:Zone1:palpate", "BOTH_HANDS",
                new List<Synchro>(){
                    sched.NewSynchro("start",        "stroke-1"),
                    sched.NewSynchro("ready",        "start+0.2"),
                    sched.NewSynchro("stroke_start", "stroke-0.2"),
                    sched.NewSynchro("stroke",       "s2:tm1"),
                    sched.NewSynchro("stroke_end",   "stroke+0.2"),
                    sched.NewSynchro("relax",        "stroke+0.4"),
                    sched.NewSynchro("end",          "stroke+0.8"),
                    }),
            Pointing(sched, "po1", "Zone1", "RIGHT_HAND",
                new List<Synchro>(){
                    sched.NewSynchro("start",        "stroke-1"),
                    sched.NewSynchro("ready",        "start+0.2"),
                    sched.NewSynchro("stroke_start", "stroke-0.2"),
                    sched.NewSynchro("stroke",       "s2:tm4"),
                    sched.NewSynchro("stroke_end",   "stroke+0.2"),
                    sched.NewSynchro("relax",        "stroke+0.4"),
                    sched.NewSynchro("end",          "stroke+0.8"),
                    }),
            Speech(sched, "s1",
                new List<Synchro>(){
                    sched.NewSynchro("start", "g1:ready"),
                    sched.NewSynchro("end",   "start+6.3"),
                    sched.NewSynchro("tm1",   "start+1.2"),
                    sched.NewSynchro("tm2",   "start+6.0"),
                    }),
            Speech(sched, "s2",
                new List<Synchro>(){
                    sched.NewSynchro("start", "l1:end"),
                    sched.NewSynchro("end",   "start+6.7"),
                    sched.NewSynchro("tm1",   "start+0.3"),
                    sched.NewSynchro("tm2",   "start+1.4"),
                    sched.NewSynchro("tm3",   "start+1.8", true),
                    sched.NewSynchro("tm4",   "start+6.4"),
                    }),
            });
        sched.AddBml("bml2", Composition.MERGE, new List<Bml.Signal>(){
            Gaze(sched, "g1", false, "User",
                new List<Synchro>(){
                    sched.NewSynchro("start", "bml1:s2:tm2"),
                    sched.NewSynchro("ready", "start+0.2"),
                    sched.NewSynchro("relax", "!!!"), // "start+1.8"),
                    sched.NewSynchro("end",   "start+2"),
                    }),
            Gesture(sched, "ge1", "beat", "RIGHT_HAND",
                new List<Synchro>(){
                    sched.NewSynchro("start",        "stroke-1"),
                    sched.NewSynchro("ready",        "start+0.2"),
                    sched.NewSynchro("stroke_start", "stroke-0.2"),
                    sched.NewSynchro("stroke",       "bml1:s2:start+2.5"),
                    sched.NewSynchro("stroke_end",   "stroke+0.2"),
                    sched.NewSynchro("relax",        "stroke+0.4"),
                    sched.NewSynchro("end",          "stroke+0.8"),
                    }),
            });
        sched.AddBml("bml3", Composition.APPEND, new List<Bml.Signal>(){
            Gaze(sched, "g1", false, "User",
                new List<Synchro>(){
                    sched.NewSynchro("start", "1"),
                    sched.NewSynchro("ready", "start+0.2"),
                    sched.NewSynchro("relax", "start+1.8"),
                    sched.NewSynchro("end",   "start+2"),
                    }),
            });
        sched.AddBml("bml4", Composition.MERGE, new List<Bml.Signal>(){
            Gaze(sched, "g1", false, "User",
                new List<Synchro>(){
                    sched.NewSynchro("start", "bml1:po1:end"),
                    sched.NewSynchro("ready", "start+0.2"),
                    sched.NewSynchro("relax", "start+1.8"),
                    sched.NewSynchro("end",   "start+4"),
                    }),
            });
    }
    */

    /*public static void Main(string[] args)
    {
        var beh = new BmlSchedulerBehaviour();
        beh.Init(); // weird!
        while (beh.Sched.Now < 1.5)
        {
            beh.Update();
        }
        Initialise(beh.Sched);
        while (beh.Sched.Now < 6.4)
        {
            beh.Update();
        }
        beh.Sched.Log(
            "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"+
            "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
        var evt = beh.Sched.FindEvent("bml1:l1:end");
        var time = beh.Sched.Now-evt.Bml.StartTime;
        var ci = new System.Globalization.CultureInfo("en-US");
        beh.Sched.ChangeSynchro(evt.Synchro, time.ToString(ci));
        while (beh.Sched.Now < 30.0)
        {
            beh.Update();
        }
    }*/
}

//----------------------------------------------------------------------------
