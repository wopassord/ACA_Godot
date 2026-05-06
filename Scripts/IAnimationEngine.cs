using Godot;

namespace Animation
{
	public interface IAnimationEngine
	{
		// Only methods to use in the contract for the expressions transition
		void LoadMeshes(string faceMesh, string jawMesh, string tongueMesh, string leftEyeMesh, string rightEyeMesh, string bodyMesh);
		int GetFaceBlenshapeCount();
		int GetJawBlenshapeCount();

		// Methods to be ignore at the moment as to not break the contract
		// void SetAgent(VirtualAgent.Character a);
		// bool TargetFound(string target);
		// int GetTargetSide(string target, string mode);
		// void StopVoice();
		// void PlayVoice(TextToSpeech.SpeechData sdata);
		// void PlayMotionCapture(string name);
		// void StopMotionCapture(string name);
		// void ChangeStance(string name);
		// VirtualAgent.Vector3 FindJointPosition(string name);
		// string GetAssetsPath();
		// void LoadHandShapes(string filename);
	}
}
