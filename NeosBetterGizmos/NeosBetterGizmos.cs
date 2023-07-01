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
	private static ModConfigurationKey<bool> AdditionGizmoIcons = new ModConfigurationKey<bool>("AdditionGizmoIcons", "Experimental: Enable additional gizmo button icons and models", () => true);

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
	[HarmonyPatch(typeof(SlotGizmo), "AddGizmoButton", new Type[] {typeof(Worker), typeof(Action<Slot>)})]
	class SlotGizmo_AddGizmoButton_Patch {
		public static bool Prefix(SlotGizmo __instance, Worker worker, SyncRef<Slot> ____buttonsSlot, ref Action<Slot> iconGenerator) {
			if (!Config.GetValue(AdditionGizmoIcons)) {
				return true; //Skip adding extra icons
			}
			if (iconGenerator is not null) {
				Msg($"Worker already has a generator: {iconGenerator}");
				return true; //Already has a generator
			}
			//LightGizmo
			var giz = worker.GizmoType;
			if (giz == typeof(BoxColliderGizmo)) {
				Msg("BoxCollider");
				iconGenerator = new Action<Slot>(GenerationEmptyIcon);
			} else if (giz == typeof(BoxMeshGizmo)) {
				Msg("BoxMesh");
				iconGenerator = new Action<Slot>(GenerationBoxMeshIcon);
			} else if (giz == typeof(MeshRendererGizmo)) {
				Msg("MeshRenderer");
				iconGenerator = new Action<Slot>(GenerationEmptyIcon);
			} else if (giz == typeof(MaterialGizmo)) {
				Msg("Material");
				iconGenerator = new Action<Slot>(GenerationMaterialIcon);
			} else if (giz == typeof(SphereColliderGizmo)) {
				Msg("SphereCollider");
				iconGenerator = new Action<Slot>(GenerationEmptyIcon);
			} else if (giz == typeof(SphereMeshGizmo)) {
				Msg("SphereMesh");
				iconGenerator = new Action<Slot>(GenerationEmptyIcon);
			} else if (giz == typeof(IcoSphereMeshGizmo)) {
				Msg("IcoSphereMesh");
				iconGenerator = new Action<Slot>(GenerationEmptyIcon);
			} else if (giz == typeof(TextGizmo)) {
				Msg("TextGizmo");
				iconGenerator = new Action<Slot>(GenerationTextIcon);
			} else {
				Msg($"No generator override found for {worker.GizmoType}");
			}
			return true;
		}
	}
	private static void GenerationBoxMeshIcon(Slot slot) {
		TubeBoxMesh sharedComponentOrCreate = slot.World.GetSharedComponentOrCreate("BoxMeshGizmo_Icon_BetterGizmos", delegate (TubeBoxMesh mesh) {
			mesh.TubeRadius.Value = 0.05f;
			mesh.Size.Value = new float3(0.9f, 0.9f, 0.9f);
		}, 0, false, false, null);
		FresnelMaterial material;
		if (Config.GetValue(RenderOnTop)) {
			material = slot.World.GetSharedComponentOrCreate("BoxMeshGizmo_Mat_Icon_Overlay_BetterGizmos", delegate (FresnelMaterial f) {
				f.NearColor.Value = new color(0f, 1f, 0f, 1f);
				f.FarColor.Value = new color(0f, 0.5f, 0f, 1f);
				f.ZTest.Value = ZTest.Always;
			}, 0, false, false, null);
		} else {
			material = slot.World.GetSharedComponentOrCreate("BoxMeshGizmo_Mat_Icon_BetterGizmos", delegate (FresnelMaterial f) {
				f.NearColor.Value = new color(0f, 1f, 0f, 1f);
				f.FarColor.Value = new color(0f, 0.5f, 0f, 1f);
			}, 0, false, false, null);
		}
		Slot box = slot.AddSlot("Box", true);
		box.AttachMesh(sharedComponentOrCreate, material, 0);
	}

	private static void GenerationMaterialIcon(Slot slot) {
		IcoSphereMesh sharedComponentOrCreate = slot.World.GetSharedComponentOrCreate("MaterialGizmo_Icon_BetterGizmos", delegate (IcoSphereMesh mesh) {
			mesh.Radius.Value = 0.5f;
			mesh.Subdivisions.Value = 1;
		}, 0, false, false, null);
		FresnelMaterial material;
		if (Config.GetValue(RenderOnTop)) {
			material = slot.World.GetSharedComponentOrCreate("MaterialGizmo_Mat_Icon_Overlay_BetterGizmos", delegate (FresnelMaterial f) {
				f.NearColor.Value = new color(0.7f, 1f);
				f.FarColor.Value = new color(0.25f, 1f);
				f.ZTest.Value = ZTest.Always;
			}, 0, false, false, null);
		} else {
			material = slot.World.GetSharedComponentOrCreate("MaterialGizmo_Mat_Icon_BetterGizmos", delegate (FresnelMaterial f) {
				f.NearColor.Value = new color(0.7f, 1f);
				f.FarColor.Value = new color(0.25f, 1f);
			}, 0, false, false, null);
		}
		Slot matorb = slot.AddSlot("Material Orb", true);
		matorb.AttachMesh(sharedComponentOrCreate, material, 0);
	}
	private static void GenerationTextIcon(Slot slot) {
		QuadMesh sharedComponentOrCreate = slot.World.GetSharedComponentOrCreate("TextGizmo_Icon_BetterGizmos", delegate (QuadMesh mesh) {
			mesh.Size.Value = float2.One;
			mesh.ScaleUVWithSize.Value = false;
		}, 0, false, false, null);
		Slot iconSlot = slot.AddSlot("EditIcon", true);
		UnlitMaterial material = slot.World.GetSharedComponentOrCreate("TextGizmo_Mat_Icon_BetterGizmos", delegate (UnlitMaterial mat) {
			mat.Texture.Target = mat.Slot.AttachTexture(NeosAssets.Testing.UI.EditText, true, false, false, false, TextureWrapMode.Repeat, null);
		}, 0, false, false, null);
		iconSlot.AttachMesh(sharedComponentOrCreate, material, 0);
	}
	private static void GenerationEmptyIcon(Slot slot) {
		BevelSoliStripeMesh sharedComponentOrCreate = slot.World.GetSharedComponentOrCreate("EmptyGizmo_Icon_BetterGizmos", delegate (BevelSoliStripeMesh mesh) {
			mesh.Width = 1.4f;
		}, 0, false, false, null);
		FresnelMaterial material;
		if (Config.GetValue(RenderOnTop)) {
			material = slot.World.GetSharedComponentOrCreate("EmptyGizmo_Mat_Icon_Overlay_BetterGizmos", delegate (FresnelMaterial f) {
				f.NearColor.Value = new color(1, 0.25f, 0.25f, 1f);
				f.FarColor.Value = new color(0, 0, 1, 1f);
				f.ZTest.Value = ZTest.Always;
			}, 0, false, false, null);
		} else {
			material = slot.World.GetSharedComponentOrCreate("EmptyGizmo_Mat_Icon_BetterGizmos", delegate (FresnelMaterial f) {
				f.NearColor.Value = new color(1, 0.25f, 0.25f, 1f);
				f.FarColor.Value = new color(0, 0, 1, 1f);
			}, 0, false, false, null);
		}
		Slot matorb = slot.AddSlot("No Icon", true);
		matorb.LocalRotation = floatQ.AxisAngle(float3.Forward, 45f);
		matorb.AttachMesh(sharedComponentOrCreate, material, 0);
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
