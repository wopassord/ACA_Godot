using System.Collections.Generic;
using Animation;

namespace VirtualAgent
{
    // We create a lightweight 'Character' class to satisfy FaceEngine's strict dependency.
    // This isolates the facial logic from the rest of the original Unity project.
    public class Character
    {
        // Adjust the List type (Blendshape or ActionUnit) based on your original LoadData definition
        public Dictionary<string, List<Blendshape>> actionUnits = new Dictionary<string, List<Blendshape>>();

        public delegate void Logger(string msg);
        private Logger logger_;
        
        // Interface connection to the Godot renderer
        public IAnimationEngine animationEngine;
        
        // The mathematical brain
        public FaceEngine fe;

        public Character(IAnimationEngine engine, Logger logger = null)
        {
            this.animationEngine = engine;
            this.logger_ = logger;
            
            // Initialize the FaceEngine, injecting this dummy class as the true agent
            this.fe = new FaceEngine(this);
        }

        public void Log(string msg)
        {
            if (logger_ != null)
            {
                logger_(msg);
            }
        }
    }
}