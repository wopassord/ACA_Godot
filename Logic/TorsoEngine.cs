//using UnityEngine;
using System.Collections.Generic;

public class TorsoKeyFrame
{
    public string name;
    public double time;
    public double amount;
    public VirtualAgent.Relative_Torso_Position rtp;
}

public class TorsoEngine
{ 
	internal VirtualAgent.Character agent_;

    public List<TorsoKeyFrame> leftTorsoKeyFrames, rightTorsoKeyFrames;

    public TorsoEngine(VirtualAgent.Character a) {
        agent_ = a;
        leftTorsoKeyFrames = new List<TorsoKeyFrame>();
        rightTorsoKeyFrames = new List<TorsoKeyFrame>();
    }
   

    public void PlayTorso(Bml.Scheduler scheduler, Bml.Event evt, VirtualAgent.TorsoShape torso, string lexeme, double amount, string target = "")
    {
        if (evt.Synchro == null || !evt.Synchro.Id.Equals("start")) return;

        float start = (float)evt.Signal.FindSynchro("start").Time;
        float ready = (float)evt.Signal.FindSynchro("ready").Time;
        float relax = (float)evt.Signal.FindSynchro("relax").Time;
        float end = (float)evt.Signal.FindSynchro("end").Time;

        if (evt.Signal.Shift)
        {
            //TODO: manage shifts
            if (torso.Equals("none"))
                ;// UnsetTorsoShift(start, start + 0.2f);
            else
                ;// SetTorsoShift(start, start + 0.2f, lexeme, (float)amount);
            return;
        }

        if (torso.side == "LEFT" || torso.side == "BOTH")
        {
            leftTorsoKeyFrames.Clear();
            AddTorsoKeyFrame(leftTorsoKeyFrames, "start", start, torso.leftTorso, amount);
            AddTorsoKeyFrame(leftTorsoKeyFrames, "ready", ready, torso.leftTorso, amount);
            AddTorsoKeyFrame(leftTorsoKeyFrames, "relax", relax, torso.leftTorso, amount);
            AddTorsoKeyFrame(leftTorsoKeyFrames, "end", end, torso.leftTorso, amount);
        }
        if (torso.side == "RIGHT" || torso.side == "BOTH")
        {
            rightTorsoKeyFrames.Clear();
            AddTorsoKeyFrame(rightTorsoKeyFrames, "start", start, torso.rightTorso, amount);
            AddTorsoKeyFrame(rightTorsoKeyFrames, "ready", ready, torso.rightTorso, amount);
            AddTorsoKeyFrame(rightTorsoKeyFrames, "relax", relax, torso.rightTorso, amount);
            AddTorsoKeyFrame(rightTorsoKeyFrames, "end", end, torso.rightTorso, amount);
        }
    }

    private void AddTorsoKeyFrame(List<TorsoKeyFrame> keyframes, string n, double t, VirtualAgent.Relative_Torso_Position rp, double a)
    {
        TorsoKeyFrame kf = new TorsoKeyFrame
        {
            name = n,
            time = t,
            amount = a,
            rtp = rp
        };
        keyframes.Insert(0, kf);
    }

    
}
