using Godot;

namespace Animation
{
	public interface IAnimationEngine
	{

		// PHASE 1 METHODS
		void LoadMeshes(string faceMesh, string jawMesh, string tongueMesh, string leftEyeMesh, string rightEyeMesh, string bodyMesh);
		int GetFaceBlenshapeCount();
		int GetJawBlenshapeCount();
		void SetAgent(VirtualAgent.Character a);
		string GetAssetsPath();

		void LoadHandShapes(string filename);
		
		void PlayVoice(TextToSpeech.SpeechData sdata);
		void StopVoice();

		bool TargetFound(string target);
		int GetTargetSide(string target, string mode);



		// Methods to be ignored at the moment as to not break the contract

		// PHASE 2 AND 3 METHODS
		// void PlayMotionCapture(string name);
		// void StopMotionCapture(string name);
		// void ChangeStance(string name);



		// VirtualAgent.Vector3 FindJointPosition(string name);


		
	}
}
