using System;
using System.IO;
using BoneLib.BoneMenu;
using BoneLib.BoneMenu.Elements;
using HarmonyLib;
using MelonLoader;
using PuppetMasta;
using UnityEngine;

namespace BreakableBonesReborn
{
	public class Loader : MelonMod
	{
		public MelonPreferences_Category prefCategory;
		public static MelonPreferences_Entry<float> cfgTorqueLimit;
		public static MelonPreferences_Entry<float> cfgForceLimit;		
		public static MelonPreferences_Entry<bool> cfgCauseDamage;

		// Bone lock settings
		public static bool x_lock = false;
		public static bool y_lock = true;
		public static bool z_lock = false;
		public static bool restoreSpawnable = true;

		// Bone menu
		public static float increment = 50f;
		public static float max = 0f;

		public override void OnInitializeMelon()
		{
			// Check to make sure the settings file exists
			Directory.CreateDirectory(MelonUtils.UserDataDirectory + "\\BreakableBonesReborn");
			if(!File.Exists(MelonUtils.UserDataDirectory + "\\BreakableBonesReborn\\breakable.cfg"))
				File.Create(MelonUtils.UserDataDirectory + "\\BreakableBonesReborn\\breakable.cfg");
			// Initialize preferences and menu
			InitSettings();			
			InitBoneMenu();

			// Inject the patch
			ApplyPatch();
		}

		private void InitSettings()
		{
			prefCategory = MelonPreferences.CreateCategory("BreakableBones");
			prefCategory.SetFilePath(MelonUtils.UserDataDirectory + "\\BreakableBonesReborn\\breakable.cfg", false);
			cfgTorqueLimit = prefCategory.CreateEntry<float>("BoneTorqueLimit", 1500f, null, null, false, false, null, null);
			cfgForceLimit = prefCategory.CreateEntry<float>("BoneForceLimit", 5000f, null, null, false, false, null, null);			
			cfgCauseDamage = prefCategory.CreateEntry<bool>("CauseDamage", true, null, null, false, false, null, null);
		}

		private void InitBoneMenu()
		{
			// Create a menu category
			MenuCategory menuCategory = MenuManager.CreateCategory("Breakable Bones Reborn", Color.cyan);

			// Sub panel for Bone Break Options
			SubPanelElement subPanelLimits = menuCategory.CreateSubPanel("Bone Break Options", Color.red);
			subPanelLimits.CreateFloatElement("Torque Limit", Color.red, cfgTorqueLimit.Value, increment, 0f, float.MaxValue, delegate (float value)
			{
				cfgTorqueLimit.Value = value;
			});
			subPanelLimits.CreateFloatElement("Force Limit", Color.red, cfgForceLimit.Value, increment, 0f, float.MaxValue, delegate (float value)
			{
				cfgForceLimit.Value = value;
			});			
			subPanelLimits.CreateBoolElement("Breaks Cause Damage?", Color.red, cfgCauseDamage.Value, delegate (bool value)
			{
				cfgCauseDamage.Value = value;
			});
			subPanelLimits.CreateBoolElement("Restore Spawnable?", Color.yellow, restoreSpawnable, delegate (bool value)
			{
				restoreSpawnable = value;
			});

#if DEBUG
			// Experimental settings
			SubPanelElement subPanelElement2 = menuCategory.CreateSubPanel("Experimental Settings [!]", Color.yellow);
			subPanelElement2.CreateFloatElement("force/torque button increment", Color.yellow, increment, 25f, 0f, float.MaxValue, delegate (float value)
			{
				increment = value;
			});
			subPanelElement2.CreateFloatElement("brokenjoint maxforce", Color.yellow, max, 1f, 0f, float.MaxValue, delegate (float value)
			{
				max = value;
			});
			subPanelElement2.CreateBoolElement("unlock_x", Color.yellow, x_lock, delegate (bool value)
			{
				x_lock = value;
			});
			subPanelElement2.CreateBoolElement("unlock_y", Color.yellow, y_lock, delegate (bool value)
			{
				y_lock = value;
			});
			subPanelElement2.CreateBoolElement("unlock_z", Color.yellow, z_lock, delegate (bool value)
			{
				z_lock = value;
			});			
#endif
		}

		private void ApplyPatch()
		{
			// Inject harmony patch
			HarmonyInstance.Patch(typeof(PuppetMaster).GetMethod("Awake"), null, new HarmonyMethod(typeof(PuppetMaster_Patches).GetMethod("Awake_Patch")));			
		}

		public override void OnDeinitializeMelon()
		{
			prefCategory.SaveToFile();			
		}
		
		public class PuppetMaster_Patches
		{
			[HarmonyPatch(typeof(PuppetMaster), "Awake")]
			[HarmonyPostfix]
			public static void Awake_Patch(PuppetMaster __instance)
			{
#if DEBUG
				MelonLogger.Msg("Applying BoneBreakHandler to " + __instance.transform.root.name);
#endif
				GameObject gameObject = __instance.gameObject;
				gameObject.AddComponent<BoneBreakHandler>().pMaster = __instance;
			}
		}
	}
}