using HarmonyLib;
using Overload;
using UnityEngine;

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
                resim_depth = 0;

                Rigidbody rb = player.c_player_ship.c_rigidbody;
                avdiff = rb.angularVelocity.sqrMagnitude;
                vdiff = rb.velocity.sqrMagnitude;

                movevec = player.cc_move_vec.sqrMagnitude;
                turnvec = player.cc_turn_vec.sqrMagnitude;

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
                    Client.m_PendingPlayerStateMessages.Dequeue();
                }
                PlayerStateToClientMessage msg = Client.m_PendingPlayerStateMessages.Peek();
              
                tick_diff = Client.m_tick - msg.m_tick;
                //if (msg.m_tick < Client.m_tick)
                if (tick_diff > 0)
                {
                    data_available = true;
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

                            player.c_player_ship.m_boost_heat = msg.m_boost_heat;
                            player.c_player_ship.m_boost_overheat_timer = msg.m_boost_overheat_timer;

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
    }
}
