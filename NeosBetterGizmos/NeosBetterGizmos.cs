using FrooxEngine;
using HarmonyLib;
using NeosModLoader;
using System;
using BaseX;
using FrooxEngine.UIX;

namespace NeosBetterGizmos;




public class NeosBetterGizmos : NeosMod {
	public override string Name => "NeosBetterGizmos";
	public override string Author => "Delta";
	public override string Version => "1.2.0";
	public override string Link => "https://github.com/XDelta/NeosBetterGizmos";

	private static ModConfiguration Config;
	public override void OnEngineInit() {
		Config = GetConfiguration();
		Config.Save(true);

		Harmony harmony = new Harmony("net.deltawolf.NeosBetterGizmos");
		harmony.PatchAll();
	}

	[AutoRegisterConfigKey]
	private static ModConfigurationKey<bool> RenderOnTop = new ModConfigurationKey<bool>("RenderOnTop", "Experimental: Render gizmos on top", () => true);


	[AutoRegisterConfigKey]
	private static ModConfigurationKey<bool> ShowRefID = new ModConfigurationKey<bool>("ShowRefID", "Show RefID on Gizmo", () => false);

	[AutoRegisterConfigKey]
	private static ModConfigurationKey<bool> PersistentGizmo = new ModConfigurationKey<bool>("PersistentGizmo", "Should gizmos be persistent", () => true);

	[AutoRegisterConfigKey]
    private static ModConfigurationKey<bool> UseUIXButtons = new ModConfigurationKey<bool>("UseUIXButtons", "Experimental: use UIX buttons instead of default box gizmos", () => false);

    private static readonly float3 uixScale = new float3(0.001f, 0.001f, 0.001f);

    [HarmonyPatch(typeof(SlotGizmo), "OnAttach")]
	class SlotGizmo_OnAttach_Patch {
		public static void Postfix(SlotGizmo __instance, TransformRelayRef ____targetSlot) {
				}
			__instance.Slot.PersistentSelf = Config.GetValue(PersistentGizmo);

			var btns = __instance.Slot.FindChild((Slot c) => c.Name == "Buttons");
			var name = __instance.Slot.FindChild((Slot c) => c.Name == "Name");
            //ReleaseLink before attaching LookAtUser
            btns.Rotation_Field.ReleaseLink(btns.Rotation_Field.ActiveLink);
			Debug("Attempted to release link for Buttons");
			name.Rotation_Field.ReleaseLink(name.Rotation_Field.ActiveLink);
			Debug("Attempted to release link for Name");

			//Shared Text Material for Rendering on top
			if (Config.GetValue(RenderOnTop)) {
				TextUnlitMaterial textMat = __instance.World.GetSharedComponentOrCreate("Text_DefaultMaterial_BetterGizmos", delegate (TextUnlitMaterial mat) {
					mat.ZTest.Value = ZTest.Always;
				}, 0, false, false, null);
				name.GetComponent<TextUnlitMaterial>().Destroy();
				name.GetComponent<TextRenderer>().Material.Target = textMat;
			}

			LookAtUser lau;

			try {
				lau = btns.AttachComponent<LookAtUser>();
				//AttachComponent may error here, if so ReleaseLink failed
				lau.TargetAtLocalUser.Value = true;
				lau.Invert.Value = true;
				lau.AroundAxis.Value = true;
			} catch (Exception) {
				Error("ReleaseLink likely failed, so AttachComponent failed to link rotation");
				throw;
			}
			//Copy rotation
			var vc = name.AttachComponent<ValueCopy<floatQ>>();
			vc.Source.Value = btns.Rotation_Field.ReferenceID;
			vc.Target.Value = name.Rotation_Field.ReferenceID;

			//Set ZTest for the name material?
			//Probably want a new material for it as we want to avoid affecting existing gizmos and other users when possible

			if (Config.GetValue(ShowRefID)) {
				Slot refslot = name.AddSlot("RefID", false);
				refslot.LocalPosition = new float3(0,0.02f,0);
				TextRenderer textRenderer = refslot.AttachComponent<TextRenderer>(true, null);
				TextUnlitMaterial textUnlitMaterial = refslot.AttachComponent<TextUnlitMaterial>(true, null);
				//textUnlitMaterial.ZTest.Value = Config.GetValue(RenderOnTop) ? ZTest.Always : ZTest.Equal;
				textUnlitMaterial.TintColor.Value = new color(1,1,1,0.6f);
				textRenderer.Bounded.Value = true;
				textRenderer.BoundsSize.Value = new float2(0.05f, 0.02f);
				textRenderer.HorizontalAutoSize.Value = true;
				textRenderer.VerticalAutoSize.Value = true;
				textRenderer.Material.Target = textUnlitMaterial;

				try {
					textRenderer.Text.Value = ____targetSlot.ReferenceID.ToString();
				} catch (Exception e) {
					Error(e);
				}
			}
			if (Config.GetValue(UseUIXButtons)) {
            }
		}
	}

	[HarmonyPatch(typeof(SlotGizmo), "RegenerateButtons")]
	class SlotGizmo_RegenerateButtons_Patch {
		public static void Postfix(SlotGizmo __instance) {
			ReplaceMats(__instance); //Replace Mats needs to run after the buttons exist
		}
	}
	private static void ReplaceMats(SlotGizmo __instance) {
		if (!Config.GetValue(RenderOnTop)) {
			return;
		}
		var btns = __instance.Slot.FindChild((Slot c) => c.Name == "Buttons");
		FresnelMaterial sR = __instance.World.GetSharedComponentOrCreate("SlotGizmo_Icon_R_BetterGizmos", delegate (FresnelMaterial f) {
			f.NearColor.Value = new color(1f, 0f, 0f, 1f);
			f.FarColor.Value = new color(0.5f, 0f, 0f, 1f);
			f.ZTest.Value = ZTest.Always;
		}, 0, false, false, null);

		FresnelMaterial sG = __instance.World.GetSharedComponentOrCreate("SlotGizmo_Icon_G_BetterGizmos", delegate (FresnelMaterial f) {
			f.NearColor.Value = new color(0f, 1f, 0f, 1f);
			f.FarColor.Value = new color(0f, 0.5f, 0f, 1f);
			f.ZTest.Value = ZTest.Always;
		}, 0, false, false, null);

		FresnelMaterial sB = __instance.World.GetSharedComponentOrCreate("SlotGizmo_Icon_B_BetterGizmos", delegate (FresnelMaterial f) {
			f.NearColor.Value = new color(0f, 0f, 1f, 1f);
			f.FarColor.Value = new color(0f, 0f, 0.5f, 1f);
			f.ZTest.Value = ZTest.Always;
		}, 0, false, false, null);

		var transformOffset = btns[2].FindChild((Slot c) => c.Name == "Offset");
		transformOffset[0].GetComponent<MeshRenderer>().ReplaceAllMaterials(sR);
		transformOffset[1].GetComponent<MeshRenderer>().ReplaceAllMaterials(sG);
		transformOffset[2].GetComponent<MeshRenderer>().ReplaceAllMaterials(sB);

		var rotationIcons = btns[3].FindChild((Slot c) => c.Name == "Icon");
		rotationIcons[0].GetComponent<MeshRenderer>().ReplaceAllMaterials(sR);
		rotationIcons[1].GetComponent<MeshRenderer>().ReplaceAllMaterials(sG);
		rotationIcons[2].GetComponent<MeshRenderer>().ReplaceAllMaterials(sB);

		var scaleOffset = btns[4].FindChild((Slot c) => c.Name == "Offset");
		scaleOffset[0].GetComponent<MeshRenderer>().ReplaceAllMaterials(sR);
		scaleOffset[1].GetComponent<MeshRenderer>().ReplaceAllMaterials(sG);
		scaleOffset[2].GetComponent<MeshRenderer>().ReplaceAllMaterials(sB);
		Msg("Replaced Mats");
	}
}
