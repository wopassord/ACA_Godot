using Godot;
using System;
using System.Collections.Generic;
using Animation; // Ensures we use the namespace of your original interface
using VirtualAgent;

public partial class GodotAnimationEngine : Node3D, IAnimationEngine
{

	[Export] 
	public MeshInstance3D faceMesh; 
	private Character dummyAgent;

	// --- INTERFACE IMPLEMENTATION ---

	// Equivalent method to UnityAnimationEngine


	// These are manually loaded from the Godot inspector at this point
	public void LoadMeshes(string face, string jaw, string tongue, string leftEye, string rightEye, string body)
	{
		GD.Print("Meshes loaded via Godot Inspector binding.");
	}

	public int GetFaceBlenshapeCount()
	{
//////// TO DO: Implement Blendshapecount 
		return 65;
	}

	public int GetJawBlenshapeCount()
	{
//////// TO DO: Implement Blendshapecount 
		return 65; 
	}

	// --- MAIN LOOP (MOCKING) ---

	// Equivalent to Unity's Update / LateUpdate.
	public override void _Process(double delta)
	{
		dummyAgent = new Character(this, GD.Print);  ///we pass the GD.print to access Godot's logging system
		LoadData.agent = dummyAgent;

		// Paths to XML files defined through Godot Insepct
		string blendshapeLibPath = "res://Assets/Agents/blendshapelibrary.xml";
		string absoluteBlendshapePath = ProjectSettings.GlobalizePath(blendshapeLibPath);

		LoadData.LoadBlendshapes(absoluteBlendshapePath, ref dummyAgent.actionUnits);
		GD.Print("SUCCESS: Action Units loaded into memory. Total count: " + dummyAgent.actionUnits.Count);

		// Prevent runtime crashes if the mesh was not assigned in the Inspector.
		if (faceMesh == null) return; 

		// 1. CREATE THE MOCK: 
		// We simulate the FaceEngine output. For instance, blendshapes 27 and 46 
		Dictionary<int, float> mockFaceBlendshapes = new Dictionary<int, float>
		{
			{ 27, 0.9f },
			{ 46, 0.5f }  
		};


		// 2. DATA INJECTION (Polling execution)

		// Iterate over the dictionary just like the original LateUpdate routine.
		foreach (KeyValuePair<int, float> bs in mockFaceBlendshapes)
		{
			// SetBlendShapeValue equivalent of face.SetBlendShapeWeight.
			faceMesh.SetBlendShapeValue(bs.Key, bs.Value);
		}
	}


}


/////////////// 