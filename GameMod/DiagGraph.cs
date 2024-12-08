using HarmonyLib;
using Overload;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.ImageEffects;
using static GameMod.Graph.DecayStructure;

/*
Graph.Commands
	// general commands for handling the graph
    graph 		creates a default graph instance
    gshow 		toggles visibility
    gsetorigin 	sets the graph origin to the mouse cursor position
    gsetwidth 	sets the width of the graph
    gsetheight 	sets the height of the graph
    gsetname 	sets the name of the x axis of the graph
    glistds 	lists all currently available decaystructures


	// commands for changing the properties of a single decaystructure
    gdselect 	selects a decaystructure to work on
    gdsetx 		sets which value should be displayed on the x axis
    gdsety 		sets which value should be displayed on the y axis
    gdshow 		toggles the visibility of the selected
    gdcolor		sets the color for the current decaystructure
    gdname 		sets the name for the current decaystructure
*/



namespace GameMod
{

    internal class GraphManager
    {
        /*
        [HarmonyPatch(typeof(GameplayManager), "LoadLevel")]
        internal class MPClientPredictionDebugReset
        {
            static void Postfix(UIElement __instance)
            {
                GameManager.m_display_fps = true;

                // kill bloom to make the graph readable
                BloomOptimized component2 = GameManager.m_viewer.c_camera.GetComponent<BloomOptimized>();
                if (component2)
                {
                    component2.enabled = false;
                }
                SENaturalBloomAndDirtyLens component3 = GameManager.m_viewer.c_camera.GetComponent<SENaturalBloomAndDirtyLens>();
                if (component3)
                {
                    component3.enabled = false;
                }
            }
        }
        */


        [HarmonyPatch(typeof(UIElement), "DrawHUD")]
        internal class GraphManager_GameManager_Starttss
        {
            public static int graphstate = 0;

            private static void Postfix(UIElement __instance)
            {
                GameManager.m_display_fps = true;
                if (GameManager.m_player_ship.m_dying || GameManager.m_player_ship.m_dead)
                {
                    graphstate = 0;
                }
                else
                {
                    graphstate++;
                }

                if (graphstate > 5) // should leave a few frames immediately following a respawn
                {
                    foreach (Graph.DecayStructure cur in Graph.data_graphs)
                    {
                        /*if (cur.name == "Frametime")
                        {
                            cur.AddElement(new float[] { FixFPSCalculation.currentFrameTime });
                            //uConsole.Log("Adding element: " + UIElement.average_fps+"   size:"+cur.size+"  limit:"+cur.element_limit);
                        }*/
                        if (MPSkipClientResimulation.data_available)
                        {
                            // temp hijacking
                            if (cur.name == "Frametime")
                            {
                                cur.AddElement(new float[] { MPSkipClientResimulation.report_time });
                                //cur.AddElement(new float[] { MPServerOptimization.current.rot_dir.sqrMagnitude });
                            }
                            
                            if (cur.name == "PosCorrect")
                            {
                                cur.AddElement(new float[] { MPSkipClientResimulation.err_distsqr });
                            }
                            if (cur.name == "RotCorrect")
                            {
                                cur.AddElement(new float[] { MPSkipClientResimulation.err_angle });
                            }
                            if (cur.name == "TickDiff")
                            {
                                cur.AddElement(new float[] { MPSkipClientResimulation.tick_diff });
                            }
                            /*if (cur.name == "TickDiff")
                            {
                                cur.AddElement(new float[] { Mathf.Abs(Client.m_tick - GameManager.m_local_player.m_last_ackknowledged_server_tick) });
                            }*/
                            if (cur.name == "PosInterp")
                            {
                                cur.AddElement(new float[] { Mathf.Abs(Vector3.Distance(MPErrorSmoothingFix.currPosition, MPErrorSmoothingFix.lastPosition)) });
                            }
                            if (cur.name == "RotInterp")
                            {
                                cur.AddElement(new float[] { Mathf.Abs(Quaternion.Angle(MPErrorSmoothingFix.currRotation, MPErrorSmoothingFix.lastRotation)) });
                            }
                            if (cur.name == "ResimDepth")
                            {
                                cur.AddElement(new float[] { MPSkipClientResimulation.resim_depth });
                            }
                            if (cur.name == "UpdInterval")
                            {
                                cur.AddElement(new float[] { Time.fixedUnscaledDeltaTime });
                            }
                            if (cur.name == "VelDiff")
                            {
                                cur.AddElement(new float[] { MPSkipClientResimulation.vdiff });
                            }
                            if (cur.name == "AngDiff")
                            {
                                cur.AddElement(new float[] { MPSkipClientResimulation.avdiff });
                            }
                            if (cur.name == "MoveVec")
                            {
                                cur.AddElement(new float[] { MPSkipClientResimulation.movevec });
                            }
                            if (cur.name == "TurnVec")
                            {
                                cur.AddElement(new float[] { MPSkipClientResimulation.turnvec });
                            }
                            if (cur.name == "SErrPos")
                            {
                                cur.AddElement(new float[] { MPSkipClientResimulation.errsmooth_pos });
                            }
                            if (cur.name == "SErrRot")
                            {
                                cur.AddElement(new float[] { MPSkipClientResimulation.errsmooth_rot });
                            }

                            if (cur.name == "StatesReceived")
                            {
                                cur.AddElement(new float[] { MPSkipClientResimulation.num_states_received });
                            }

                            if (cur.name == "TickAckGap")
                            {
                                cur.AddElement(new float[] { MPSkipClientResimulation.tick_ack_gap });
                            }
                        }
                        if (MPSkipClientResimulation.overage_available)
                        {
                            if (cur.name == "XOverage")
                            {
                                cur.AddElement(new float[] { MPSkipClientResimulation.xoverage });
                            }
                            if (cur.name == "YOverage")
                            {
                                cur.AddElement(new float[] { MPSkipClientResimulation.yoverage });
                            }
                            if (cur.name == "RotationX")
                            {
                                cur.AddElement(new float[] { MPSkipClientResimulation.xrotation });
                            }
                            if (cur.name == "RotationY")
                            {
                                cur.AddElement(new float[] { MPSkipClientResimulation.yrotation });
                            }
                            if (cur.name == "MouseX")
                            {
                                cur.AddElement(new float[] { MPSkipClientResimulation.mouseX });
                            }
                            if (cur.name == "MouseY")
                            {
                                cur.AddElement(new float[] { MPSkipClientResimulation.mouseY });
                            }
                            if (cur.name == "MouseXRaw")
                            {
                                cur.AddElement(new float[] { MPSkipClientResimulation.mouseXraw });
                            }
                            if (cur.name == "MouseYRaw")
                            {
                                cur.AddElement(new float[] { MPSkipClientResimulation.mouseYraw });
                            }
                        }
                        if (MPSkipClientResimulation.tick_diff_available)
                        {
                            if (cur.name == "TickDiffAck")
                            {
                                cur.AddElement(new float[] { MPSkipClientResimulation.tick_diff_ack });
                            }
                        }
                    }
                }
                MPSkipClientResimulation.data_available = false;
                MPSkipClientResimulation.overage_available = false;
                MPSkipClientResimulation.tick_diff_available = false;

                foreach (Graph gr in graphs)
                {
                    if (gr.visible)
                    {
                        gr.Draw(__instance);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(GameManager), "Start")]
        public static class GraphManager_GameManager_Start
        {
            public static void Postfix(GameManager __instance)
            {
                uConsole.RegisterCommand("ccgraphs", "creates CCFireball's graph presets", new uConsole.DebugCommand(CmdCreateGraphsCC));


                // general commands for handling the graph
                uConsole.RegisterCommand("addgraph", "creates a default graph instance", new uConsole.DebugCommand(CmdCreateGraphInstance));
                uConsole.RegisterCommand("cleargraphs", "clears all graphs", new uConsole.DebugCommand(CmdClearGraphs));
                uConsole.RegisterCommand("glist", "lists all graph instances", new uConsole.DebugCommand(CmdListGraphs));
                uConsole.RegisterCommand("gselect", "selects a graph instance", new uConsole.DebugCommand(CmdSelectGraph));

                uConsole.RegisterCommand("gshow", "toggles visibility", new uConsole.DebugCommand(CmdToggleVisibility));
                uConsole.RegisterCommand("gsetorigin", "sets the graph origin to the mouse cursor position", new uConsole.DebugCommand(CmdOriginToMousePos));
                uConsole.RegisterCommand("gsetoriginx", "sets the graph origin X coordinate", new uConsole.DebugCommand(CmdSetOriginX));
                uConsole.RegisterCommand("gsetoriginy", "sets the graph origin Y coordinate", new uConsole.DebugCommand(CmdSetOriginY));
                uConsole.RegisterCommand("gsetwidth", "sets the width of the graph", new uConsole.DebugCommand(CmdSetGraphWidth));
                uConsole.RegisterCommand("gsetheight", "sets the height of the graph", new uConsole.DebugCommand(CmdSetGraphHeight));
                uConsole.RegisterCommand("gsetname", "sets the name of the x axis of the graph", new uConsole.DebugCommand(CmdSetGraphName));
                uConsole.RegisterCommand("glistds", "lists all currently available decaystructures", new uConsole.DebugCommand(CmdListDecayStructures));


                // commands for changing the properties of a single decaystructure
                uConsole.RegisterCommand("gdselect", "selects a decaystructure to work on", new uConsole.DebugCommand(CmdSelectDecayStructure));
                uConsole.RegisterCommand("gdsetx", "sets which value should be displayed on the x axis", new uConsole.DebugCommand(CmdSetXAxis));
                uConsole.RegisterCommand("gdsety", "sets which value should be displayed on the y axis", new uConsole.DebugCommand(CmdSetYAxis));
                uConsole.RegisterCommand("gdshow", "toggles the visibility of the selected ", new uConsole.DebugCommand(CmdToggleShowSelectedDecayStructure));
                uConsole.RegisterCommand("gdcolor", "sets the color for the current decaystructure", new uConsole.DebugCommand(CmdSetColor));
                uConsole.RegisterCommand("gdname", "sets the name for the current decaystructure", new uConsole.DebugCommand(CmdSetName));

                uConsole.RegisterCommand("players", "lists off all players in order, according to the client", new uConsole.DebugCommand(CmdPlayers));

                uConsole.RegisterCommand("maxqueue", "sets the QualitySettings.maxQueuedFrames amount (default 2)", new uConsole.DebugCommand(CmdSetMaxQueuedFrames));

                // debug commands
                // uConsole.RegisterCommand("genline", "", new uConsole.DebugCommand(CmdLine));
                // uConsole.RegisterCommand("gencurve", "", new uConsole.DebugCommand(CmdCurve));
            }

            private static void CmdPlayers()
            {
                Debug.Log("-------------------------------");
                Debug.Log("CCF Player order is:");
                Debug.Log("-------------------------------");

                int x = 0;
                foreach (Player p in NetworkManager.m_Players)
                {
                    Debug.Log(x + " - " + p.m_mp_name);
                    x++;
                }

                Debug.Log("-------------------------------");
            }

            private static void CmdSetMaxQueuedFrames()
            {
                QualitySettings.maxQueuedFrames = uConsole.GetInt();
                MPSkipClientResimulation.maxQueued = QualitySettings.maxQueuedFrames;
                Debug.Log("-------------------------------");
                Debug.Log("CCF QualitySettings.maxQueuedFrames set to " + MPSkipClientResimulation.maxQueued);
                Debug.Log("-------------------------------");
            }

            public static void CmdCreateGraphsCC()
            {
                if (MPServerOptimization.gaugesOn)
                    return;

                //QualitySettings.maxQueuedFrames = 1;
                //MPSkipClientResimulation.maxQueued = QualitySettings.maxQueuedFrames;
                //Debug.Log("CCF - QualitySettings.maxQueuedFrames " + MPSkipClientResimulation.maxQueued);

                Graph g1 = new Graph(new Vector2(-445f, 333f), 100, 45, "Frametime"); // shown
                //Graph g2 = new Graph(new Vector2(-80f, 345f), 125, 45, "PosCorrect");
                //Graph g3 = new Graph(new Vector2(80f, 345f), 125, 45, "RotCorrect");
                Graph g2 = new Graph(new Vector2(-80f, 345f), 125, 45, "ErrCorrect");
                Graph g4 = new Graph(new Vector2(445f, 333f), 100, 45, "TickDiff"); // shown
                Graph g5 = new Graph(new Vector2(210f, 250f), 100, 45, "PosInterp");
                Graph g6 = new Graph(new Vector2(330f, 250f), 100, 45, "RotInterp");
                Graph g7 = new Graph(new Vector2(330f, 340f), 100, 45, "ResimDepth"); // shown
                //Graph g7 = new Graph(new Vector2(210f, 340f), 100, 45, "ResimDepth");
                //Graph g8 = new Graph(new Vector2(330f, 340f), 100, 45, "UpdInterval");
                Graph g8 = new Graph(new Vector2(-445f, 333f), 100, 45, "UpdInterval");
                Graph g9 = new Graph(new Vector2(210f, 250f), 100, 45, "VelDiff");
                Graph g10 = new Graph(new Vector2(330f, 250f), 100, 45, "AngDiff");
                Graph g11 = new Graph(new Vector2(210f, 250f), 100, 45, "MoveVec");
                Graph g12 = new Graph(new Vector2(330f, 250f), 100, 45, "TurnVec");
                Graph g13 = new Graph(new Vector2(80f, 345f), 125, 45, "SErrPos"); // NOT shown
                Graph g14 = new Graph(new Vector2(-80f, 345f), 125, 45, "SErrRot"); // NOT shown
                Graph g15 = new Graph(new Vector2(-80f, 345f), 125, 45, "Overage"); // NOT shown
                Graph g16 = new Graph(new Vector2(80f, 345f), 125, 45, "Rotation"); // shown
                Graph g17 = new Graph(new Vector2(330f, 250f), 100, 45, "TickAckGap"); // shown
                Graph g18 = new Graph(new Vector2(80f, 345f), 125, 45, "MouseX"); // NOT shown
                Graph g19 = new Graph(new Vector2(-80f, 345f), 125, 45, "MouseY"); // NOT shown

                g1.data_show[0] = true;
                g2.data_show[1] = true;
                g2.data_show[2] = true;
                //g3.data_show[2] = true;
                g4.data_show[3] = true; // TickDiff
                g4.data_show[19] = true; // also shows the tick_diff_ack
                g5.data_show[4] = true;
                g6.data_show[5] = true;
                g7.data_show[6] = true; // Resim
                g7.data_show[18] = true; // also shows the number of received states
                g8.data_show[7] = true;
                g9.data_show[8] = true;
                g10.data_show[9] = true;
                g11.data_show[10] = true;
                g12.data_show[11] = true;
                g13.data_show[12] = true;
                g14.data_show[13] = true;
                g15.data_show[14] = true; // shows both rotation amounts
                g15.data_show[15] = true;
                g16.data_show[16] = true; // shows both overage mouse amounts
                g16.data_show[17] = true;
                g17.data_show[20] = true;
                //g18.data_show[21] = true; // shows X and XRaw
                //g18.data_show[22] = true;
                //g19.data_show[23] = true; // shows Y and YRaw
                //g19.data_show[24] = true;

                graphs.Add(g1);
                graphs.Add(g2);
                //graphs.Add(g3);
                graphs.Add(g4);
                graphs.Add(g5);
                graphs.Add(g6);
                graphs.Add(g7);
                graphs.Add(g8);
                graphs.Add(g9);
                graphs.Add(g10);
                graphs.Add(g11);
                graphs.Add(g12);
                graphs.Add(g13);
                graphs.Add(g14);
                graphs.Add(g15);
                graphs.Add(g16);
                graphs.Add(g17);
                graphs.Add(g18);
                graphs.Add(g19);
                //graphs.Add(g18);

                //g1.visible = false; // # of active graphs getting too much
                //g2.visible = false;
                //g3.visible = false;
                g5.visible = false; // interesting, but not super useful
                g6.visible = false; // interesting, but not super useful
                g8.visible = false;
                g9.visible = false; 
                g10.visible = false;
                g11.visible = false;
                g12.visible = false;
                g13.visible = false;
                g14.visible = false;
                g15.visible = false;
                //g16.visible = false;
                g18.visible = false;
                g19.visible = false;

                // currently showing g1, g4, g7, g15, g16

                g = g1;

                MPServerOptimization.gaugesOn = true;
            }

            private static void CmdCreateGraphInstance()
            {
                Graph gr = new Graph(new Vector2(532f, -175), 150, 75, "default");
                graphs.Add(gr);
                if (g == null)
                {
                    g = gr;
                }
            }
            private static void CmdClearGraphs()
            {
                graphs.Clear();
                g = null;
            }
            private static void CmdListGraphs()
            {
                int index = 0;
                foreach (Graph gr in graphs)
                {
                    uConsole.Log(index++ + ": " + gr.name);
                }
            }
            private static void CmdSelectGraph()
            {
                int index = uConsole.GetInt();
                if (graphs.Count == 0 || index < 0 || index >= graphs.Count )
                    return;

                g = graphs[index];
                uConsole.Log(g.name + " selected");
            }
            private static void CmdToggleVisibility()
            {
                if (g == null)
                    return;

                g.visible = !g.visible;
                uConsole.Log(g.name + " is now " + (g.visible ? "visible" : "hidden"));
            }
            private static void CmdOriginToMousePos()
            {
                if (g == null)
                    return;

                g.origin = UIManager.m_mouse_pos;
                uConsole.Log("Graph origin set to " + g.origin);
            }
            private static void CmdSetOriginX()
            {
                if (g == null)
                    return;

                g.origin.x = uConsole.GetFloat();
                uConsole.Log("Graph origin set to " + g.origin);
            }
            private static void CmdSetOriginY()
            {
                if (g == null)
                    return;

                g.origin.y = uConsole.GetFloat();
                uConsole.Log("Graph origin set to " + g.origin);
            }
            private static void CmdSetGraphWidth()
            {
                int range = uConsole.GetInt();
                
                if (g == null)
                    return;

                if (range > 0)
                {
                    g.xrange = range;
                    g.qxrange = range / 4;
                }
            }
            private static void CmdSetGraphHeight()
            {
                if (g == null)
                    return;

                int range = uConsole.GetInt();
                if (range > 0)
                {
                    g.yrange = range;
                    g.qyrange = range / 4;
                }
            }
            private static void CmdSetGraphName()
            {
                if (g == null)
                    return;

                g.name = uConsole.GetString();
            }
            private static void CmdListDecayStructures()
            {
                int index = 0;
                foreach(Graph.DecayStructure curr in Graph.data_graphs)
                {
                    uConsole.Log(index++ + ": " + curr.name + "  (" + curr.size + ")");
                }
            }


            private static void CmdSelectDecayStructure()
            {
                int index = uConsole.GetInt();
                if(index >= 0 && index < Graph.data_graphs.Count)
                {
                    selected_ds = index;
                }
            }
            private static void CmdSetXAxis()
            {
                Graph.data_graphs[selected_ds].draw_x = uConsole.GetInt();
            }
            private static void CmdSetYAxis()
            {
                Graph.data_graphs[selected_ds].draw_y = uConsole.GetInt();
            }
            private static void CmdToggleShowSelectedDecayStructure()
            {
                //Graph.data_graphs[selected_ds].show = !Graph.data_graphs[selected_ds].show;
                g.data_show[selected_ds] = !g.data_show[selected_ds];
            }
            private static void CmdSetColor()
            {
                string s = uConsole.GetString();
                int n = 0;
                if (s != null)
                {
                    if (s.StartsWith("#"))
                        s = s.Substring(1);
                    if ((s.Length != 3 && s.Length != 6) ||
                        !int.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out n))
                    {
                        uConsole.Log("Invalid color: " + s);
                        return;
                    }
                    if (s.Length == 3)
                        n = ((((n >> 8) & 0xf) * 0x11) << 16) | ((((n >> 4) & 0xf) * 0x11) << 8) | ((n & 0xf) * 0x11);
                }
                Graph.data_graphs[selected_ds].color = n;
            }
            private static void CmdSetName()
            {
                Graph.data_graphs[selected_ds].name = uConsole.GetString();
            }
        }

        private static int selected_ds = 0;
        public static Graph g;
        public static List<Graph> graphs = new List<Graph>();
    }



    class Graph
    {
        // general variables
        public string name;
        public Vector2 origin = new Vector2(532f, -175);
        public int xrange = 150;
        public int yrange = 75;
        public int qxrange;
        public int qyrange;

        public bool visible = true;
        public float max_x, max_y; // hold the current maximum value of all displayed graphs for the x and y axis

        public static List<DecayStructure> data_graphs = new List<DecayStructure>(); // get populated through InputExperiments.cs
        public bool[] data_show;

        public Graph(Vector2 origin_, int x_, int y_, string name_)
        {
            origin = origin_;
            xrange = x_;
            yrange = y_;
            qxrange = xrange / 4;
            qyrange = yrange / 4;
            name = name_;

            data_show = new bool[data_graphs.Count];
        }

        static Graph()
        {
            // 0
            data_graphs.Add(new DecayStructure(700));
            //data_graphs.Add(new DecayStructure(1400));
            data_graphs[data_graphs.Count - 1].name = "Frametime";
            data_graphs[data_graphs.Count - 1].draw_x = -1;
            data_graphs[data_graphs.Count - 1].draw_y = 0;
            // 1
            data_graphs.Add(new DecayStructure(700));
            data_graphs[data_graphs.Count - 1].name = "PosCorrect";
            data_graphs[data_graphs.Count - 1].draw_x = -1;
            data_graphs[data_graphs.Count - 1].draw_y = 0;
            // 2
            data_graphs.Add(new DecayStructure(700));
            data_graphs[data_graphs.Count - 1].name = "RotCorrect";
            data_graphs[data_graphs.Count - 1].draw_x = -1;
            data_graphs[data_graphs.Count - 1].draw_y = 0;
            data_graphs[data_graphs.Count - 1].color = 11534501; // pink
            // 3
            data_graphs.Add(new DecayStructure(700));
            data_graphs[data_graphs.Count - 1].name = "TickDiff";
            data_graphs[data_graphs.Count - 1].draw_x = -1;
            data_graphs[data_graphs.Count - 1].draw_y = 0;
            // 4
            data_graphs.Add(new DecayStructure(700));
            data_graphs[data_graphs.Count - 1].name = "PosInterp";
            data_graphs[data_graphs.Count - 1].draw_x = -1;
            data_graphs[data_graphs.Count - 1].draw_y = 0;
            // 5
            data_graphs.Add(new DecayStructure(700));
            data_graphs[data_graphs.Count - 1].name = "RotInterp";
            data_graphs[data_graphs.Count - 1].draw_x = -1;
            data_graphs[data_graphs.Count - 1].draw_y = 0;
            // 6
            data_graphs.Add(new DecayStructure(700));
            data_graphs[data_graphs.Count - 1].name = "ResimDepth";
            data_graphs[data_graphs.Count - 1].draw_x = -1;
            data_graphs[data_graphs.Count - 1].draw_y = 0;
            // 7
            data_graphs.Add(new DecayStructure(700));
            data_graphs[data_graphs.Count - 1].name = "UpdInterval";
            data_graphs[data_graphs.Count - 1].draw_x = -1;
            data_graphs[data_graphs.Count - 1].draw_y = 0;
            // 8
            data_graphs.Add(new DecayStructure(700));
            data_graphs[data_graphs.Count - 1].name = "VelDiff";
            data_graphs[data_graphs.Count - 1].draw_x = -1;
            data_graphs[data_graphs.Count - 1].draw_y = 0;
            // 9
            data_graphs.Add(new DecayStructure(700));
            data_graphs[data_graphs.Count - 1].name = "AngDiff";
            data_graphs[data_graphs.Count - 1].draw_x = -1;
            data_graphs[data_graphs.Count - 1].draw_y = 0;
            // 10
            data_graphs.Add(new DecayStructure(700));
            data_graphs[data_graphs.Count - 1].name = "MoveVec";
            data_graphs[data_graphs.Count - 1].draw_x = -1;
            data_graphs[data_graphs.Count - 1].draw_y = 0;
            // 11
            data_graphs.Add(new DecayStructure(700));
            data_graphs[data_graphs.Count - 1].name = "TurnVec";
            data_graphs[data_graphs.Count - 1].draw_x = -1;
            data_graphs[data_graphs.Count - 1].draw_y = 0;
            // 12
            data_graphs.Add(new DecayStructure(700));
            data_graphs[data_graphs.Count - 1].name = "SErrPos";
            data_graphs[data_graphs.Count - 1].draw_x = -1;
            data_graphs[data_graphs.Count - 1].draw_y = 0;
            // 13
            data_graphs.Add(new DecayStructure(700));
            data_graphs[data_graphs.Count - 1].name = "SErrRot";
            data_graphs[data_graphs.Count - 1].draw_x = -1;
            data_graphs[data_graphs.Count - 1].draw_y = 0;
            //data_graphs[data_graphs.Count - 1].color = 16736501; // hot pink
            // 14
            data_graphs.Add(new DecayStructure(700));
            data_graphs[data_graphs.Count - 1].name = "XOverage";
            data_graphs[data_graphs.Count - 1].draw_x = -1;
            data_graphs[data_graphs.Count - 1].draw_y = 0;
            // 15
            data_graphs.Add(new DecayStructure(700));
            data_graphs[data_graphs.Count - 1].name = "YOverage";
            data_graphs[data_graphs.Count - 1].draw_x = -1;
            data_graphs[data_graphs.Count - 1].draw_y = 0;
            data_graphs[data_graphs.Count - 1].color = 11534501; // pink
            //data_graphs[data_graphs.Count - 1].color = 16736501; // hot pink
            // 16
            data_graphs.Add(new DecayStructure(700));
            data_graphs[data_graphs.Count - 1].name = "RotationX";
            data_graphs[data_graphs.Count - 1].draw_x = -1;
            data_graphs[data_graphs.Count - 1].draw_y = 0;
            // 17
            data_graphs.Add(new DecayStructure(700));
            data_graphs[data_graphs.Count - 1].name = "RotationY";
            data_graphs[data_graphs.Count - 1].draw_x = -1;
            data_graphs[data_graphs.Count - 1].draw_y = 0;
            data_graphs[data_graphs.Count - 1].color = 11534501; // pink
            // 18
            data_graphs.Add(new DecayStructure(700));
            data_graphs[data_graphs.Count - 1].name = "StatesReceived";
            data_graphs[data_graphs.Count - 1].draw_x = -1;
            data_graphs[data_graphs.Count - 1].draw_y = 0;
            data_graphs[data_graphs.Count - 1].color = 11534501; // pink
            // 19
            data_graphs.Add(new DecayStructure(700));
            data_graphs[data_graphs.Count - 1].name = "TickDiffAck";
            data_graphs[data_graphs.Count - 1].draw_x = -1;
            data_graphs[data_graphs.Count - 1].draw_y = 0;
            data_graphs[data_graphs.Count - 1].color = 11534501; // pink
            // 20
            data_graphs.Add(new DecayStructure(700));
            data_graphs[data_graphs.Count - 1].name = "TickAckGap";
            data_graphs[data_graphs.Count - 1].draw_x = -1;
            data_graphs[data_graphs.Count - 1].draw_y = 0;
            //data_graphs[data_graphs.Count - 1].color = 11534501; // pink
            // 21
            data_graphs.Add(new DecayStructure(700));
            data_graphs[data_graphs.Count - 1].name = "MouseX";
            data_graphs[data_graphs.Count - 1].draw_x = -1;
            data_graphs[data_graphs.Count - 1].draw_y = 0;
            // 22
            data_graphs.Add(new DecayStructure(700));
            data_graphs[data_graphs.Count - 1].name = "MouseXRaw";
            data_graphs[data_graphs.Count - 1].draw_x = -1;
            data_graphs[data_graphs.Count - 1].draw_y = 0;
            data_graphs[data_graphs.Count - 1].color = 11534501; // pink
            // 23
            data_graphs.Add(new DecayStructure(700));
            data_graphs[data_graphs.Count - 1].name = "MouseY";
            data_graphs[data_graphs.Count - 1].draw_x = -1;
            data_graphs[data_graphs.Count - 1].draw_y = 0;
            // 24
            data_graphs.Add(new DecayStructure(700));
            data_graphs[data_graphs.Count - 1].name = "MouseYRaw";
            data_graphs[data_graphs.Count - 1].draw_x = -1;
            data_graphs[data_graphs.Count - 1].draw_y = 0;
            data_graphs[data_graphs.Count - 1].color = 11534501; // pink

            uConsole.Log("GRAPHING - " + data_graphs.Count + " DataGraphs initialized");
        }


        internal class DecayStructure
        {
            public Element first, last;
            public int size = 0;
            public bool alltimeMaximum = false;
            public int element_limit = 150; // -1 for no limit

            public string name = "";
            public int color = 42069;
            //public bool show = false;
            public int draw_x, draw_y;

            public DecayStructure(int limit)
            {
                element_limit = limit;
            }

            public void AddElement(float[] val)
            {
                if (size == 0)
                {
                    first = new Element(val);
                    last = first;
                    size++;
                }
                else
                {
                    last.next = new Element(val);
                    last = last.next;
                    size++;
                }
                if (size > element_limit && element_limit != -1)
                {
                    first = first.next;
                    size--;
                }
            }

            public float findMaximumForIndex(int index)
            {
                Element curr = first;
                if (curr == null)
                    return 0;

                float max = curr.getFloatAtIndex(index);
                while (curr.next != null)
                {
                    if (curr.next.getFloatAtIndex(index) > max)
                    {
                        max = curr.next.getFloatAtIndex(index);
                    }
                    curr = curr.next;
                }
                return max;
            }

            public class Element
            {
                public float[] values;
                public Element next = null;

                public Element(float[] val)
                {
                    if (val.Length > 0)
                    {
                        values = val;
                    }
                    else
                    {
                        Debug.Log("Error at InputExperiment.DecayStructure.Element: empty array passed to constructor ");
                        values = new float[] { 1.7625f };
                    }
                }

                public float getMaximum()
                {
                    float max = values[0];
                    foreach (float value in values)
                    {
                        if (value > max) max = value;
                    }
                    return max;
                }

                public float getFloatAtIndex( int index)
                {
                    
                    if( index < values.Length && index >= 0 )
                    {
                        return values[index];
                    }
                    return -1.7625f;
                }
            }
        }


        public void Draw(UIElement instance)
        {
            if(visible)
            {
                DrawStatsAxes(instance, origin, xrange, yrange);
                // figure out the maximum bounds for all graphs
                int index = 0;

                //float local_max_y = -1f;
                foreach (DecayStructure curr in data_graphs)
                {
                    if (data_show[index])
                    {
                        float cur_x = curr.findMaximumForIndex(curr.draw_x);
                        float cur_y = curr.findMaximumForIndex(curr.draw_y);
                        if (cur_x > max_x) max_x = cur_x;
                        if (cur_y > max_y) max_y = cur_y;

                        DrawDecayStructureToGraph(curr, origin, instance, max_y);
                    }
                    index++;
                }

                instance.DrawStringSmall("Max: " + max_y, origin, 0.4f, StringOffset.CENTER, UIManager.m_col_ui0, 1f, -1f);
                max_y = 0f;
                /*
                // draw all graphs that are marked to be shown
                foreach (DecayStructure curr in data_graphs)
                {
                    if (data_show[index])
                    {
                        DrawDecayStructureToGraph(curr, origin, instance);
                    }
                }*/

                index++;
            }
        }

        public void DrawStatsAxes(UIElement __instance, Vector2 initial_pos, int xrange, int yrange)
        {
            Vector2 zero = initial_pos;
            Color c = UIManager.m_col_ub2;
            c.a = 1f * 0.75f;
            zero.y -= qyrange;
            UIManager.DrawQuadBarHorizontal(zero, 1f, 1f, xrange, c, 4);
            zero.y += qyrange;
            UIManager.DrawQuadBarHorizontal(zero, 1f, 1f, xrange, c, 4);
            zero.y += qyrange;
            UIManager.DrawQuadBarHorizontal(zero, 1f, 1f, xrange, c, 4);
            zero.y = initial_pos.y;

            zero.x -= qxrange;
            UIManager.DrawQuadBarVertical(zero, 1f, 1f, yrange, c, 4);
            zero.x += qxrange;
            UIManager.DrawQuadBarVertical(zero, 1f, 1f, yrange, c, 4);
            zero.x += qxrange;
            UIManager.DrawQuadBarVertical(zero, 1f, 1f, yrange, c, 4);

            zero.x = initial_pos.x;
            UIManager.DrawFrameEmptyCenter(zero, 4f, 4f, xrange - (5 + ((-500 + xrange) / 50)), yrange - (5 + ((-200 + yrange) / 20)), c, 8);
            c = UIManager.m_col_ui0;
            c.a = 0.8f;

            zero = initial_pos;
            zero.y += yrange * 0.7f;
            zero.x += xrange * 0.55f;
            //__instance.DrawStringSmall("[" + RUtility.ConvertFloatToSeconds(GameplayManager.m_game_time, false) + "]", zero, 0.3f, StringOffset.RIGHT, UIManager.m_col_ui0, 1f, -1f);
            zero.x -= xrange * 1.1f;
            //__instance.DrawStringSmall("[0:00]", zero, 0.3f, StringOffset.LEFT, UIManager.m_col_ui0, 1f, -1f);
            zero.x = initial_pos.x;
            __instance.DrawStringSmall(name, zero, 0.3f, StringOffset.CENTER, UIManager.m_col_ui0, 1f, -1f);
        }

        public void DrawDecayStructureToGraph(DecayStructure ds, Vector2 initial_pos, UIElement instance, float local_max_y)
        {
            //float local_max_y = -1f;
            if (ds.size > 0)
            {
                Color color = new Color((ds.color >> 16) / 255f, ((ds.color >> 8) & 0xff) / 255f, (ds.color & 0xff) / 255f);
                float local_max_x = 0f; 
                if(ds.draw_x != -1)
                {
                    local_max_x = ds.findMaximumForIndex(ds.draw_x);
                }
                // local_max_y = ds.findMaximumForIndex(ds.draw_y);
                // float local_max_y = ds.findMaximumForIndex(ds.draw_y);
                float resolution = -1;
                if (ds.draw_x == -1)
                {
                    resolution = ds.element_limit != -1 ? (float)(xrange) / ds.element_limit : (float)xrange / ds.size;
                }
                
                Vector2 start = Vector2.zero;
                Vector2 end = Vector2.zero;

                Element current = ds.first;
                start.y = (initial_pos.y + yrange / 2) - (current.values[ds.draw_y] / local_max_y) * yrange;
                start.x = (initial_pos.x - xrange / 2);

                while (current.next != null)
                {
                    current = current.next;

                    end.y = (initial_pos.y + yrange / 2) - (current.values[ds.draw_y] / local_max_y) * yrange;
                    if (resolution != -1)
                    {
                        end.x = start.x + resolution;
                    }
                    else
                    {
                        end.x = (initial_pos.x - xrange / 2) + (current.values[ds.draw_x] / local_max_x) * xrange;
                    }

                    UIManager.DrawQuadCenterLine(start, end, 0.4f, 0f, color, 4);
                    start = end;
                }

                //instance.DrawStringSmall("Max: " + local_max_y, initial_pos, 0.4f, StringOffset.CENTER, UIManager.m_col_ui0, 1f, -1f);
                //instance.DrawStringSmall("Max: " + local_max_y, initial_pos, 0.4f, StringOffset.CENTER, Color.(ds.color), 1f, -1f);
            }
            //return local_max_y;
        }
    }
}
