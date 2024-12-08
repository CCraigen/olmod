using HarmonyLib;
using Overload;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

namespace GameMod
{
    public static class MPSkipClientResimulation
    {
        public static float err_distsqr = 0f;
        public static float err_angle = 0f;
        public static int tick_diff = 0;
        public static bool data_available = false;
        public static int resim_depth = 0;
        public static float avdiff = 0f;
        public static float vdiff = 0f;
        public static float movevec = 0f;
        public static float turnvec = 0f;
        public static float errsmooth_pos = 0f;
        public static float errsmooth_rot = 0f;
        public static float xoverage = 0f;
        public static float yoverage = 0f;
        public static float xrotation = 0f;
        public static float yrotation = 0f;
        public static bool overage_available = false;
        public static int num_states_received = 0;
        public static int tick_diff_ack = 0;
        public static bool tick_diff_available = false;
        public static float last_time;
        public static float report_time = 0f;
        public static int tick_ack_gap = 0;

        public static int maxQueued = 0;

        public static float mouseX = 0f;
        public static float mouseY = 0f;
        public static float mouseXraw = 0f;
        public static float mouseYraw = 0f;

        public static float FIXEDUPDATE
        {
            get
            {
                if (GameplayManager.IsDedicatedServer())
                    return CLIENTUPDATE;
                else
                    return Time.fixedDeltaTime;
            }
        }

        public static float FIXEDUPDATE_FLEX
        {
            get
            {
                if (GameplayManager.IsDedicatedServer())
                    return SERVERUPDATE;
                else
                    return CLIENTUPDATE;
            }
        }

        public const float SERVERUPDATE = 1f / 120f;
        public const float CLIENTUPDATE = 1f / 60f;


        // Client.ReconcileServerPlayerState is called at every fixed physics tick
        // it will replay the local simulation from the last packet we've seen
        // from the server.
        //
        // This means that we get a ping-dependent amount of physics resimulation steps
        // at each single physics tick, which can be be quite expensive.
        //
        // However, the resimulation is only necessary if the ship's state seen by the
        // server differs from our client's view (for example by collisions with
        // other ships or being hit by projectiles, which aren't accurate on the client
        // due to the lag).
        //
        // This patch skips the client side resimulation if we detect that our positon
        // and rotation we had in the past at some tick does not differ from the
        // position and rotation the server sent back to us for that particular tick.
        //
        // NOTE: we do not compare any other members of the PlayerState here.
        // The rationale behind this is that in practice, the server never really
        // changes those without also affecting position and rotation, and even if
        // it does, it will lead to a positional or rotational error during one of the
        // next ticks, so the sync will happen anyway, just one or two ticks later.
        [HarmonyPatch(typeof(Client), "ReconcileServerPlayerState")]
        private class MPSkipClientResimIfNotNecessary_ReconcileServerPlayerState
        {
            private static bool Prefix(Player player, PlayerState[] ___m_player_state_history)
            {
                int newQueued = QualitySettings.maxQueuedFrames;
                if (maxQueued != newQueued)
                {
                    maxQueued = newQueued;
                    Debug.Log("CCF - QualitySettings.maxQueuedFrames has CHANGED to " + newQueued);
                }

                resim_depth = 0;
                num_states_received = 0;
                tick_ack_gap = 0;
                data_available = true;

                Rigidbody rb = player.c_player_ship.c_rigidbody;
                avdiff = rb.angularVelocity.sqrMagnitude;
                vdiff = rb.velocity.sqrMagnitude;

                movevec = player.cc_move_vec.sqrMagnitude;
                turnvec = player.cc_turn_vec.sqrMagnitude;

                //errsmooth_pos = player.m_error_pos.sqrMagnitude;
                //errsmooth_rot = Mathf.Abs(Quaternion.Angle(player.m_error_rot, Quaternion.identity));

                if (Client.m_PendingPlayerStateMessages.Count < 1)
                {
                    // nothing to do, we can skip the original as it doesn't do anything anyway
                    return false;
                }
                // the original ReconcileServerPlayerState removes all elements from the queue
                // and uses the last one, if there is one.
                // We remove all but the last one, and only peek at that
                while (Client.m_PendingPlayerStateMessages.Count > 1)
                {
                    // Client.m_PendingPlayerStateMessages.Dequeue();
                    num_states_received++;
                    Client.m_PendingPlayerStateMessages.Dequeue();

                }
                num_states_received++;
                PlayerStateToClientMessage msg = Client.m_PendingPlayerStateMessages.Peek();

                //tick_ack_gap = Mathf.Abs(Client.m_last_acknowledged_tick - msg.m_tick);
                tick_ack_gap = Client.m_last_acknowledged_tick - msg.m_tick;

                tick_diff = Client.m_tick - msg.m_tick;
                //if (msg.m_tick < Client.m_tick)
                if (tick_diff > 0)
                {
                    //data_available = true;
                    PlayerState s = ___m_player_state_history[msg.m_tick & 1023];
                    if (s != null)
                    {
                        //float err_distsqr = (msg.m_player_pos - s.m_pos).sqrMagnitude;
                        //float err_angle = Mathf.Abs(Quaternion.Angle(msg.m_player_rot, s.m_rot));
                        //bool skip = (err_distsqr < 0.0004f) && (err_angle < 0.5f);
                        err_distsqr = (msg.m_player_pos - s.m_pos).sqrMagnitude;
                        err_angle = Mathf.Abs(Quaternion.Angle(msg.m_player_rot, s.m_rot));
                        bool skip = (err_distsqr < 0.0004f) && (err_angle < 0.5f);
                        if (skip)
                        {
                            // we are skipping the resimulation, consume the message right here
                            if (Client.m_last_acknowledged_tick < msg.m_tick)
                            {
                                Client.m_last_acknowledged_tick = msg.m_tick;
                            }

                            err_distsqr = 0f;
                            err_angle = 0f;

                            //player.c_player_ship.m_boost_heat = msg.m_boost_heat; // CCF
                            //player.c_player_ship.m_boost_overheat_timer = msg.m_boost_overheat_timer; // CCF

                            // CCF Optimization patch -- these are technically wrong and delay your boost energy recharge etc. but there isn't a good way to get them up to date if resim is being skipped. They will be a few frames out of sync.
                            // If we're using the new client-side physics, we can skip this, as it's handled in SniperPacket's OnPlayerAddResource() now. This *will* be in sync.
                            if (!MPServerOptimization.enabled)
                            {
                                player.c_player_ship.m_boost_heat = msg.m_boost_heat;
                                player.c_player_ship.m_boost_overheat_timer = msg.m_boost_overheat_timer;
                            }

                            Client.m_PendingPlayerStateMessages.Dequeue();

                            return false;
                        }
                    }
                }
                return true;
            }

            private static void Postfix(Player player)
            {
                Rigidbody rb = player.c_player_ship.c_rigidbody;
                avdiff = Mathf.Abs(avdiff - rb.angularVelocity.sqrMagnitude);
                vdiff = Mathf.Abs(vdiff - rb.velocity.sqrMagnitude);

                movevec = Mathf.Abs(movevec - player.cc_move_vec.sqrMagnitude);
                turnvec = Mathf.Abs(turnvec - player.cc_turn_vec.sqrMagnitude);
            }
        }

        [HarmonyPatch(typeof(NetworkSim), "SimulateMessageFromThePast")]
        private class MPSkipClientResimIfNotNecessary_SimulateMessageFromThePast
        {
            private static void Postfix()
            {
                resim_depth++;
            }
        }

        [HarmonyPatch(typeof(Player), "UpdateSmoothingError")]
        public static class MPSkipClientResimIfNotNecessary_UpdateSmoothingError
        {
            public static bool Prefix(Player __instance)
            {
                //resim_depth++; // repurposing temporarily

                errsmooth_pos = __instance.m_error_pos.sqrMagnitude;
                errsmooth_rot = Mathf.Abs(Quaternion.Angle(__instance.m_error_rot, Quaternion.identity));

                if (__instance.m_error_pos != Vector3.zero)
                {
                    __instance.m_error_pos *= Player.POS_SMOOTH_FACTOR; // 0.9f
                    //__instance.m_error_pos *= 0.95f;
                    if (__instance.m_error_pos.magnitude < 1E-05f)
                    //if (__instance.m_error_pos.magnitude < 1f)
                    {
                        __instance.m_error_pos = Vector3.zero;
                    }
                }
                if (__instance.m_error_rot != Quaternion.identity)
                {
                    __instance.m_error_rot = Quaternion.Slerp(__instance.m_error_rot, Quaternion.identity, Player.ROT_SMOOTH_FACTOR); // 0.1f, effectively 0.9f
                    //__instance.m_error_rot = Quaternion.Slerp(__instance.m_error_rot, Quaternion.identity, 0.05f);
                    float num = Mathf.Abs(Quaternion.Angle(__instance.m_error_rot, Quaternion.identity));
                    if (num < 0.0001f)
                    //if (num < 1f)
                    {
                        __instance.m_error_rot = Quaternion.identity;
                    }
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(PlayerShip), "FixedUpdateProcessControls")]
        public static class MPPlayerShip_FixedUpdateProcessControls
        {
            public static void Prefix(PlayerShip __instance)
            {
                report_time = Time.realtimeSinceStartup - last_time;
                //fupc_runs++;
                overage_available = true;
                xoverage = Mathf.Abs(__instance.m_turn_overage_yaw);
                yoverage = Mathf.Abs(__instance.m_turn_overage_pitch);
            }

            public static void Postfix(PlayerShip __instance)
            {
                Vector3 localAngularVelocity = __instance.c_transform.InverseTransformDirection(__instance.c_rigidbody.angularVelocity);
                xrotation = Mathf.Abs(localAngularVelocity.y); // right hand rule - x rotation is curling around the Y axis, +y = left x
                yrotation = Mathf.Abs(localAngularVelocity.x); // right hand rule - y rotation is curling around the X axis, +x = up y (I think)
                last_time = Time.realtimeSinceStartup;
            }
        }

        /*
        [HarmonyPatch(typeof(PlayerShip), "FixedUpdateProcessControlsInternal")]
        public static class MPPlayerShip_FixedUpdateProcessControlsInternal
        {
            public static void Prefix(PlayerShip __instance)
            {
                report_time = Time.realtimeSinceStartup - last_time;
                overage_available = true;
                xoverage = Mathf.Abs(__instance.m_turn_overage_yaw);
                yoverage = Mathf.Abs(__instance.m_turn_overage_pitch);
            }

            public static void Postfix(PlayerShip __instance)
            {
                Vector3 localAngularVelocity = __instance.c_transform.InverseTransformDirection(__instance.c_rigidbody.angularVelocity);
                xrotation = Mathf.Abs(localAngularVelocity.y); // right hand rule - x rotation is curling around the Y axis, +y = left x
                yrotation = Mathf.Abs(localAngularVelocity.x); // right hand rule - y rotation is curling around the X axis, +x = up y (I think)
                last_time = Time.realtimeSinceStartup;
            }
        }
        */

        [HarmonyPatch(typeof(Client), "OnAckInputsToClient")]
        public static class MP_OnAckInputsToClient
        {
            public static bool Prefix(NetworkMessage msg)
            {
                tick_diff_available = true;

                IntegerMessage integerMessage = msg.ReadMessage<IntegerMessage>();
                int incoming_tick = integerMessage.value;

                tick_diff_ack = Client.m_tick - incoming_tick;

                if (incoming_tick > Client.m_last_acknowledged_tick && incoming_tick <= Client.m_tick)
                {
                    Client.m_last_acknowledged_tick = incoming_tick;
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(GameManager), "FixedUpdate")]
        public static class MP_GameManager_FixedUpdate
        {
            //static bool tick = true;
            public static bool Prefix()
            {
                if (GameManager.m_fatal_error_message == null)
                {
                    if (GameManager.m_game_state == GameManager.GameState.GAMEPLAY)
                    {
                        GameplayManager.FixedUpdate();
                        UpdateDynamicManager.FixedUpdateDynamicObjects();
                    }
                    Overload.NetworkManager.FixedUpdate();
                    PlayerShip.FixedUpdateAll();
                }
                return false;
            }
        }

        /*
        // TICK EXPERIMENT BEGINS HERE
        [HarmonyPatch(typeof(PlayerShip), "FixedUpdateAll")]
        public static class MP_PlayerShip_FixedUpdateAll
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
            {
                foreach (var code in codes)
                {
                    if (code.opcode == OpCodes.Call && code.operand == AccessTools.Method(typeof(Time), "get_fixedDeltaTime"))
                    {
                        code.operand = AccessTools.Method(typeof(MPSkipClientResimulation), "get_FIXEDUPDATE");
                    }
                    yield return code;
                }
            }
        }

        // TICK EXPERIMENT CONTINUES
        [HarmonyPatch(typeof(GameManager), "FixedUpdate")]
        public static class MP_GameManager_FixedUpdate
        {
            static bool tick = true;
            public static bool Prefix()
            {
                if (GameManager.m_fatal_error_message == null)
                {
                    if (!tick && GameplayManager.IsDedicatedServer())
                    {
                        tick = true;
                    }
                    else
                    {
                        if (GameManager.m_game_state == GameManager.GameState.GAMEPLAY)
                        {
                            GameplayManager.FixedUpdate();
                            UpdateDynamicManager.FixedUpdateDynamicObjects();
                        }
                        Overload.NetworkManager.FixedUpdate();
                        //PlayerShip.FixedUpdateAll();
                        tick = false;
                    }

                    PlayerShip.FixedUpdateAll();
                }
                return false;
            }
        }

        // TICK EXPERIMENT CONTINUES
        [HarmonyPatch(typeof(GameManager), "Update")]
        public static class MP_GameManager_Update
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
            {
                int count = 4;

                foreach (var code in codes)
                {
                    if (code.opcode == OpCodes.Ldloc_1) // check for the isMultiplayer check on line 925
                    {
                        count = 0;
                    }
                    else if (count < 4 && code.opcode == OpCodes.Ldc_R4) // change the tick rate for the 3 floats being set
                    {
                        count++;
                        code.opcode = OpCodes.Call;
                        code.operand = AccessTools.Method(typeof(MPSkipClientResimulation), "get_FIXEDUPDATE_FLEX"); // This is the -only- place we want it to actually set to a 120hz tick. Everything else should think it's at 60.
                    }
                    else if (code.opcode == OpCodes.Call && code.operand == AccessTools.Method(typeof(Time), "get_fixedDeltaTime")) // this targets only the RUtility.FRAMETIME_FIXED shortcut set calls
                        {
                            code.operand = AccessTools.Method(typeof(MPSkipClientResimulation), "get_FIXEDUPDATE");
                        }
                    yield return code;
                }
            }
        }
        */

        // TICK EXPERIMENT CONTINUES
        [HarmonyPatch(typeof(Server), "AccelerateInputs")]
        public static class MP_ServerAccelerate
        {
            static Player max_player;
            static int player_id = 0;
            static int last_id = 0;

            static int[] prev = new int[1024]; // overkill woo

            public static bool Prefix()
            {
                int num = 0;

                foreach (Player player in Overload.NetworkManager.m_Players)
                {
                    if (player != null && !player.m_spectator)
                    {
                        player.m_input_deficit = 60; // from MPClientExtrapolation

                        //if (player.m_InputToProcessOnServer.Count > 1) // THIS IS TO TEST DIFFERENT CONTROL BUFFER AMOUNTS
                        if (player.m_InputToProcessOnServer.Count > MPServerOptimization.InputBufferLength) // THIS IS TO TEST DIFFERENT CONTROL BUFFER AMOUNTS
                        //if (player.m_InputToProcessOnServer.Count >= 4)
                        {
                            player.m_num_inputs_to_accelerate = Mathf.Max(1, Mathf.FloorToInt((float)player.m_InputToProcessOnServer.Count * 0.1f));
                        }
                        else
                        {
                            player.m_num_inputs_to_accelerate = 0;
                        }
                        if (!player.c_player_ship.m_dying && !player.c_player_ship.m_dead)
                        {
                            player.m_num_inputs_to_accelerate = Mathf.Clamp(player.m_num_inputs_to_accelerate, 0, player.m_input_deficit);
                        }
                        player.m_input_deficit -= player.m_num_inputs_to_accelerate;
                        player.m_input_deficit = Mathf.Clamp(player.m_input_deficit, 0, int.MaxValue);
                        if (player.m_num_inputs_to_accelerate > num)
                        {
                            player_id = player.connectionToClient.connectionId; // new
                            max_player = player; // new

                            num = player.m_num_inputs_to_accelerate;
                        }
                    }
                }

                /*
                // LOGGING
                if (player_id != last_id)
                {
                    last_id = player_id;
                    Debug.Log("CCFDebug - New max packet backlog: " + max_player.m_mp_name + " -- packets in backlog: " + max_player.m_InputToProcessOnServer.Count);
                }
                */

                if (num == 0)
                {
                    return false;
                }
                NetworkSim.PauseAllRigidBodiesExceptPlayers();

                //for (int i = 0; i < num; i++)
                for (int i = 0; i < num + 1; i++) // Executing 1 extra time for logging purposes now.
                {
                    foreach (Player player2 in Overload.NetworkManager.m_Players)
                    {
                        int id = player2.connectionToClient.connectionId;

                        if (!(player2 == null) && !player2.m_spectator)
                        {
                            if (player2.m_num_inputs_to_accelerate == 0)
                            {
                                if (id < 1024 || id >= 0)
                                {
                                    if (prev[id] != player2.m_InputToProcessOnServer.Count)
                                    {
                                        Debug.Log("CCFDebug - Input backlog remaining for " + player2.m_mp_name + " -- " + player2.m_InputToProcessOnServer.Count);
                                        prev[id] = player2.m_InputToProcessOnServer.Count;
                                    }
                                }
                                else
                                {
                                    Debug.Log("CCFDebug - ConnectionId over 1024 or less than 0.");
                                }


                                NetworkSim.PauseRigidBody(player2.c_player_ship.c_rigidbody);
                                continue;
                            }
                            PlayerEncodedInputWithTick playerEncodedInputWithTick = player2.m_InputToProcessOnServer.Dequeue();

                            player2.m_updated_state.m_tick = playerEncodedInputWithTick.m_tick; // Trying this here as well as in ProcessCachedControlsRemote() to see what happens
                            player2.m_send_updated_state = true; // Trying this here as well as in ProcessCachedControlsRemote() to see what happens

                            player2.ApplyFixedUpdateInputMessage(playerEncodedInputWithTick.m_input);
                            SendJustPressedOrJustReleasedMessage(player2, CCInput.FIRE_WEAPON);
                            SendJustPressedOrJustReleasedMessage(player2, CCInput.FIRE_MISSILE);
                            player2.c_player_ship.FixedUpdateProcessControls();

                            player2.ProcessRemotePlayerFiringControlsPost(); // Trying this here as well as in ProcessCachedControlsRemote() to see what happens

                            player2.m_num_inputs_to_accelerate--;
                        }
                    }
                    // TICK EXPERIMENT
                    Physics.Simulate(Time.fixedDeltaTime);
                    //Physics.Simulate(FIXEDUPDATE);
                }
                NetworkSim.ResumeAllPausedRigidBodies();

                return false;
            }

            private static void SendJustPressedOrJustReleasedMessage(Player player, CCInput button)
            {
                if (player.m_input_count[(int)button] == 1)
                {
                    ButtonJustPressedMessage msg = new ButtonJustPressedMessage(player.netId, button);
                    NetworkServer.SendToAll(66, msg);
                }
                else if (player.m_input_count[(int)button] == -1)
                {
                    ButtonJustReleasedMessage msg2 = new ButtonJustReleasedMessage(player.netId, button);
                    NetworkServer.SendToAll(67, msg2);
                }
            }
        }

        [HarmonyPatch(typeof(Controls), "MouseAimCache")]
        public static class MP_MouseAimCache
        {


            public static bool Prefix()
            {
                Controls.mouse_pos_add.x += Input.GetAxis("Mouse X");
                Controls.mouse_pos_add.y += Input.GetAxis("Mouse Y");

                //MP_MouseAim.mouse_raw_add.x += Input.GetAxisRaw("Mouse X");
                //MP_MouseAim.mouse_raw_add.y += Input.GetAxisRaw("Mouse Y");

                MP_MouseAim.mouse_raw_add.x += Rewired.ReInput.controllers.Mouse.GetAxisRaw(0);
                MP_MouseAim.mouse_raw_add.y += Rewired.ReInput.controllers.Mouse.GetAxisRaw(1);

                return false;
            }
        }

        [HarmonyPatch(typeof(Controls), "MouseAim")]
        public static class MP_MouseAim
        {
            public static Vector2 mouse_raw_add;

            public static bool Prefix(ref Vector2 __result)
            {
                Vector2 result = default(Vector2);
                result.x = Controls.mouse_pos_add.x;
                result.y = Controls.mouse_pos_add.y;
                Controls.mouse_pos_add.x = 0f;
                Controls.mouse_pos_add.y = 0f;
                result.y *= -1f;

                Vector2 raw = default(Vector2);
                raw.x = mouse_raw_add.x;
                raw.y = mouse_raw_add.y;
                mouse_raw_add.x = 0;
                mouse_raw_add.y = 0;
                raw.y *= -1f;

                if (result.sqrMagnitude > 0.001f)
                {
                    result.x = Mathf.Clamp(result.x, -1000f, 1000f);
                    result.y = Mathf.Clamp(result.y, -1000f, 1000f);
                    __result = result;

                    MPSkipClientResimulation.mouseX = result.x;
                    MPSkipClientResimulation.mouseY = result.y;
                    MPSkipClientResimulation.mouseXraw = Mathf.Clamp(raw.x, -1000f, 1000f);
                    MPSkipClientResimulation.mouseYraw = Mathf.Clamp(raw.y, -1000f, 1000f); ;
                }
                else
                {
                    __result = Vector2.zero;
                }

                return false;
            }
        }

        /*
        [HarmonyPatch(typeof(Controls), "InitControl")]
        public static class MP_InitControl
        {
            public static void Prefix()
            {
                Rewired.ReInput.ConfigHelper config = Rewired.ReInput.configuration;

                Debug.Log("------------------------");
                Debug.Log("REWIRED SETTINGS");
                Debug.Log("------------------------");

                Debug.Log("Windows default source - " + config.windowsStandalonePrimaryInputSource);
                Debug.Log("Disable native input - " + config.nativeMouseSupport);
                Debug.Log("Native mouse - " + config.nativeMouseSupport);
                config.nativeMouseSupport = true;
                Debug.Log("Native mouse set to - " + config.nativeMouseSupport);


                Debug.Log("------------------------");
            }
        }
        */
    }
}
