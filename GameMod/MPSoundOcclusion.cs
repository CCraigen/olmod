using HarmonyLib;
using Overload;
using UnityEngine;
using UnityEngine.Audio;
using System;


namespace GameMod
{
	public static class MPSoundOcclusion
	{
		public const float MAXDIST = 85f;
		public const float MINDIST = 15f;
		public static AudioSource[] m_a_source = new AudioSource[512];
		public static AudioLowPassFilter[] m_a_filter = new AudioLowPassFilter[512];
	}

	[HarmonyPatch(typeof(UnityAudio), "CreateAudioSourceAndObject")]
	internal class UnityAudio_CreateAudioSourceAndObject_MPSoundOcclusion
	{
		static void Postfix(ref GameObject[] ___m_a_object, int i)
		{
			if (___m_a_object != null)
			{
				MPSoundOcclusion.m_a_source[i] = ___m_a_object[i].GetComponent<AudioSource>();
				if (___m_a_object[i].GetComponent<AudioLowPassFilter>() == null)
				{
					MPSoundOcclusion.m_a_filter[i] = ___m_a_object[i].AddComponent<AudioLowPassFilter>();
				}
				else
				{
					MPSoundOcclusion.m_a_filter[i] = ___m_a_object[i].GetComponent<AudioLowPassFilter>();
					// this *shouldn't* happen but who knows with Overload
				}
				MPSoundOcclusion.m_a_filter[i].cutoffFrequency = 22000f;
				MPSoundOcclusion.m_a_filter[i].enabled = false;
			}
		}
	}

	[HarmonyPatch(typeof(GameplayManager), "DoneLevel")]
	internal class GameplayManager_DoneLevel_MPSoundOcclusion
	{
		static void Postfix()
		{
			foreach (AudioLowPassFilter f in MPSoundOcclusion.m_a_filter)
			{
				f.cutoffFrequency = 22000f;
				f.enabled = false;
			}
		}
	}

	[HarmonyPatch(typeof(UnityAudio), "PlaySound")]
	internal class UnityAudio_PlaySound_MPSoundOcclusion
	{
		static void Postfix(int __result, Vector3 pos3d)
		{
			if (!GameplayManager.IsDedicatedServer() && !MPObserver.Enabled && GameplayManager.IsMultiplayerActive && __result != -1 && GameplayManager.)
			{
				RaycastHit ray1;

				Vector3 shipPos = GameManager.m_player_ship.transform.localPosition;

				// If pos3d = Vector3.zero, then it's almost without a doubt a 2D cue on the local client. It's beyond infeasible that sound could accidentally come from *exactly* this point.
				// If it ever does, well then you get 1 glitched cue and you should have bought a lottery ticket. Oh well.
				if (Physics.Linecast(pos3d, shipPos, out ray1, 67256320) && pos3d != Vector3.zero) // check line-of-sight to sound source.
				{
					// we don't have line-of-sight
					// This is the "Tier 3" approach, taking distance to target and thickness of obstruction into account

					float p2pDist = Vector3.Distance(pos3d, shipPos); // point to point distance
					RaycastHit ray2;
					Physics.Linecast(shipPos, pos3d, out ray2, 67256320);
					float thick = Mathf.Clamp(p2pDist - ray1.distance - ray2.distance, 1f, MPSoundOcclusion.MAXDIST); // how thick the obstruction is, clamped
					p2pDist = Mathf.Clamp(p2pDist, MPSoundOcclusion.MINDIST, MPSoundOcclusion.MAXDIST); // clamp the p2pDist value
					float factor = ((MPSoundOcclusion.MAXDIST) - (0.6f * thick + 0.4f * p2pDist)) / (MPSoundOcclusion.MAXDIST);

					MPSoundOcclusion.m_a_filter[__result].cutoffFrequency = 500 + (11000 * factor * factor); // exponential curve, actual cap currently is ~7khz since we are clamping to 15 units minimum on distance
																											 //MPSoundOcclusion.m_a_filter[__result].cutoffFrequency = 500 + (11000 * factor); // linear curve

					//Debug.Log("CCC playing occluded, factor " + factor);
					//Debug.Log("CCC playing occluded, original volume " + MPSoundOcclusion.m_a_source[__result].volume);

					MPSoundOcclusion.m_a_source[__result].volume = MPSoundOcclusion.m_a_source[__result].volume + 0.25f * (0.85f - factor);
					MPSoundOcclusion.m_a_filter[__result].enabled = true;
					//Debug.Log("CCC playing occluded, new volume " + MPSoundOcclusion.m_a_source[__result].volume);
					//Debug.Log("CCC playing occluded, distance " + p2pDist +", thickness " + thick + ", factor is " + factor + ", cutoff frequency is " + (500 + (11000 * factor * factor)));
				}
				else
				{
					// we have line-of-sight, restore the normal filter
					MPSoundOcclusion.m_a_filter[__result].cutoffFrequency = 22000f;
					MPSoundOcclusion.m_a_filter[__result].enabled = false;
				}
			}
		}
	}
}