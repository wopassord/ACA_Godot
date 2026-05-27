using Godot;
using System;
using System.Xml;
using System.Collections.Generic;
using Animation; // Ensures we use the namespace of your original interface
using VirtualAgent;
using TextToSpeech;

public partial class GodotAnimationEngine : Node3D, IAnimationEngine
{

	[Export] 
	public MeshInstance3D faceMesh; 
	[Export]
	public MeshInstance3D jawMesh;

	private AudioStreamPlayer audioPlayer;
	internal Character agent_;
	public BmlSchedulerBehaviour scheduler;

	public class HandShape
	{
		public Dictionary<string, Godot.Quaternion> fingers;
		public Godot.Vector3 size;
	}

	public Dictionary<string, HandShape> handTable = new Dictionary<string, HandShape>();

	// --- INTERFACE IMPLEMENTATION ---

	// Equivalent method to UnityAnimationEngine

	public void SetAgent(VirtualAgent.Character a)
	{
		agent_ = a;
	}

	// These are manually loaded from the Godot inspector at this point
	public void LoadMeshes(string face, string jaw, string tongue, string leftEye, string rightEye, string body)
	{
		GD.Print("Meshes loaded via Godot Inspector binding.");
	}

	public int GetFaceBlenshapeCount()
	{
		if (faceMesh != null && faceMesh.Mesh != null)
		{
			int count = faceMesh.GetBlendShapeCount();
			GD.Print("DEBUG CRÍTICO - FaceMesh tiene exactamente: " + count + " blendshapes.");
			
			// Parche temporal de supervivencia: Si Godot lee 0, forzamos 100 para que no crashee
			return count > 0 ? count : 100; 
		}
		return 100; 
	}

	public int GetJawBlenshapeCount()
	{
		if (jawMesh != null && jawMesh.Mesh != null)
		{
			int count = jawMesh.GetBlendShapeCount();
			GD.Print("DEBUG CRÍTICO - JawMesh tiene exactamente: " + count + " blendshapes.");
			
			return count > 0 ? count : 100;
		}
		return 100; 
	}

	public string GetAssetsPath() //required by Character.cs to build the path to XML files
	{
		return ProjectSettings.GlobalizePath("res://Assets/StreamingAssets");
	}

//usually the xml reading is done my LoadData. We assume the authors implemented this xml reading "LoadHandShapes" directly on the
//animation engine to make Character and LoadData "agnostic" about Vector and Quaternion logic, native to each animation engine.
	public void LoadHandShapes(string filename) 
		{
			System.Globalization.NumberStyles ns = System.Globalization.NumberStyles.Float;
			System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo("en-US");

			System.Xml.XmlTextReader reader = new System.Xml.XmlTextReader(filename);

			while (reader.Read())
			{
				string name = "";
				if (reader.NodeType == XmlNodeType.Element && reader.Name.ToUpper() == "HANDSHAPE")
				{
					HandShape hs = new HandShape();
					hs.fingers = new Dictionary<string, Godot.Quaternion>();
					//hand offset
					float size_x = 0, size_y = 0, size_z = 0;
					for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
					{
						reader.MoveToAttribute(attInd);
						if (reader.Name == "name")
							name = reader.Value.ToUpper();
						if (reader.Name == "size_x")
							try { float.TryParse(reader.Value, ns, ci, out size_x); }
							catch { }
						if (reader.Name == "size_y")
							try { float.TryParse(reader.Value, ns, ci, out size_y); }
							catch { }
						if (reader.Name == "size_z")
							try { float.TryParse(reader.Value, ns, ci, out size_z); }
							catch { }
					}
					hs.size = new Godot.Vector3(size_x, size_y, size_z);//MG

					while (reader.Read())
					{
						if (reader.NodeType == XmlNodeType.EndElement) break;
						if (reader.NodeType != XmlNodeType.Element) continue;
						string finger = reader.Name.ToUpper();
						float x = 0, y = 0, z = 0, w = 0;
						for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
						{
							reader.MoveToAttribute(attInd);
							if (reader.Name == "x")
								try { float.TryParse(reader.Value, ns, ci, out x); }
								catch { }
							if (reader.Name == "y")
								try { float.TryParse(reader.Value, ns, ci, out y); }
								catch { }
							if (reader.Name == "z")
								try { float.TryParse(reader.Value, ns, ci, out z); }
								catch { }
							if (reader.Name == "w")
								try { float.TryParse(reader.Value, ns, ci, out w); }
								catch { }
						}
						hs.fingers.Add(finger, new Godot.Quaternion(x, y, z, w));
					}

					HandShape handLeft = new HandShape();
					HandShape handRight = new HandShape();
					handLeft.fingers = new Dictionary<string, Godot.Quaternion>();
					handLeft.size = hs.size;
					handRight.fingers = new Dictionary<string, Godot.Quaternion>();
					handRight.size = hs.size;
					foreach (KeyValuePair<string, Godot.Quaternion> h in hs.fingers)
					{
						string start = h.Key.Substring(0, 2);
						if (start != "R_")
						{
							string finger = h.Key;
							if (h.Key[0] == 'L' && h.Key[1] == '_') finger = h.Key.Substring(2);
							handLeft.fingers.Add(finger, h.Value);
						}
						if (start != "L_")
						{
							string finger = h.Key;
							if (h.Key[0] == 'R' && h.Key[1] == '_') finger = h.Key.Substring(2);
							handRight.fingers.Add(finger, h.Value);
						}
					}
					handTable.Add('L' + name, handLeft);
					handTable.Add('R' + name, handRight);
				}
			}
			handTable.Add("LNONE", new HandShape());
			handTable.Add("RNONE", new HandShape());
			//for debug
			//printHandTable(handTable);
		}

	

	public void PlayVoice(TextToSpeech.SpeechData sdata)
{
	if (audioPlayer == null)
	{
		GD.Print("audioPlayer was null, creating it now...");
		audioPlayer = new AudioStreamPlayer();
		AddChild(audioPlayer);
	}

	if (sdata == null)
	{
		GD.PrintErr("SpeechData is null");
		return;
	}

	if (sdata.Audiobuf == null || sdata.Audiobuf.Length == 0)
	{
		GD.PrintErr("SpeechData audio buffer is empty");
		return;
	}

	GD.Print("Playing CereProc voice");

	byte[] audioBytes = new byte[sdata.Audiobuf.Length * 2];

	Buffer.BlockCopy(
		sdata.Audiobuf,
		0,
		audioBytes,
		0,
		audioBytes.Length
	);

	AudioStreamWav wav = new AudioStreamWav();
	wav.Data = audioBytes;
	wav.Format = AudioStreamWav.FormatEnum.Format16Bits;
	wav.MixRate = sdata.Audiorate;
	wav.Stereo = false;

	audioPlayer.Stream = wav;
	audioPlayer.Play();

	GD.Print("Audio rate: " + sdata.Audiorate);
	GD.Print("Samples: " + sdata.Audiobuf.Length);

	if (sdata.Phonemes != null)
		GD.Print("Phonemes: " + sdata.Phonemes.Length);
}

	public void StopVoice()
	{
		if (audioPlayer == null) return;
		audioPlayer.Stop();
		audioPlayer.Stream = null; 
	}

//Equivalent to Start() on Unity (called when Node is ready (both the node and its children entered the tree))
	public override void _Ready()
	{
		//PHASE 2 VERSION
		// // Dummy Character instantiation 
		// agent_ = new Character(this, GD.Print);  ///GD.print is Godot's logging system
		// new LoadData(agent_); //apparently LoadData only needs an "agent_" object to use the Log Channel :P

		// // Paths to XML files defined through Godot Insepct
		// string blendshapeLibPath = "res://Assets/StreamingAssets/agents/blendshapelibrary.xml";
		// string absoluteBlendshapePath = ProjectSettings.GlobalizePath(blendshapeLibPath);

		// LoadData.LoadBlendshapes(absoluteBlendshapePath, ref agent_.actionUnits);
		// GD.Print("SUCCESS: Action Units loaded into memory. Total count: " + agent_.actionUnits.Count);
		//PHASE 2 VERSION
		
		audioPlayer = new AudioStreamPlayer();
		AddChild(audioPlayer);
		
		float time = Time.GetTicksMsec() / 1000.0f;

		scheduler = new BmlSchedulerBehaviour(); //bml scheduler initialization
		scheduler.Init(time);

		string voicePath =
	ProjectSettings.GlobalizePath("res://cereproc/voices_48k");

TextToSpeech.Engine realTTS =
	new TextToSpeech.Engine(
		voicePath,
		msg => GD.Print(msg)
	);

VirtualAgent.ReactiveBehavior dummyRB =
	new VirtualAgent.ReactiveBehavior();

agent_ = new Character(
	"Elisa",
	this,
	realTTS,
	scheduler,
	dummyRB,
	null,
	GD.Print
);

		GD.Print("SUCCESS: BML Brain and Character initialized.");
	}

	// --- MAIN LOOP (MOCKING) ---
	
	// Equivalent to Unity's Update / LateUpdate.
	public override void _Process(double delta)
	{
		

		if (faceMesh == null || jawMesh == null) return;	// Catches unassigned mesh in editor
		float time = Time.GetTicksMsec() / 1000.0f;

		if (scheduler != null)
		{
			scheduler.Step(time);
		}

		agent_.fe.ResetBlendshapesDictionaries(); //vital for not accumulating expressions, we reset on each tick
		agent_.fe.GetCurrentFacialBlendshapes("speech", time);
		agent_.fe.GetCurrentFacialBlendshapes("face", time);

		// DATA INJECTION (Polling execution)

		// Iterate over the dictionary just like the original LateUpdate routine.
		foreach (KeyValuePair<int, float> bs in agent_.fe.face_blendshapes)
		{
			faceMesh.SetBlendShapeValue(bs.Key, bs.Value / 100.0f); //FaceEngine works with values in the 100's order
		}
		foreach (KeyValuePair<int, float> bs in agent_.fe.jaw_tongue_blendshapes)
		{
			jawMesh.SetBlendShapeValue(bs.Key, bs.Value / 100.0f);
		}
	}

	public override void _Input(InputEvent @event)
	{
		// Si presionamos la barra espaciadora...
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo && keyEvent.Keycode == Key.Space)
		{
			// 1. Définir le chemin et lire tout le fichier XML sous forme de chaîne de caractères (string)
			string bmlPath = ProjectSettings.GlobalizePath("res://Assets/StreamingAssets/xml/kate.xml");
			string rawBmlString = System.IO.File.ReadAllText(bmlPath);

			// 2. Injecter le BML directement dans le cerveau de l'agent.
			// Character.cs va automatiquement parser la balise <speech>, appeler Cereproc, 
			// récupérer les phonèmes et synchroniser le FaceEngine avec le BmlScheduler.
			agent_.AddBml(rawBmlString, "Elisa");

			GD.Print("BML kate.xml injecté dans le cerveau de l'agent !");
		}
	}

}


/////////////// 
