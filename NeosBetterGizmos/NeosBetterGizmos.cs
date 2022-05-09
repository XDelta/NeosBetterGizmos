using FrooxEngine;
using HarmonyLib;
using NeosModLoader;
using System;
using BaseX;

namespace NeosBetterGizmos {
	public class NeosBetterGizmos : NeosMod {
		public override string Name => "NeosBetterGizmos";
		public override string Author => "Delta";
		public override string Version => "1.0.0";
		public override string Link => "https://github.com/XDelta/NeosBetterGizmos";
		public override void OnEngineInit() {
			Harmony harmony = new Harmony("tk.deltawolf.NeosBetterGizmos");
			harmony.PatchAll();
		}

		[HarmonyPatch(typeof(SlotGizmo), "OnAttach")]
		class SlotGizmo_OnAttach_Patch {
			public static void Postfix(SlotGizmo __instance) {
				var btns = __instance.Slot.FindChild((Slot c) => c.Name == "Buttons");
				var name = __instance.Slot.FindChild((Slot c) => c.Name == "Name");

				//ReleaseLink before attaching LookAtUser so
				btns.Rotation_Field.ReleaseLink(btns.Rotation_Field.ActiveLink);
				Msg("Attempted to release link for Buttons");
				name.Rotation_Field.ReleaseLink(name.Rotation_Field.ActiveLink);
				Msg("Attempted to release link for Name");

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
			}
		}
	}
}