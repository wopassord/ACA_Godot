using System;
using System.Collections.Generic;
using System.Linq;

namespace VirtualAgent
{ 
internal struct BlendshapePhase
{
    public Blendshape blendshape;
    public string side;
    public float start, end;
    public float weightStart, weightEnd;
}

internal struct FacialAction
{
    internal string lexeme;
    internal float start, end;
    internal List<(int, List<BlendshapePhase>)> bsPhases;
}

internal class FacialShift
{
    internal float start, end;
    internal float amount_start, amount_end;
    internal List<Blendshape> blendshapes;
    internal List<Blendshape> previous_blendshapes;
    internal float previous_amount;
}

internal class GazePhase
{
    internal string type;
    internal bool shift;
    internal float start, end;
    internal string target_from, target_to;
}

internal struct HeadDirectionPhase
{
    internal string type;
    internal float amountFrom;
    internal float amountTo;
    internal float start, end;
    internal string from, to;

    public void modifyTo(string t, float a)
    {
        this.to = t;
        this.amountTo = a;
    }
}

//MG
/*internal class HeadPositionPhase
{
    //internal string type;
    //internal bool shift;
    internal float amount;
    internal float start, end;
    internal Vector3 fromPos, toPos;
}*/

    /*
    internal class HeadExpr
    {
        private System.Random random = new System.Random();
        internal HeadShape headShape_;
        private Quaternion starting_head_rotation_;
        internal double x1_, x2_, period_, hold_, repetition_;
        internal double amount_; 

        public HeadExpr(HeadShape headShape, Quaternion shr, double x1, double x2, int repetition, double amount, string target)
        {
            headShape_ = headShape;
            starting_head_rotation_ = shr;
            x1_ = x1; x2_ = x2;
            amount_ = amount;
            double duration = x2 - x1;
            hold_ = 0;
            if (headShape_.min_repetition_ >= 1)
            {
                // This is a repeted signal (nod, shake, wobble)
                if (repetition != -1)
                    repetition_ = repetition;
                else
                    repetition_ = (int)(random.NextDouble() * (headShape_.max_repetition_ - headShape_.min_repetition_) + headShape_.min_repetition_);
            }
            else
            {
                repetition_ = headShape_.min_repetition_; //else get the repetition of a single movement described in the database
                if (duration > 0.5)
                {  //there is a hold phase
                    hold_ = duration - 0.5;
                }
            }
            period_ = duration / repetition_;
        }

        public Vector3 Rotation(double time)
        {
            Vector3 rotation = new Vector3(0, 0, 0);
            //0.3 secondes minimum head movement duration
            //0.6 seconds enough to have fluid head movement even with high amplitude
            if (period_ > 0.6) rotation = headShape_.max_rotation_;
            else if (period_ < 0.3) rotation = headShape_.min_rotation_;
            else
            {
                rotation.x = ((float)period_ - 0.3f) * (headShape_.max_rotation_.x - headShape_.min_rotation_.x) / 0.3f + headShape_.min_rotation_.x;
                rotation.y = ((float)period_ - 0.3f) * (headShape_.max_rotation_.y - headShape_.min_rotation_.y) / 0.3f + headShape_.min_rotation_.y;
                rotation.z = ((float)period_ - 0.3f) * (headShape_.max_rotation_.z - headShape_.min_rotation_.z) / 0.3f + headShape_.min_rotation_.z;
            }
            rotation *= (float)amount_;

            float t = (float)(time - x1_);
            double duration = x2_ - x1_;
            double offset = (duration - hold_) / 2;
            float a = 0;

            if (hold_ == 0 )
            {
                //there is no hold phase
                a = Mathf.Sin(2 * Mathf.PI / (float)period_ * t);
            }
            else
            {
                //there is a hold phase
                if (t >= 0 && t <= offset)
                {
                    a = Mathf.Sin(2 * Mathf.PI / (float)(4 * offset) * t);
                }
                else if(t>=(duration - offset) && t<=duration)
                {
                    float t1 = (float)(t - duration + 2*offset);
                    a = Mathf.Sin(2 * Mathf.PI / (float)(4 * offset) * t1);
                }
                else if(t>offset && t<duration - offset)
                {
                    a = 1;
                }
            }

            Vector3 result = new Vector3(rotation.x * a, rotation.y * a, rotation.z * a);
            //TODO: improve angles when gaze is involved
            return result;
        }
    }
    */

    public class FaceEngine
    {

        private System.Random random_ = new System.Random();
        private bool start_done=false;//MGcorr

        internal Character agent_;
        internal Dictionary<int, float> face_blendshapes;
        internal Dictionary<int, float> jaw_tongue_blendshapes;

        internal Dictionary<string, (string, string)> headMovements_ = new Dictionary<string, (string, string)>();

        internal List<FacialAction> speechActions_ = new List<FacialAction>();
        internal List<FacialAction> facialActions_ = new List<FacialAction>();
        internal FacialShift facialShift_ = null;
        internal List<GazePhase> gazePhases_ = new List<GazePhase>();
        internal string gazeShift_ = "";
        internal Dictionary<string, List<HeadDirectionPhase>> headDirections_ = new Dictionary<string, List<HeadDirectionPhase>>();
        internal List<HeadDirectionPhase> headActions_ = new List<HeadDirectionPhase>();
        //internal List<HeadPositionPhase> headPositions_ = new List<HeadPositionPhase>();//MG
        internal ValueTuple<List<string>, float> headShift_ = new ValueTuple<List<string>, float>(null, 0f);

        public FaceEngine(Character a)
        {
            agent_ = a;

            face_blendshapes = new Dictionary<int, float>();
            jaw_tongue_blendshapes = new Dictionary<int, float>();

            //init facial dictionaries
            for (int i = 0; i < agent_.animationEngine.GetFaceBlenshapeCount(); i++)
                face_blendshapes.Add(i, 0);
            for (int i = 0; i < agent_.animationEngine.GetJawBlenshapeCount(); i++)
                jaw_tongue_blendshapes.Add(i, 0);

            headMovements_.Add("nod", ("up", "down"));
            headMovements_.Add("shake", ("left", "right"));
            headMovements_.Add("wobble", ("tiltl", "tiltr"));

            headDirections_.Add("up_down", new List<HeadDirectionPhase>());
            headDirections_.Add("left_right", new List<HeadDirectionPhase>());
            headDirections_.Add("tiltl_tiltr", new List<HeadDirectionPhase>());
            headDirections_.Add("forward_backward", new List<HeadDirectionPhase>());

            /*
            headDirections_.Add("up", new List<HeadDirectionPhase>());
            headDirections_.Add("down", new List<HeadDirectionPhase>());
            headDirections_.Add("left", new List<HeadDirectionPhase>());
            headDirections_.Add("right", new List<HeadDirectionPhase>());
            headDirections_.Add("tiltl", new List<HeadDirectionPhase>());
            headDirections_.Add("tiltr", new List<HeadDirectionPhase>());
            headDirections_.Add("forward", new List<HeadDirectionPhase>());
            headDirections_.Add("backward", new List<HeadDirectionPhase>());
             */
        }



        public void ResetBlendshapesDictionaries()
        {
            for (int i = 0; i < face_blendshapes.Count; i++)
                face_blendshapes[i] = 0.0f;
            for (int i = 0; i < jaw_tongue_blendshapes.Count; i++)
                jaw_tongue_blendshapes[i] = 0.0f;
        }

// ALL COMMENTED FOR THIS INSTANCE (FOCUS AT THE MOMENT: FACE BLENDSHAPES) /////////////////////////////

        // /************************************   HEAD  ******************************************/

        // public void PlayHead(Bml.Scheduler scheduler, Bml.Event evt, HeadShape head, int repetition, double amount, string target)
        // {
        //     if (evt.Synchro == null || !evt.Synchro.Id.Equals("start")) return;

        //     float start = (float)evt.Signal.FindSynchro("start").Time;
        //     if (head.direction_)
        //     {
        //         if (evt.Signal.Shift)
        //         { 
        //             if (head.lexeme_.Equals("none"))
        //                 UnsetHeadDirectionShift(start, start + 0.2f);
        //             else
        //                 SetHeadDirectionShift(start, start + 0.2f, head.lexeme_, (float)amount);
        //             return;
        //         }

        //         AddHeadDirectionPhases(start, start + 0.2f, // (float)evt.Signal.FindSynchro("ready").Time,
        //                               (float)evt.Signal.FindSynchro("end").Time - 0.2f, // (float)evt.Signal.FindSynchro("relax").Time,
        //                               (float)evt.Signal.FindSynchro("end").Time, head.lexeme_, (float)amount);  //and amount ?!?
        //     }
        //     else
        //     {
        //         //Debug.Log(head.lexeme_ + "  start:" + evt.Signal.FindSynchro("start").Time + "  dur:" + (evt.Signal.FindSynchro("end").Time - evt.Signal.FindSynchro("start").Time) + "  rep: " + repetition);
        //         AddHeadActionPhases((float)evt.Signal.FindSynchro("start").Time, (float)evt.Signal.FindSynchro("end").Time, head.lexeme_, repetition, (float)amount);
        //     }
        // }

        // private void SetHeadDirectionShift(float st, float r, string lexeme, float a)
        // {
        //     List<string> single_mov = new List<string>(lexeme.Split('_'));

        //     foreach (var sm in single_mov)
        //     {
        //         string s = "";
        //         foreach (var hd in headDirections_)
        //         {
        //             s = hd.Key.Contains(sm) ? hd.Key : "";
        //             if (s != "") break;
        //         }

        //         if (s == "") return;

        //         if (headDirections_[s].Count == 0)
        //         {
        //             //start->ready
        //             headDirections_[s].Add(new HeadDirectionPhase
        //             {
        //                 type = "start",
        //                 amountFrom = 1,
        //                 amountTo = a,
        //                 start = st,
        //                 end = r,
        //                 from = "none",
        //                 to = sm
        //             });
        //         }
        //     }
        //     headShift_ = new ValueTuple<List<string>, float>(single_mov, a);

        //     // FOR DEBUG
            
        //     /*foreach (var elem in headDirections_)
        //     {
        //         if (elem.Value.Count > 0)
        //         {
        //             agent_.Log("************ " + elem.Key);
        //             foreach (var t in elem.Value)
        //                 agent_.Log("from " + t.from + " " + t.amountFrom + " at " + t.start + " to " + t.to + " " + t.amountTo + " at " + t.end);
        //         }
        //     }*/
        // }

        // private void UnsetHeadDirectionShift(float st, float r)
        // {
        //     if (headShift_.Item1 != null)
        //     {
        //         foreach (var sm in headShift_.Item1)
        //         {
        //             string s = "";
        //             foreach (var hd in headDirections_)
        //             {
        //                 s = hd.Key.Contains(sm) ? hd.Key : "";
        //                 if (s != "") break;
        //             }

        //             if (s == "") return;

        //             if (headDirections_[s].Count == 0)
        //             {
        //                 //relax->end
        //                 headDirections_[s].Add(new HeadDirectionPhase
        //                 {
        //                     type = "relax",
        //                     amountFrom = headShift_.Item2,
        //                     amountTo = 1,
        //                     start = st,
        //                     end = r,
        //                     from = sm,
        //                     to = "none"
        //                 });
        //             }
        //         }
        //     }
        //     headShift_ = new ValueTuple<List<string>, float>(null, 0f);

        //     // FOR DEBUG
        //     /*foreach (var elem in headDirections_)
        //     {
        //         if (elem.Value.Count > 0)
        //         {
        //             agent_.Log("************ " + elem.Key);
        //             foreach (var t in elem.Value)
        //                 agent_.Log("from " + t.from + " " + t.amountFrom + " at " + t.start + " to " + t.to + " " + t.amountTo + " at " + t.end);
        //         }
        //     }*/
        // }

        // private void AddHeadDirectionPhases(float start, float ready, float relax, float end, string lexeme, float a)
        // {
        //     string[] single_mov = lexeme.Split('_');
        //     foreach (var sm in single_mov)
        //     {
        //         string s = "";
        //         foreach (var hd in headDirections_)
        //         {
        //             s = hd.Key.Contains(sm) ? hd.Key : "";
        //             if (s != "") break;
        //         }

        //         if (s == "") continue;

        //         var keyframesList = headDirections_[s];
        //         var newList = new List<HeadDirectionPhase>();
                
        //         float amountFrom = 1;
        //         string from = "none";

        //         //find start posiotion for the new signal
        //         if (keyframesList.Count > 0)
        //         {
        //             var keyframe = keyframesList[0];
        //             //if a previous signal was starting or ending, let's finish
        //             //rapidly its current phase before starting the new signal
        //             if (keyframe.type == "start" || keyframe.type == "relax")
        //             {
        //                 start = keyframe.end;
        //                 ready = ready < start ? start + 0.1f : ready;
        //                 relax = relax < ready ? ready + 0.1f : relax;
        //                 end   = end   < relax ? relax + 0.1f : end;
        //                 newList.Add(keyframe);
        //             }
        //             amountFrom = keyframe.amountTo;
        //             from = keyframe.to;
        //         }
        //         else
        //         {
        //             //if a headshift is running, the start position of the new signal
        //             //is this shift position
        //             if(headShift_.Item1 != null && headShift_.Item1.Contains(sm))
        //             {
        //                 amountFrom = headShift_.Item2;
        //                 from = sm;
        //             }
        //         }

        //         //start-ready
        //         newList.Add(new HeadDirectionPhase 
        //         {
        //             type = "start",
        //             amountFrom = amountFrom,
        //             amountTo = a,
        //             start = start,
        //             end = ready,
        //             from = from,
        //             to = sm

        //         });

        //         //ready-relax
        //         newList.Add(new HeadDirectionPhase
        //         {
        //             type = "ready",
        //             amountFrom = a,
        //             amountTo = a,
        //             start = ready,
        //             end = relax,
        //             from = sm,
        //             to = sm

        //         });

        //         //maintain previous signals if they last longer than the new one
        //         //the end position of the new signal must be the older and longer one 
        //         float amountTo = 1;
        //         string to = "none";
        //         int i = keyframesList.FindIndex(x => x.end > end && x.type.Equals("ready"));
        //         if(i != -1)
        //         {
        //             amountTo = keyframesList[i].amountTo;
        //             to = keyframesList[i].to;
        //         }

        //         //relax-end
        //         newList.Add(new HeadDirectionPhase
        //         {
        //             type = "relax",
        //             amountFrom = a,
        //             amountTo = amountTo,
        //             start = relax,
        //             end = end,
        //             from = sm,
        //             to = to
        //         });

        //         //the other keyframes of the previous signals, if any
        //         if (i != -1)
        //         {
        //             var kf = keyframesList[i];
        //             newList.Add(new HeadDirectionPhase
        //             {
        //                 type = kf.type,
        //                 amountFrom = kf.amountTo,
        //                 amountTo = kf.amountTo,
        //                 start = newList[newList.Count - 1].end,
        //                 end = kf.end,
        //                 from = kf.to,
        //                 to = kf.to
        //             });
        //             keyframesList.RemoveRange(0, i+1);
        //             newList.AddRange(keyframesList);
        //         }

        //         headDirections_[s] = newList;
        //     }

        //     // FOR DEBUG
        //     /*foreach (var elem in headDirections_)
        //     {
        //         if (elem.Value.Count > 0)
        //         {
        //             agent_.Log("************ " + elem.Key);
        //             foreach (var t in elem.Value)
        //                 agent_.Log(elem.Key + ":    from " + t.from + " " + t.amountFrom + " at " + t.start + " to " + t.to + " " + t.amountTo + " at " + t.end);
        //         }
        //     }*/
        // }

        // private void AddHeadActionPhases(float st, float e, string lexeme, int repetition, float amount)
        // {
        //     List<string> single_mov = new List<string>(lexeme.Split('_'));
        //     string dir = "";
            
        //     if(single_mov.Count > 1)
        //     {
        //         dir = single_mov[0];
        //         lexeme = single_mov[1];
        //     }

        //     if (!headMovements_.ContainsKey(lexeme))
        //         return;
            
        //     if (headActions_.Count != 0)
        //     {
        //         var previousEnd = headActions_[0];
        //         headActions_.Clear();

        //         //finish the previous head action
        //         headActions_.Add(new HeadDirectionPhase
        //         {
        //             type = previousEnd.type,
        //             amountFrom = previousEnd.amountFrom,
        //             amountTo = previousEnd.amountTo,
        //             start = previousEnd.start,
        //             end = previousEnd.end + 0.1f,
        //             from = previousEnd.from,
        //             to = previousEnd.to
        //         });
        //         st += 0.1f;
        //     }

        //     if (repetition <= 0)
        //         repetition = (int)(random_.NextDouble() * 2) + 2; //random repetitions, between 1 and 3  ???? TO CORRECT

        //     float period = (e - st) / repetition;

        //     string item1 = headMovements_[lexeme].Item1;
        //     string item2 = headMovements_[lexeme].Item2;

        //     string one = dir.Equals(item1) ? item1 : dir.Equals(item2) ? item2 : item1; //the last one should be random
        //     string two = one.Equals(item1) ? item2 : item1;

        //     string from = "none";
        //     float amountFrom = 1;

        //     foreach (var dirs in headDirections_)
        //     {
        //         if(dirs.Key.Equals(one) && dirs.Value.Count > 0)
        //         {
        //             var keyframe = dirs.Value[0];
        //             if (keyframe.type == "start" || keyframe.type == "relax")
        //             {
        //                 var delta = keyframe.end - st;
        //                 st = keyframe.end;
        //                 e += delta;
        //             }
        //             amountFrom = keyframe.amountTo;
        //             from = keyframe.to;
        //         }
        //         else
        //         {
        //             if (headShift_.Item1 != null && headShift_.Item1.Contains(one))
        //             {
        //                 amountFrom = headShift_.Item2;
        //                 from = one;
        //             }
        //         }
        //     }

        //     headActions_.Add(new HeadDirectionPhase
        //     {
        //         type = "start",
        //         amountFrom = 1,
        //         amountTo = amount,
        //         start = st,
        //         end = st + 0.25f * period,
        //         from = "none",
        //         to = one
        //     });

        //     for (int i = 0; i < repetition; ++i)
        //     {
        //         st += i > 0 ? period : 0;

        //         headActions_.Add(new HeadDirectionPhase
        //         {
        //             type = "stroke",
        //             amountFrom = amount,
        //             amountTo = amount,
        //             start = st + 0.25f * period,
        //             end = st + 0.75f * period,
        //             from = one,
        //             to = two
        //         });
        //         (two, one) = (one, two);  //swap values

        //         if (repetition > 1 && i != repetition - 1)
        //         {
        //             headActions_.Add(new HeadDirectionPhase
        //             {
        //                 type = "stroke",
        //                 amountFrom = amount,
        //                 amountTo = amount,
        //                 start = st + 0.75f * period,
        //                 end = st + 1.25f * period,
        //                 from = one,
        //                 to = two
        //             });
        //             (two, one) = (one, two);
        //         }
        //     }

        //     headActions_.Add(new HeadDirectionPhase
        //     {
        //         type = "relax",
        //         amountFrom = amount,
        //         amountTo = 1,
        //         start = st + 0.75f * period,
        //         end = st + period,
        //         from = one,
        //         to = "none"
        //     });
        // }

       

        // public bool GetCurrentHeadDirection(float time, ref List<Animation.HeadDirectionPhase> headDirections)
        // {
        //     bool flag = false;
        //     headDirections = new List<Animation.HeadDirectionPhase>();
        //     foreach (var elem in headDirections_)
        //     {
        //         var dirs = elem.Value;

        //         //remove ended phase
        //         if(dirs.Count > 0)
        //             if (dirs[0].end < time) dirs.RemoveAt(0);

        //         if(dirs.Count > 0)
        //         {
        //             var dir = dirs[0];
        //             if(time >= dir.start && time <= dir.end)
        //             {
        //                 flag = true;
        //                 string from = dir.from, to = dir.to;
        //                 float amountFrom = dir.amountFrom, amountTo = dir.amountTo;

        //                 if (dir.to.Equals("none") && headShift_.Item1 != null)
        //                 {
        //                     var k = headShift_.Item1.FindIndex(x => elem.Key.Contains(x));
        //                     if (k != -1)
        //                     {
        //                         to = headShift_.Item1[k];
        //                         amountTo = headShift_.Item2;
        //                     }
        //                 }

        //                 headDirections.Add(new Animation.HeadDirectionPhase
        //                 {
        //                     type = dir.type,
        //                     lerpTime = (time - dir.start) / (dir.end - dir.start),
        //                     from = from,
        //                     to = to,
        //                     amountFrom = amountFrom,
        //                     amountTo = amountTo
        //                 });
        //             }
        //         }
        //         else
        //         {
        //             if (headShift_.Item1 != null)
        //             {
        //                 flag = true;
        //                 var k = headShift_.Item1.FindIndex(x => elem.Key.Contains(x));
        //                 if (k != -1)
        //                 {
        //                     string to = headShift_.Item1[k];
        //                     headDirections.Add(new Animation.HeadDirectionPhase
        //                     {
        //                         type = "stroke",
        //                         lerpTime = 1,
        //                         from = to,
        //                         to = to,
        //                         amountFrom = headShift_.Item2,
        //                         amountTo = headShift_.Item2
        //                     });
        //                 }
        //             }
        //         }
        //     }

        //     // FOR DEBUG
        //     /*foreach(var e in headDirections_)
        //     {
        //         Debug.Log("!!!! " + e.from + " " + e.amountFrom + " to: " + e.to + " " + e.amountTo + " lerp: " + e.lerpTime);
        //     }*/
            
        //     return flag;
        // }

        // public Animation.HeadDirectionPhase GetCurrentHeadAction(float time)
        // {
        //     //remove ended phase
        //     if (headActions_.Count > 0)
        //         if (headActions_[0].end < time) headActions_.RemoveAt(0);

        //     if (headActions_.Count > 0)
        //     {
        //         var act = headActions_[0];
        //         if (time >= act.start && time <= act.end)
        //         {
        //             string from = act.from, to = act.to;
        //             float amountFrom = act.amountFrom, amountTo = act.amountTo;

        //             /*if (act.to.Equals("none") && headShift_.Item1.Contains(elem.Key))
        //             {
        //                 to = elem.Key;
        //                 amountTo = headShift_.Item2;
        //             }*/

        //             return new Animation.HeadDirectionPhase
        //             {
        //                 type = act.type,
        //                 lerpTime = (time - act.start) / (act.end - act.start),
        //                 from = from,
        //                 to = to,
        //                 amountFrom = amountFrom,
        //                 amountTo = amountTo
        //             };
        //         }
        //     }

        //     // FOR DEBUG
        //     /*foreach(var e in headDirections_)
        //     {
        //         Debug.Log("!!!! " + e.from + " " + e.amountFrom + " to: " + e.to + " " + e.amountTo + " lerp: " + e.lerpTime);
        //     }*/

        //     return new Animation.HeadDirectionPhase
        //     {
        //         type = "",
        //         lerpTime = 0,
        //         from = "",
        //         to = "",
        //         amountFrom = 0,
        //         amountTo = 0
        //     }; 
        // }


        // /****************************************************************************************/

        // /************************************   GAZE   ******************************************/

        // public void PlayGaze(Bml.Scheduler scheduler, Bml.Event evt, string target)
        // {
        //     if (evt.Synchro == null || !evt.Synchro.Id.Equals("start")) return;
        //     if (!evt.Signal.Shift && !target.Equals("none") && agent_.animationEngine.TargetFound(target) == false) return;

        //     if (evt.Signal.Shift)
        //     {
        //         if (target.Equals("none"))
        //             UnsetGazeShift((float)evt.Signal.FindSynchro("start").Time, (float)evt.Signal.FindSynchro("start").Time + 0.2f);
        //         else
        //             SetGazeShift((float)evt.Signal.FindSynchro("start").Time, (float)evt.Signal.FindSynchro("start").Time + 0.2f, target);
        //         return;
        //     }

        //     AddGazePhases((float)evt.Signal.FindSynchro("start").Time, (float)evt.Signal.FindSynchro("ready").Time,
        //                     (float)evt.Signal.FindSynchro("relax").Time, (float)evt.Signal.FindSynchro("end").Time, target);
        // }

        // private void SetGazeShift(float st, float r, string target)
        // {
        //     if (gazePhases_.Count == 0)
        //     {
        //         //start->ready
        //         var gph = new GazePhase
        //         {
        //             type = "start",
        //             shift = true,
        //             start = st,
        //             end = r,
        //             target_from = "",
        //             target_to = target
        //         };
        //         if (!gazeShift_.Equals(""))
        //             gph.target_from = gazeShift_;

        //         gazePhases_.Add(gph);
        //     }
        //     gazeShift_ = target;
        // }

        // private void UnsetGazeShift(float st, float r)
        // {
        //     if (gazePhases_.Count == 0 && !gazeShift_.Equals(""))
        //     {
        //         //start->ready
        //         var gph = new GazePhase
        //         {
        //             type = "relax",
        //             shift = true,
        //             start = st,
        //             end = r,
        //             target_from = gazeShift_,
        //             target_to = ""
        //         };
        //         gazePhases_.Add(gph);
        //     }
        //     gazeShift_ = "";
        // }

        // private void AddGazePhases(float st, float r, float re, float e, string target)
        // {
        //     string from = "";
        //     List<GazePhase> oldGazePhases = new List<GazePhase>();
        //     if (gazePhases_.Count != 0)
        //     {
        //         if (gazePhases_[gazePhases_.Count - 1].type.Equals("start") || gazePhases_[gazePhases_.Count - 1].type.Equals("ready"))
        //             from = gazePhases_[gazePhases_.Count - 1].target_to;
        //         else
        //             from = gazePhases_[gazePhases_.Count - 1].target_from;
        //         oldGazePhases = new List<GazePhase>();
        //         oldGazePhases.AddRange(gazePhases_);
        //         gazePhases_.Clear();
        //     }
        //     //relax->end
        //     var gph = new GazePhase
        //     {
        //         type = "relax",
        //         shift = false,
        //         start = re,
        //         end = e,
        //         target_from = target,
        //         target_to = ""
        //     };
        //     gazePhases_.Add(gph);

        //     //ready->relax
        //     gph = new GazePhase
        //     {
        //         type = "ready",
        //         shift = false,
        //         start = r,
        //         end = re,
        //         target_from = target,
        //         target_to = target
        //     };
        //     gazePhases_.Add(gph);

        //     //start->ready
        //     gph = new GazePhase
        //     {
        //         type = "start",
        //         shift = false,
        //         start = st,
        //         end = r,
        //         target_from = from,
        //         target_to = target
        //     };
        //     gazePhases_.Add(gph);

        //     var index = oldGazePhases.FindLastIndex(x => x.type.Equals("ready") && x.end > gazePhases_[0].end);
        //     if (index != -1)
        //     {
        //         oldGazePhases[index].start = gazePhases_[0].end;
        //         gazePhases_[0].target_to = oldGazePhases[index].target_from;
        //         for (int i = index; i >= 0; --i)
        //             gazePhases_.Insert(0, oldGazePhases[i]);
        //         oldGazePhases.Clear();
        //     }

        //     foreach(var g in gazePhases_)
        //     {
        //         //agent_.Log(g.type + " " + g.start + " " + g.end);
        //     }
        // }

        // public bool GetCurrentGazeDirection(float time, ref float lerpTime, ref string from, ref string to)
        // {
        //     //clean ended phases
        //     if (gazePhases_.Count >= 1 && time >= gazePhases_[gazePhases_.Count - 1].end)
        //         gazePhases_.RemoveAt(gazePhases_.Count - 1);

        //     if (gazePhases_.Count >= 1)
        //     {
        //         GazePhase gph = gazePhases_[gazePhases_.Count - 1];

        //         from = gph.target_from.Equals("") && !gph.shift ? gazeShift_ : gph.target_from;
        //         to = gph.target_to.Equals("") && !gph.shift ? gazeShift_ : gph.target_to;

        //         lerpTime = (time - gph.start) / (gph.end - gph.start);
        //     }
        //     else if (!gazeShift_.Equals(""))
        //     {
        //         lerpTime = 1;
        //         from = to = gazeShift_;
        //     }
        //     else
        //         return false;
        //     return true;
        // }

        /****************************************************************************************/

        /********************************   SPEECH & FACE  **************************************/

        public void PlaySpeech(Bml.Scheduler scheduler, Bml.Event evt, TextToSpeech.SpeechData sdata) //, List<ActionUnit> visemes)
        {
            if (!evt.Synchro.Id.Equals("start")) return;
            if (speechActions_.Count > 0)
            {
                //already playing
//////////COMMENTED FOR THIS INSTANCE (FOCUS ON BLENDSHAPES)
                // InterruptSpeech();
            }

            var action = new FacialAction
            {
                lexeme = "speech",
                start = (float)evt.Signal.FindSynchro("start").Time,
                end = (float)evt.Signal.FindSynchro("end").Time,
                bsPhases = new List<(int, List<BlendshapePhase>)>()
            };

           foreach (var p in sdata.Phonemes)
            {
                 if (p.Type == TextToSpeech.SpeechData.PhonemeType.PHONEME)
                {
                    float s = p.Start + (float)evt.Signal.FindSynchro("start").Time;
                    float e = p.End + (float)evt.Signal.FindSynchro("start").Time;
                    string name = p.Name;
                    if (!p.Name.Equals("sil"))
                    {
                        float overlap = (e - s) * 0.3f; //30% of overlap on the previous and the next phoneme
                        s = (s - overlap) > 0 ? s - overlap : 0;
                        e = e + overlap;
                    }

                    if (!agent_.actionUnits.ContainsKey(name))
                    {
                        //TODO : CHANGE DEBUG.LOG !!!
                        //Debug.Log("Viseme for phoneme " + name + " does not exists, 'd' viseme will be use instead");
                        name = "d";
                    }

                    float relax = e - (e - s) * 0.4f;
                    float ready = s + (e - s) * 0.4f;

                    AddBlendshapePhases(s, ready, relax, e, new ActionUnit(name, "BOTH", 1.0f), 1,
                                        ref action.bsPhases);
                }
                speechActions_.Add(action);
            }
////////// COMMENTED FOR THIS INSTANCE (FOCUS ON BLENDSHAPES)
            // //start playing
            // agent_.animationEngine.PlayVoice(sdata);
        }
///////////// COMMENTED FOR THIS INSTANCE (FOCUS ON BLENDSHAPES)
        // public void InterruptSpeech()
        // {
        //     //stop playing
        //     speechActions_.Clear();
        //     agent_.animationEngine.StopVoice();
        //     //agent_.audioS.Stop();
        //     //agent_.audioS.clip.UnloadAudioData();

        //     //TODO : reset lips blendshapes for visemes
        //     //ResetBlendshapesDictionaries(); //this cleans all blendshapes, just the visemes should be reset
        // }

        public void PlayFace(Bml.Scheduler scheduler, Bml.Event evt, string lexeme, double amount, List<ActionUnit> aus)
        {
            if (evt.Synchro == null) return;

            if (evt.Synchro.Id.Equals("start"))
            {
                if (evt.Signal.Shift)
                {
                    if (lexeme.Equals("none"))
                        UnsetFacialShift((float)evt.Signal.FindSynchro("start").Time, amount);
                    else
                        SetFacialShift((float)evt.Signal.FindSynchro("start").Time, (float)amount, aus);
                    return;
                }

                var action = new FacialAction
                {
                    start = (float)evt.Signal.FindSynchro("start").Time,
                    end = (float)evt.Signal.FindSynchro("end").Time,
                    bsPhases = new List<(int, List<BlendshapePhase>)>()
                };
                foreach (var au in aus)
                {
                    AddBlendshapePhases(action.start, (float)evt.Signal.FindSynchro("ready").Time,
                                       (float)evt.Signal.FindSynchro("relax").Time, action.end, au, (float)amount,
                                       ref action.bsPhases);
                }
                facialActions_.Add(action);
            }
        }

        private void SetFacialShift(float start, float amount, List<ActionUnit> aus)
        {
            if (facialShift_ == null)
            {
                facialShift_ = new FacialShift();
                facialShift_.blendshapes = new List<Blendshape>();
            }
            else
            {
                facialShift_.previous_amount = facialShift_.amount_end;
                facialShift_.previous_blendshapes = new List<Blendshape>();
                facialShift_.previous_blendshapes.AddRange(facialShift_.blendshapes);
                facialShift_.blendshapes.Clear();
            }
            facialShift_.start = start;
            facialShift_.end = start + 0.25f;
            facialShift_.amount_start = 0;
            facialShift_.amount_end = amount;
            foreach (var au in aus)
            {
                var blendshapeList = agent_.actionUnits[au.au];
                facialShift_.blendshapes.AddRange(blendshapeList);
            }
        }

        private void UnsetFacialShift(float start, double amount)
        {
            if (facialShift_ == null) return;
            facialShift_.start = start;
            facialShift_.end = start + 0.25f;
            facialShift_.amount_start = facialShift_.amount_end;
            facialShift_.amount_end = 0;
            facialShift_.previous_amount = 0;
            facialShift_.previous_blendshapes = null;
        }

        private void AddBlendshapePhases(float start, float ready, float relax, float end, ActionUnit au,
                                        float amount, ref List<(int, List<BlendshapePhase>)> bsPhases)
        {
            //for a given Action Unit, this function adds three phases
            //(start->ready, ready->relax, relax->end)
            //for each blendshape in the AU
            //Firstly, it checks if the AU is composed of blendshapes 
            //which have delayed start and/or andicipated end

            var blendshapeList = agent_.actionUnits[au.au];
            foreach (var bs in blendshapeList)
            {
                bool modified = false;

                if (bs.delayed_start > 0)
                {
                    start = start + (end - start) * bs.delayed_start;
                    modified = true;
                }

                if (bs.anticipated_end > 0)
                {
                    end = end - (end - start) * (1 - bs.anticipated_end);
                    modified = true;
                }

                relax = modified ? end - (end - start) * 0.4f : relax;
                ready = modified ? start + (end - start) * 0.4f : ready;

                var phases = new List<BlendshapePhase>();
                //relax->end
                var aup = new BlendshapePhase();
                aup.blendshape = bs;
                aup.side = au.side;
                aup.start = relax;
                aup.weightStart = (float)amount * bs.weight;
                aup.end = end;
                aup.weightEnd = 0.0f;
                phases.Add(aup);

                //ready->relax
                aup = new BlendshapePhase();
                aup.blendshape = bs;
                aup.side = au.side;
                aup.start = ready;
                aup.weightStart = (float)amount * bs.weight;
                aup.end = relax;
                aup.weightEnd = (float)amount * bs.weight;
                phases.Add(aup);

                //start->ready
                aup = new BlendshapePhase();
                aup.blendshape = bs;
                aup.side = au.side;
                aup.start = start;
                aup.weightStart = 0.0f;
                aup.end = ready;
                aup.weightEnd = (float)amount * bs.weight;
                phases.Add(aup);

                bsPhases.Add((bs.code, phases));
            }
        }

        public void GetCurrentFacialBlendshapes(string modality, float time)
        {
            List<FacialAction> facialActions = modality.Equals("speech") ? speechActions_ : facialActions_;
            //clean finished expressions
            for (int i = facialActions.Count - 1; i >= 0; i--)
                if (time >= facialActions[i].end)
                    facialActions.RemoveAt(i);

            //Firstly : add facial shift if present
            if (facialShift_ != null)
            {
                if (time >= facialShift_.end && facialShift_.amount_end == 0)
                    facialShift_ = null;
                else
                {
                    if (time >= facialShift_.start && time < facialShift_.end)
                    {
                        if (facialShift_.previous_blendshapes != null)
                        {
                            foreach (var bs in facialShift_.previous_blendshapes)
                            {
                                var w1 = bs.weight * facialShift_.previous_amount;
                                SetBlendshape(time, facialShift_.start, facialShift_.end, w1, 0, bs, "BOTH");
                            }
                        }
                        //in start->ready or relax->end phase
                        foreach (var bs in facialShift_.blendshapes)
                        {
                            var w1 = bs.weight * facialShift_.amount_start;
                            var w2 = bs.weight * facialShift_.amount_end;
                            SetBlendshape(time, facialShift_.start, facialShift_.end, w1, w2, bs, "BOTH");
                        }
                    }
                    else
                    {
                        foreach (var bs in facialShift_.blendshapes)
                        {

                            float y = bs.weight * facialShift_.amount_end;
                            SetBlendshape(0, 0, 1, y, 0, bs, "BOTH");
                        }
                    }
                }
            }

            //Secondly : add facial expression or speech viseme blendshapes
            foreach (var fe in facialActions)
            {
                if (time >= fe.start && time < fe.end)
                {
                    foreach (var bs in fe.bsPhases)
                    {
                        //clean finished phases
                        for (int i = bs.Item2.Count - 1; i >= 0; i--)
                            if (time >= bs.Item2[i].end)
                                bs.Item2.RemoveAt(i);

                        if (bs.Item2.Count > 0)
                        {
                            var blendshapePhase = bs.Item2[bs.Item2.Count - 1];
                            SetBlendshape(time, blendshapePhase.start, blendshapePhase.end, blendshapePhase.weightStart,
                                          blendshapePhase.weightEnd, blendshapePhase.blendshape, blendshapePhase.side);
                        }
                    }
                }
            }
        }

        private void SetBlendshape(float time, float start, float end, float ws, float we, Blendshape bs, string expr_side)
        {
            float t1 = (time - start) / (end - start);
            float y = (ws * (1 - t1) + we * t1) * 100.0f;

            if (bs.type == Blendshape.Type.LIP)
            {
                if (expr_side == "BOTH" || (expr_side == bs.side))
                    face_blendshapes[bs.code] = face_blendshapes[bs.code] >= y ? face_blendshapes[bs.code] : y;
            }
            if (bs.type == Blendshape.Type.JAW || bs.type == Blendshape.Type.TONGUE)
            {
                if (expr_side == "BOTH" || (expr_side == bs.side))
                    jaw_tongue_blendshapes[bs.code] = jaw_tongue_blendshapes[bs.code] >= y ? jaw_tongue_blendshapes[bs.code] : y;
            }
        }
    }
}
