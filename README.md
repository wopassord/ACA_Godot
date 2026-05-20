/// # Elisa Virtual Agent - Godot 4 Port

## Overview
This project is a Godot 4 (C#) port of the Elisa Virtual Agent. It features a procedural animation system (`FaceEngine`) that mathematically calculates and interpolates facial expressions and lip-syncing in real-time. The engine reads XML databases for action units and blendshapes, applying them dynamically to 3D meshes without relying on pre-baked animation clips.

## Prerequisites
* **Godot Engine 4.x (.NET / C# Version)**: You MUST use the .NET-enabled version of Godot to run this project, as the core logic is written in C#.

## Quick Start (How to test the engine)
If you have never touched this project before, follow these steps to verify that the mathematical engine and the meshes are communicating correctly:

1. **Import the Project**: Open Godot, click on **Import**, and select the `project.godot` file located in this folder.
2. **Build the Code**: Before doing anything else, click the **Build button (the hammer icon)** in the top-right corner of the Godot editor. This will compile the C# solution.
3. **Open the Scene**: Open the main scene containing the `Elisa_Unity` node.
4. **Run**: Press **F6** (or click the "Play Scene" button) to start the simulation.
5. **Trigger the Action**: Once the window opens, **press the SPACEBAR**. You should see the agent smoothly perform the **"wi"** facial animation, proving that the procedural engine is successfully injecting data into the 3D mesh!

## Project Roadmap & Current Status

This migration project is divided into structured phases to safely port the logic from Unity to Godot.

*   **Phase 1: Foundation & Presentation (Completed)**
    *   Setup the C# environment in Godot.
    *   Implement the `IAnimationEngine` interface to bridge the logic and the rendering engine.
    *   Load and bind the 3D meshes (Face, Jaw, Body, etc.).
*   **Phase 2: Logical Integration (Completed)**
    *   Migrate the core mathematical brain (`FaceEngine`).
    *   Port the XML parsing system (`LoadData.cs`) to dynamically load Action Units and blendshape data.
    *   Synchronize the engine's update loop with Godot's `_Process` cycle to calculate real-time blendshape weights.
    *   *Current State: The face engine is fully autonomous and can process isolated expression commands.*
*   **Phase 3: BML Synchronization (In Progress)**
    *   Migrate the `BmlSchedulerBehaviour` to act as the agent's central nervous system.
    *   Parse Behavior Markup Language (BML) scripts.
    *   Implement Text-To-Speech (TTS) using raw PCM data injection via Godot's `AudioStreamPlayer3D`.
    *   Synchronize audio output exactly with generated facial visemes (lip-syncing).
*   **Phase 4: Body Mechanics & Locomotion (Planned)**
    *   Migrate the `GestureEngine`, `TorsoEngine`, and Head controllers.
    *   Translate Unity's Inverse Kinematics (IK) and look-at solvers to Godot's `SkeletonIK3D` and `Node3D` transforms.
    *   Implement procedural head movements, hand gestures, and body shifts synchronized via BML.