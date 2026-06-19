// //using UnityEngine;
// using System;
// using System.Collections.Generic;

// namespace VirtualAgent
// {
//     public class HandKeyFrame
//     {
//         internal string id;
//         internal string name;
//         internal double time;
//         public string target;
//         public string modality;
//         public Relative_Hand_Position rhp;
//         public bool useHandOffset;//MG

//         //for start keyframe
//         public Vector3 handPosition;
//         public Quaternion handRotation;
//         public Vector3 armSwivel;
//     }

//     public class GestureKeyFrame
//     {
//         public List<HandKeyFrame> left, right;
//     }

//     public class Posture
//     {
//         public string name;
//         public HandKeyFrame LeftHandPosture, RightHandPosture;
//     }


//     public class GestureEngine
//     {
//         /* AGENT BASIC INFORMATION */

//         internal Character agent_;

//         //current posture
//         public List<Posture> currentPostures = new List<Posture>();

//         //keyframes to animate for gestures on left and right arm
//         public GestureKeyFrame keyFrames = new GestureKeyFrame();

//         //keyframes to animate for posture change on left and right arm
//         public GestureKeyFrame posturekeyFrames = new GestureKeyFrame();

//         public GestureEngine(Character a)
//         {
//             keyFrames.left = new List<HandKeyFrame>();
//             keyFrames.right = new List<HandKeyFrame>();
//             posturekeyFrames.left = new List<HandKeyFrame>();
//             posturekeyFrames.right = new List<HandKeyFrame>();


//             agent_ = a;

//         }



//         //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~


//         public void ChangePosture(Bml.Scheduler scheduler, Bml.Event evt, string stance, string category,
//             string target, string facing, PostureDescription posture, MocapDescription mocap)
//         { 
//             string id = evt.Bml.Id + ':' + evt.Signal.Id;
//             /*********** " + id + " " + stance + " " + evt.Synchro.Id + " " + evt.Synchro.Time);
//             foreach (var sync in evt.Signal.Synchros)
//                 agent_.Log(id + " " + sync.Id + " " + sync.Time + " " + sync.Expr);*/
//             if (mocap != null)
//             {
//                 if (evt.Synchro.Id == "start")
//                 {
//                     agent_.animationEngine.ChangeStance(mocap.clip);
//                 }
//                 if (evt.Synchro.Id == "end")
//                 {
//                     agent_.animationEngine.ChangeStance("idle1");
//                 }
//                 return;
//             }

//             if (evt.Synchro.Id == "start")
//             {
//                 Posture p = new Posture()
//                 {
//                     name = stance,
//                     LeftHandPosture = posture.leftHand == null ? null :
//                                                                  new HandKeyFrame()
//                                                                  {
//                                                                      id = id,
//                                                                      name = stance,
//                                                                      rhp = posture.leftHand
//                                                                  },
//                     RightHandPosture = posture.rightHand == null ? null :
//                                                                  new HandKeyFrame()
//                                                                  {
//                                                                      id = id,
//                                                                      name = stance,
//                                                                      rhp = posture.rightHand
//                                                                  }

//                 };
//                 currentPostures.Add(p);
//            }

//             if (evt.Synchro.Id.Equals("start"))
//             {
//                 //foreach(var sync in evt.Signal.Synchros)
//                   //  agent_.Log(id + " " + sync.Id + " " + sync.Time);
//                 if (posture.leftHand != null)
//                 {
//                     if ((keyFrames.left.Count == 3 || keyFrames.left.Count == 2) && keyFrames.left[keyFrames.left.Count - 1].name.Equals("end"))
//                     {
//                         keyFrames.left.Clear();
//                     }
//                     if (keyFrames.left.Count == 0)
//                     {
//                         keyFrames.left.Add(GetKeyFrameNEW(id, "start", "posture", evt.Synchro.Time, 'L', null));
//                         keyFrames.left.Add(GetKeyFrameNEW(id, "start", "posture", evt.Synchro.Time, 'L', null));
//                         keyFrames.left.Add(GetKeyFrameNEW(id, "ready", "posture", evt.Signal.FindSynchro("ready").Time, 'L', posture.leftHand));
//                     }
//                 }
//                 if (posture.rightHand != null)
//                 {
//                     if ((keyFrames.right.Count == 3 || keyFrames.right.Count == 2) && keyFrames.right[keyFrames.right.Count - 1].name.Equals("end"))
//                     {
//                         keyFrames.right.Clear();
//                     }
//                     if (keyFrames.right.Count == 0)
//                     {
//                         keyFrames.right.Add(GetKeyFrameNEW(id, "start", "posture", evt.Synchro.Time, 'R', null));
//                         keyFrames.right.Add(GetKeyFrameNEW(id, "start", "posture", evt.Synchro.Time, 'R', null));
//                         keyFrames.right.Add(GetKeyFrameNEW(id, "ready", "posture", evt.Signal.FindSynchro("ready").Time, 'R', posture.rightHand));
//                     }
//                 }
//             }

//             if (evt.Synchro.Id.Equals("relax") && currentPostures.Count>0 && currentPostures[currentPostures.Count-1].name.Equals(stance))
//             {
//                 //agent_.Log(id + " " + evt.Synchro.Id + " " + evt.Synchro.Time);
//                 if (currentPostures[currentPostures.Count - 1].LeftHandPosture != null)
//                 {
//                     if ((keyFrames.left.Count == 3 || keyFrames.left.Count == 2) && keyFrames.left[keyFrames.left.Count - 1].name.Equals("end"))
//                     {
//                         keyFrames.left.Clear();
//                     }

//                     if(keyFrames.left.Count == 0)
//                     {
//                         keyFrames.left.Add(GetKeyFrameNEW(id, "start", "posture", evt.Synchro.Time, 'L', null));
//                         keyFrames.left.Add(GetKeyFrameNEW(id, "start", "posture", evt.Synchro.Time, 'L', null));
//                         if(currentPostures.Count > 1)
//                             keyFrames.left.Add(GetKeyFrameNEW(id, "ready", "posture", evt.Signal.FindSynchro("end").Time, 'L', currentPostures[currentPostures.Count - 2].LeftHandPosture.rhp));
//                         else
//                             keyFrames.left.Add(GetKeyFrameNEW(id, "end", "posture", evt.Signal.FindSynchro("end").Time, 'L', null));
//                     }
//                 }
//                 if (currentPostures[currentPostures.Count - 1].RightHandPosture != null)
//                 {
//                     if ((keyFrames.right.Count == 3 || keyFrames.right.Count == 2) && keyFrames.right[keyFrames.right.Count - 1].name.Equals("end"))
//                     {
//                         keyFrames.right.Clear();
//                     }

//                     if (keyFrames.right.Count == 0)
//                     {
//                         keyFrames.right.Add(GetKeyFrameNEW(id, "start", "posture", evt.Synchro.Time, 'R', null));
//                         keyFrames.right.Add(GetKeyFrameNEW(id, "start", "posture", evt.Synchro.Time, 'R', null));
//                         if (currentPostures.Count > 1)
//                             keyFrames.right.Add(GetKeyFrameNEW(id, "ready", "posture", evt.Signal.FindSynchro("end").Time, 'R', currentPostures[currentPostures.Count - 2].RightHandPosture.rhp));
//                         else
//                             keyFrames.right.Add(GetKeyFrameNEW(id, "end", "posture", evt.Signal.FindSynchro("end").Time, 'R', null));
//                     }
//                 }
//                 //agent_.Log("REMOVING current posture: " + id);
//                 currentPostures.RemoveAt(currentPostures.Count - 1);
//             }
//             else if (evt.Synchro.Id.Equals("relax") && currentPostures.Count > 0 && !currentPostures[currentPostures.Count - 1].Equals(stance))
//             {
//                 /*foreach (var cp in currentPostures)
//                 {
//                     agent_.Log("In currentPostures: " + cp.LeftHandPosture.id + " " + cp.name);
//                 }*/
//                 int index = currentPostures.FindIndex(x => x.name.Equals(stance));
//                 if (index != -1)
//                 {
//                     //agent_.Log("REMOVING a posture (not the current one): " + currentPostures[index].LeftHandPosture.id + " " + currentPostures[index].name);
//                     currentPostures.RemoveAt(index);
//                 }
//             }
//         }



//         public void PlayGesture(Bml.Scheduler scheduler, Bml.Event evt, string name, string modality, string mode,
//             GestureDescription gestureShape_, string target, MocapDescription mocap)
//         {
//             if (evt.Synchro.Id.Equals("end") && modality == "gesture" && mocap != null)
//             {
//                 agent_.animationEngine.StopMotionCapture(name);
//                 return;
//             }

//             if (!evt.Synchro.Id.Equals("start"))
//                 return;
//             string id = evt.Bml.Id +':'+ evt.Signal.Id;

//             if (modality == "gesture" && mocap != null)
//             {
//                 agent_.animationEngine.PlayMotionCapture(name);
//             }
//             else
//             {
//                 string side = gestureShape_.side;
//                 bool mandatory = gestureShape_.shapeMandatory;
//                 if (modality.Equals("pointing"))// to do for gesture on target?? quoi faire si non ateignable?
//                 {
//                     int s = agent_.animationEngine.GetTargetSide(target, mode);

//                     if (s == 0 || s == -1)
//                     {
//                         agent_.Log("Target " + name + " not found or behind agent's back. Pointing gesture will be discarded.");
//                         return;
//                     }

//                     side = s == 1 ? "LEFT" : "RIGHT";
//                     mandatory = true;
//                 }
//                 /*else //A revoir car on peut montrer le geste dans le vide à l'emplacement par défaut
//                 {
//                     //If a gesture depends on a target but this target is not in the scene,
//                     //then the gesture will be discarded
//                     foreach (var ph in gestureShape_.phases)
//                     {
//                         if((ph.leftHand != null && ph.leftHand.target && target != null) ||
//                            (ph.rightHand != null && ph.rightHand.target && target != null))
//                         {
//                             if(!agent_.animationEngine.TargetFound(target))
//                             {
//                                 agent_.Log("Gesture " + name + " depends on target " + target + " but target not found. Gesture will be discarded.");
//                                 return;
//                             }
//                         }
//                     }
//                 }*/
//                 char[] left = new char[2] ;
//                 char[] right = new char[2];


//                 //Managing conflicts. Change side when possible otherwise replace current gesture
//                 if (side == "LEFT" && (mandatory == true || mode == "" || mode == "LEFT"))
//                 {
//                     left[0] = 'L'; left[1] = 'L';
//                     //addKeyFrames(gestureShape_, modality, name, target, ref keyFrames.left, evt.Signal.Synchros, 'L', 'L');
//                 }
//                 else if (side == "LEFT" && mandatory == false && mode == "RIGHT")
//                 {
//                     right[0] = 'L'; right[1] = 'R';
//                     //addKeyFrames(gestureShape_, modality, name, target, ref keyFrames.right, evt.Signal.Synchros, 'L', 'R');
//                 }
//                 else if (side == "LEFT" && mandatory == false && mode == "BOTH")
//                 {
//                     left[0] = 'L'; left[1] = 'L';
//                     right[0] = 'L'; right[1] = 'R';
//                     //addKeyFrames(gestureShape_, modality, name, target, ref keyFrames.left, evt.Signal.Synchros, 'L', 'L');
//                     //addKeyFrames(gestureShape_, modality, name, target, ref keyFrames.right, evt.Signal.Synchros, 'L', 'R');
//                 }

//                 if (side == "RIGHT" && (mandatory == true || mode == "" || mode == "RIGHT"))
//                 {
//                     right[0] = 'R'; right[1] = 'R';
//                     //addKeyFrames(gestureShape_, modality, name, target, ref keyFrames.right, evt.Signal.Synchros, 'R', 'R');
//                 }
//                 else if (side == "RIGHT" && mandatory == false && mode == "LEFT")
//                 {
//                     left[0] = 'R'; left[1] = 'L';
//                     //addKeyFrames(gestureShape_, modality, name, target, ref keyFrames.left, evt.Signal.Synchros, 'R', 'L');
//                 }
//                 else if (side == "RIGHT" && mandatory == false && mode == "BOTH")
//                 {
//                     left[0] = 'R'; left[1] = 'L';
//                     right[0] = 'R'; right[1] = 'R';
//                     //addKeyFrames(gestureShape_, modality, name, target, ref keyFrames.right, evt.Signal.Synchros, 'R', 'R');
//                     //addKeyFrames(gestureShape_, modality, name, target, ref keyFrames.left, evt.Signal.Synchros, 'R', 'L');
//                 }

//                 if ((side == "BOTH" || side == "LEFT_RIGHT") && (mandatory == true || mode == "" || mode == "BOTH"))
//                 {
//                     left[0] = 'L'; left[1] = 'L';
//                     right[0] = 'R'; right[1] = 'R';
//                     //addKeyFrames(gestureShape_, modality, name, target, ref keyFrames.left, evt.Signal.Synchros, 'L', 'L');
//                     //addKeyFrames(gestureShape_, modality, name, target, ref keyFrames.right, evt.Signal.Synchros, 'R', 'R');
//                 }
//                 else if ((side == "BOTH" || side == "LEFT_RIGHT") && (mandatory == false && mode == "LEFT"))
//                 {
//                     left[0] = 'L'; left[1] = 'L';
//                     //addKeyFrames(gestureShape_, modality, name, target, ref keyFrames.left, evt.Signal.Synchros, 'L', 'L');
//                 }
//                 else if ((side == "BOTH" || side == "LEFT_RIGHT") && (mandatory == false && mode == "RIGHT"))
//                 {
//                     right[0] = 'R'; right[1] = 'R';
//                     //addKeyFrames(gestureShape_, modality, name, target, ref keyFrames.right, evt.Signal.Synchros, 'R', 'R');
//                 }

//                 if(left[0] != 0)
//                     addKeyFrames(id, gestureShape_, modality, name, target, ref keyFrames.left, evt.Signal.Synchros, left[0], left[1]);
//                 if (right[0] != 0)
//                     addKeyFrames(id, gestureShape_, modality, name, target, ref keyFrames.right, evt.Signal.Synchros, right[0], right[1]);
//             }
//         }


//         private void addKeyFrames(string id, GestureDescription gd, string modality, string name, string target, ref List<HandKeyFrame> frames,
//             IEnumerable<Bml.Synchro> sync, char side, char askedSide)
//         {
//             List<Bml.Synchro> synchros = new List<Bml.Synchro>(sync);
//             synchros.Sort(delegate (Bml.Synchro p1, Bml.Synchro p2)
//             {
//                 return p1.Time.CompareTo(p2.Time);
//             });

//             double start = synchros.Find(x => x.Id == "start").Time;
//             int newGestureStrokeIndex = synchros.FindIndex(x => x.Id == "stroke");
//             int newGestureFirstPhase = 0;

//             var previousGestureStrokeIndex = frames.FindIndex(x => x.name.Equals("stroke"));
            

//             if (previousGestureStrokeIndex == -1)
//             {
//                 //the overlapping is after the stroke of the current gesture
//                 //erase all remaining phases of the current gesture and add start for the new one (twice for spline interpolation)
//                 frames.Clear();
//                 frames.Add(GetKeyFrameNEW(id, "start", modality, start, askedSide, null));
//                 frames.Add(GetKeyFrameNEW(id, "start", modality, start, askedSide, null));

//             }
//             else if (synchros[newGestureStrokeIndex].Time <= frames[previousGestureStrokeIndex].time + 0.2)
//             {
//                 agent_.Log("Conflicting gestures: " + name + " won't be played");
//                 return;
//             }
//             else if (synchros[newGestureStrokeIndex].Time > frames[previousGestureStrokeIndex].time + 0.25) //0.25 is enough to pass smoothly from a keyframe to another
//             {
//                 //gestures overlap
//                 //look for first keyframe to animate in the incoming gesture
//                 double stroke_time = frames[previousGestureStrokeIndex].time;
//                 var k = synchros.FindIndex(x => x.Time > stroke_time);
//                 for (; k <= newGestureStrokeIndex; k++)
//                 {
//                     newGestureFirstPhase = gd.phases.FindIndex(x => x.name == synchros[k].Id);
//                     if (newGestureFirstPhase != -1)
//                         break;
//                 }

//                 //look for last keyframe to animate in the current gesture
//                 int j = previousGestureStrokeIndex + 1;
//                 for (; j < frames.Count; j++)
//                     if (frames[j].time > synchros[k].Time) { j -= 1; break; }
//                 if (j == frames.Count) j = frames.Count - 1;

//                 //Create start keyframe for the incoming gesture at the same time of the last keyframe of the current gesture
//                 HandKeyFrame key = GetKeyFrameNEW(id, "start", modality, frames[j].time, askedSide, null);

//                 frames.RemoveRange(j, frames.Count - j);

//                 //Replace last keyframe in the current gesture with the start of the next one
//                 frames.Add(key);
//             }

//             List<Relative_Phase> phases = gd.phases;

//             for (int i = newGestureFirstPhase; i < phases.Count; i++)
//             {
//                 Relative_Phase rp = gd.phases[i];
//                 Relative_Hand_Position rhp;
//                 if (side == 'L')
//                     rhp = rp.leftHand;
//                 else
//                     rhp = rp.rightHand;

//                 //agent_.Log("Adding key frame for " + id + ":" + rp.name + " at: " + synchros.Find(x => x.Id == rp.name).Time);
//                 HandKeyFrame key = GetKeyFrameNEW(id, rp.name, modality, synchros.Find(x => x.Id == rp.name).Time, askedSide, rhp, target);
//                 frames.Add(key);
//             }

//             //add end keyframe
//             //agent_.Log("Adding key frame for " + id + ":end at: " + synchros.Find(x => x.Id == "end").Time);
//             frames.Add(GetKeyFrameNEW(id, "end", modality, synchros.Find(x => x.Id == "end").Time, askedSide, null, name));

//             //test spline interpolation
//             //frames.Insert(0, GetKeyFrameNEW(id, "end", modality, synchros.Find(x => x.Id == "end").Time, askedSide, null, name));
//             //end test spline interpolation

//             //For debug
//             /*agent_.Log("!!!!!!!!!!!! Adding kframe for: " + id);
//             foreach (var g in frames)
//             {
//                 agent_.Log(g.id + " " + g.name + " " + g.time);
//             }*/
//         }

//         private HandKeyFrame GetKeyFrameNEW(string id, string name, string modality, double start, char side, Relative_Hand_Position rhp, string target = "")
//         {
//             HandKeyFrame hand = new HandKeyFrame()
//             {
//                 id = id,
//                 name = name,
//                 time = start,
//                 target = target,
//                 //MGoffset
//                 useHandOffset = (modality == "gesture" && target != "") ? true : false,
//                 modality = modality,
//                 rhp = rhp
//             };

//             return hand;
//         }
//     }
// }

// /*    
// public static Vector3 CosInterpolate3D(Vector3 a, Vector3 b, float time)
//         {
//             float y = (1 - (float)Math.Cos(time * Math.PI)) / 2;
//             return new Vector3(a.x * (1 - y) + b.x * y, a.y * (1 - y) + b.y * y, a.z * (1 - y) + b.z * y);
//         }

//         public static float CosInterpolate2D(float a, float b, float time)
//         {
//             float y = (1 - (float)Math.Cos(time * Math.PI)) / 2;
//             return a * (1 - y) + b * y;
//         }

// public Quaternion PointingWristRotation(Vector3 pointingPosition, Transform cible, char side)//MG : Adapté de version des gestes#2
// {
//     float angleX = 0.0f;
//     float angleY = 0.0f;
//     float angleZ = 0.0f;
//     float pointing_wrist_angle = 0; // 90.0f;

//     Transform hand = agent_.findChild(agent_.agent.transform, "LeftHand");
//     if (side == 'R') hand = agent_.findChild(agent_.agent.transform, "RightHand");

//     //Transform cible = GameObject.Find(target).transform;//MG mercredi
//     if (cible.position == pointingPosition)
//     { //MG mercredi GetAbsoluteSwivelPosition(2)
//         if (side == 'L') pointingPosition = transform.TransformPoint(GetAbsoluteSwivelPosition(2));// correspond au armswivel init
//         else pointingPosition = transform.TransformPoint(GetAbsoluteSwivelPosition(-2));// correspond au armswivel init
//     }

//     //position qu'à le poignet en se collant à l'effecteur non tourné (? A VERIFIER)
//     // Matrix4x4 handrotinit = (side=='L'? Matrix4x4.Rotate(agent_transform.rotation)*Matrix4x4.Rotate(Quaternion.Euler(wristInitLeftRot)): Matrix4x4.Rotate(agent_transform.rotation)*Matrix4x4.Rotate(Quaternion.Euler(wristInitRightRot)));//Le repère diffère de 180° en X selon la main choisie 
//     Matrix4x4 handrotinit = (side == 'L' ? Matrix4x4.Rotate(agent_transform.rotation) * Matrix4x4.Rotate(wristInitLeftRot) : Matrix4x4.Rotate(agent_transform.rotation) * Matrix4x4.Rotate(wristInitRightRot));
//     //angleX = (side == 'L' ? -pointing_wrist_angle:pointing_wrist_angle);//MG mercredi
//     angleX = pointing_wrist_angle;//MG mercredi
//     Matrix4x4 rotX = handrotinit * Matrix4x4.Rotate(Quaternion.Euler(angleX, 0.0f, 0.0f)) * handrotinit.inverse;

//     //Matrix corresponding to the rotation around the Y hand axis to point the target (angle between hand position and target), expressed in the global axis
//     Matrix4x4 targethand = (rotX * handrotinit).inverse * Matrix4x4.Translate(cible.position - pointingPosition) * (rotX * handrotinit);

//     //MG mercredi
//     // Cas où l'objet est derriere non traité (le pointage est annulé?)
//     // int sign = ((cible.position.z - GameObject.Find(agent_transform.gameObject.name + "master").transform.position.z) >= 0.0 ? 0 : 1);Debug.Log("sign :"+sign);
//     //Debug.Log(targethand.m03 + "  " + targethand.m13 + "  " + targethand.m23);
//     angleY = -(float)Math.Atan((targethand.m23) / (targethand.m03)) * 180.0f / (float)Math.PI;

//     //Corrections liées au artan (car tan à un modif répétitif), déduit des tests, devrait se calculer en fonction des quadrants //MG mercredi
//     if (side == 'L' && targethand.m03 < 0) angleY += 180.0f;
//     else if (side == 'R' && targethand.m03 > 0) angleY += 180.0f;

//     //The angle is defined for the hand, but the index is a little bit translated from this axis => We have to compensate
//     //The compensation is a compromise between the arm/hand and index axes because the overall aspect is more important than only the index direction (therefore it is not optimal)
//     float anglecompY = -8f;
//     Matrix4x4 rotY = (rotX * handrotinit) * Matrix4x4.Rotate(Quaternion.Euler(0.0f, angleY - anglecompY, 0.0f)) * (rotX * handrotinit).inverse;

//     //Matrix corresponding to the rotation around the Z hand axis to point the target (angle between hand position and target), expressed in the global axis
//     Matrix4x4 targethand2 = (rotY * rotX * handrotinit).inverse * Matrix4x4.Translate(cible.position - pointingPosition) * (rotY * rotX * handrotinit);
//     //int sign2 = ((cible.position.x - GameObject.Find(agent_transform.gameObject.name + "master").transform.position.x) >= 0.0 ? 1 : 0); Debug.Log("sign2 :" + sign2);
//     angleZ = (float)Math.Atan((targethand2.m13) / (targethand2.m03)) * 180.0f / (float)Math.PI;// + sign2*180;// 
//     float anglecompZ = hand.GetChild(0).localRotation.eulerAngles.z;//Mettre une valeur arbitraire si besoin
//     Matrix4x4 rotZ = (rotY * rotX * handrotinit) * Matrix4x4.Rotate(Quaternion.Euler(0.0f, 0.0f, angleZ - anglecompZ)) * (rotY * rotX * handrotinit).inverse;

//     //Final rotation
//     Matrix4x4 rot = Matrix4x4.Rotate(agent_transform.rotation).inverse * (rotZ * rotY * rotX * handrotinit);
//     Vector3 angles = rot.rotation.eulerAngles;
//     for (int i = 0; i < 3; i++)
//     {
//         if (angles[i] > 180) angles[i] -= 360f;
//         else if (angles[i] <= -180) angles[i] += 360f;
//     }

//     return Quaternion.Euler(angles.x, angles.y, angles.z);
// }




// /*   private void moveArm(List<HandKeyFrame> frames, double time, char side)
//    {
//        IKEffector effector;
//        FBIKChain armChain;
//        HandKeyFrame posture;
//        Quaternion handRotation;
//        Transform shoulder;

//        float sign = 1;
//        Dictionary<Relative_Hand_Position.ArmSwivel, ValueTuple<Vector3, Vector3>> swivel;

//        if (side == 'L')
//        {
//            shoulder = leftShoulder;
//            effector = fbbik.solver.leftHandEffector;
//            armChain = fbbik.solver.leftArmChain;
//            swivel = symbolicPositions.leftArmSwivel;
//            posture = LeftHandPosture;
//            handRotation = leftHand.rotation;
//        }
//        else
//        {
//            shoulder = rightShoulder;
//            effector = fbbik.solver.rightHandEffector;
//            armChain = fbbik.solver.rightArmChain;
//            swivel = symbolicPositions.rightArmSwivel;
//            posture = RightHandPosture;
//            handRotation = rightHand.rotation;
//            sign = -1;
//        }

//        if (frames.Count > 0)
//        {
//            int i = frames.Count - 1;
//            //flagHandUpdate = true;
//            double endTime = -1;

//            if (i >= 1)
//            {
//                double lerpTime = (time - frames[i].time) / (frames[i - 1].time - frames[i].time);
//                endTime = frames[i - 1].time;

//                //wrist rotation
//                effector.rotationWeight = 1.0f;
//                Quaternion qa = agent_.agent.transform.rotation * frames[i].handRotation;//MG
//                Quaternion qb = agent_.agent.transform.rotation * frames[i - 1].handRotation;//MG
//                effector.rotation = Quaternion.Lerp(qa, qb, (float)lerpTime);

//                effector.positionWeight = 1f;
//                armChain.bendConstraint.weight = bendConstraint_weight;
//                armChain.bendConstraint.bendGoal.position = transform.TransformPoint(Vector3.Lerp(frames[i].armSwivel, frames[i - 1].armSwivel, (float)lerpTime));
//                //Hand offset computation when target or pointing
//                if (false) //(frames[i - 1].useHandOffset)
//                {

//                    offset = sign * (Matrix4x4.Rotate(effector.rotation) * Matrix4x4.Translate(frames[i - 1].handShape.size * transform.localScale.y) * Matrix4x4.Rotate(effector.rotation).inverse).GetColumn(3);
//                    /******************************************************************************/
// //MG mercredi //version avec épaisseur objet pour pointing
// /*float objectThickness = 0.0f;
// if (frames[i - 1].pointingTarget != null) objectThickness = frames[i - 1].pointingTarget.localScale.x / 2; // TO BE COMPUTED : épaisseur de l'objet dans la direction de la main

// //pas besoin offset => remis à 0
// if (frames[i - 1].pointingTarget != null && Vector3.Distance(shoulder.position, frames[i - 1].handPosition + offset) < (Vector3.Distance(shoulder.position, frames[i - 1].pointingTarget.position) - objectThickness))
// { offset.x = 0.0f; offset.y = 0.0f; offset.z = 0.0f; }

// //besoin offset 
// else if (frames[i - 1].pointingTarget != null)
// {
//     Vector3 deltaObjectThickness = new Vector3(0.0f, 0.0f, 0.0f);
//     if (side == 'R') objectThickness = -objectThickness;
//     deltaObjectThickness = (Matrix4x4.Rotate(effector.rotation) * Matrix4x4.Translate(new Vector3(objectThickness, 0.0f, 0.0f)) * Matrix4x4.Rotate(effector.rotation).inverse).GetColumn(3);

//     //l'effecteur doit être plus petit que la taille de la main + objectThickness (cas où l'effecteur n'est pas placé sur la cible)
//     if (Vector3.Distance(shoulder.position, frames[i - 1].handPosition) < Vector3.Distance(shoulder.position, frames[i - 1].pointingTarget.position))
//     {
//         Vector3 dist = new Vector3(Vector3.Distance(frames[i - 1].handPosition, frames[i - 1].pointingTarget.position), 0f, 0f);
//         if (side == 'R') dist = -dist;
//         Vector3 delta = (Vector3)(Matrix4x4.Rotate(effector.rotation) * Matrix4x4.Translate(dist) * Matrix4x4.Rotate(effector.rotation).inverse).GetColumn(3);
//         offset = offset + deltaObjectThickness - delta;
//     }
//     //l'effecteur est au centre de la cible
//     else offset += deltaObjectThickness;
// }

// if (frames[i].name == "start") effector.position = Vector3.Lerp(frames[i].handPosition, frames[i - 1].handPosition - offset, (float)lerpTime);
// else effector.position = Vector3.Lerp(frames[i].handPosition, frames[i - 1].handPosition, (float)lerpTime) - offset;
// */

// /*********************************************************************************/

// //MG mercredi //version sans épaisseur objet
// /*if (frames[i - 1].pointingTarget != null && Vector3.Distance(shoulder.position, frames[i - 1].handPosition + offset) < (Vector3.Distance(shoulder.position, frames[i - 1].pointingTarget.position)))
// {
//     offset.x = 0; offset.y = 0; offset.z = 0;
// }
// //Cas où l'offset est trop grand car l'effecteur n'est pas sur la cible (mais le doigt la dépasse quand même)
// else if(frames[i - 1].pointingTarget != null && Vector3.Distance(shoulder.position, frames[i - 1].handPosition + offset) > (Vector3.Distance(shoulder.position, frames[i - 1].pointingTarget.position))&& Vector3.Distance(shoulder.position, frames[i - 1].handPosition)< Vector3.Distance(shoulder.position, frames[i - 1].pointingTarget.position))
// {
//     Vector3 dist = new Vector3(Vector3.Distance(frames[i - 1].handPosition, frames[i - 1].pointingTarget.position), 0f, 0f);
//     if (side == 'R') dist = -dist;
//     Vector3 delta = (Vector3)(Matrix4x4.Rotate(effector.rotation) * Matrix4x4.Translate(dist) * Matrix4x4.Rotate(effector.rotation).inverse).GetColumn(3);
//     offset = offset - delta;
// }

// if (frames[i].name == "start") effector.position = Vector3.Lerp(frames[i].handPosition, frames[i - 1].handPosition - offset, (float)lerpTime);
// else effector.position = Vector3.Lerp(frames[i].handPosition, frames[i - 1].handPosition, (float)lerpTime) - offset;

// /*********************************************************************************/
// /*}
// else
// {
//     effector.position = Vector3.Lerp(frames[i].handPosition, frames[i - 1].handPosition, (float)lerpTime);
//     offset.x = 0; offset.y = 0; offset.z = 0;
// }
// }
// else if (i == 0 && posture != null)
// {
// double lerpTime = (time - frames[i].time) / 0.2; // (frames[i].endTime - frames[i].time);
// endTime = frames[i].time + 0.2; // frames[i].endTime;

// //with wrist rotation
// effector.rotationWeight = 1.0f;
// Quaternion qa = agent_.agent.transform.rotation * frames[i].handRotation;
// Quaternion qb = agent_.agent.transform.rotation * posture.handRotation;
// effector.rotation = Quaternion.Lerp(qa, qb, (float)lerpTime);
// ////

// effector.positionWeight = 1f;
// armChain.bendConstraint.weight = bendConstraint_weight;
// effector.position = Vector3.Lerp(frames[i].handPosition - offset, posture.handPosition, (float)lerpTime);
// armChain.bendConstraint.bendGoal.position = transform.TransformPoint(Vector3.Lerp(frames[i].armSwivel, posture.armSwivel, (float)lerpTime));
// }
// else
// {
// double lerpTime = 1.0f - ((time - frames[i].time) / 0.2); // (frames[i].endTime - frames[i].time));
// endTime = frames[i].time + 0.2;  //endTime = frames[i].endTime;
// effector.rotationWeight = (float)lerpTime;
// armChain.bendConstraint.weight = LinearInterpolate2D(0, bendConstraint_weight, (float)lerpTime);
// effector.positionWeight = (float)lerpTime;
// }

// if (time > endTime)
// {
// frames.RemoveAt(i);
// }
// }
// }*/

// /*
//     private HandKeyFrame GetKeyFrame(string name, double start, double end, char side)
//     {
//         //keyframe for current position, just for "start" phases
//         HandKeyFrame hand = new HandKeyFrame();
//         hand.name = name;
//         hand.time = start;
//         hand.endTime = end;
//         hand.useHandOffset = false;
//         hand.pointingTarget = null;
//         Transform hand_transform;

//         if (side == 'L')
//         {
//             hand_transform = agent_.findChild(agent_.agent.transform, "LeftHand");
//             hand.armSwivel = transform.InverseTransformPoint(leftArmSwivelAttractor.transform.position);
//         }
//         else
//         {
//             hand_transform = agent_.findChild(agent_.agent.transform, "RightHand");
//             hand.armSwivel = transform.InverseTransformPoint(rightArmSwivelAttractor.transform.position);
//         }
//         hand.handPosition = hand_transform.position;
//         hand.handRotation = agent_.agent.transform.rotation * hand_transform.rotation;

//         HandShape current = new HandShape();
//         current.fingers = new Dictionary<string, Quaternion>();
//         GetHandCurrentShape(current.fingers, side);
//         hand.handShape = current;

//         return hand;
//     }

//     private HandKeyFrame GetKeyFrame(string name, double start, double end, char side, Vector3 pointingPosition, Relative_Hand_Position rhp, string target)
//     {
//         //keyframe for pointing
//         HandKeyFrame hand = new HandKeyFrame();
//         hand.name = name;
//         hand.time = start;
//         hand.endTime = end;
//         hand.useHandOffset = true;
//         Transform cible = GameObject.Find(target).transform;
//         hand.pointingTarget = cible;

//         hand.handPosition = new Vector3(pointingPosition.x, pointingPosition.y, pointingPosition.z);
//         hand.handRotation = PointingWristRotation(pointingPosition, cible, side);
//         if(side == 'L')
//             hand.armSwivel = symbolicPositions.leftArmSwivel[Relative_Hand_Position.ArmSwivel.NORMAL].Item1;
//         else
//             hand.armSwivel = symbolicPositions.rightArmSwivel[Relative_Hand_Position.ArmSwivel.NORMAL].Item1;
//         hand.handShape = setHandShape("POINT1", side);

//         return hand;
//     }
//     */

// /*
//    private HandKeyFrame GetKeyFrame(string name, double start, double end, char side, Relative_Hand_Position rhp, string target="")
//    {
//        //keyframe for gesture
//        HandKeyFrame hand = new HandKeyFrame();
//        hand.name = name;
//        hand.time = start;
//        hand.endTime = end;
//        if (target == "") hand.useHandOffset = false;
//        else hand.useHandOffset = true;
//        hand.pointingTarget = null;

//        //to eliminate unity
//        hand.rhp = rhp;

//        SetHand(ref hand, rhp, side, target);
//        return hand;
//    }
//    */
// /*
// private void SetHand(ref HandKeyFrame p, Relative_Hand_Position rhp, char side, string target="")
// {
//     Dictionary<Relative_Hand_Position.RadialOrientation, ValueTuple<float, float>> radialOrientation;
//     Dictionary<Relative_Hand_Position.ArmSwivel, ValueTuple<Vector3, Vector3>> armSwivel;
//     //Quaternion initWristOrientation = Quaternion.identity;
//     if (side == 'L')
//     {
//         //initWristOrientation = rhp.initLeftWristOrientation;
//         radialOrientation = symbolicPositions.leftRadialOrientation;
//         armSwivel = symbolicPositions.leftArmSwivel;
//     }
//     else
//     {
//         //initWristOrientation = rhp.initRightWristOrientation;
//         radialOrientation = symbolicPositions.rightRadialOrientation;
//         armSwivel = symbolicPositions.rightArmSwivel;
//     }

//     GameObject objTarget = GameObject.Find(target);

//     if (target == "" || objTarget == null)
//     {
//         p.handPosition = agent_transform.TransformPoint(radialOrientation[rhp.radialOrientation].Item1 + radialOrientation[rhp.radialOrientation].Item2 * rhp.radialOrientationPercentage,
//                                                 symbolicPositions.height[rhp.height].Item1 + symbolicPositions.height[rhp.height].Item2 * rhp.heightPercentage,
//                                                 symbolicPositions.distance[rhp.distance].Item1 + symbolicPositions.distance[rhp.distance].Item2 * rhp.distancePercentage);
//     }
//     else
//     {
//         //POSITION DU TARGET, attention aux offset pour les mains
//         p.handPosition = new Vector3(objTarget.transform.position.x + rhp.radialOrientationOffset * agent_transform.localScale.x,
//                                      objTarget.transform.position.y + rhp.heightOffset * agent_transform.localScale.y,
//                                      objTarget.transform.position.z + rhp.distanceOffset * agent_transform.localScale.z);
//     }

//     p.handRotation = new Quaternion();
//     //Quaternion askedRot=new Quaternion(symbolicPositions.wristXOrientation[rhp.wristX].Item1 + symbolicPositions.wristXOrientation[rhp.wristX].Item2 * rhp.wristXPercentage,
//     //                             symbolicPositions.wristYOrientation[rhp.wristY].Item1 + symbolicPositions.wristYOrientation[rhp.wristY].Item2 * rhp.wristYPercentage,
//     //                             symbolicPositions.wristZOrientation[rhp.wristZ].Item1 + symbolicPositions.wristZOrientation[rhp.wristZ].Item2 * rhp.wristZPercentage, 1);

//     p.handRotation = rhp.wristRotation;// initWristOrientation; // * askedRot;

//     p.armSwivel = armSwivel[rhp.armSwivel].Item1 + armSwivel[rhp.armSwivel].Item2 * rhp.armSwivelPercentage;

//     p.handShape = setHandShape(rhp.handShape, side);
// }
// */

// /*private void takeHandShape(List<HandKeyFrame> frames, Dictionary<string, Transform> hand, double time, char side)
//    {
//        HandKeyFrame posture;
//        Dictionary<string, Quaternion> rest;
//        if (side == 'L') { posture = LeftHandPosture; rest = restLeftHand; }
//        else { posture = RightHandPosture; rest = restRightHand; }

//        if (frames.Count > 1)
//        {
//            int i = frames.Count - 1;
//            flagFingersUpdate = true;
//            double lerpTime = (time - frames[i].time) / (frames[i - 1].time - frames[i].time);
//            if (i >= 1)
//            {
//                if (frames[i - 1].name != "end")
//                    moveHand(hand, frames[i].handShape.fingers, frames[i - 1].handShape.fingers, (float)lerpTime);
//                else
//                {
//                    if (posture != null) moveHand(hand, frames[i].handShape.fingers, posture.handShape.fingers, (float)lerpTime);
//                    else moveHand(hand, frames[i].handShape.fingers, rest, (float)lerpTime);
//                }
//            }
//        }
//        else
//        {
//            //Force hand shape each frame when there is a posture
//            //otherwise force hand shape to rest position just once
//            if (posture != null) moveHand(hand, posture.handShape.fingers, null, -1);
//            else if (flagFingersUpdate == true)
//            {
//                moveHand(hand, rest, null, -1);
//                flagFingersUpdate = false;
//            }
//        }
//    }*/

// /*public void createSymbolicPositions()
//     {
//         //TODO: maybe this is not very generic. With other agents the name of the bones must
//         //be checked, probably they are not the same. Check distances and x axis direction
//         //distance position should be done better. distance_touch is note generic (just hips + 0.2f)
//         //I do not know yet how to compute the agent thickness and the symbolic position for distance_touch
//         //is not the same if it is on the chest or on the belt (especially for a woman!)

//         symbolicPositions = new SymbolicPositions();
//         symbolicPositions.leftRadialOrientation = new Dictionary<Relative_Hand_Position.RadialOrientation, ValueTuple<float, float>>();
//         symbolicPositions.rightRadialOrientation = new Dictionary<Relative_Hand_Position.RadialOrientation, ValueTuple<float, float>>();
//         symbolicPositions.height = new Dictionary<Relative_Hand_Position.Height, ValueTuple<float, float>>();
//         symbolicPositions.distance = new Dictionary<Relative_Hand_Position.Distance, ValueTuple<float, float>>();
        
//         symbolicPositions.leftArmSwivel = new Dictionary<Relative_Hand_Position.ArmSwivel, ValueTuple<Vector3, Vector3>>();
//         symbolicPositions.rightArmSwivel = new Dictionary<Relative_Hand_Position.ArmSwivel, ValueTuple<Vector3, Vector3>>();

//         symbolicPositions.wristXOrientation = new Dictionary<Relative_Hand_Position.ObjectXOrientation, ValueTuple<float, float>>();
//         symbolicPositions.wristYOrientation = new Dictionary<Relative_Hand_Position.ObjectYOrientation, ValueTuple<float, float>>();
//         symbolicPositions.wristZOrientation = new Dictionary<Relative_Hand_Position.ObjectZOrientation, ValueTuple<float, float>>();

//         Transform hips = agent_.findChild(agent_transform, "Hips");
//         Transform leftEye = agent_.findChild(agent_transform, "LeftEye");
//         Transform leftHand = agent_.findChild(agent_transform, "LeftHand");
//         Transform neck = agent_.findChild(agent_transform, "Neck");
//         Transform headEnd = agent_.findChild(agent_transform, "HeadEnd");
//         Transform pelvis = agent_.findChild(agent_transform, "LeftUpLeg");

//         Transform headTop = agent_.findChild(agent_transform, "HeadEnd");
//         Transform leftShoulder = agent_.findChild(agent_transform, "LeftShoulder");
//         Transform leftForeArm = agent_.findChild(agent_transform, "LeftForeArm");
//         Transform leftArm = agent_.findChild(agent_transform, "LeftArm");
//         Transform spine2 = agent_.findChild(agent_transform, "Spine2");
//         Transform spine1 = agent_.findChild(agent_transform, "Spine1");
//         Transform spine = agent_.findChild(agent_transform, "Spine");
//         Transform leftLeg = agent_.findChild(agent_transform, "LeftLeg");
//         Transform leftIndex = agent_.findChild(agent_transform, "LeftHandIndex4");

//         armLength = Vector3.Distance(leftForeArm.position, leftArm.position) + Vector3.Distance(leftForeArm.position, leftHand.position); // * transform.localScale.x;

//         float fromY = agent_transform.InverseTransformPoint(hips.position).y;
//         float above_head = (Vector3.Distance(hips.position, headEnd.position) + Vector3.Distance(hips.position, spine1.position)*0.5f) / transform.localScale.y;
//         float head = Vector3.Distance(hips.position, leftEye.position) / transform.localScale.y;
//         float shoulder = Vector3.Distance(hips.position, leftShoulder.position) / transform.localScale.y;
//         float chest = (Vector3.Distance(hips.position, spine2.position) + Vector3.Distance(neck.position, spine2.position)*0.33f) / transform.localScale.y;
//         float abdomen = Vector3.Distance(hips.position, spine1.position) / transform.localScale.y;
//         float belt = 0;
//         float below_belt = Vector3.Distance(hips.position, spine1.position) / transform.localScale.y;

//         symbolicPositions.height.Add(Relative_Hand_Position.Height.ABOVE_HEAD, (fromY + above_head, Mathf.Abs(head - above_head)));
//         symbolicPositions.height.Add(Relative_Hand_Position.Height.HEAD, (fromY + head, Mathf.Abs(head - above_head)));
//         symbolicPositions.height.Add(Relative_Hand_Position.Height.SHOULDER, (fromY + shoulder, Mathf.Abs(shoulder - head)));
//         symbolicPositions.height.Add(Relative_Hand_Position.Height.CHEST, (fromY + chest, Mathf.Abs(chest - shoulder)));
//         symbolicPositions.height.Add(Relative_Hand_Position.Height.ABDOMEN, (fromY + abdomen, Mathf.Abs(abdomen - chest)));
//         symbolicPositions.height.Add(Relative_Hand_Position.Height.BELT, (fromY + belt, Mathf.Abs(belt - abdomen)));
//         symbolicPositions.height.Add(Relative_Hand_Position.Height.BELOW_BELT, (fromY - below_belt, Mathf.Abs(below_belt - belt)));

//         //radial orientation symbolic position. A calculer au tout debut quand bras pas baisses
//         float fromX = agent_transform.InverseTransformPoint(hips.position).x;
//         float farout = (Vector3.Distance(leftArm.position, leftShoulder.position) + Vector3.Distance(leftForeArm.position, leftArm.position) + Vector3.Distance(leftForeArm.position, leftHand.position)) / transform.localScale.x;
//         //float oriz = Math.Abs(agent_transform.InverseTransformPoint(hips.position).x - agent_transform.InverseTransformPoint(leftHand.position).x) / 3.0f;
//         //float farout = agent_transform.InverseTransformPoint(hips.transform.position).x - (Vector3.Distance(leftArm.position, leftShoulder.position) + Vector3.Distance(leftForeArm.position, leftArm.position) + Vector3.Distance(leftForeArm.position, leftHand.position)) / transform.localScale.x; // 3.0f * oriz;
//         float ou = (Vector3.Distance(leftArm.position, leftShoulder.position) + Vector3.Distance(leftForeArm.position, leftArm.position)) / transform.localScale.x; //2.0f * oriz;
//         float side = (Vector3.Distance(leftArm.position, leftShoulder.position)*1.4f) / transform.localScale.x; //1.0f * oriz;
//         float front = (Vector3.Distance(leftArm.position, leftShoulder.position)*0.5f) / transform.localScale.x; //0.1f * oriz; ;
//         float inward =  Vector3.Distance(leftArm.position, leftShoulder.position) / transform.localScale.x; //0.7f * oriz;
//         symbolicPositions.leftRadialOrientation.Add(Relative_Hand_Position.RadialOrientation.FAR_OUT, (fromX - farout, -Mathf.Abs(farout-ou)));
//         symbolicPositions.leftRadialOrientation.Add(Relative_Hand_Position.RadialOrientation.OUT, (fromX - ou, -Mathf.Abs(ou - farout)));
//         symbolicPositions.leftRadialOrientation.Add(Relative_Hand_Position.RadialOrientation.SIDE, (fromX - side, -Mathf.Abs(side - ou)));
//         symbolicPositions.leftRadialOrientation.Add(Relative_Hand_Position.RadialOrientation.FRONT, (fromX - front, -Mathf.Abs(front - side)));
//         symbolicPositions.leftRadialOrientation.Add(Relative_Hand_Position.RadialOrientation.INWARD, (fromX + inward, -Mathf.Abs(inward + front)));

//         symbolicPositions.rightRadialOrientation.Add(Relative_Hand_Position.RadialOrientation.FAR_OUT, (fromX + farout, Mathf.Abs(farout - ou)));
//         symbolicPositions.rightRadialOrientation.Add(Relative_Hand_Position.RadialOrientation.OUT, (fromX + ou, Mathf.Abs(ou - farout)));
//         symbolicPositions.rightRadialOrientation.Add(Relative_Hand_Position.RadialOrientation.SIDE, (fromX + side, Mathf.Abs(side - ou)));
//         symbolicPositions.rightRadialOrientation.Add(Relative_Hand_Position.RadialOrientation.FRONT, (fromX + front, Mathf.Abs(front - side)));
//         symbolicPositions.rightRadialOrientation.Add(Relative_Hand_Position.RadialOrientation.INWARD, (fromX - inward, Mathf.Abs(inward + front)));

//         //distance symbolic position
//         float fromZ = agent_transform.InverseTransformPoint(leftShoulder.position).z;
//         float thickness = Vector3.Distance(spine2.position, leftShoulder.position) / transform.localScale.y;
//         float touch = 0.1f * thickness;
//         float close = 0.5f * thickness;
//         float normal = thickness;
//         float far = 1.5f * thickness;
//         symbolicPositions.distance.Add(Relative_Hand_Position.Distance.TOUCH, (fromZ - touch, Mathf.Abs(touch + close)));
//         symbolicPositions.distance.Add(Relative_Hand_Position.Distance.CLOSE, (fromZ + close, Mathf.Abs(close - normal)));
//         symbolicPositions.distance.Add(Relative_Hand_Position.Distance.NORMAL, (fromZ + normal, Mathf.Abs(normal - far)));
//         symbolicPositions.distance.Add(Relative_Hand_Position.Distance.FAR, (fromZ + far, Mathf.Abs(far - normal)));

//         float asz = transform.InverseTransformPoint(pelvis.position).z;
//         float radius = Vector3.Distance(hips.position, neck.position) / transform.localScale.y; // Math.Abs(agent_transform.InverseTransformPoint(neck.position - hips.position).y);
//         Vector3 stouch = new Vector3(transform.InverseTransformPoint(hips.position).x - radius / 4f, transform.InverseTransformPoint(hips.position).y, asz);
//         Vector3 snormal = new Vector3(transform.InverseTransformPoint(hips.position).x - radius * (2f / 4f), transform.InverseTransformPoint(hips.position).y + radius / 3f, asz);
//         Vector3 sout = new Vector3(transform.InverseTransformPoint(hips.position).x - radius * (3f / 4f), transform.InverseTransformPoint(hips.position).y + radius * (2f / 3f), asz);
//         Vector3 sortho = new Vector3(transform.InverseTransformPoint(hips.position).x - radius, transform.InverseTransformPoint(hips.position).y + radius, asz);
//         symbolicPositions.leftArmSwivel.Add(Relative_Hand_Position.ArmSwivel.TOUCH, (stouch, new Vector3(-Mathf.Abs(stouch.x - snormal.x), Mathf.Abs(stouch.y - snormal.y), 0)));
//         symbolicPositions.leftArmSwivel.Add(Relative_Hand_Position.ArmSwivel.NORMAL, (snormal, new Vector3(-Mathf.Abs(snormal.x - sout.x), Mathf.Abs(snormal.y - sout.y), 0)));
//         symbolicPositions.leftArmSwivel.Add(Relative_Hand_Position.ArmSwivel.OUT, (sout, new Vector3(-Mathf.Abs(sout.x - sortho.x), Mathf.Abs(sout.y - sortho.y), 0)));
//         symbolicPositions.leftArmSwivel.Add(Relative_Hand_Position.ArmSwivel.ORTHOGONAL, (sortho, new Vector3(-Mathf.Abs(sout.x - sortho.x), Mathf.Abs(sout.y - sortho.y), 0)));

//         stouch = new Vector3(transform.InverseTransformPoint(hips.position).x + radius / 4f, transform.InverseTransformPoint(hips.position).y, asz);
//         snormal = new Vector3(transform.InverseTransformPoint(hips.position).x + radius * (2f / 4f), transform.InverseTransformPoint(hips.position).y + radius / 3f, asz);
//         sout = new Vector3(transform.InverseTransformPoint(hips.position).x + radius * (3f / 4f), transform.InverseTransformPoint(hips.position).y + radius * (2f / 3f), asz);
//         sortho = new Vector3(transform.InverseTransformPoint(hips.position).x + radius, transform.InverseTransformPoint(hips.position).y + radius, asz);
//         symbolicPositions.rightArmSwivel.Add(Relative_Hand_Position.ArmSwivel.TOUCH, (stouch, new Vector3(Mathf.Abs(stouch.x - snormal.x), Mathf.Abs(stouch.y - snormal.y), 0)));
//         symbolicPositions.rightArmSwivel.Add(Relative_Hand_Position.ArmSwivel.NORMAL, (snormal, new Vector3(Mathf.Abs(snormal.x - sout.x), Mathf.Abs(snormal.y - sout.y), 0)));
//         symbolicPositions.rightArmSwivel.Add(Relative_Hand_Position.ArmSwivel.OUT, (sout, new Vector3(Mathf.Abs(sout.x - sortho.x), Mathf.Abs(sout.y - sortho.y), 0)));
//         symbolicPositions.rightArmSwivel.Add(Relative_Hand_Position.ArmSwivel.ORTHOGONAL, (sortho, new Vector3(Mathf.Abs(sout.x - sortho.x), Mathf.Abs(sout.y - sortho.y), 0)));

//         symbolicPositions.wristYOrientation.Add(Relative_Hand_Position.ObjectYOrientation.AWAY, (-180, 180));
//         symbolicPositions.wristYOrientation.Add(Relative_Hand_Position.ObjectYOrientation.NORMAL, (0f, 0f));
//         symbolicPositions.wristYOrientation.Add(Relative_Hand_Position.ObjectYOrientation.TOWARD, (180, -180));
//         symbolicPositions.wristZOrientation.Add(Relative_Hand_Position.ObjectZOrientation.OUTWARD, (-180f, 180f));
//         symbolicPositions.wristZOrientation.Add(Relative_Hand_Position.ObjectZOrientation.NORMAL, (0f, 0f));
//         symbolicPositions.wristZOrientation.Add(Relative_Hand_Position.ObjectZOrientation.INWARD, (180f, -180f));
//         symbolicPositions.wristXOrientation.Add(Relative_Hand_Position.ObjectXOrientation.DOWN, (-180f, 180f));
//         symbolicPositions.wristXOrientation.Add(Relative_Hand_Position.ObjectXOrientation.NORMAL, (0f, 0f));
//         symbolicPositions.wristXOrientation.Add(Relative_Hand_Position.ObjectXOrientation.UP, (180f, -180f));

//     }*/

// /*  public void ResetSymbolic(float diff)//MG : si hauteur a été modifiée (donc axe y)
//   {
//       symbolicPositions.leftArmSwivel[Relative_Hand_Position.ArmSwivel.TOUCH] =(symbolicPositions.leftArmSwivel[Relative_Hand_Position.ArmSwivel.TOUCH].Item1+new Vector3(0.0f,diff,0.0f), symbolicPositions.leftArmSwivel[Relative_Hand_Position.ArmSwivel.TOUCH].Item2);
//       symbolicPositions.leftArmSwivel[Relative_Hand_Position.ArmSwivel.NORMAL] = (symbolicPositions.leftArmSwivel[Relative_Hand_Position.ArmSwivel.NORMAL].Item1 + new Vector3(0.0f, diff, 0.0f), symbolicPositions.leftArmSwivel[Relative_Hand_Position.ArmSwivel.NORMAL].Item2);
//       symbolicPositions.leftArmSwivel[Relative_Hand_Position.ArmSwivel.OUT] = (symbolicPositions.leftArmSwivel[Relative_Hand_Position.ArmSwivel.OUT].Item1 + new Vector3(0.0f, diff, 0.0f), symbolicPositions.leftArmSwivel[Relative_Hand_Position.ArmSwivel.OUT].Item2);
//       symbolicPositions.leftArmSwivel[Relative_Hand_Position.ArmSwivel.ORTHOGONAL] = (symbolicPositions.leftArmSwivel[Relative_Hand_Position.ArmSwivel.ORTHOGONAL].Item1 + new Vector3(0.0f, diff, 0.0f), symbolicPositions.leftArmSwivel[Relative_Hand_Position.ArmSwivel.ORTHOGONAL].Item2);

//       symbolicPositions.rightArmSwivel[Relative_Hand_Position.ArmSwivel.TOUCH] = (symbolicPositions.rightArmSwivel[Relative_Hand_Position.ArmSwivel.TOUCH].Item1 + new Vector3(0.0f, diff, 0.0f), symbolicPositions.rightArmSwivel[Relative_Hand_Position.ArmSwivel.TOUCH].Item2);
//       symbolicPositions.rightArmSwivel[Relative_Hand_Position.ArmSwivel.NORMAL] = (symbolicPositions.rightArmSwivel[Relative_Hand_Position.ArmSwivel.NORMAL].Item1 + new Vector3(0.0f, diff, 0.0f), symbolicPositions.rightArmSwivel[Relative_Hand_Position.ArmSwivel.NORMAL].Item2);
//       symbolicPositions.rightArmSwivel[Relative_Hand_Position.ArmSwivel.OUT] = (symbolicPositions.rightArmSwivel[Relative_Hand_Position.ArmSwivel.OUT].Item1 + new Vector3(0.0f, diff, 0.0f), symbolicPositions.rightArmSwivel[Relative_Hand_Position.ArmSwivel.OUT].Item2);
//       symbolicPositions.rightArmSwivel[Relative_Hand_Position.ArmSwivel.ORTHOGONAL] = (symbolicPositions.rightArmSwivel[Relative_Hand_Position.ArmSwivel.ORTHOGONAL].Item1 + new Vector3(0.0f, diff, 0.0f), symbolicPositions.rightArmSwivel[Relative_Hand_Position.ArmSwivel.ORTHOGONAL].Item2);

//       symbolicPositions.height[Relative_Hand_Position.Height.ABOVE_HEAD] = (symbolicPositions.height[Relative_Hand_Position.Height.ABOVE_HEAD].Item1 + diff, symbolicPositions.height[Relative_Hand_Position.Height.ABOVE_HEAD].Item2);
//       symbolicPositions.height[Relative_Hand_Position.Height.SHOULDER] = (symbolicPositions.height[Relative_Hand_Position.Height.SHOULDER].Item1 + diff, symbolicPositions.height[Relative_Hand_Position.Height.SHOULDER].Item2);
//       symbolicPositions.height[Relative_Hand_Position.Height.CHEST] = (symbolicPositions.height[Relative_Hand_Position.Height.CHEST].Item1 + diff, symbolicPositions.height[Relative_Hand_Position.Height.CHEST].Item2);
//       symbolicPositions.height[Relative_Hand_Position.Height.ABDOMEN] = (symbolicPositions.height[Relative_Hand_Position.Height.ABDOMEN].Item1 + diff, symbolicPositions.height[Relative_Hand_Position.Height.ABDOMEN].Item2);
//       symbolicPositions.height[Relative_Hand_Position.Height.BELT] = (symbolicPositions.height[Relative_Hand_Position.Height.BELT].Item1 + diff, symbolicPositions.height[Relative_Hand_Position.Height.BELT].Item2);
//       symbolicPositions.height[Relative_Hand_Position.Height.BELOW_BELT] = (symbolicPositions.height[Relative_Hand_Position.Height.BELOW_BELT].Item1 + diff, symbolicPositions.height[Relative_Hand_Position.Height.BELOW_BELT].Item2);
//   }*/
