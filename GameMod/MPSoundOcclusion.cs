﻿using HarmonyLib;
using Overload;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace GameMod
{
	public static class MPSoundOcclusion
    {	
		//									N/A		LOW		MED		STRONG			
		public static float[] MAXDISTS =	{ 0f,	100f,	95f,	85f };
		public static float[] BOOSTS =		{ 0f,	0.15f,	0.20f,	0.25f };
		public static float[] LOWFREQS =	{ 0f,	800f,	500f ,	500f };
		public static float[] CUTOFFS =		{ 0f,	9500f,	10000f,	10500f };
		// actual cutoff starting point is currently targetted at ~7khz since we are clamping to 15 units minimum distance below

		// Change this at your peril, gotta recalculate the curves if you do
		public const float MINDIST = 15f;

		public static float CutoffFreq;
		public static float BoostAmount;
		public static bool Occluded;
	}

	[HarmonyPatch(typeof(UnityAudio), "CreateAudioSourceAndObject")]
	internal class MPSoundOcclusion_UnityAudio_CreateAudioSourceAndObject
	{
		static void Postfix(ref GameObject[] ___m_a_object, int i)
        {
			//GameManager.m_audio.m_debug_sounds = true;

			if (___m_a_object != null)
			{
				MPSoundExt.m_a_source[i] = ___m_a_object[i].GetComponent<AudioSource>();
				if (___m_a_object[i].GetComponent<AudioLowPassFilter>() == null)
				{
					MPSoundExt.m_a_filter[i] = ___m_a_object[i].AddComponent<AudioLowPassFilter>();
				}
				else
				{
					MPSoundExt.m_a_filter[i] = ___m_a_object[i].GetComponent<AudioLowPassFilter>();
					// this *shouldn't* happen but who knows with Overload
				}
				MPSoundExt.m_a_filter[i].cutoffFrequency = 22000f;
				MPSoundExt.m_a_filter[i].enabled = false;
			}
		}
	}

	[HarmonyPatch(typeof(GameplayManager), "DoneLevel")]
	internal class MPSoundOcclusion_GameplayManager_DoneLevel
	{
		static void Postfix()
		{
			foreach (AudioLowPassFilter f in MPSoundExt.m_a_filter)
			{
				f.cutoffFrequency = 22000f;
				f.enabled = false;
			}
		}
	}

	// Sets a bool for use with Occlusion in the Cue play commands since they can fire off several layers simultaneously.
	// Previously this was resulting in several Linecasts from the same position. Should make things more efficient by allowing
	// the PlaySound method to only do the check once for each cue.
	[HarmonyPatch(typeof(SFXCueManager), "PlayCue2D")]
	internal class MPSoundOcclusion_SFXCueManager_PlayCue2D
	{
		static void Prefix()
		{
			MPSoundExt.CueState = 1;
		}

		static void Postfix()
		{
			MPSoundExt.CueState = 0;
		}
	}

	// same here as above with PlayCue2D
	[HarmonyPatch(typeof(SFXCueManager), "PlayCuePos")]
	internal class MPSoundOcclusion_SFXCueManager_PlayCuePos
	{
		static void Prefix()
		{
			MPSoundExt.CueState = 1;
		}

		static void Postfix()
		{
			MPSoundExt.CueState = 0;
		}
	}

	// same here as above with the PlayCue methods
	[HarmonyPatch(typeof(SFXCueManager), "PlayThunderboltFire")]
	internal class MPSoundOcclusion_SFXCueManager_PlayThunderboltFire
	{
		static void Prefix()
		{
			MPSoundExt.CueState = 1;
		}

		static void Postfix()
		{
			MPSoundExt.CueState = 0;
		}
	}

	// Main logic happens here
	[HarmonyPatch(typeof(UnityAudio), "PlaySound")]
	internal class MPSoundOcclusion_UnityAudio_PlaySound
	{
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
		{
			foreach (var code in codes)
			{
				if (code.opcode == OpCodes.Call && code.operand == AccessTools.Method(typeof(AudioSource), "Play"))
				{
					yield return new CodeInstruction(OpCodes.Nop);
				}
				else
				{
					yield return code;
				}
			}
		}

		static void Postfix(int __result, Vector3 pos3d)
		{	
			if (__result != -1) // if -1 then we ran out of audio slots, skip the whole thing
			{
				// If pos3d == Vector3.zero, then it's almost without a doubt a 2D cue on the local client. It's beyond infeasible that sound could accidentally come from *exactly* this point.
				// If it ever does, well then you get 1 glitched cue and you should have bought a lottery ticke
				if (Menus.mms_audio_occlusion_strength == 0 || pos3d == Vector3.zero)
                {
					MPSoundOcclusion.Occluded = false;
                }
				else
				{
					if ((!MPObserver.Enabled || MPObserver.ObservedPlayer != null) && !GameplayManager.IsDedicatedServer()) // last check probably not necessary but whatever
					{
						// if we're mid-cue there's multiple sounds being played from the same location - use the previous Linecast calculations for efficiency
						if (MPSoundExt.CueState < 2)
						{
							Vector3 shipPos = GameManager.m_player_ship.transform.localPosition;

							RaycastHit ray1;
							if (Physics.Linecast(pos3d, shipPos, out ray1, 67256320))
							{
								// we don't have line-of-sight
								// This is the "Tier 3" approach, taking both distance to target and thickness of obstruction into account
								RaycastHit ray2;
								Physics.Linecast(shipPos, pos3d, out ray2, 67256320);

								float maxdist = MPSoundOcclusion.MAXDISTS[Menus.mms_audio_occlusion_strength];
								float cutoff = MPSoundOcclusion.CUTOFFS[Menus.mms_audio_occlusion_strength];
								float lowfreq = MPSoundOcclusion.LOWFREQS[Menus.mms_audio_occlusion_strength];
								float boost = MPSoundOcclusion.BOOSTS[Menus.mms_audio_occlusion_strength];

								float p2pDist = Vector3.Distance(pos3d, shipPos); // point to point distance

								float thick = Mathf.Clamp(p2pDist - ray1.distance - ray2.distance, 1f, maxdist); // how thick the obstruction is, clamped
								p2pDist = Mathf.Clamp(p2pDist, MPSoundOcclusion.MINDIST, maxdist); // clamp the p2pDist value
								float factor = (maxdist - (0.6f * thick + 0.4f * p2pDist)) / (maxdist);

								MPSoundOcclusion.CutoffFreq = lowfreq + (cutoff * factor * factor); // exponential curve
								MPSoundOcclusion.BoostAmount = boost * (0.85f - factor);
								MPSoundOcclusion.Occluded = true;
							}
							else
							{
								MPSoundOcclusion.Occluded = false;
							}
						}
						if (MPSoundExt.CueState == 1) // we're in a multicue, pause calculations
                        {
							MPSoundExt.CueState = 2;
						}
					}
				}
				if (MPSoundOcclusion.Occluded)
				{
					MPSoundExt.m_a_filter[__result].cutoffFrequency = MPSoundOcclusion.CutoffFreq;
					MPSoundExt.m_a_source[__result].volume = MPSoundExt.m_a_source[__result].volume + MPSoundOcclusion.BoostAmount; // slight boost to volume as range increases to counter the HF rolloff
					MPSoundExt.m_a_filter[__result].enabled = true;
				}
				else
				{
					// restore the normal filter
					MPSoundExt.m_a_filter[__result].cutoffFrequency = 22000f;
					MPSoundExt.m_a_filter[__result].enabled = false;
				}

				MPSoundExt.m_a_source[__result].Play();  // Nop'd out in the transpiler
			}
		}
	}
}