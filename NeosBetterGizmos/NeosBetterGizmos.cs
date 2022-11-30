using FrooxEngine;
using HarmonyLib;
using NeosModLoader;
using System;
using BaseX;

namespace NeosBetterGizmos {
	public class NeosBetterGizmos : NeosMod {
		public override string Name => "NeosBetterGizmos";
		public override string Author => "Delta";
		public override string Version => "1.1.0";
		public override string Link => "https://github.com/XDelta/NeosBetterGizmos";

		private static ModConfiguration Config;
		public override void OnEngineInit() {
			Config = GetConfiguration();
			Config.Save(true);

			Harmony harmony = new Harmony("tk.deltawolf.NeosBetterGizmos");
			harmony.PatchAll();
		}

		[AutoRegisterConfigKey]
		private static ModConfigurationKey<bool> ShowRefID = new ModConfigurationKey<bool>("ShowRefID", "Show RefID on Gizmo", () => false);

		[AutoRegisterConfigKey]
		private static ModConfigurationKey<bool> PersistentGizmo = new ModConfigurationKey<bool>("PersistentGizmo", "Should gizmos be persistent", () => true);
		[HarmonyPatch(typeof(SlotGizmo), "OnAttach")]
		class SlotGizmo_OnAttach_Patch {
			public static void Postfix(SlotGizmo __instance, TransformRelayRef ____targetSlot) {

				__instance.Slot.PersistentSelf = Config.GetValue(PersistentGizmo);

				var btns = __instance.Slot.FindChild((Slot c) => c.Name == "Buttons");
				var name = __instance.Slot.FindChild((Slot c) => c.Name == "Name");

				//ReleaseLink before attaching LookAtUser
				btns.Rotation_Field.ReleaseLink(btns.Rotation_Field.ActiveLink);
				Debug("Attempted to release link for Buttons");
				name.Rotation_Field.ReleaseLink(name.Rotation_Field.ActiveLink);
				Debug("Attempted to release link for Name");

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
				if (Config.GetValue(ShowRefID)) {
					Slot refslot = name.AddSlot("RefID", false);
					refslot.LocalPosition = new float3(0,0.02f,0);
					TextRenderer textRenderer = refslot.AttachComponent<TextRenderer>(true, null);
					TextUnlitMaterial textUnlitMaterial = refslot.AttachComponent<TextUnlitMaterial>(true, null);
					textUnlitMaterial.ZTest.Value = Config.GetValue(RenderOnTop) ? ZTest.Always : ZTest.Equal;
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
			}
		}
	}
}