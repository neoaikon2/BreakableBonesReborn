using System;
using System.Collections.Generic;
using System.IO;
using AudioImportLib;
using MelonLoader;
using PuppetMasta;
using UnityEngine;
using UnhollowerBaseLib;

namespace BreakableBonesReborn
{
	[RegisterTypeInIl2Cpp]
	internal class BoneBreakHandler : MonoBehaviour
	{
		public PuppetMaster pMaster;
		public Muscle[] muscles;
		private BehaviourBaseNav baseNav = null;
		private static List<AudioClip> boneBreakSFX = new List<AudioClip>(0);
		private bool brokenFlag = false;

		public BoneBreakHandler(IntPtr intPtr) : base(intPtr) {}

		private void Start()
		{
			muscles = pMaster.muscles;
			// Load the bone breaking audio sfx (if we haven't already)
			if (boneBreakSFX.Count == 0)
			{
#if DEBUG
				MelonLogger.Msg("Loading audio clips...");
#endif
				LoadAudioClips();
#if DEBUG
				if (boneBreakSFX.Count == 0)
					MelonLogger.Msg("Issue loading the audio file(s) from the mod directory");
				else
					MelonLogger.Msg("Successfully loaded " + boneBreakSFX.Count.ToString() + " audio file!");
#endif
			}
			baseNav = pMaster.transform.root.GetComponentInChildren<BehaviourBaseNav>();
		}

#if DEBUG
		float heartbeat_t = 0;
		public void Debug_Heartbeat()
		{
			heartbeat_t += Time.deltaTime;
			if(heartbeat_t > 3)
			{
				heartbeat_t = 0;
				MelonLogger.Msg(ConsoleColor.Cyan, "Heartbeat <3!");
			}
		}
#endif

		private void LoadAudioClips()
		{
			string[] sfxFiles = Directory.GetFiles(MelonUtils.UserDataDirectory + "\\BreakableBonesReborn\\bonebreaks");
			foreach(string fileName in sfxFiles)
			{				
				boneBreakSFX.Add(
					API.LoadAudioClip(
						System.IO.Path.Combine(
							MelonUtils.UserDataDirectory + "\\BreakableBonesReborn\\bonebreaks", fileName)
						)
					);
			}
		}

		private AudioClip SelRandomAudioclip()
		{
			return boneBreakSFX[UnityEngine.Random.Range(0, boneBreakSFX.Count)];
		}

		private void Update()
		{
#if DEBUG
			Debug_Heartbeat();
#endif
			// Loop through the muscles
			foreach (Muscle muscle in muscles)
			{
				// Skip muscles that have already been broken
				if (brokenFlag)
				{
#if DEBUG
					MelonLogger.Msg("This bone is already broken...skipping");
#endif
					break;
				}

				// Get the forces being applied to the configurable joint for this muscle
				ConfigurableJoint joint = muscle.joint;
				float magnitude = joint.currentTorque.magnitude;
				float magnitude2 = joint.currentForce.magnitude;
				
				// If it exceeds our values for a bone break, break it
				if (magnitude > Loader.cfgTorqueLimit.Value || magnitude2 > Loader.cfgForceLimit.Value)
				{
#if DEBUG
					MelonLogger.Msg(joint.transform.root.name + " has had a bone broken!");
#endif
					// Apply angular locks
					joint.angularXMotion = (Loader.x_lock ? ConfigurableJointMotion.Free : ConfigurableJointMotion.Limited);
					joint.angularYMotion = (Loader.y_lock ? ConfigurableJointMotion.Free : ConfigurableJointMotion.Limited);
					joint.angularZMotion = (Loader.z_lock ? ConfigurableJointMotion.Free : ConfigurableJointMotion.Limited);

					// Get the audio source on this muscle (if there isn't one add it)
					//AudioSource audioSource = joint.connectedBody.gameObject.GetComponent<AudioSource>() ? joint.connectedBody.gameObject.GetComponent<AudioSource>() : joint.connectedBody.gameObject.AddComponent<AudioSource>();
					AudioSource audioSource = joint.connectedBody.gameObject.AddComponent<AudioSource>();

					// Set a random pitch and play the bone breaking sfx
					audioSource.pitch = UnityEngine.Random.Range(0.75f, 1.15f);
					audioSource.PlayOneShot(SelRandomAudioclip(), UnityEngine.Random.Range(0.8f, 1.3f));
					
					// Set the 
					if (Loader.cfgCauseDamage.Value)
					{
						baseNav.health.cur_hp -= 25f;
					}
				}
			}
		}

		// Token: 0x06000004 RID: 4 RVA: 0x0000222C File Offset: 0x0000042C
		private void OnDestroy()
		{			
			if (Loader.restoreSpawnable)
			{
				foreach (Muscle muscle in muscles)
				{					
					if (!brokenFlag)
					{
						return;
					}
					ConfigurableJoint joint = muscle.joint;
					joint.angularYMotion = ConfigurableJointMotion.Limited;
					joint.angularZMotion = ConfigurableJointMotion.Limited;					
				}
				pMaster.muscles = pMaster.defaultMuscles;
				pMaster.FixMusclePositions();
				pMaster.FixTargetTransforms();
				pMaster.Resurrect();
			}
		}
	}
}