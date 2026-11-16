using System.IO;
using System.Threading.Tasks;
using ReadLens.App;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Management;

namespace ReadLens.EditorTools
{
    // Command-line project setup and build, so the Quest config is reproducible without clicking through menus.
    //   Unity -batchmode -buildTarget Android -projectPath . -executeMethod ReadLens.EditorTools.Bootstrap.Setup
    //   Unity -batchmode -quit -buildTarget Android -projectPath . -executeMethod ReadLens.EditorTools.Bootstrap.Build
    public static class Bootstrap
    {
        const string ScenePath = "Assets/ReadLens/Scenes/Main.unity";
        const string ApkPath = "Builds/ReadLens.apk";

        [MenuItem("ReadLens/Setup Project")]
        public static async void Setup()
        {
            try
            {
                ConfigurePlayer();
                ConfigureXR();
                ConfigureMeta();
                ImportTmpEssentials();
                AssetDatabase.SaveAssets();
                // Fixes can unlock further tasks, so run a few passes.
                for (int i = 0; i < 3; i++) await OVRProjectSetup.FixAllAsync(BuildTargetGroup.Android);
                FixOpenXRIssues();
                AssetDatabase.SaveAssets();
                Debug.Log("[ReadLens] Setup done");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        [MenuItem("ReadLens/Build APK")]
        public static void Build()
        {
            CreateScene(); // always regenerated: the scene is an output of this script, not hand-edited
            Directory.CreateDirectory("Builds");
            var report = BuildPipeline.BuildPlayer(new[] { ScenePath }, ApkPath, BuildTarget.Android, BuildOptions.None);
            Debug.Log($"[ReadLens] Build {report.summary.result}, {report.summary.totalSize / 1e6:F1} MB");
            if (Application.isBatchMode && report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                EditorApplication.Exit(1);
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "ReadLens";
            PlayerSettings.productName = "ReadLens";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.readlens.quest");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
            PlayerSettings.colorSpace = ColorSpace.Linear;
        }

        static void ConfigureXR()
        {
            if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget perTarget))
            {
                Directory.CreateDirectory("Assets/XR");
                perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(perTarget, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perTarget, true);
            }
            if (!perTarget.HasSettingsForBuildTarget(BuildTargetGroup.Android)) perTarget.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Android);
            if (!perTarget.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android)) perTarget.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);
            XRPackageMetadataStore.AssignLoader(perTarget.ManagerSettingsForBuildTarget(BuildTargetGroup.Android),
                "UnityEngine.XR.OpenXR.OpenXRLoader", BuildTargetGroup.Android);
        }

        static void ConfigureMeta()
        {
            var config = OVRProjectConfig.CachedProjectConfig;
            config.handTrackingSupport = OVRProjectConfig.HandTrackingSupport.ControllersAndHands;
            config.insightPassthroughSupport = OVRProjectConfig.FeatureSupport.Required;
            config.sceneSupport = OVRProjectConfig.FeatureSupport.Supported;
            config.isPassthroughCameraAccessEnabled = true;
            OVRProjectConfig.CommitProjectConfig(config);
        }

        static void FixOpenXRIssues()
        {
            var issues = new System.Collections.Generic.List<UnityEngine.XR.OpenXR.Features.OpenXRFeature.ValidationRule>();
            for (int pass = 0; pass < 3; pass++)
            {
                UnityEditor.XR.OpenXR.OpenXRProjectValidation.GetCurrentValidationIssues(issues, BuildTargetGroup.Android);
                foreach (var issue in issues)
                {
                    Debug.Log($"[ReadLens] OpenXR {(issue.error ? "error" : "warning")}: {issue.message}");
                    if (issue.fixItAutomatic) issue.fixIt?.Invoke();
                }
            }
        }

        static void ImportTmpEssentials()
        {
            if (AssetDatabase.FindAssets("t:TMP_Settings").Length > 0) return;
            var pkg = Directory.GetFiles("Library/PackageCache", "TMP Essential Resources.unitypackage", SearchOption.AllDirectories)[0];
            AssetDatabase.ImportPackage(pkg, false);
        }

        // Window: 36 x 40 cm (1 UI unit = 0.4 mm), 50 cm away, slightly below eye level and tilted like a book stand.
        static readonly Vector2 WindowPixels = new Vector2(900, 1000);
        const float UnitsToMeters = 0.0004f;
        const int UILayer = 5;

        static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);

            var rig = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.meta.xr.sdk.core/Prefabs/OVRCameraRig.prefab"));
            var manager = rig.GetComponent<OVRManager>() ?? rig.AddComponent<OVRManager>();
            manager.isInsightPassthroughEnabled = true;
            manager.trackingOriginType = OVRManager.TrackingOrigin.EyeLevel;
            var managerSo = new SerializedObject(manager);
            managerSo.FindProperty("requestPassthroughCameraAccessPermissionOnStartup").boolValue = true;
            managerSo.ApplyModifiedPropertiesWithoutUndo();
            rig.AddComponent<OVRPassthroughLayer>();
            foreach (var cam in rig.GetComponentsInChildren<Camera>())
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.clear; // passthrough shows behind the window
                cam.cullingMask &= ~(1 << UILayer); // the window reaches the eyes through its compositor layer
            }
            Oculus.Interaction.OVR.Editor.QuickActions.OVRQuickActionsAPI.AddOVRInteractionRig();
            var head = rig.transform.Find("TrackingSpace/CenterEyeAnchor");
            // The interaction rig ships with locomotion (thumb-swipe snap turn). In a seated MR reading app
            // turning only makes the window jump sideways, so it is off and the thumb swipe turns pages instead.
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.name == "Locomotor") t.gameObject.SetActive(false);
            var thumbHands = new GameObject("Thumb Gestures");
            var leftHand = HandSource(rig.transform.Find("TrackingSpace/LeftHandAnchor"), OVRHand.Hand.HandLeft);
            var rightHand = HandSource(rig.transform.Find("TrackingSpace/RightHandAnchor"), OVRHand.Hand.HandRight);
            var thumbs = thumbHands.AddComponent<ThumbGestures>();
            Wire(thumbs, ("left", leftHand), ("right", rightHand));

            var window = new GameObject("ReadLens Window");
            window.transform.SetPositionAndRotation(new Vector3(0f, -0.12f, 0.5f), Quaternion.Euler(12f, 0f, 0f));

            // The canvas renders into a texture (2x supersampled) shown as a compositor layer:
            // the headset samples it once at display resolution, so text stays sharp.
            var canvasGo = new GameObject("Canvas", typeof(Canvas)) { layer = UILayer };
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var canvas = (RectTransform)canvasGo.transform;
            canvas.SetParent(window.transform, false);
            canvas.sizeDelta = WindowPixels;
            canvas.localScale = Vector3.one * UnitsToMeters;
            var renderTexture = canvasGo.AddComponent<Oculus.Interaction.UnityCanvas.CanvasRenderTexture>();
            renderTexture.InjectAllCanvasRenderTexture(canvasGo.GetComponent<Canvas>(), Mathf.RoundToInt(1f / UnitsToMeters), 2, 1 << UILayer, false);
            Oculus.Interaction.Editor.QuickActions.QuickActionsAPI.AddPokeCanvasInteraction(canvasGo);
            Oculus.Interaction.Editor.QuickActions.QuickActionsAPI.AddRayCanvasInteraction(canvasGo);

            var surface = new GameObject("Window Surface", typeof(MeshFilter), typeof(MeshRenderer));
            surface.transform.SetParent(window.transform, false);
            var rect = surface.AddComponent<Oculus.Interaction.UnityCanvas.CanvasRect>();
            rect.InjectAllCanvasRect(renderTexture, surface.GetComponent<MeshFilter>());
            var layerRenderer = surface.AddComponent<Oculus.Interaction.UnityCanvas.OVRCanvasMeshRenderer>();
            layerRenderer.InjectAllOVRCanvasMeshRenderer(renderTexture, surface.GetComponent<MeshRenderer>(), rect);
            // Underlay: hands drawn in the eye buffer stay visible in front of the window.
            layerRenderer.InjectOptionalRenderingMode(Oculus.Interaction.UnityCanvas.OVRRenderingMode.Underlay);
            layerRenderer.InjectOptionalDoUnderlayAntiAliasing(true);
            layerRenderer.InjectOptionalEnableSuperSampling(true);

            // Grab bar under the window: pinch it (near or with the ray) to move the whole window.
            var handle = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            handle.name = "Move Handle";
            handle.transform.SetParent(window.transform, false);
            handle.transform.localPosition = new Vector3(0f, -WindowPixels.y * UnitsToMeters / 2f - 0.035f, 0f);
            handle.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            handle.transform.localScale = new Vector3(0.022f, 0.08f, 0.022f);
            handle.GetComponent<Renderer>().sharedMaterial = Mat("Handle", "Standard", new Color(0.86f, 0.88f, 0.92f));
            Oculus.Interaction.Editor.QuickActions.QuickActionsAPI.AddGrabInteraction(handle);
            Oculus.Interaction.Editor.QuickActions.QuickActionsAPI.AddRayGrabInteraction(handle);
            var grabbable = handle.GetComponent<Oculus.Interaction.Grabbable>();
            grabbable.InjectOptionalTargetTransform(window.transform);
            var body = handle.GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            // "Page detected" suggestion: a small separate canvas that floats under the detected text.
            var badgeGo = new GameObject("Page Detected Badge", typeof(Canvas));
            badgeGo.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var badge = (RectTransform)badgeGo.transform;
            badge.sizeDelta = new Vector2(640, 130);
            badge.localScale = Vector3.one * UnitsToMeters;
            Oculus.Interaction.Editor.QuickActions.QuickActionsAPI.AddPokeCanvasInteraction(badgeGo);
            Oculus.Interaction.Editor.QuickActions.QuickActionsAPI.AddRayCanvasInteraction(badgeGo);

            var outlineGo = new GameObject("Frame Outline", typeof(LineRenderer));
            var outline = outlineGo.GetComponent<LineRenderer>();
            outline.sharedMaterial = Mat("Outline", "Sprites/Default", Color.white);
            outline.useWorldSpace = true;
            outline.enabled = false;

            var services = new GameObject("Services");
            var cameraAccess = services.AddComponent<Meta.XR.PassthroughCameraAccess>();
            var ocr = services.AddComponent<OcrService>();
            Wire(ocr, ("cameraAccess", cameraAccess));
            services.AddComponent<DisplayQuality>();
            var frame = services.AddComponent<FrameGesture>();
            Wire(frame, ("outline", outline), ("head", head));

            var follow = window.AddComponent<WindowFollow>();
            Wire(follow, ("head", head), ("grab", grabbable));

            Wire(window.AddComponent<ReadLensApp>(),
                ("canvas", canvas),
                ("badgeCanvas", badge),
                ("head", head),
                ("openDyslexic", AssetDatabase.LoadAssetAtPath<Font>("Assets/ReadLens/Fonts/OpenDyslexic-Regular.otf")),
                ("atkinson", AssetDatabase.LoadAssetAtPath<Font>("Assets/ReadLens/Fonts/AtkinsonHyperlegible-Regular.ttf")),
                ("atkinsonBold", AssetDatabase.LoadAssetAtPath<Font>("Assets/ReadLens/Fonts/AtkinsonHyperlegible-Bold.ttf")),
                ("classic", AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset")),
                ("ocr", ocr),
                ("frameGesture", frame),
                ("follow", follow),
                ("thumbGestures", thumbs));

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        static Material Mat(string name, string shader, Color color)
        {
            var path = $"Assets/ReadLens/Materials/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            mat = new Material(Shader.Find(shader)) { color = color };
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static OVRHand HandSource(Transform anchor, OVRHand.Hand type)
        {
            var go = new GameObject(type + " Microgestures");
            go.transform.SetParent(anchor, false);
            var hand = go.AddComponent<OVRHand>();
            var so = new SerializedObject(hand);
            so.FindProperty("HandType").intValue = (int)type;
            so.ApplyModifiedPropertiesWithoutUndo();
            return hand;
        }

        static void Wire(Object target, params (string field, Object value)[] refs)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in refs)
            {
                var prop = so.FindProperty(field) ?? throw new System.ArgumentException($"{target.GetType().Name}.{field} not found");
                prop.objectReferenceValue = value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
