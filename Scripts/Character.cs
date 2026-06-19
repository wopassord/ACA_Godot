using System;
using System.Xml;
using System.IO;
using System.Collections.Generic;
using System.Net.Sockets;
using Animation;

namespace VirtualAgent
{
		public class Character
	{
		static readonly System.Random random = new System.Random();
		static readonly System.Globalization.NumberStyles ns = System.Globalization.NumberStyles.Float;
		static readonly System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo("en-US");

		public delegate void Logger(string msg);
		private Logger logger_;

		internal string name;
		public Animation.IAnimationEngine animationEngine;
		public ClientUDP udpClient_;


		public BmlSchedulerBehaviour scheduler;
		internal TextToSpeech.Engine te_;
		internal TextToSpeech.Channel ttsc;
		public double speechDuration = 0;
		internal FaceEngine fe;


		//NOT IMPLEMENTED YET
		// internal GestureEngine ge;
		// internal TorsoEngine toe;

		//internal AnimationEngine ae;

		internal ReactiveBehavior rb_;  //TODO : totalement � refaire !!!

		public Dictionary<string, List<Blendshape>> actionUnits = new Dictionary<string, List<Blendshape>>();
		internal Dictionary<string, List<ActionUnit>> faceExpressions = new Dictionary<string, List<ActionUnit>>();
		internal Dictionary<string, HeadShape> headShapes_ = new Dictionary<string, HeadShape>();
		internal Dictionary<string, TorsoShape> torsoShapes_ = new Dictionary<string, TorsoShape>();
		internal Dictionary<string, PostureDescription> postureShapes_ = new Dictionary<string, PostureDescription>();
		internal Dictionary<string, MocapDescription> mocaps_ = new Dictionary<string, MocapDescription>();

		/**** Gesture and hand table = First version code ****/
		public Dictionary<string, GestureDescription> gestureTable = new Dictionary<string, GestureDescription>();
		//public Dictionary<string, HandShape> handTable = new Dictionary<string, HandShape>();
		/*****************************************************/

		int i = 0;


		public Character(string n, Animation.IAnimationEngine ae, TextToSpeech.Engine te, BmlSchedulerBehaviour bsb, ReactiveBehavior rb, ClientUDP udpc = null, Logger logger = null)
		{
			name = n;
			te_ = te;
			animationEngine = ae;
			ae.SetAgent(this);
			scheduler = bsb;
			rb_ = rb;
			rb_.SetAgent(this);
			logger_ = logger;
			udpClient_ = udpc;

			//test architecture
			//Log("Architecture: " + System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString());
			//https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.architecture?view=netstandard-2.0

			//System.Xml.XmlTextReader reader = new System.Xml.XmlTextReader(Application.streamingAssetsPath + "/agents/" + name + ".xml");
			string path = animationEngine.GetAssetsPath() + "/agents/";
			System.Xml.XmlTextReader reader = new System.Xml.XmlTextReader(path + name + ".xml");
			string faceMesh = "";
			string jawMesh = "";
			string tongueMesh = "";
			string leftEyeMesh = "";
			string rightEyeMesh = "";
			string bodyMesh = "";

			while (reader.Read())
			{
				string readerName = reader.Name.ToLower();
				switch (readerName)
				{
					case "tts":
						string voiceName = reader.GetAttribute("voiceName");
						voiceName = voiceName == null ? "" : voiceName;
						int voiceID = te_.VoiceID(voiceName);
						if (voiceID <= -1 || voiceID >= te_.VoiceCount)
						{
							Log("ATTENTION: voiceID is not valid. The first possible voice is assigned to the agent." + voiceName);
							if (te_.VoiceCount > 0)
								ttsc = new TextToSpeech.Channel(te_, 0);
						}
						else
							ttsc = new TextToSpeech.Channel(te_, voiceID);
						break;
					case "blendshape":
						string visemeFile = reader.GetAttribute("table");
						visemeFile = visemeFile == null ? "" : visemeFile;
						if (visemeFile == "")
						{
							visemeFile = "blendshapelibrary.xml";
							Log("ATTENTION: blendshapes file is not valid. Default blendshapes are assigned to the agent.");
						}
						LoadData.LoadBlendshapes(path + visemeFile, ref actionUnits);
						break;
					case "facelibrary":
						string facelibraryFile = reader.GetAttribute("table");
						facelibraryFile = facelibraryFile == null ? "" : facelibraryFile;
						if (facelibraryFile == "")
						{
							facelibraryFile = "facelibrary.xml";
							Log("ATTENTION: facelibrary file is not valid. Default facial expressions are assigned to the agent.");
						}
						LoadData.LoadFacialExpressions(path + facelibraryFile, ref faceExpressions);
						break;
					case "headlibrary":
						string headlibraryFile = reader.GetAttribute("table");
						headlibraryFile = headlibraryFile == null ? "" : headlibraryFile;
						if (headlibraryFile == "")
						{
							headlibraryFile = "headlibrary.xml";
							Log("ATTENTION: headlibrary file is not valid. Default head movements are assigned to the agent.");
						}
						LoadData.LoadHeadShapes(path + headlibraryFile, ref headShapes_);
						break;
					case "gesturelibrary":
						string gesturelibraryFile = reader.GetAttribute("table");
						gesturelibraryFile = gesturelibraryFile == null ? "" : gesturelibraryFile;
						if (gesturelibraryFile == "")
						{
							gesturelibraryFile = "gesturelibrary.xml";
							Log("ATTENTION: gesturelibrary file is not valid. Default gestures are assigned to the agent.");
						}
						LoadData.LoadGestures(path + gesturelibraryFile, ref gestureTable);
						break;
					case "handlibrary":
						string handlibraryFile = reader.GetAttribute("table");
						handlibraryFile = handlibraryFile == null ? "" : handlibraryFile;
						if (handlibraryFile == "")
						{
							handlibraryFile = "handlibrary.xml";
							Log("ATTENTION: handlibrary file is not valid. Default hand shapes are assigned to the agent.");
						}
						animationEngine.LoadHandShapes(path + handlibraryFile);
						//LoadData.LoadHandShapes(path + handlibraryFile, ref handTable);
						break;
					case "torsolibrary":
						string torsolibraryFile = reader.GetAttribute("table");
						torsolibraryFile = torsolibraryFile == null ? "" : torsolibraryFile;
						if (torsolibraryFile == "")
						{
							torsolibraryFile = "torsolibrary.xml";
							Log("ATTENTION: torsolibrary file is not valid. Default postures are assigned to the agent.");
						}
						LoadData.LoadTorsoShapes(path + torsolibraryFile, ref torsoShapes_);
						break;
					case "posturelibrary":
						string posturelibraryFile = reader.GetAttribute("table");
						posturelibraryFile = posturelibraryFile == null ? "" : posturelibraryFile;
						if (posturelibraryFile == "")
						{
							posturelibraryFile = "posturelibrary.xml";
							Log("ATTENTION: posturelibrary file is not valid. Default postures are assigned to the agent.");
						}
						LoadData.LoadPostures(path + posturelibraryFile, ref postureShapes_);
						break;
					case "mocaplibrary":
						string mocapslibraryFile = reader.GetAttribute("table");
						mocapslibraryFile = mocapslibraryFile == null ? "" : mocapslibraryFile;
						if (mocapslibraryFile == "")
						{
							mocapslibraryFile = "mocaplibrary.xml";
							Log("ATTENTION: mocaplibrary file is not valid. Default mocaps are assigned to the agent.");
						}
						LoadData.LoadMocaps(path + mocapslibraryFile, ref mocaps_);
						break;
					case "facemesh":
						faceMesh = reader.GetAttribute("name");
						faceMesh = faceMesh == null ? "" : faceMesh;
						break;
					case "bodyemesh":
						bodyMesh = reader.GetAttribute("name");
						bodyMesh = bodyMesh == null ? "" : bodyMesh;
						break;
					case "jawmesh":
						jawMesh = reader.GetAttribute("name");
						jawMesh = jawMesh == null ? "" : jawMesh;
						break;
					case "tonguemesh":
						tongueMesh = reader.GetAttribute("name");
						tongueMesh = tongueMesh == null ? "" : tongueMesh;
						break;
					case "lefteyemesh":
						leftEyeMesh = reader.GetAttribute("name");
						leftEyeMesh = leftEyeMesh == null ? "" : leftEyeMesh;
						break;
					case "righteyemesh":
						rightEyeMesh = reader.GetAttribute("name");
						rightEyeMesh = rightEyeMesh == null ? "" : rightEyeMesh;
						break;
					default: break;
				}
			}

			ae.LoadMeshes(faceMesh, jawMesh, tongueMesh, leftEyeMesh, rightEyeMesh, bodyMesh);


			fe = new FaceEngine(this);  //agent.AddComponent<FaceEngine>();
										//fe.Init(this);
//NOT IMPLEMENTED YET
			// toe = new TorsoEngine(this); //toe = agent.AddComponent<TorsoEngine>();
			// 							 //toe.Init(this);

			// ge = new GestureEngine(this); //ge = agent.AddComponent<GestureEngine>();
			// 							  //ge.Init(this);
		}

		public void Log(string msg)
		{
			if (logger_ != null)
			{
				logger_(msg);
			}
		}

		public void Send(string msg)
		{
			if (udpClient_ != null)
			{
				udpClient_.Send(name + ":" + msg);
			}
		}

		public void ChangeVoice(string name)
		{
			int voiceID = te_.VoiceID(name);
			if (voiceID != -1)
				ttsc = new TextToSpeech.Channel(te_, voiceID);
		}


		public Bml.Synchro AddSynchro(string id, string expr, bool stress = false)
		{
			return scheduler.Sched.NewSynchro(id, expr, stress);
		}


		private TextToSpeech.SpeechData getGeneralSpeechSynchros(string text, ref List<Bml.Synchro> synchros)
		{
			TextToSpeech.SpeechData sdata = ttsc.Speak(text, true);

			Bml.Synchro start = synchros.Find(x => x.Id == "start");
			if(start == null)
			{
				synchros.Add(scheduler.Sched.NewSynchro("start", "0"));
				speechDuration += sdata.Phonemes[sdata.Phonemes.Length - 1].End;
			}
			else
			{
				double st = 0;
				if (Double.TryParse(start.Expr, ns, ci, out st))
					speechDuration += st + sdata.Phonemes[sdata.Phonemes.Length - 1].End;
				//else
				//{
				//speechDuration cannot be known here, the start of the speech signal depends on another signal
				//TODO : find a better solution in BML.cs
				//}
			}

			//TO MODIFY if speech end point is useful
			int i = synchros.FindIndex(x => x.Id == "end");
			if (i >= 0)
				synchros.RemoveAt(i);
			synchros.Add(scheduler.Sched.NewSynchro("end", "start +" + (sdata.Phonemes[sdata.Phonemes.Length - 1].End).ToString(ci)));

			foreach (var p in sdata.Phonemes)
			{
				if (p.Type == TextToSpeech.SpeechData.PhonemeType.TIMEMARKER)
				{
					synchros.Add(scheduler.Sched.NewSynchro(p.Name, "start + " + (p.Start).ToString(ci)));
				}
				if (p.Type == TextToSpeech.SpeechData.PhonemeType.PHONEME && p.Stress > 0)
				{
					synchros.Add(scheduler.Sched.NewSynchro("stress", "start + " + ((p.Start + p.End) / 2.0).ToString(ci), true));
				}
			}

			return sdata;
		}


		public void addFourSynchros(ref List<Bml.Synchro> synchros, bool shift = false)
		{
			if (synchros.Count == 0)
				synchros.Add(scheduler.Sched.NewSynchro("start", "0"));

			if (synchros.Find(x => x.Id == "start") == null)
				synchros.Add(scheduler.Sched.NewSynchro("start", "!!!"));
			if (synchros.Find(x => x.Id == "ready") == null)
				synchros.Add(scheduler.Sched.NewSynchro("ready", "!!!"));
			if (synchros.Find(x => x.Id == "relax") == null)
				synchros.Add(scheduler.Sched.NewSynchro("relax", "!!!"));
			if (synchros.Find(x => x.Id == "end") == null)
				synchros.Add(scheduler.Sched.NewSynchro("end", "!!!"));
		}

		public void addSevenSynchros(ref List<Bml.Synchro> synchros)
		{
			if (synchros.Count == 0)
				synchros.Add(scheduler.Sched.NewSynchro("start", "0"));

			if (synchros.Find(x => x.Id == "start") == null)
				synchros.Add(scheduler.Sched.NewSynchro("start", "!!!"));
			if (synchros.Find(x => x.Id == "ready") == null)
				synchros.Add(scheduler.Sched.NewSynchro("ready", "!!!"));
			if (synchros.Find(x => x.Id == "stroke_start") == null)
				synchros.Add(scheduler.Sched.NewSynchro("stroke_start", "!!!"));
			if (synchros.Find(x => x.Id == "stroke") == null)
				synchros.Add(scheduler.Sched.NewSynchro("stroke", "!!!"));
			if (synchros.Find(x => x.Id == "stroke_end") == null)
				synchros.Add(scheduler.Sched.NewSynchro("stroke_end", "!!!"));
			if (synchros.Find(x => x.Id == "relax") == null)
				synchros.Add(scheduler.Sched.NewSynchro("relax", "!!!"));
			if (synchros.Find(x => x.Id == "end") == null)
				synchros.Add(scheduler.Sched.NewSynchro("end", "!!!"));
		}

		private bool UniqueId(List<Bml.Signal> signals, string id)
		{
			//CHECK SIGNALS ID, they must be unique
			foreach (var s in signals)
			{
				if (s.Id.Equals(id))
				{
					Log("ERROR! The signal id " + id + " must be unique");
					return false;
				}
			}
			return true;
		}
		public int AddBml(string b, string name)
		{
			string bml_id = "";
			string agent = "";
			speechDuration = 0;
			Bml.Composition composition = Bml.Composition.MERGE;
			var signals = new List<Bml.Signal>();
			var stresses = new Dictionary<string, List<double>>();

			using (var reader = XmlReader.Create(new StringReader(b)))
			{
				while (reader.Read())
				{
					if (reader.NodeType == XmlNodeType.Element && reader.Name.ToLower() != "required")
					{
						var synchros = new List<Bml.Synchro>();
						var aus = new List<ActionUnit>();
						string lexeme = "";
						string category = "";
						float amount = 1;
						string side = "";
						string au = "";
						string target = "no_direction";
						bool shift = false;

						//TODO : to improve to check if an id is not specified
						string id = reader.GetAttribute("id");

						string modality = reader.Name.ToLower();
						switch (modality) //reader.Name.ToLower())
						{
							case "bml":
								for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
								{
									reader.MoveToAttribute(attInd);
									switch (reader.Name.ToLower())
									{
										case "id":
											bml_id = id;
											if (scheduler.Sched.HasBml(id))
											{
												Log("ERROR! The bml id " + id + " must be unique");
												//return 1; //The bml id is not unique
												bml_id += (i++).ToString();
											}
											break;
										case "characterid":
											agent = reader.Value;
											//if (agent != name)
											//    return 2; //The bml is not for the right agent
											break;
										case "composition":
											switch (reader.Value.ToUpper())
											{
												case "APPEND": composition = Bml.Composition.APPEND; break;
												case "REPLACE": composition = Bml.Composition.REPLACE; break;
												default: composition = Bml.Composition.MERGE; break;
											}
											break;
									}
								}
								break;
							case "gazedirectionshift":
							case "gaze":
								if (!UniqueId(signals, id)) break;
								string influence = "";
								double offsetAngle = 0.0f;
								string offsetDirection = "";
								target = "no_direction";
								try
								{
									target = reader.GetAttribute("target");
								}
								catch (Exception) { }
								for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
								{
									reader.MoveToAttribute(attInd);
									switch (reader.Name.ToLower())
									{
										case "influence": influence = reader.Value; break;
										case "start": synchros.Add(scheduler.Sched.NewSynchro("start", reader.Value)); break;
										case "ready": synchros.Add(scheduler.Sched.NewSynchro("ready", reader.Value)); break;
										case "relax": synchros.Add(scheduler.Sched.NewSynchro("relax", reader.Value)); break;
										case "end": synchros.Add(scheduler.Sched.NewSynchro("end", reader.Value)); break;
										case "offsetangle":
											Double.TryParse(reader.Value, ns, ci, out offsetAngle);
											break;
										case "offsetdirection": offsetDirection = reader.Value; break;
									}
								}
								shift = modality.Equals("gazedirectionshift");
								addFourSynchros(ref synchros, shift);
								foreach (var s in synchros)
								{
									//Log("gaze synchros  " + s.Id + " " + s.Expr + " " + s.Time);
								}
								signals.Add(SignalBuilder.Gaze(scheduler.Sched, id, fe, shift, target, synchros));
								break;

							case "face":
								if (!UniqueId(signals, id)) break;
								for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
								{
									reader.MoveToAttribute(attInd);
									switch (reader.Name.ToLower())
									{
										case "amount":
											float.TryParse(reader.Value, ns, ci, out amount);
											break;
										case "start": synchros.Add(scheduler.Sched.NewSynchro("start", reader.Value)); break;
										case "attack_peak": synchros.Add(scheduler.Sched.NewSynchro("ready", reader.Value)); break;
										case "relax": synchros.Add(scheduler.Sched.NewSynchro("relax", reader.Value)); break;
										case "end": synchros.Add(scheduler.Sched.NewSynchro("end", reader.Value)); break;
									}
								}
								addFourSynchros(ref synchros);

								while (reader.Read())
								{
									if (reader.NodeType == XmlNodeType.EndElement) break;
									if (reader.NodeType != XmlNodeType.Element) continue;
									switch (reader.Name.ToLower())
									{
										case "lexeme":
											float amountLexeme = amount;
											for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
											{
												reader.MoveToAttribute(attInd);
												switch (reader.Name.ToLower())
												{
													case "lexeme":
														lexeme = reader.Value.ToLower(); break;
													case "amount":
														float.TryParse(reader.Value, ns, ci, out amountLexeme);
														break;
												}
											}
											if (faceExpressions.ContainsKey(lexeme))
											{
												var aux = faceExpressions[lexeme];
												for (int i = 0; i < aux.Count; i++)
												{
													var app = aux[i];
													app.amount = amount;
													aux[i] = app;
													aus.Add(aux[i]);
												}
											}
											break;

										case "ext:facs":
											float amountAU = amount;
											for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
											{
												reader.MoveToAttribute(attInd);
												switch (reader.Name.ToLower())
												{
													case "au": au = reader.Value.ToLower(); break;
													case "side": side = reader.Value.ToUpper(); break;
													case "amount":
														float.TryParse(reader.Value, ns, ci, out amountAU);
														break;
												}
											}
											aus.Add(new ActionUnit(au, side, amountAU));
											break;
									}
								}
								shift = modality.Equals("faceshift");
								signals.Add(SignalBuilder.Face(scheduler.Sched, id, "facs", amount, false, aus, fe, synchros));
								break;

							case "faceshift":
							case "facelexeme":
								if (!UniqueId(signals, id)) break;
								for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
								{
									reader.MoveToAttribute(attInd);
									switch (reader.Name.ToLower())
									{
										case "lexeme":
											lexeme = reader.Value.ToLower();
											if (faceExpressions.ContainsKey(lexeme))
												aus = faceExpressions[lexeme];
											else
												Log("Facial expression " + lexeme + "not found");
											for (int i = 0; i < aus.Count; i++)
											{
												var app = aus[i];
												app.amount = amount;
												aus[i] = app;
											}
											break;
										case "amount":
											float.TryParse(reader.Value, ns, ci, out amount);
											break;
										case "start": synchros.Add(scheduler.Sched.NewSynchro("start", reader.Value)); break;
										case "attack_peak": synchros.Add(scheduler.Sched.NewSynchro("ready", reader.Value)); break;
										case "relax": synchros.Add(scheduler.Sched.NewSynchro("relax", reader.Value)); break;
										case "end": synchros.Add(scheduler.Sched.NewSynchro("end", reader.Value)); break;
									}
								}
								addFourSynchros(ref synchros);
								shift = modality.Equals("faceshift");
								signals.Add(SignalBuilder.Face(scheduler.Sched, id, lexeme, amount, shift, aus, fe, synchros));
								break;

							case "ext:facefacs":
								if (!UniqueId(signals, id)) break;
								for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
								{
									reader.MoveToAttribute(attInd);
									switch (reader.Name.ToLower())
									{
										case "au": au = reader.Value.ToLower(); break;
										case "amount":
											float.TryParse(reader.Value, ns, ci, out amount);
											break;
										case "side": side = reader.Value.ToUpper(); break;
										case "start": synchros.Add(scheduler.Sched.NewSynchro("start", reader.Value)); break;
										case "attack_peak": synchros.Add(scheduler.Sched.NewSynchro("ready", reader.Value)); break;
										case "relax": synchros.Add(scheduler.Sched.NewSynchro("relax", reader.Value)); break;
										case "end": synchros.Add(scheduler.Sched.NewSynchro("end", reader.Value)); break;
									}
								}
								addFourSynchros(ref synchros);
								aus.Add(new ActionUnit(au, side, amount));
								signals.Add(SignalBuilder.Face(scheduler.Sched, id, "facs", amount, false, aus, fe, synchros));
								break;

							case "head":
							case "headdirectionshift":
								if (!UniqueId(signals, id)) break;
								int repetition = -1;
								for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
								{
									reader.MoveToAttribute(attInd);
									switch (reader.Name.ToLower())
									{
										case "lexeme": lexeme = reader.Value.ToLower(); break;
										case "repetition":
											int.TryParse(reader.Value, ns, ci, out repetition);
											break;
										case "amount":
											float.TryParse(reader.Value, ns, ci, out amount);
											break;
										case "target": target = reader.Value; break;
										case "start": synchros.Add(scheduler.Sched.NewSynchro("start", reader.Value)); break;
										case "ready": synchros.Add(scheduler.Sched.NewSynchro("ready", reader.Value)); break;
										case "stroke_start": synchros.Add(scheduler.Sched.NewSynchro("stroke_start", reader.Value)); break;
										case "stroke": synchros.Add(scheduler.Sched.NewSynchro("stroke", reader.Value)); break;
										case "stroke_end": synchros.Add(scheduler.Sched.NewSynchro("stroke_end", reader.Value)); break;
										case "relax": synchros.Add(scheduler.Sched.NewSynchro("relax", reader.Value)); break;
										case "end": synchros.Add(scheduler.Sched.NewSynchro("end", reader.Value)); break;
									}
								}

								shift = modality.Equals("headdirectionshift");

								lexeme = lexeme.Replace("tilt_right", "tiltr");
								lexeme = lexeme.Replace("tilt_left", "tiltl");

								HeadShape result;
								if (headShapes_.TryGetValue(lexeme, out result))
								{
									addSevenSynchros(ref synchros);
									signals.Add(SignalBuilder.Head(scheduler.Sched, id, fe, result, shift, lexeme, target, amount, repetition, synchros));
								}
								break;
			// BODY CASES NOT IMPLEMENTED YET	
							// case "gesture":
							// 	if (!UniqueId(signals, id)) break;
							// 	string[] lex;
							// 	side = "";
							// 	target = "";
							// 	for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
							// 	{
							// 		reader.MoveToAttribute(attInd);
							// 		switch (reader.Name.ToLower())
							// 		{
							// 			case "lexeme": //category:lexeme:target
							// 				lex = reader.Value.Split(':');
							// 				if (lex.Length == 1)
							// 					lexeme = lex[0];
							// 				if (lex.Length == 2)
							// 				{
							// 					if (lex[0].Equals("mocap"))
							// 					{
							// 						category = lex[0];
							// 						lexeme = lex[1];
							// 					}
							// 					else
							// 					{
							// 						lexeme = lex[0];
							// 						target = lex[1];
							// 					}
							// 				}
							// 				if (lex.Length == 3)
							// 				{
							// 					category = lex[0];
							// 					lexeme = lex[1];
							// 					target = lex[2];
							// 				}
							// 				break;
							// 			case "mode": side = reader.Value.ToUpper(); break;
							// 			case "start": synchros.Add(scheduler.Sched.NewSynchro("start", reader.Value)); break;
							// 			case "ready": synchros.Add(scheduler.Sched.NewSynchro("ready", reader.Value)); break;
							// 			case "stroke_start": synchros.Add(scheduler.Sched.NewSynchro("stroke_start", reader.Value)); break;
							// 			case "stroke": synchros.Add(scheduler.Sched.NewSynchro("stroke", reader.Value)); break;
							// 			case "stroke_end": synchros.Add(scheduler.Sched.NewSynchro("stroke_end", reader.Value)); break;
							// 			case "relax": synchros.Add(scheduler.Sched.NewSynchro("relax", reader.Value)); break;
							// 			case "end": synchros.Add(scheduler.Sched.NewSynchro("end", reader.Value)); break;
							// 		}
							// 	}

							// 	if (side == "LEFT_HAND") side = "LEFT";
							// 	else if (side == "RIGHT_HAND") side = "RIGHT";
							// 	else if (side == "BOTH_HANDS") side = "BOTH";

							// 	/****  Add gesture description - First version ****/
							// 	MocapDescription md = null;
							// 	GestureDescription gd = null;
							// 	if (category.Equals("mocap"))
							// 		mocaps_.TryGetValue(lexeme, out md);
							// 	else
							// 	{
							// 		gestureTable.TryGetValue(lexeme, out gd);
							// 		if (gd == null)
							// 			gestureTable.TryGetValue(/*category + ":" +*/ lexeme, out gd);
							// 	}
							// 	if (gd != null || md != null)
							// 	{
							// 		addSevenSynchros(ref synchros);
							// 		signals.Add(SignalBuilder.Gesture(scheduler.Sched, id, ge, lexeme, side, gd, synchros, target, md));
							// 	}
							// 	else Log("Gesture description " + lexeme + " has not been found");
							// 	break;

							// case "pointing":
							// 	if (!UniqueId(signals, id)) break;
							// 	side = "LEFT_HAND";
							// 	for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
							// 	{
							// 		reader.MoveToAttribute(attInd);
							// 		switch (reader.Name.ToLower())
							// 		{
							// 			case "mode": side = reader.Value.ToUpper(); break;
							// 			case "target": target = reader.Value; break;
							// 			case "start": synchros.Add(scheduler.Sched.NewSynchro("start", reader.Value)); break;
							// 			case "ready": synchros.Add(scheduler.Sched.NewSynchro("ready", reader.Value)); break;
							// 			case "stroke_start": synchros.Add(scheduler.Sched.NewSynchro("stroke_start", reader.Value)); break;
							// 			case "stroke": synchros.Add(scheduler.Sched.NewSynchro("stroke", reader.Value)); break;
							// 			case "stroke_end": synchros.Add(scheduler.Sched.NewSynchro("stroke_end", reader.Value)); break;
							// 			case "relax": synchros.Add(scheduler.Sched.NewSynchro("relax", reader.Value)); break;
							// 			case "end": synchros.Add(scheduler.Sched.NewSynchro("end", reader.Value)); break;
							// 		}
							// 	}

							// 	if (side == "LEFT_HAND") side = "LEFT";
							// 	else if (side == "RIGHT_HAND") side = "RIGHT";
							// 	else if (side == "BOTH_HANDS") side = "BOTH";

							// 	addSevenSynchros(ref synchros);

							// 	GestureDescription po;
							// 	gestureTable.TryGetValue("pointing", out po);
							// 	if (po != null && (side == "RIGHT" || side == "LEFT" || side == "BOTH"))
							// 		signals.Add(SignalBuilder.Pointing(scheduler.Sched, ge, id, target, side, po, synchros));
							// 	else if (side == "HEAD")
							// 		signals.Add(SignalBuilder.Head(scheduler.Sched, id, fe, headShapes_["neutral"], false, "pointing", target, 1, 1, synchros));
							// 	break;

							// case "locomotion":
							// 	if (!UniqueId(signals, id)) break;
							// 	string manner = "WALK";
							// 	for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
							// 	{
							// 		reader.MoveToAttribute(attInd);
							// 		switch (reader.Name.ToLower())
							// 		{
							// 			case "manner": manner = reader.Value.ToUpper(); break;
							// 			case "target": target = reader.Value.ToUpper(); break; //TODO : to add in bml
							// 			case "start": synchros.Add(scheduler.Sched.NewSynchro("start", reader.Value)); break;
							// 			case "end": synchros.Add(scheduler.Sched.NewSynchro("end", reader.Value)); break;
							// 		}
							// 	}
							// 	if (synchros.Find(x => x.Id == "end") == null)
							// 		synchros.Add(scheduler.Sched.NewSynchro("end", "???"));

							// 	signals.Add(SignalBuilder.Locomotion(scheduler.Sched, id, target, manner, synchros));
							// 	break;

							case "speech":
								if (!UniqueId(signals, id)) break;
								string ssml = "";

								//text variable is necessary to be BML compliant, but its use depends on the used TTS
								//with Cereproc it is not necessary
								//#pragma warning disable 0219  // variable assigned but not used. To avoid useless warning
								string text = "";

								for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
								{
									reader.MoveToAttribute(attInd);
									switch (reader.Name.ToLower())
									{
										case "start": synchros.Add(scheduler.Sched.NewSynchro("start", reader.Value)); break;
										case "end": synchros.Add(scheduler.Sched.NewSynchro("end", reader.Value)); break;
									}
								}

								while (reader.Read())
								{
									if (reader.NodeType == XmlNodeType.EndElement) break;
									if (reader.NodeType != XmlNodeType.Element) continue;
									if (reader.Name.ToLower() == "text")
										text = reader.ReadOuterXml();
									if (reader.Name.ToLower() == "description")
									{
										ssml = reader.ReadInnerXml();
										ssml = ssml.ToString();
										break;
									}
								}
								if (ssml != "")
								{
									var sdata = getGeneralSpeechSynchros(ssml, ref synchros);
									signals.Insert(0, SignalBuilder.Speech(scheduler.Sched, fe, id, sdata, synchros));
								}
								break;
			// BODY CASES NOT IMPLEMENTED YET	
							// case "postureshift":
							// case "posture":
							// 	if (!UniqueId(signals, id)) break;
							// 	string[] type;
							// 	string typeStance = "";
							// 	string facing = "front";
							// 	for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
							// 	{
							// 		reader.MoveToAttribute(attInd);
							// 		switch (reader.Name.ToLower())
							// 		{
							// 			case "start": synchros.Add(scheduler.Sched.NewSynchro("start", reader.Value)); break;
							// 			case "ready": synchros.Add(scheduler.Sched.NewSynchro("ready", reader.Value)); break;
							// 			case "relax": synchros.Add(scheduler.Sched.NewSynchro("relax", reader.Value)); break;
							// 			case "end": synchros.Add(scheduler.Sched.NewSynchro("end", reader.Value)); break;
							// 		}
							// 	}

							// 	while (reader.Read())
							// 	{
							// 		if (reader.NodeType == XmlNodeType.EndElement) break;
							// 		if (reader.NodeType != XmlNodeType.Element) continue;
							// 		switch (reader.Name.ToLower())
							// 		{
							// 			case "stance": //category:stance
							// 				type = reader.GetAttribute("type").ToLower().Split(':');
							// 				typeStance = type[type.Length - 1];
							// 				if (type.Length > 1)
							// 					category = type[0];
							// 				break;
							// 			case "target":
							// 				target = reader.GetAttribute("name").ToLower();
							// 				facing = reader.GetAttribute("facing").ToLower();
							// 				break;
							// 		}
							// 	}

							// 	shift = modality.Equals("postureshift");
							// 	MocapDescription pmd = null;
							// 	PostureDescription post = null;
							// 	if (category.Equals("mocap"))
							// 		mocaps_.TryGetValue(typeStance, out pmd);
							// 	else
							// 		postureShapes_.TryGetValue(typeStance, out post);
							// 	if (post != null || pmd != null)
							// 	{
							// 		addFourSynchros(ref synchros);
							// 		signals.Add(SignalBuilder.Posture(scheduler.Sched, ge, id, shift, typeStance, category, target, facing, synchros, post, pmd));
									
							// 	}
							// 	else Log("Posture description " + typeStance + " has not been found");
							// 	break;

							// case "torsoshift":
							// case "torso":
							// 	if (!UniqueId(signals, id)) break;
							// 	lexeme = "";
							// 	for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
							// 	{
							// 		reader.MoveToAttribute(attInd);
							// 		switch (reader.Name.ToLower())
							// 		{
							// 			case "lexeme": lexeme = reader.Value.ToLower(); break;
							// 			case "amount":
							// 				float.TryParse(reader.Value, ns, ci, out amount);
							// 				break;
							// 			case "start": synchros.Add(scheduler.Sched.NewSynchro("start", reader.Value)); break;
							// 			case "ready": synchros.Add(scheduler.Sched.NewSynchro("ready", reader.Value)); break;
							// 			case "relax": synchros.Add(scheduler.Sched.NewSynchro("relax", reader.Value)); break;
							// 			case "end": synchros.Add(scheduler.Sched.NewSynchro("end", reader.Value)); break;
							// 		}
							// 	}

							// 	shift = modality.Equals("torsoshift");
							// 	TorsoShape tresult;
							// 	if (torsoShapes_.TryGetValue(lexeme, out tresult))
							// 	{
							// 		addFourSynchros(ref synchros);
							// 		signals.Add(SignalBuilder.Torso(scheduler.Sched, id, shift, lexeme, toe, tresult, amount, synchros));
							// 	}
							// 	break;
							default: break;
						}
					}
				}
			}
			//all signals have been read, modify synchro time to be synchronized with speach.
			/*foreach (var s in signals)
			{
				Debug.Log(s.id + " " + s.shape_.getModality());
				foreach (var sy in s.synchros_)
					Debug.Log("ID: " + sy.id + " expr: " + sy.expr + " time: " + sy.time);
			}*/

			Log("Adding BML " + bml_id);
			scheduler.Sched.AddBml(bml_id, composition, signals);

			return 0;
		}


	}
}
