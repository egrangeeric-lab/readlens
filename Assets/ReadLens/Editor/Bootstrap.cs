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

        // Window: 48 x 57.5 cm (1 UI unit = 0.5 mm), 62 cm away, slightly below eye level and tilted like a book stand.
        static readonly Vector2 WindowPixels = new Vector2(960, 1150);
        const float UnitsToMeters = 0.0005f;

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
            }
            Oculus.Interaction.OVR.Editor.QuickActions.OVRQuickActionsAPI.AddOVRInteractionRig();

            var window = new GameObject("ReadLens Window");
            window.transform.SetPositionAndRotation(new Vector3(0f, -0.1f, 0.62f), Quaternion.Euler(12f, 0f, 0f));

            var canvasGo = new GameObject("Canvas", typeof(Canvas));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var canvas = (RectTransform)canvasGo.transform;
            canvas.SetParent(window.transform, false);
            canvas.sizeDelta = WindowPixels;
            canvas.localScale = Vector3.one * UnitsToMeters;
            Oculus.Interaction.Editor.QuickActions.QuickActionsAPI.AddPokeCanvasInteraction(canvasGo);
            Oculus.Interaction.Editor.QuickActions.QuickActionsAPI.AddRayCanvasInteraction(canvasGo);

            // Grab bar under the window: pinch it (near or with the ray) to move the whole window.
            var handle = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            handle.name = "Move Handle";
            handle.transform.SetParent(window.transform, false);
            handle.transform.localPosition = new Vector3(0f, -WindowPixels.y * UnitsToMeters / 2f - 0.045f, 0f);
            handle.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            handle.transform.localScale = new Vector3(0.024f, 0.09f, 0.024f);
            handle.GetComponent<Renderer>().sharedMaterial = HandleMaterial();
            Oculus.Interaction.Editor.QuickActions.QuickActionsAPI.AddGrabInteraction(handle);
            Oculus.Interaction.Editor.QuickActions.QuickActionsAPI.AddRayGrabInteraction(handle);
            handle.GetComponent<Oculus.Interaction.Grabbable>().InjectOptionalTargetTransform(window.transform);
            var body = handle.GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            var services = new GameObject("Services");
            var cameraAccess = services.AddComponent<Meta.XR.PassthroughCameraAccess>();
            var ocr = services.AddComponent<OcrService>();

            var app = window.AddComponent<ReadLensApp>();
            var so = new SerializedObject(app);
            so.FindProperty("canvas").objectReferenceValue = canvas;
            so.FindProperty("openDyslexic").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>("Assets/ReadLens/Fonts/OpenDyslexic-Regular.otf");
            so.FindProperty("atkinson").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>("Assets/ReadLens/Fonts/AtkinsonHyperlegible-Regular.ttf");
            so.FindProperty("atkinsonBold").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>("Assets/ReadLens/Fonts/AtkinsonHyperlegible-Bold.ttf");
            so.FindProperty("classic").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            so.FindProperty("cameraAccess").objectReferenceValue = cameraAccess;
            so.FindProperty("ocr").objectReferenceValue = ocr;
            so.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        static Material HandleMaterial()
        {
            const string path = "Assets/ReadLens/Materials/Handle.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            mat = new Material(Shader.Find("Standard")) { color = new Color(0.86f, 0.88f, 0.92f) };
            mat.SetFloat("_Glossiness", 0.6f);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }
    }
}
