//----------------------------------------------------------------------------
//using UnityEngine;
using System;
using System.Xml;
using System.Collections.Generic;

namespace VirtualAgent
{
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x=0, float y=0, float z=0)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }

    public struct Quaternion
    {
        public float x, y, z, w;

        public Quaternion(float x=0, float y=0, float z=0, float w=1)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public Quaternion Identity()
        {
            return new Quaternion { x=0, y=0, z=0, w=1 };
        }
    }

    public struct Blendshape
    {
        public enum Type { LIP, TONGUE, JAW };

        internal readonly Type type;
        internal readonly int code;
        internal readonly string side;
        internal readonly float weight;
        internal readonly float delayed_start;
        internal readonly float anticipated_end;

        public Blendshape(Type t, int c, string s, float w, float ds_, float ae_)
        {
            type = t;
            code = c;
            side = s;
            weight = w;
            delayed_start = ds_;
            anticipated_end = ae_;
        }
    }

    public struct ActionUnit
    {
        internal readonly string au;
        internal readonly string side;
        internal float amount;

        public ActionUnit(string a, string s, float am)
        {
            au = a; side = s; amount = am;
        }
    }

    public struct HeadShape
    {
        public string lexeme_;
        internal bool direction_;
        internal Vector3 max_rotation_, min_rotation_;
        internal Vector3 translation_;//MGtest
        internal double max_duration_, min_duration_;
        internal float max_repetition_, min_repetition_;
    }

    public class TorsoShape
    {
        public string side;
        public Relative_Torso_Position leftTorso;
        public Relative_Torso_Position rightTorso;
    }

    //Animation Engine takes care of loading hand shapes
    /*public class HandShape
    {
        public Dictionary<string, Quaternion> fingers;
        public Vector3 size;
    }*/

    public class Relative_Torso_Position
    {
        public enum Height { DOWN = -1, NORMAL = 0, UP = 1 };
        public enum Distance { BACKWARD = -1, NORMAL = 0, FORWARD = 1 };
        public enum RadialOrientation { INWARD = 1, NORMAL = 0, OUTWARD = -1 };

        public Height height;
        public float heightPercentage;
        public Distance distance;
        public float distancePercentage;
        public RadialOrientation radialOrientation;
        public float radialOrientationPercentage;
    }

    public class PostureDescription
    {
        public string side;
        public bool useThigh;
        public Quaternion initLeftWristRotation;
        public Quaternion initRightWristRotation;
        public Relative_Hand_Position leftHand;
        public Relative_Hand_Position rightHand;
    }

    public class MocapDescription
    {
        public bool posture;
        public string clip;
        public string side;
        public float duration;
        public float stroke;
        public float ready;
        public float stroke_start;
        public float stroke_end;
        public float relax;
    }

    /*****************************************************/
    /* Gesture relative description = First version code */
    /*****************************************************/

    public class GestureDescription
    {
        public bool shapeMandatory;
        public bool timeMandatory;
        public string side;
        public bool useThigh;
        public double duration;
        public List<Relative_Phase> phases;
    }

    public class Relative_Phase
    {
        public string name;
        public double time;
        public string side;
        public bool repeatable;
        public Quaternion initLeftWristRotation;
        public Quaternion initRightWristRotation;
        public Relative_Hand_Position leftHand;
        public Relative_Hand_Position rightHand;
    }

    public class Relative_Hand_Position
    {
        public enum Height { NONE = 0, ABOVE_HEAD = 1, HEAD = 2, SHOULDER = 3, CHEST = 4, ABDOMEN = 5, BELT = 6, BELOW_BELT = 7 };
        public enum Distance { NONE = 0, TOUCH = 1, CLOSE = 2, NORMAL = 3, FAR = 4 };
        public enum RadialOrientation { NONE = 0, INWARD = 1, FRONT = 2, SIDE = 3, OUT = 4, FAR_OUT = 5 };
        public enum ArmSwivel { NONE = 0, TOUCH = 1, NORMAL = 2, OUT = 3, ORTHOGONAL = 4 };

        public Height height;
        public float heightPercentage;
        public Distance distance;
        public float distancePercentage;
        public RadialOrientation radialOrientation;
        public float radialOrientationPercentage;
        public ArmSwivel armSwivel;
        public float armSwivelPercentage;

        public float wristX, wristY, wristZ;
        public Quaternion wristRotation;

        public float heightOffset;
        public float distanceOffset;
        public float radialOrientationOffset;
        public bool target;
        public string handShape;
    }
    /*****************************************************/


    public class LoadData
    {
        static private Character agent;
        public LoadData(Character a)
        {
            agent = a;
        }
        static readonly System.Globalization.NumberStyles ns = System.Globalization.NumberStyles.Float;
        static readonly System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo("en-US");
        static readonly System.Globalization.NumberStyles nsint = System.Globalization.NumberStyles.Integer;

        public static void LoadBlendshapes(string filename, ref Dictionary<string, List<Blendshape>> actionUnits)
        {
            System.Xml.XmlTextReader reader = new System.Xml.XmlTextReader(filename);

            while (reader.Read())
            {
                string name = "";
                if (reader.NodeType == XmlNodeType.Element && reader.Name.ToLower() == "action")
                {
                    name = reader.GetAttribute("name");
                    List<Blendshape> lau = new List<Blendshape>();
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.EndElement) break;
                        if (reader.NodeType != XmlNodeType.Element) continue;
                        if (reader.Name.ToLower() == "blendshape")
                        {
                            float ds = 0;
                            float ae = 0;
                            float weight = 0.7f;
                            int code = 0;
                            string side = "BOTH";
                            Blendshape.Type type = Blendshape.Type.LIP;
                            for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
                            {
                                reader.MoveToAttribute(attInd);
                                switch (reader.Name.ToLower())
                                {
                                    case "code":
                                        int.TryParse(reader.Value, nsint, ci, out code);
                                        break;
                                    case "weight":
                                        float.TryParse(reader.Value, ns, ci, out weight);
                                        weight /= 100.0f;
                                        break;
                                    case "delayed_start":
                                        float.TryParse(reader.Value, ns, ci, out ds);
                                        break;
                                    case "anticipated_end":
                                        float.TryParse(reader.Value, ns, ci, out ae);
                                        break;
                                    case "side":
                                        switch (reader.Value.ToUpper())
                                        {
                                            case "RIGHT": side = "RIGHT"; break;
                                            case "LEFT": side = "LEFT"; break;
                                        }
                                        break;
                                    case "type":
                                        switch (reader.Value.ToUpper())
                                        {
                                            //case "LIP": type = Blendshape.Type.LIP; break;
                                            case "TONGUE": type = Blendshape.Type.TONGUE; break;
                                            case "JAW": type = Blendshape.Type.JAW; break;
                                        }
                                        break;
                                }
                            }
                            Blendshape au = new Blendshape(type, code, side, weight, ds, ae);
                            lau.Add(au);
                        }
                    }
                    actionUnits.Add(name, lau);
                }
            }
            //for debug
            //printActionUnitsTable(actionUnits);
        }

        public static void LoadFacialExpressions(string filename, ref Dictionary<string, List<ActionUnit>> faceExpressions)
        {
            System.Xml.XmlTextReader reader = new System.Xml.XmlTextReader(filename);

            while (reader.Read())
            {
                string lexeme = "";
                if (reader.NodeType == XmlNodeType.Element && reader.Name.ToLower() == "expression")
                {
                    lexeme = reader.GetAttribute("lexeme").ToLower();
                    var lau = new List<ActionUnit>();
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.EndElement) break;
                        if (reader.NodeType != XmlNodeType.Element) continue;
                        if (reader.Name.ToLower() == "action")
                        {
                            string name = "";
                            string side = "BOTH";
                            for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
                            {
                                reader.MoveToAttribute(attInd);
                                if (reader.Name.ToLower() == "name")
                                    name = reader.Value.ToLower();
                                if (reader.Name.ToLower() == "side")
                                {
                                    switch (reader.Value.ToUpper())
                                    {
                                        case "RIGHT": side = "RIGHT"; break;
                                        case "LEFT": side = "LEFT"; break;
                                        default: break;
                                    }
                                }
                            }
                            lau.Add(new ActionUnit(name, side, 1.0f));
                        }
                    }
                    faceExpressions.Add(lexeme, lau);
                }
            }
            //for debug
            //printFaceExpressionsTable(faceExpressions);
        }

        public static void LoadTorsoShapes(string filename, ref Dictionary<string, TorsoShape> torsoShapes)
        {
            System.Xml.XmlTextReader reader = new System.Xml.XmlTextReader(filename);

            while (reader.Read())
            {
                string name = "";
                if (reader.NodeType == XmlNodeType.Element && reader.Name.ToLower() == "torso")
                {
                    TorsoShape ps = new TorsoShape();
                    ps.leftTorso = ps.rightTorso = null;

                    name = reader.GetAttribute("name").ToLower();
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.EndElement) break;
                        if (reader.NodeType != XmlNodeType.Element) continue;
                        if (reader.Name.ToLower() == "position")
                        {
                            string side = "";
                            Relative_Torso_Position rhp = new Relative_Torso_Position();
                            rhp.height = Relative_Torso_Position.Height.NORMAL;
                            rhp.distance = Relative_Torso_Position.Distance.NORMAL;
                            rhp.radialOrientation = Relative_Torso_Position.RadialOrientation.NORMAL;

                            //TODO : percentages are not in the library yet
                            rhp.radialOrientationPercentage = 0;
                            rhp.heightPercentage = 0;
                            rhp.distancePercentage = 0;

                            for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
                            {
                                reader.MoveToAttribute(attInd);
                                switch (reader.Name.ToLower())
                                {
                                    case "side":
                                        side = reader.Value.ToUpper(); break;
                                    case "height":
                                        switch (reader.Value.ToLower())
                                        {
                                            case "up": rhp.height = Relative_Torso_Position.Height.UP; break;
                                            case "down": rhp.height = Relative_Torso_Position.Height.DOWN; break;
                                            case "normal": rhp.height = Relative_Torso_Position.Height.NORMAL; break;
                                            default: //Debug.Log("Error in posture " + name + " description"); 
                                                break;
                                        }
                                        break;
                                    case "radialOrientation":
                                        switch (reader.Value.ToLower())
                                        {
                                            case "inward": rhp.radialOrientation = Relative_Torso_Position.RadialOrientation.INWARD; break;
                                            case "outward": rhp.radialOrientation = Relative_Torso_Position.RadialOrientation.OUTWARD; break;
                                            case "normal": rhp.radialOrientation = Relative_Torso_Position.RadialOrientation.NORMAL; break;
                                            default: //Debug.Log("Error in posture " + name + " description"); 
                                                break;
                                        }
                                        break;
                                    case "distance":
                                        switch (reader.Value.ToLower())
                                        {
                                            case "backward": rhp.distance = Relative_Torso_Position.Distance.BACKWARD; break;
                                            case "forward": rhp.distance = Relative_Torso_Position.Distance.FORWARD; break;
                                            case "normal": rhp.distance = Relative_Torso_Position.Distance.NORMAL; break;
                                            default: agent.Log("Error in posture " + name + " description"); 
                                                break;
                                        }
                                        break;
                                    default: break;
                                }
                            }
                            if (side == "LEFT" || side == "BOTH")
                                ps.leftTorso = rhp;
                            if (side == "RIGHT" || side == "BOTH")
                                ps.rightTorso = rhp;
                        }
                    }
                    if (ps.leftTorso != null && ps.rightTorso != null)
                        ps.side = "BOTH";
                    else
                    {
                        if (ps.leftTorso != null)
                            ps.side = "LEFT";
                        else
                            ps.side = "RIGHT";
                    }
                    torsoShapes.Add(name, ps);
                }
            }
            //for debug
            //printTorsoTable(torsoShapes);
        }

        public static void LoadHeadShapes(string filename, ref Dictionary<string, HeadShape> headShapes)
        {
            System.Xml.XmlTextReader reader = new System.Xml.XmlTextReader(filename);

            while (reader.Read())
            {
                string lexeme = "";
                bool direction = true;
                if (reader.NodeType == XmlNodeType.Element && reader.Name.ToLower() == "head")
                {
                    HeadShape hs = new HeadShape();
                    lexeme = reader.GetAttribute("lexeme").ToLower();
                    direction = reader.GetAttribute("direction").ToLower().Equals("true");
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.EndElement) break;
                        if (reader.NodeType != XmlNodeType.Element) continue;
                        switch (reader.Name.ToLower())
                        {
                            case "rotation":
                                float maxx = 0, minx = 0, maxy = 0, miny = 0, maxz = 0, minz = 0;
                                for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
                                {
                                    reader.MoveToAttribute(attInd);
                                    switch (reader.Name.ToLower())
                                    {
                                        case "minx":
                                            float.TryParse(reader.Value, ns, ci, out minx); break;
                                        case "maxx":
                                            float.TryParse(reader.Value, ns, ci, out maxx); break;
                                        case "miny":
                                            float.TryParse(reader.Value, ns, ci, out miny); break;
                                        case "maxy":
                                            float.TryParse(reader.Value, ns, ci, out maxy); break;
                                        case "minz":
                                            float.TryParse(reader.Value, ns, ci, out minz); break;
                                        case "maxz":
                                            float.TryParse(reader.Value, ns, ci, out maxz); break;
                                        default: break;
                                    }
                                }
                                //Debug.Log(reader.Name.ToLower());
                                hs.min_rotation_ = new Vector3(minx, miny, minz);
                                hs.max_rotation_ = new Vector3(maxx, maxy, maxz);
                                break;
                            case "translation"://MGtest
                                float tx = 0, ty = 0, tz = 0;
                                for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
                                {
                                    reader.MoveToAttribute(attInd);
                                    switch (reader.Name.ToLower())
                                    {
                                        case "x":
                                            float.TryParse(reader.Value, ns, ci, out tx); break;
                                        case "y":
                                            float.TryParse(reader.Value, ns, ci, out ty); break;
                                        case "z":
                                            float.TryParse(reader.Value, ns, ci, out tz); break;
                                        default: break;
                                    }
                                }
                                hs.translation_= new Vector3(tx, ty, tz);
                                break;
                            case "duration":
                                float maxd = 0, mind = 0;
                                for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
                                {
                                    reader.MoveToAttribute(attInd);
                                    switch (reader.Name.ToLower())
                                    {
                                        case "min":
                                            float.TryParse(reader.Value, ns, ci, out mind); break;
                                        case "max":
                                            float.TryParse(reader.Value, ns, ci, out maxd); break;
                                        default: break;
                                    }
                                }
                                hs.min_duration_ = mind;
                                hs.max_duration_ = maxd;
                                break;
                            case "repetition":
                                float maxr = 0, minr = 0;
                                for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
                                {
                                    reader.MoveToAttribute(attInd);
                                    switch (reader.Name.ToLower())
                                    {
                                        case "min":
                                            float.TryParse(reader.Value, ns, ci, out minr); break;
                                        case "max":
                                            float.TryParse(reader.Value, ns, ci, out maxr); break;
                                        default: break;
                                    }
                                }
                                hs.min_repetition_ = minr;
                                hs.max_repetition_ = maxr;
                                break;
                            default: break;
                        }
                    }
                    hs.direction_ = direction;
                    hs.lexeme_ = lexeme;
                    headShapes.Add(lexeme, hs);
                }
            }
            //for debug
            //printHeadMovementsTable(headShapes);
        }

        public static void LoadMocaps(string filename, ref Dictionary<string, MocapDescription> mocapsTable)
        {
            System.Xml.XmlTextReader reader = new System.Xml.XmlTextReader(filename);

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "mocap")
                {
                    string lexeme = "";
                    MocapDescription md = new MocapDescription();
                    md.stroke = -1; md.ready = -1; md.stroke_start = -1; md.stroke_end = -1; md.relax = -1;
                    md.posture = false;
                    for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
                    {
                        reader.MoveToAttribute(attInd);
                        if (reader.Name.ToLower() == "lexeme")
                            lexeme = reader.Value.ToLower();
                        if (reader.Name.ToLower() == "clipname")
                            md.clip = reader.Value.ToLower();
                        if (reader.Name.ToLower() == "side")
                            md.side = reader.Value.ToUpper();
                        if (reader.Name.ToLower() == "posture")
                        {
                            if (reader.Value.ToLower() != "false")
                                md.posture = true;
                        }
                        if (reader.Name.ToLower() == "duration")
                            float.TryParse(reader.Value, ns, ci, out md.duration);
                        if (reader.Name.ToLower() == "stroke")
                            float.TryParse(reader.Value, ns, ci, out md.stroke);
                        if (reader.Name.ToLower() == "ready")
                            float.TryParse(reader.Value, ns, ci, out md.ready);
                        if (reader.Name.ToLower() == "stroke_start")
                            float.TryParse(reader.Value, ns, ci, out md.stroke_start);
                        if (reader.Name.ToLower() == "stroke_end")
                            float.TryParse(reader.Value, ns, ci, out md.stroke_end);
                        if (reader.Name.ToLower() == "relax")
                            float.TryParse(reader.Value, ns, ci, out md.relax);

                    }
                    if (md.stroke == -1 && md.posture == false)
                        agent.Log("Error in mocap description " + lexeme + " a mocap gesture must have a stroke time");
                    else
                        mocapsTable.Add(lexeme, md);
                }
            }
            //for Debug
            //printMocapsTable(mocapsTable);
        }

        /*******************************************************************/
        /* Load gesture and hand relative description = First version code */
        /*******************************************************************/

        public static void LoadGestures(string filename, ref Dictionary<string, GestureDescription> gestureTable)
        {
            System.Xml.XmlTextReader reader = new System.Xml.XmlTextReader(filename);

            while (reader.Read())
            {
                string name = "";
                bool error = false;
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "gesture")
                {
                    GestureDescription gd = new GestureDescription();
                    Quaternion initLeftWirstOrientation = new Quaternion();
                    Quaternion initRightWirstOrientation = new Quaternion();
                    gd.shapeMandatory = false;
                    gd.timeMandatory = false;
                    gd.useThigh = false;
                    gd.duration = -1f;
                    for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
                    {
                        reader.MoveToAttribute(attInd);
                        if (reader.Name.ToUpper() == "LEXEME")
                            name = reader.Value;
                        if (reader.Name.ToUpper() == "SIDE")
                            gd.side = reader.Value;
                        if (reader.Name.ToUpper() == "TIMEMANDATORY")
                        {
                            if (reader.Value.ToUpper() != "FALSE")
                                gd.timeMandatory = true;
                        }
                        if (reader.Name.ToUpper() == "SHAPEMANDATORY")
                        {
                            if (reader.Value.ToUpper() != "FALSE")
                                gd.shapeMandatory = true;
                        }
                        if (reader.Name.ToUpper() == "USETHIGH")
                        {
                            if (reader.Value.ToUpper() != "FALSE")
                                gd.useThigh = true;
                        }
                        if (reader.Name.ToUpper() == "DURATION")
                            double.TryParse(reader.Value, ns, ci, out gd.duration);
                    }

                    gd.phases = new List<Relative_Phase>();

                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.EndElement) break;
                        if (reader.NodeType != XmlNodeType.Element) continue;
                        if (reader.Name == "initLeftWirstOrientation")
                        {
                            float x = 0.0f, y = 0.0f, z = 0.0f, w = 1.0f;
                            for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
                            {
                                reader.MoveToAttribute(attInd);
                                if (reader.Name == "x") float.TryParse(reader.Value, ns, ci, out x);
                                if (reader.Name == "y") float.TryParse(reader.Value, ns, ci, out y);
                                if (reader.Name == "z") float.TryParse(reader.Value, ns, ci, out z);
                                if (reader.Name == "w") float.TryParse(reader.Value, ns, ci, out w);
                            }
                            initLeftWirstOrientation = new Quaternion(x, y, z, w);
                        }
                        if (reader.Name == "initRightWirstOrientation")
                        {
                            float x = 0.0f, y = 0.0f, z = 0.0f, w = 1.0f;
                            for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
                            {
                                reader.MoveToAttribute(attInd);
                                if (reader.Name == "x") float.TryParse(reader.Value, ns, ci, out x);
                                if (reader.Name == "y") float.TryParse(reader.Value, ns, ci, out y);
                                if (reader.Name == "z") float.TryParse(reader.Value, ns, ci, out z);
                                if (reader.Name == "w") float.TryParse(reader.Value, ns, ci, out w);
                            }
                            initRightWirstOrientation = new Quaternion(x, y, z, w);
                        }
                        if (reader.Name == "phase")
                        {
                            Relative_Phase p = new Relative_Phase();
                            p.leftHand = null;
                            p.rightHand = null;
                            p.time = -1;
                            p.repeatable = false;
                            p.side = "LEFT";

                            for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
                            {
                                reader.MoveToAttribute(attInd);
                                if (reader.Name.ToLower() == "type")
                                {
                                    p.name = reader.Value.ToLower();
                                    if (!(p.name == "start" || p.name == "ready" || p.name == "stroke_start" || p.name == "stroke" || p.name == "stroke_end" || p.name == "relax" || p.name == "end"))
                                    {
                                        agent.Log("Error in gesturelibrary file, in gesture: " + name +
                                                      " a phase type is not correctly defined.");
                                        error = true;
                                    }
                                    switch (reader.Value.ToUpper())
                                    {
                                        case "START": p.name = "start"; break;
                                        case "READY": p.name = "ready"; break;
                                        case "STROKE_START": p.name = "stroke_start"; break;
                                        case "STROKE": p.name = "stroke"; break;
                                        case "STROKE_END": p.name = "stroke_end"; break;
                                        case "RELAX": p.name = "relax"; break;
                                        case "END": p.name = "end"; break;
                                        default:
                                            agent.Log("Error in gesturelibrary file, in gesture: " + name +
                                                      " a phase type is not correctly defined.");
                                            error = true;
                                            break;
                                    }
                                }
                                if (reader.Name.ToLower() == "time")
                                    double.TryParse(reader.Value, ns, ci, out p.time);
                                if (reader.Name.ToLower() == "repeatable")
                                    if (reader.Value.ToLower() == "true")
                                        p.repeatable = true;
                            }

                            while (reader.Read())
                            {
                                if (reader.NodeType == XmlNodeType.EndElement) break;
                                if (reader.NodeType != XmlNodeType.Element) continue;
                                if (reader.Name.ToLower() == "hand")
                                {
                                    bool target = false;

                                    for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
                                    {
                                        reader.MoveToAttribute(attInd);
                                        if (reader.Name.ToLower() == "side")
                                            p.side = reader.Value.ToUpper();
                                        if (reader.Name.ToLower() == "target")
                                            if (reader.Value.ToLower() == "true")
                                                target = true;
                                    }

                                    Relative_Hand_Position.Height height = Relative_Hand_Position.Height.ABDOMEN;
                                    float heightPercentage = 0;
                                    Relative_Hand_Position.Distance distance = Relative_Hand_Position.Distance.NORMAL;
                                    float distancePercentage = 0;
                                    Relative_Hand_Position.RadialOrientation radial = Relative_Hand_Position.RadialOrientation.SIDE;
                                    float radialPercentage = 0;
                                    Relative_Hand_Position.ArmSwivel armswivel = Relative_Hand_Position.ArmSwivel.NORMAL;
                                    float armswivelPercentage = 0;

                                    float heightOffset = 0;
                                    float distanceOffset = 0;
                                    float radialOrientationOffset = 0;

                                    float x = 0, y = 0, z = 0, w = 1;

                                    string handShape = "NONE";

                                    while (reader.Read())
                                    {
                                        if (reader.NodeType == XmlNodeType.EndElement) break;
                                        if (reader.NodeType != XmlNodeType.Element) continue;

                                        if (reader.Name.ToLower() == "height")
                                        {
                                            for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
                                            {
                                                reader.MoveToAttribute(attInd);
                                                if (reader.Name.ToLower() == "value")
                                                {
                                                    switch (reader.Value.ToUpper())
                                                    {
                                                        case "ABOVE_HEAD": height = Relative_Hand_Position.Height.ABOVE_HEAD; break;
                                                        case "HEAD": height = Relative_Hand_Position.Height.HEAD; break;
                                                        case "SHOULDER": height = Relative_Hand_Position.Height.SHOULDER; break;
                                                        case "CHEST": height = Relative_Hand_Position.Height.CHEST; break;
                                                        case "ABDOMEN": height = Relative_Hand_Position.Height.ABDOMEN; break;
                                                        case "BELT": height = Relative_Hand_Position.Height.BELT; break;
                                                        case "BELOW_BELT": height = Relative_Hand_Position.Height.BELOW_BELT; break;
                                                        default:
                                                            agent.Log("Error in gesture library file in gesture: " + name +
                                                                        " Height value is wrong. Default value is assigned");
                                                            break;
                                                    }
                                                }
                                                if (reader.Name.ToLower() == "percentage")
                                                    float.TryParse(reader.Value, ns, ci, out heightPercentage);
                                                if (reader.Name.ToLower() == "targetoffset")
                                                    float.TryParse(reader.Value, ns, ci, out heightOffset);
                                            }

                                        }
                                        if (reader.Name.ToLower() == "distance")
                                        {
                                            for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
                                            {
                                                reader.MoveToAttribute(attInd);
                                                if (reader.Name == "value")
                                                {
                                                    switch (reader.Value.ToUpper())
                                                    {
                                                        case "FAR": distance = Relative_Hand_Position.Distance.FAR; break;
                                                        case "NORMAL": distance = Relative_Hand_Position.Distance.NORMAL; break;
                                                        case "CLOSE": distance = Relative_Hand_Position.Distance.CLOSE; break;
                                                        case "TOUCH": distance = Relative_Hand_Position.Distance.TOUCH; break;
                                                        default:
                                                            agent.Log("Error in gesturelibrary file, in gesture: " + name +
                                                                      " Distance value is wrong. Default value is assigned");
                                                            break;
                                                    }
                                                }
                                                if (reader.Name.ToLower() == "percentage")
                                                    float.TryParse(reader.Value, ns, ci, out distancePercentage);
                                                if (reader.Name.ToLower() == "targetoffset")
                                                    float.TryParse(reader.Value, ns, ci, out distanceOffset);
                                            }
                                        }
                                        if (reader.Name.ToLower() == "radialorientation")
                                        {
                                            for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
                                            {
                                                reader.MoveToAttribute(attInd);
                                                if (reader.Name == "value")
                                                {
                                                    switch (reader.Value.ToUpper())
                                                    {
                                                        case "FAR_OUT": radial = Relative_Hand_Position.RadialOrientation.FAR_OUT; break;
                                                        case "OUT": radial = Relative_Hand_Position.RadialOrientation.OUT; break;
                                                        case "SIDE": radial = Relative_Hand_Position.RadialOrientation.SIDE; break;
                                                        case "FRONT": radial = Relative_Hand_Position.RadialOrientation.FRONT; break;
                                                        case "INWARD": radial = Relative_Hand_Position.RadialOrientation.INWARD; break;
                                                        default:
                                                            agent.Log("Error in gesturelibrary file, in gesture: " + name +
                                                                      " Radial Orientation value is wrong. Default value is assigned");
                                                            break;
                                                    }
                                                }
                                                if (reader.Name.ToLower() == "percentage")
                                                    float.TryParse(reader.Value, ns, ci, out radialPercentage);
                                                if (reader.Name.ToLower() == "targetoffset")
                                                    float.TryParse(reader.Value, ns, ci, out radialOrientationOffset);
                                            }
                                        }
                                        if (reader.Name.ToLower() == "armswivel")
                                        {
                                            for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
                                            {
                                                reader.MoveToAttribute(attInd);
                                                if (reader.Name.ToLower() == "value")
                                                {
                                                    switch (reader.Value.ToUpper())
                                                    {
                                                        case "ORTHOGONAL": armswivel = Relative_Hand_Position.ArmSwivel.ORTHOGONAL; break;
                                                        case "OUT": armswivel = Relative_Hand_Position.ArmSwivel.OUT; break;
                                                        case "NORMAL": armswivel = Relative_Hand_Position.ArmSwivel.NORMAL; break;
                                                        case "TOUCH": armswivel = Relative_Hand_Position.ArmSwivel.TOUCH; break;
                                                        default:
                                                            error = true;
                                                            agent.Log("Error in gesturelibrary file, in gesture: " + name +
                                                                      " Arm Swivel value is wrong. Default value is assigned");
                                                            break;
                                                    }
                                                }
                                                if (reader.Name.ToLower() == "percentage")
                                                    float.TryParse(reader.Value, ns, ci, out armswivelPercentage);
                                            }
                                        }
                                        
                                        if (reader.Name.ToLower() == "wristrotation")
                                        {
                                            for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
                                            {
                                                reader.MoveToAttribute(attInd);

                                                if (reader.Name.ToLower() == "x")
                                                    float.TryParse(reader.Value, ns, ci, out x);

                                                if (reader.Name.ToLower() == "y")
                                                    float.TryParse(reader.Value, ns, ci, out y);

                                                if (reader.Name.ToLower() == "z")
                                                    float.TryParse(reader.Value, ns, ci, out z);

                                                if (reader.Name.ToLower() == "w")
                                                    float.TryParse(reader.Value, ns, ci, out w);
                                            }
                                        }

                                        if (reader.Name.ToLower() == "handshape")
                                        {
                                            handShape = reader.GetAttribute("shape").ToUpper();
                                        }
                                    }

                                    if (p.side == "LEFT" || p.side == "BOTH")
                                    {
                                        p.leftHand = new Relative_Hand_Position();
                                        p.leftHand.height = height;
                                        p.leftHand.heightPercentage = heightPercentage;
                                        p.leftHand.distance = distance;
                                        p.leftHand.distancePercentage = distancePercentage;
                                        p.leftHand.radialOrientation = radial;
                                        p.leftHand.radialOrientationPercentage = radialPercentage;
                                        p.leftHand.armSwivel = armswivel;
                                        p.leftHand.armSwivelPercentage = armswivelPercentage;
                                        p.leftHand.wristRotation = new Quaternion(x, y, z, w);

                                        p.leftHand.heightOffset = heightOffset;
                                        p.leftHand.distanceOffset = distanceOffset;
                                        p.leftHand.radialOrientationOffset = radialOrientationOffset;
                                        p.leftHand.target = target;
                                        p.leftHand.handShape = handShape;

                                        p.initLeftWristRotation = initLeftWirstOrientation;
                                    }

                                    if (p.side == "RIGHT" || p.side == "BOTH")
                                    {
                                        p.rightHand = new Relative_Hand_Position();
                                        p.rightHand.height = height;
                                        p.rightHand.heightPercentage = heightPercentage;
                                        p.rightHand.distance = distance;
                                        p.rightHand.distancePercentage = distancePercentage;
                                        p.rightHand.radialOrientation = radial;
                                        p.rightHand.radialOrientationPercentage = radialPercentage;
                                        p.rightHand.armSwivel = armswivel;
                                        p.rightHand.armSwivelPercentage = armswivelPercentage;
                                        p.rightHand.wristRotation = new Quaternion(x, y, z, w);

                                        p.rightHand.heightOffset = heightOffset;
                                        p.rightHand.distanceOffset = distanceOffset;
                                        p.rightHand.radialOrientationOffset = radialOrientationOffset;
                                        p.rightHand.target = target;
                                        p.rightHand.handShape = handShape;

                                        p.initRightWristRotation = initRightWirstOrientation;
                                    }
                                }
                            }
                            gd.phases.Add(p);
                        }

                        //if a gesture uses both hands then its shape is mandatory by default
                        //if (gd.side == "BOTH" || gd.side == "LEFT_RIGHT")
                        //  gd.shapeMandatory = true;
                    }
                    if (error == false)
                    {
                        gd.phases.Sort(delegate (Relative_Phase p1, Relative_Phase p2)
                        {
                            return p1.time.CompareTo(p2.time);
                        });
                        gestureTable.Add(name, gd);
                    }
                }
            }
            //for debug
            //printGestureTable(gestureTable);
        }

        //Animation Engine takes care of loading hand shapes
        /*public static void LoadHandShapes(string filename, ref Dictionary<string, HandShape> handTable)
        {
            System.Xml.XmlTextReader reader = new System.Xml.XmlTextReader(filename);

            while (reader.Read())
            {
                string name = "";
                if (reader.NodeType == XmlNodeType.Element && reader.Name.ToUpper() == "HANDSHAPE")
                {
                    HandShape hs = new HandShape();
                    //hs.fingers = new Dictionary<string, Vector3>();
                    hs.fingers = new Dictionary<string, Quaternion>();
                    //EB hand offset
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
                            if (reader.Name == "w")//MG
                                try { float.TryParse(reader.Value, ns, ci, out w); }
                                catch { }
                        }
                        hs.fingers.Add(finger, new Quaternion(x, y, z, w));//MG
                                                                           //EB hand offset
                        hs.size = new Vector3(size_x, size_y, size_z);
                        // hs.fingers.Add(finger, Quaternion.Euler(x, y, z));
                    }
                    handTable.Add(name, hs);
                }
            }
            //for debug
            //printHandTable(handTable);
        }*/

        public static void LoadPostures(string filename, ref Dictionary<string, PostureDescription> postureTable)
        {
            System.Xml.XmlTextReader reader = new System.Xml.XmlTextReader(filename);

            while (reader.Read())
            {
                string name = "";
                bool error = false;
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "posture")
                {
                    PostureDescription gd = new PostureDescription();
                    Quaternion initLeftWirstOrientation = new Quaternion();
                    Quaternion initRightWirstOrientation = new Quaternion();
                    gd.useThigh = false;
                    for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
                    {
                        reader.MoveToAttribute(attInd);
                        if (reader.Name.ToLower() == "lexeme")
                            name = reader.Value.ToLower();
                        if (reader.Name.ToLower() == "side")
                            gd.side = reader.Value;
                        if (reader.Name.ToLower() == "usethigh")
                        {
                            if (reader.Value.ToLower() != "false")
                                gd.useThigh = true;
                        }
                    }

                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.EndElement) break;
                        if (reader.NodeType != XmlNodeType.Element) continue;

                        if (reader.Name == "initLeftWirstOrientation")
                        {
                            float x = 0.0f, y = 0.0f, z = 0.0f, w = 0.0f;
                            for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
                            {
                                reader.MoveToAttribute(attInd);
                                if (reader.Name == "x") float.TryParse(reader.Value, ns, ci, out x);
                                if (reader.Name == "y") float.TryParse(reader.Value, ns, ci, out y);
                                if (reader.Name == "z") float.TryParse(reader.Value, ns, ci, out z);
                                if (reader.Name == "w") float.TryParse(reader.Value, ns, ci, out w);
                            }
                            initLeftWirstOrientation = new Quaternion(x, y, z, w);
                        }
                        if (reader.Name == "initRightWirstOrientation")
                        {
                            float x = 0.0f, y = 0.0f, z = 0.0f, w = 0.0f;
                            for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
                            {
                                reader.MoveToAttribute(attInd);
                                if (reader.Name == "x") float.TryParse(reader.Value, ns, ci, out x);
                                if (reader.Name == "y") float.TryParse(reader.Value, ns, ci, out y);
                                if (reader.Name == "z") float.TryParse(reader.Value, ns, ci, out z);
                                if (reader.Name == "w") float.TryParse(reader.Value, ns, ci, out w);
                            }
                            initRightWirstOrientation = new Quaternion(x, y, z, w);
                        }
                        if (reader.Name.ToLower() == "hand")
                        {
                            string side = reader.GetAttribute("side").ToUpper();
                            Relative_Hand_Position.Height height = Relative_Hand_Position.Height.ABDOMEN;
                            float heightPercentage = 0;
                            Relative_Hand_Position.Distance distance = Relative_Hand_Position.Distance.NORMAL;
                            float distancePercentage = 0;
                            Relative_Hand_Position.RadialOrientation radial = Relative_Hand_Position.RadialOrientation.SIDE;
                            float radialPercentage = 0;
                            Relative_Hand_Position.ArmSwivel armswivel = Relative_Hand_Position.ArmSwivel.NORMAL;
                            float armswivelPercentage = 0;

                            float x = 0, y = 0, z = 0, w = 1;

                            string handShape = "NONE";

                            while (reader.Read())
                            {
                                if (reader.NodeType == XmlNodeType.EndElement) break;
                                if (reader.NodeType != XmlNodeType.Element) continue;

                                if (reader.Name.ToLower() == "height")
                                {
                                    for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
                                    {
                                        reader.MoveToAttribute(attInd);
                                        if (reader.Name.ToLower() == "value")
                                        {
                                            switch (reader.Value.ToUpper())
                                            {
                                                case "ABOVE_HEAD": height = Relative_Hand_Position.Height.ABOVE_HEAD; break;
                                                case "HEAD": height = Relative_Hand_Position.Height.HEAD; break;
                                                case "SHOULDER": height = Relative_Hand_Position.Height.SHOULDER; break;
                                                case "CHEST": height = Relative_Hand_Position.Height.CHEST; break;
                                                case "ABDOMEN": height = Relative_Hand_Position.Height.ABDOMEN; break;
                                                case "BELT": height = Relative_Hand_Position.Height.BELT; break;
                                                case "BELOW_BELT": height = Relative_Hand_Position.Height.BELOW_BELT; break;
                                                default:
                                                    error = true;
                                                    agent.Log("Error in gesture library file in gesture: " + name +
                                                                " Height value is wrong. Default value is assigned");
                                                    break;
                                            }
                                        }
                                        if (reader.Name.ToLower() == "percentage")
                                            float.TryParse(reader.Value, ns, ci, out heightPercentage);
                                    }

                                }
                                if (reader.Name.ToLower() == "distance")
                                {
                                    for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
                                    {
                                        reader.MoveToAttribute(attInd);
                                        if (reader.Name == "value")
                                        {
                                            switch (reader.Value.ToUpper())
                                            {
                                                case "FAR": distance = Relative_Hand_Position.Distance.FAR; break;
                                                case "NORMAL": distance = Relative_Hand_Position.Distance.NORMAL; break;
                                                case "CLOSE": distance = Relative_Hand_Position.Distance.CLOSE; break;
                                                case "TOUCH": distance = Relative_Hand_Position.Distance.TOUCH; break;
                                                default:
                                                    error = true;
                                                    agent.Log("Error in gesturelibrary file, in gesture: " + name +
                                                                " Distance value is wrong. Default value is assigned");
                                                    break;
                                            }
                                        }
                                        if (reader.Name.ToLower() == "percentage")
                                            float.TryParse(reader.Value, ns, ci, out distancePercentage);
                                    }
                                }
                                if (reader.Name.ToLower() == "radialorientation")
                                {
                                    for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
                                    {
                                        reader.MoveToAttribute(attInd);
                                        if (reader.Name == "value")
                                        {
                                            switch (reader.Value.ToUpper())
                                            {
                                                case "FAR_OUT": radial = Relative_Hand_Position.RadialOrientation.FAR_OUT; break;
                                                case "OUT": radial = Relative_Hand_Position.RadialOrientation.OUT; break;
                                                case "SIDE": radial = Relative_Hand_Position.RadialOrientation.SIDE; break;
                                                case "FRONT": radial = Relative_Hand_Position.RadialOrientation.FRONT; break;
                                                case "INWARD": radial = Relative_Hand_Position.RadialOrientation.INWARD; break;
                                                default:
                                                    error = true;
                                                    agent.Log("Error in gesturelibrary file, in gesture: " + name +
                                                                " Radial Orientation value is wrong. Default value is assigned");
                                                    break;
                                            }
                                        }
                                        if (reader.Name.ToLower() == "percentage")
                                            float.TryParse(reader.Value, ns, ci, out radialPercentage);
                                    }
                                }
                                if (reader.Name.ToLower() == "armswivel")
                                {
                                    for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
                                    {
                                        reader.MoveToAttribute(attInd);
                                        if (reader.Name.ToLower() == "value")
                                        {
                                            switch (reader.Value.ToUpper())
                                            {
                                                case "ORTHOGONAL": armswivel = Relative_Hand_Position.ArmSwivel.ORTHOGONAL; break;
                                                case "OUT": armswivel = Relative_Hand_Position.ArmSwivel.OUT; break;
                                                case "NORMAL": armswivel = Relative_Hand_Position.ArmSwivel.NORMAL; break;
                                                case "TOUCH": armswivel = Relative_Hand_Position.ArmSwivel.TOUCH; break;
                                                default:
                                                    error = true;
                                                    agent.Log("Error in gesturelibrary file, in gesture: " + name +
                                                                " Arm Swivel value is wrong. Default value is assigned");
                                                    break;
                                            }
                                        }
                                        if (reader.Name.ToLower() == "percentage")
                                            float.TryParse(reader.Value, ns, ci, out armswivelPercentage);
                                    }
                                }

                                if (reader.Name.ToLower() == "wristrotation")
                                {
                                    for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
                                    {
                                        reader.MoveToAttribute(attInd);

                                        if (reader.Name.ToLower() == "x")
                                            float.TryParse(reader.Value, ns, ci, out x);

                                        if (reader.Name.ToLower() == "y")
                                            float.TryParse(reader.Value, ns, ci, out y);

                                        if (reader.Name.ToLower() == "z")
                                            float.TryParse(reader.Value, ns, ci, out z);

                                        if (reader.Name.ToLower() == "w")
                                            float.TryParse(reader.Value, ns, ci, out w);
                                    }
                                }

                                if (reader.Name.ToLower() == "handshape")
                                {
                                    handShape = reader.GetAttribute("shape").ToUpper();
                                }
                            }

                            if (side == "LEFT" || side == "BOTH")
                            {
                                gd.leftHand = new Relative_Hand_Position();
                                gd.initLeftWristRotation = initLeftWirstOrientation;
                                gd.leftHand.height = height;
                                gd.leftHand.heightPercentage = heightPercentage;
                                gd.leftHand.distance = distance;
                                gd.leftHand.distancePercentage = distancePercentage;
                                gd.leftHand.radialOrientation = radial;
                                gd.leftHand.radialOrientationPercentage = radialPercentage;
                                gd.leftHand.armSwivel = armswivel;
                                gd.leftHand.armSwivelPercentage = armswivelPercentage;

                                gd.leftHand.handShape = handShape;
                                gd.leftHand.wristRotation = new Quaternion(x, y, z, w);
                            }

                            if (side == "RIGHT" || side == "BOTH")
                            {
                                gd.rightHand = new Relative_Hand_Position();
                                gd.initRightWristRotation = initRightWirstOrientation;
                                gd.rightHand.height = height;
                                gd.rightHand.heightPercentage = heightPercentage;
                                gd.rightHand.distance = distance;
                                gd.rightHand.distancePercentage = distancePercentage;
                                gd.rightHand.radialOrientation = radial;
                                gd.rightHand.radialOrientationPercentage = radialPercentage;
                                gd.rightHand.armSwivel = armswivel;
                                gd.rightHand.armSwivelPercentage = armswivelPercentage;

                                gd.rightHand.handShape = handShape;
                                gd.rightHand.wristRotation = new Quaternion(x, y, z, w);
                            }
                        }
                    }
                    if (error == false)
                        postureTable.Add(name, gd);
                }
            }
            //for debug
            //printPostureTable(postureTable);
        }

        /*******************************************************************/



        /* DEBBUG FUNCTIONS - print data tables*/

        private static void printActionUnitsTable(Dictionary<string, List<Blendshape>> actionUnits)
        {
            agent.Log("ACTION UNITS");
            foreach (KeyValuePair<string, List<Blendshape>> bs in actionUnits)
            {
                agent.Log("name: " + bs.Key + " contains: " + bs.Value.Count + " blendshapes");
                foreach (var b in bs.Value)
                {
                    agent.Log(" code:" + b.code + " type:" + b.type + " weight:" + b.weight + " delayed_start:" + b.delayed_start + " anticipated_end:" + b.anticipated_end);
                }
            }
        }

        private static void printFaceExpressionsTable(Dictionary<string, List<ActionUnit>> faceExpressions)
        {
            agent.Log("FACE EXPRESSIONS");
            foreach (KeyValuePair<string, List<ActionUnit>> fe in faceExpressions)
            {
                agent.Log("lexeme: " + fe.Key + " contains: " + fe.Value.Count + " action units");
                foreach (var au in fe.Value)
                {
                    agent.Log(" AU:" + au.au + " side:" + au.side + " amount:" + au.amount);
                }
            }
        }

        private static void printHeadMovementsTable(Dictionary<string, HeadShape> headShapes)
        {
            foreach (KeyValuePair<string, HeadShape> hs in headShapes)
            {
                agent.Log("name: " + hs.Key + " minrot: " + hs.Value.min_rotation_ + " maxrot: " + hs.Value.max_rotation_ +
                          " mind: " + hs.Value.min_duration_ + " maxd: " + hs.Value.max_duration_ +
                          " minr: " + hs.Value.min_repetition_ + " maxr: " + hs.Value.max_repetition_);
            }
        }

        private static void printTorsoTable(Dictionary<string, TorsoShape> torsoShapes)
        {
            foreach (KeyValuePair<string, TorsoShape> ps in torsoShapes)
            {
                agent.Log("name: " + ps.Key + " side: " + ps.Value.side);
                if (ps.Value.side == "LEFT" || ps.Value.side == "BOTH")
                    agent.Log("left   ro: " + ps.Value.leftTorso.radialOrientation + " h: " + ps.Value.leftTorso.height + " d: " + ps.Value.leftTorso.distance);
                    if (ps.Value.side == "RIGHT" || ps.Value.side == "BOTH")
                        agent.Log("right   ro: " + ps.Value.rightTorso.radialOrientation + " h: " + ps.Value.rightTorso.height + " d: " + ps.Value.rightTorso.distance);
            }
        }


        /********************************************************************/
        /* Print gesture and hand relative description = First version code */
        /********************************************************************/

        //Animation Engine takes care of loading hand shapes
        /*private static void printHandTable(Dictionary<string, HandShape> handTable)
        {
            //Debug.Log("HAND SHAPES");
            foreach (KeyValuePair<string, HandShape> bs in handTable)
            {
                //Debug.Log("hand shape name: " + bs.Key + " offset " + bs.Value.size);
                foreach (KeyValuePair<string, Quaternion> fs in bs.Value.fingers)
                {
                    //Debug.Log("\tfinger:" + fs.Key + " vector: " + fs.Value);
                }
            }
        }*/

        private static void printGestureTable(Dictionary<string, GestureDescription> gestureTable)
        {
            agent.Log("GESTURES");
            foreach (KeyValuePair<string, GestureDescription> bs in gestureTable)
            {
                agent.Log("gesture: " + bs.Key + " duration: " + bs.Value.duration);
                foreach (Relative_Phase b in bs.Value.phases)
                {
                    agent.Log("\ttype:" + b.name + " time: " + b.time + " side " + b.side);
                    if (b.leftHand != null)
                        agent.Log("\t\t left hand x:" + b.leftHand.radialOrientation + " y:" + b.leftHand.height + " z:" + b.leftHand.distance + "  " + b.leftHand.wristRotation);
                        if (b.rightHand != null)
                            agent.Log("\t\t right hand x:" + b.rightHand.radialOrientation + " y:" + b.rightHand.height + " z:" + b.rightHand.distance);
                }
            }
        }

        private static void printPostureTable(Dictionary<string, PostureDescription> postureTable)
        {
            agent.Log("POSTURES");
            foreach (KeyValuePair<string, PostureDescription> bs in postureTable)
            {
                agent.Log("posture: " + bs.Key + " side: " + bs.Value.side + " " + bs.Value.initLeftWristRotation + " " + bs.Value.initRightWristRotation);
                Relative_Hand_Position left = bs.Value.leftHand;
                Relative_Hand_Position right = bs.Value.rightHand;
                if (left != null)
                    agent.Log("\t\t left hand x:" + left.radialOrientation + " y:" + left.height + " z:" + left.distance);
                    if (right != null)
                        agent.Log("\t\t left hand x:" + right.radialOrientation + " y:" + right.height + " z:" + right.distance);
            }
        }

        private static void printMocapsTable(Dictionary<string, MocapDescription> mocapsTable)
        {
            agent.Log("MOCAPS");
            foreach (KeyValuePair<string, MocapDescription> bs in mocapsTable)
            {
                agent.Log("mocaps: " + bs.Key + " clip: " + bs.Value.clip + " posture: " + bs.Value.posture + " duration : " + bs.Value.duration +
                          " side: " + bs.Value.side + "\n stroke: " + bs.Value.stroke + " ready: " + bs.Value.ready + " stroke_start: " + bs.Value.stroke_start +
                           " stroke_end: " + bs.Value.stroke_end + " relax: " + bs.Value.relax);
            }
        }

        /********************************************************************/
    }
}

//----------------------------------------------------------------------------