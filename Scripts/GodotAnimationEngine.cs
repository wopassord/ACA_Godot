using Godot;
using System;
using System.Xml;
using System.Collections.Generic;
using Animation; // Ensures we use the namespace of your original interface
using VirtualAgent;
using TextToSpeech;

namespace Animation
{

	public struct HeadDirectionPhase
	{
		public string type;
		public float lerpTime;
		public string from, to;
		public float amountFrom, amountTo;
	}
	public partial class GodotAnimationEngine : Node3D, IAnimationEngine
	{

		[Export] 
		public MeshInstance3D faceMesh; 
		[Export]
		public MeshInstance3D jawMesh;

		[ExportGroup("Body Bones")]
		[Export] public Skeleton3D agentSkeleton;
		[Export] public string headBoneName = "Head";
		[Export] public string leftEyeBoneName = "h_L_eye";
		[Export] public string rightEyeBoneName = "h_R_eye";
		private int headBoneIdx = -1;
		private int leftEyeBoneIdx = -1;
		private int rightEyeBoneIdx = -1;

		// Logic for automatic eye recognition and traslation of their coordinate systems
		private Node3D leftEyePivot;
		private Node3D rightEyePivot;

		
		// Dictionary Translated to native Godot classes
		private Dictionary<string, (Godot.Vector3 rot, Godot.Vector3 pos)> headDirections_;

		[Export]
		public AudioStreamPlayer3D audioPlayer;
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

		private Node3D GetTargetNode(string target)
		{
			Node node = GetTree().Root.FindChild(target, true, false);

			if (node is Node3D node3D)
				return node3D;

			return null;
		}
		public bool TargetFound(string target)
		{
			return GetTargetNode(target) != null;
		}

		public int GetTargetSide(string target, string mode)
		{
			Node3D obj = GetTargetNode(target);

			if (obj == null)
				return 0;

			int side = 0;

			Godot.Vector3 dist = obj.GlobalPosition - GlobalPosition;

			Godot.Vector3 right = GlobalTransform.Basis.X.Normalized();
			Godot.Vector3 forward = -GlobalTransform.Basis.Z.Normalized();

			float projectionOnRight = dist.Dot(right);       // - left, + right
			float projectionOnForward = dist.Dot(forward);   // - behind, + forward

			if (projectionOnForward < 0)
				return -1; // Object behind

			if (projectionOnRight >= 0)
			{
				if (mode == "LEFT" && projectionOnRight < 0.15f)
					side = 1; // LEFT
				else
					side = 2; // RIGHT
			}
			else
			{
				if (mode == "RIGHT" && projectionOnRight > -0.15f)
					side = 2; // RIGHT
				else
					side = 1; // LEFT
			}

			return side;
		}

		//Head Movement Methods

		private void InitHeadDirections()
		{
			headDirections_ = new Dictionary<string, (Godot.Vector3, Godot.Vector3)>();

		//Original head directions left to make evident the axis changes necessary when going from Unity to Godot
		// Unity: X points upwards, and Y points sideways | Godot: Y points upwards, and X points sideways 

			// headDirections_.Add("up", (new Godot.Vector3(0, 15, 0), new Godot.Vector3(0, 0, 0)));
			// headDirections_.Add("down", (new Godot.Vector3(0, -15, 0), new Godot.Vector3(0, 0, 0)));
			// headDirections_.Add("left", (new Godot.Vector3(15, 0, 0), new Godot.Vector3(0, 0, 0)));
			// headDirections_.Add("right", (new Godot.Vector3(-15, 0, 0), new Godot.Vector3(0, 0, 0)));

			headDirections_.Add("up", (new Godot.Vector3(15, 0, 0), Godot.Vector3.Zero));
			headDirections_.Add("down", (new Godot.Vector3(-15, 0, 0), Godot.Vector3.Zero));
			headDirections_.Add("left", (new Godot.Vector3(0, 15, 0), Godot.Vector3.Zero));
			headDirections_.Add("right", (new Godot.Vector3(0, -15, 0), Godot.Vector3.Zero));
			headDirections_.Add("tiltl", (new Godot.Vector3(0, 0, -10), new Godot.Vector3(0, 0, 0)));
			headDirections_.Add("tiltr", (new Godot.Vector3(0, 0, 10), new Godot.Vector3(0, 0, 0)));
			headDirections_.Add("forward", (new Godot.Vector3(0, 0, 0), new Godot.Vector3(0f, 0, -0.05f)));
			headDirections_.Add("backward", (new Godot.Vector3(0, 0, 0), new Godot.Vector3(0f, 0, 0.05f)));
			headDirections_.Add("none", (new Godot.Vector3(0, 0, 0), new Godot.Vector3(0, 0, 0)));
		}

		// Adapted version of original MoveHeadAction in Unity (radians instaead of º, pure quaternion logic)
		private Godot.Quaternion MoveHeadAction(float time)
		{
			Animation.HeadDirectionPhase action = agent_.fe.GetCurrentHeadAction(time);
			if (action.from == null || action.from.Equals("")) return Godot.Quaternion.Identity;

			float lerpTime = action.lerpTime;
			Godot.Vector3 rotation_from = headDirections_[action.from].rot * action.amountFrom;
			Godot.Vector3 rotation_to = headDirections_[action.to].rot * action.amountTo;

			float deg2Rad = Mathf.Pi / 180.0f;
			Godot.Quaternion qFrom = Godot.Quaternion.FromEuler(rotation_from * deg2Rad);
			Godot.Quaternion qTo = Godot.Quaternion.FromEuler(rotation_to * deg2Rad);

			return qFrom.Slerp(qTo, lerpTime);
		}
		
		private (Godot.Quaternion rot, Godot.Vector3 pos) MoveHeadDirection(float time)
		{
			List<Animation.HeadDirectionPhase> headDirections = null;
			if (!agent_.fe.GetCurrentHeadDirection(time, ref headDirections))
				return (Godot.Quaternion.Identity, Godot.Vector3.Zero);

			Godot.Quaternion finalRot = Godot.Quaternion.Identity;
			Godot.Vector3 finalPos = Godot.Vector3.Zero;
			float deg2Rad = Mathf.Pi / 180.0f;

			foreach (var hd in headDirections)
			{
				float lerpTime = hd.lerpTime;
				Godot.Vector3 fromPos = headDirections_[hd.from].pos * hd.amountFrom;
				Godot.Vector3 toPos = headDirections_[hd.to].pos * hd.amountTo;

				Godot.Vector3 rotation_from = headDirections_[hd.from].rot * hd.amountFrom;
				Godot.Vector3 rotation_to = headDirections_[hd.to].rot * hd.amountTo;

				Godot.Quaternion qFrom = Godot.Quaternion.FromEuler(rotation_from * deg2Rad);
				Godot.Quaternion qTo = Godot.Quaternion.FromEuler(rotation_to * deg2Rad);

				// Slerp directo entre los dos ángulos relativos
				Godot.Quaternion qStep = qFrom.Slerp(qTo, lerpTime);
				finalRot *= qStep;
				finalPos += fromPos.Lerp(toPos, lerpTime);
			}

			return (finalRot, finalPos);
		}

		

		private void SetupModularEyes()

		// Function implemented to solve the following issue:

		// FBX models in the character repository incorrect axes and origins (e.g. eye bones at (0,0,0)). Direct rotation of the eyes results impossible (windshield wiper effect)
		// Solution: define a BoneAttachment3D Node ("HeadSocket") attached to the head bone in the scene, with two "Marker3D" nodes (left and right eye sockets).
		// which were manually centered in the eyes. Programmatic rotation (in this case the Vestibulo-Ocular Reflex) is injected directly onto these during _Process() 
		{
			//Recuperates the defined sockets
			leftEyePivot = agentSkeleton.GetNodeOrNull<Node3D>("HeadSocket/LeftEyeSocket");
			rightEyePivot = agentSkeleton.GetNodeOrNull<Node3D>("HeadSocket/RightEyeSocket");

			if (leftEyePivot == null || rightEyePivot == null)
			{
				GD.PrintErr("Eye Sockets missing. Be sure to add the LeftEyeSocket and RightEyeSocket to the agent's HeadSocket.");
				return;
			}

			// 2. Escaneamos buscando las mallas originales para SECUESTRARLAS
			foreach (Node child in agentSkeleton.GetChildren())
			{
				if (child is MeshInstance3D meshInstance)
				{
					string nameLower = meshInstance.Name.ToString().ToLower();
					
					// Ojo Izquierdo
					if (nameLower.Contains("l_eye") || nameLower.Contains("left_eye"))
					{
						HijackEyeMesh(meshInstance, leftEyePivot);
					}
					// Ojo Derecho
					else if (nameLower.Contains("r_eye") || nameLower.Contains("right_eye"))
					{
						HijackEyeMesh(meshInstance, rightEyePivot);
					}
				}
			}		
			
		// 	// 2. Escaneamos buscando las mallas originales para DIAGNOSTICAR
		// foreach (Node child in agentSkeleton.GetChildren())
		// {
		// 	if (child is MeshInstance3D meshInstance)
		// 	{
		// 		string nameLower = meshInstance.Name.ToString().ToLower();
				
		// 		if (nameLower.Contains("l_eye") || nameLower.Contains("left_eye"))
		// 		{
		// 			RunSpatialDiagnostics(meshInstance, leftEyePivot);
		// 			// HijackEyeMesh(meshInstance, leftEyePivot); // <-- COMENTADO
		// 		}
		// 		else if (nameLower.Contains("r_eye") || nameLower.Contains("right_eye"))
		// 		{
		// 			RunSpatialDiagnostics(meshInstance, rightEyePivot);
		// 			// HijackEyeMesh(meshInstance, rightEyePivot); // <-- COMENTADO
		// 		}
		// 	}
		// }
		}

	private void HijackEyeMesh(MeshInstance3D originalEye, Node3D targetSocket)
	{
		// 1. Guardamos la textura/mesh original
		Mesh cachedMesh = originalEye.Mesh;

		// 2. Desvinculamos el esqueleto. La malla vuelve a su T-Pose en el origen (0,0,0)
		originalEye.Skin = null;
		originalEye.Skeleton = new NodePath("");

		// 3. LA EXTRACCIÓN MATEMÁTICA
		// Obtenemos la caja que envuelve a los vértices (el globo ocular físico)
		Aabb bounds = originalEye.GetAabb();
		// Calculamos el centro exacto de esa caja en el espacio local de la malla
		Godot.Vector3 geometricCenter = bounds.Position + (bounds.Size / 2.0f);

		// 4. Creamos el plato giratorio dentro del Socket (que ya está en la cabeza)
		Node3D turntable = new Node3D();
		turntable.Name = "TurntableFix";
		targetSocket.AddChild(turntable);

		// 5. Reparentamos
		originalEye.GetParent().RemoveChild(originalEye);
		turntable.AddChild(originalEye);

		// 6. EL ANCLAJE ABSOLUTO
		// Al restarle el centro geométrico, forzamos a que los vértices viajen desde su 
		// offset original y queden clavados exactamente en el centro del Turntable.
		originalEye.Position = -geometricCenter;
		originalEye.Rotation = Godot.Vector3.Zero; // Reseteamos rotaciones espurias

		// Restauramos la visualización
		originalEye.Mesh = cachedMesh;
		originalEye.Visible = true; 

		// 7. CORRECCIÓN DEL OJO INVERTIDO
		// Rotamos el plato 180 grados. Como el plato está ahora en el núcleo exacto 
		// de la malla, el ojo rotará sobre su propio eje sin desplazarse ni un milímetro.
		// turntable.Rotation = new Godot.Vector3(0, Mathf.Pi, 0); 
	}

	private void RunSpatialDiagnostics(MeshInstance3D originalEye, Node3D targetSocket)
	{
		GD.Print($"\n=== INICIO DIAGNÓSTICO ESPACIAL: {originalEye.Name} ===");

		// 1. Agente Raíz (Para verificar si el modelo importado tiene escalas raras)
		Node3D agentRoot = agentSkeleton.GetParent() as Node3D;
		PrintNodeData("Agent Root", agentRoot);

		// 2. Esqueleto
		PrintNodeData("Skeleton", agentSkeleton);

		// 3. Socket de Destino (El Marker3D que vos ubicaste)
		PrintNodeData("Target Socket", targetSocket);

		// 4. Malla Original (ESTADO CRUDO ANTES DEL SECUESTRO)
		PrintNodeData("Original Eye Mesh", originalEye);

		// 5. El ancla del hueso
		PrintNodeData("Head Bone Attachment", targetSocket.GetParent() as Node3D);

		GD.Print("==================================================\n");
	}

	private void PrintNodeData(string label, Node3D node)
	{
		if (node == null) 
		{ 
			GD.Print($"{label}: NULL"); 
			return; 
		}
		GD.Print($"--- {label} ({node.Name}) ---");
		GD.Print($"Global Pos: {node.GlobalPosition}");
		GD.Print($"Local Pos:  {node.Position}");
		GD.Print($"Global Rot: {node.GlobalRotationDegrees}");
		GD.Print($"Local Rot:  {node.RotationDegrees}");
		GD.Print($"Scale:      {node.Scale}");
	}


		public void PlayVoice(TextToSpeech.SpeechData sdata)
		{
		if (audioPlayer == null)
		{
			GD.Print("audioPlayer was null, creating it now...");
			audioPlayer = new AudioStreamPlayer3D();
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
			
			audioPlayer = new AudioStreamPlayer3D();
			AddChild(audioPlayer);
			
			float time = Time.GetTicksMsec() / 1000.0f;

			scheduler = new BmlSchedulerBehaviour(); //bml scheduler initialization
			scheduler.Init(time);

			string voicePath = ProjectSettings.GlobalizePath("res://Assets/StreamingAssets/cereproc/voices_48k");

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


			// Head Movement Logic

			InitHeadDirections();

			if (agentSkeleton != null)
			{
				SetupModularEyes();
			}
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

			//HEAD BONE POSITION UPDATING
			if (agentSkeleton != null)
			{
				// Cache the bone IDs only once for performance
				if (headBoneIdx == -1) headBoneIdx = agentSkeleton.FindBone(headBoneName);
				if (leftEyeBoneIdx == -1) leftEyeBoneIdx = agentSkeleton.FindBone(leftEyeBoneName);
				if (rightEyeBoneIdx == -1) rightEyeBoneIdx = agentSkeleton.FindBone(rightEyeBoneName);

				if (headBoneIdx != -1)
				{
					// 1. Retrieve the calculations from the logic engine
					var directionData = MoveHeadDirection(time);
					Godot.Quaternion actionRot = MoveHeadAction(time);

					// 2. Combine rotations for the skull
					Godot.Quaternion totalHeadRotation = directionData.rot * actionRot;

					// 3. Apply rotation to the head
					agentSkeleton.SetBonePoseRotation(headBoneIdx, totalHeadRotation);

					// 4. Vestibulo-Ocular Reflex (Socket System)
					// Calculamos el contrapeso exacto a la rotación de la cabeza
					Godot.Quaternion eyeCounterRotation = actionRot.Inverse();

					// Inyectamos la rotación pura directamente a nuestros Sockets modulares.
					// Al ser hijos físicos de HeadSocket, cancelarán exactamente el movimiento de la cabeza.
					if (leftEyePivot != null)
					{
						leftEyePivot.Quaternion = eyeCounterRotation;
					}
					if (rightEyePivot != null)
					{
						rightEyePivot.Quaternion = eyeCounterRotation;
					}
				}
			}
		}

		public override void _Input(InputEvent @event)
		{
			// Check that it is a key press and not the "echo" of holding the key down
			if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
			{
				// Generate a unique ID to prevent BML Scheduler crashes (cannot find start synchro)
				string uId = Time.GetTicksMsec().ToString();
				string testBml = "";

				if (keyEvent.Keycode == Key.C)
				{
					// TRIGGER C: Head rotation (Up and to the left)
					// We use 'headdirectionshift' to look away, and at 4.0 seconds we send another shift with "none" to return to rest.
					testBml = $@"
					<act>
						<bml id=""bml_{uId}"">
							<headdirectionshift id=""h_c1_{uId}"" lexeme=""up_left"" amount=""0.8"" start=""0.0""/>
							<headdirectionshift id=""h_c2_{uId}"" lexeme=""none"" start=""4.0""/>
						</bml>
					</act>";
					
					GD.Print("TEST C triggered: Head rotation (up_left) with auto-reset");
					agent_.AddBml(testBml, "Elisa");
				}
				else if (keyEvent.Keycode == Key.V)
				{
					// TRIGGER V: Big Smile (Blendshapes / FACS)
					// Added xmlns:ext to prevent XmlReader Exception: 'ext' is an undeclared prefix.
					testBml = $@"
					<act>
						<bml xmlns:ext=""urn:ext"" id=""bml_{uId}"">
							<face id=""f_v_{uId}"" amount=""1.0"" start=""0.0"" ready=""0.5"" relax=""4.5"" end=""5.0"">
								<ext:facs au=""12"" side=""BOTH"" amount=""1.0""/> <!-- Lip Corner Puller (Smile) -->
								<ext:facs au=""6"" side=""BOTH"" amount=""0.5""/>  <!-- Cheek Raiser (Cheekbone realism) -->
							</face>
						</bml>
					</act>";
					
					GD.Print("TEST V triggered: Big Smile (AU 12 and 6)");
					agent_.AddBml(testBml, "Elisa");
				}
				else if (keyEvent.Keycode == Key.B)
				{
					// TRIGGER B: Vestibulo-Ocular Reflex (Slow head movement, fixed gaze)
					// A slow 6-second 'shake' will cause the head to turn smoothly from side to side.
					testBml = $@"
					<act>
						<bml id=""bml_{uId}"">
							<head id=""h_b_{uId}"" lexeme=""shake"" amount=""0.6"" repetition=""2"" start=""0.0"" end=""6.0""/>
						</bml>
					</act>";
					
					GD.Print("TEST B triggered: Vestibulo-Ocular Reflex (Slow shake)");
					agent_.AddBml(testBml, "Elisa");
				}
				else if (keyEvent.Keycode == Key.N)
				{
					// TRIGGER N: Current combination (Speech + Head movements)
					testBml = $@"
					<act>
						<bml id=""bml_{uId}"">
							<speech id=""s_n_{uId}"" start=""0.0"">
								<description priority=""2"" type=""application/ssml+xml"">
									<speak>Bonjour! Mon nom est Elisa.</speak>
								</description>
							</speech> 
							<head id=""h_n1_{uId}"" lexeme=""tiltl"" amount=""0.5"" start=""0.0"" end=""1.5""/>
							<head id=""h_n2_{uId}"" lexeme=""nod"" amount=""0.7"" start=""2.0"" end=""3.5""/>
						</bml>
					</act>";
					
					GD.Print("TEST N triggered: Complete combination (Speech + Head)");
					agent_.AddBml(testBml, "Elisa");
				}
			}
		}

	}
}



/////////////// 
