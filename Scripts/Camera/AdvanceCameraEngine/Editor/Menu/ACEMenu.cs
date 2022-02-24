#if !UNITY_2019_3_OR_NEWER
#define CINEMACHINE_PHYSICS
#define CINEMACHINE_PHYSICS_2D
#endif

using UnityEngine;
using UnityEditor;
using System;
using System.IO;

namespace Ace.Editor
{
    internal static class ACEMenu
    {



        [MenuItem("Ace/Create Virtual Camera", false, 1)]
        public static ACEVirtualCamera CreateVirtualCamera()
        {
            return InternalCreateVirtualCamera(
                "CM vcam", true);
        }
        [MenuItem("Ace/Create FreeLook Camera", false, 1)]
        private static void CreateFreeLookCamera()
        {
            CreateCameraBrainIfAbsent();
            GameObject go = InspectorUtility.CreateGameObject(
                    GenerateUniqueObjectName(typeof(ACEFreeLook), "CM FreeLook"),
                    typeof(ACEFreeLook));
            if (SceneView.lastActiveSceneView != null)
                go.transform.position = SceneView.lastActiveSceneView.pivot;
            Selection.activeGameObject = go;
        }




        [MenuItem("Ace/Create State-Driven Camera", false, 1)]
        private static void CreateStateDivenCamera()
        {
            CreateCameraBrainIfAbsent();
            GameObject go = InspectorUtility.CreateGameObject(
                    GenerateUniqueObjectName(typeof(ACEStateDrivenCamera), "CM StateDrivenCamera"),
                    typeof(ACEStateDrivenCamera));
            if (SceneView.lastActiveSceneView != null)
                go.transform.position = SceneView.lastActiveSceneView.pivot;
            Undo.RegisterCreatedObjectUndo(go, "create state driven camera");
            Selection.activeGameObject = go;

            // Give it a child
            Undo.SetTransformParent(CreateDefaultVirtualCamera().transform, go.transform, "create state driven camera");
        }

#if CINEMACHINE_PHYSICS
        [MenuItem("Ace/Create ClearShot Camera", false, 1)]
        private static void CreateClearShotVirtualCamera()
        {
            CreateCameraBrainIfAbsent();
            GameObject go = InspectorUtility.CreateGameObject(
                    GenerateUniqueObjectName(typeof(CinemachineClearShot), "CM ClearShot"),
                    typeof(CinemachineClearShot));
            if (SceneView.lastActiveSceneView != null)
                go.transform.position = SceneView.lastActiveSceneView.pivot;
            Undo.RegisterCreatedObjectUndo(go, "create ClearShot camera");
            Selection.activeGameObject = go;

            // Give it a child
            var child = CreateDefaultVirtualCamera();
            Undo.SetTransformParent(child.transform, go.transform, "create ClearShot camera");
            var collider = Undo.AddComponent<CinemachineCollider>(child.gameObject);
            collider.m_AvoidObstacles = false;
            Undo.RecordObject(collider, "create ClearShot camera");
        }
#endif











#if !UNITY_2019_1_OR_NEWER
        [MenuItem("Ace/Import Post Processing V2 Adapter Asset Package")]
        private static void ImportPostProcessingV2Package()
        {
            var message = "In Cinemachine 2.4.0 and up, the PostProcessing adapter is built-in, and "
                + "can be auto-enabled by Unity 2019 and up.\n\n"
                + "Unity 2018.4 is unable to auto-detect the presence of PostProcessing, so you must "
                + "manually add a define to your player settings to enable the code.\n\n"
                + "To enable support for PostProcessing v2, please do the following:\n\n"
                + "1. Delete the CinemachinePostProcessing folder from your assets, if it's present\n\n"
                + "2. Open the Player Settings tab in Project Settings\n\n"
                + "3. Add this define: CINEMACHINE_POST_PROCESSING_V2";

            EditorUtility.DisplayDialog("Cinemachine Adapter Code for PostProcessing V2", message, "OK");
        }

        [MenuItem("Ace/Import CinemachineExamples Asset Package")]
        private static void ImportExamplePackage()
        {
            string pkgFile = ScriptableObjectUtility.CinemachineInstallPath
                + "/Extras~/CinemachineExamples.unitypackage";
            if (!System.IO.File.Exists(pkgFile))
                Debug.LogError("Missing file " + pkgFile);
            else
                AssetDatabase.ImportPackage(pkgFile, true);
        }
#endif

        /// <summary>
        /// Create a default Virtual Camera, with standard components
        /// </summary>
        public static ACEVirtualCamera CreateDefaultVirtualCamera()
        {
            return InternalCreateVirtualCamera(
                "CM vcam", false);
        }

        /// <summary>
        /// Create a static Virtual Camera, with no procedural components
        /// </summary>
        public static ACEVirtualCamera CreateStaticVirtualCamera()
        {
            return InternalCreateVirtualCamera("CM vcam", false);
        }

        /// <summary>
        /// Create a Virtual Camera, with components
        /// </summary>
        static ACEVirtualCamera InternalCreateVirtualCamera(
            string name, bool selectIt, params Type[] components)
        {
            // Create a new virtual camera
            var brain = CreateCameraBrainIfAbsent();
            GameObject go = InspectorUtility.CreateGameObject(
                    GenerateUniqueObjectName(typeof(ACEVirtualCamera), name),
                    typeof(ACEVirtualCamera));
            ACEVirtualCamera vcam = go.GetComponent<ACEVirtualCamera>();
            SetVcamFromSceneView(vcam);
            Undo.RegisterCreatedObjectUndo(go, "create " + name);
            GameObject componentOwner = vcam.GetComponentOwner().gameObject;
            foreach (Type t in components)
                Undo.AddComponent(componentOwner, t);
            vcam.InvalidateComponentPipeline();
            if (brain != null && brain.OutputCamera != null)
                vcam.m_Lens = LensSettings.FromCamera(brain.OutputCamera);
            if (selectIt)
                Selection.activeObject = go;
            return vcam;
        }

        public static void SetVcamFromSceneView(ACEVirtualCamera vcam)
        {
            if (SceneView.lastActiveSceneView != null)
            {
                vcam.transform.position = SceneView.lastActiveSceneView.camera.transform.position;
                vcam.transform.rotation = SceneView.lastActiveSceneView.camera.transform.rotation;
                var lens = LensSettings.FromCamera(SceneView.lastActiveSceneView.camera);
                // Don't grab these
                lens.NearClipPlane = LensSettings.Default.NearClipPlane;
                lens.FarClipPlane = LensSettings.Default.FarClipPlane;
                vcam.m_Lens = lens;
            }
        }

        /// <summary>
        /// If there is no ACEBrain in the scene, try to create one on the main camera
        /// </summary>
        public static ACEBrain CreateCameraBrainIfAbsent()
        {
            ACEBrain[] brains = UnityEngine.Object.FindObjectsOfType(
                    typeof(ACEBrain)) as ACEBrain[];
            ACEBrain brain = (brains != null && brains.Length > 0) ? brains[0] : null;
            if (brain == null)
            {
                Camera cam = Camera.main;
                if (cam == null)
                {
                    Camera[] cams = UnityEngine.Object.FindObjectsOfType(
                            typeof(Camera)) as Camera[];
                    if (cams != null && cams.Length > 0)
                        cam = cams[0];
                }
                if (cam != null)
                {
                    brain = Undo.AddComponent<ACEBrain>(cam.gameObject);
                }
            }
            return brain;
        }

        /// <summary>
        /// Generate a unique name with the given prefix by adding a suffix to it
        /// </summary>
        public static string GenerateUniqueObjectName(Type type, string prefix)
        {
            int count = 0;
            UnityEngine.Object[] all = Resources.FindObjectsOfTypeAll(type);
            foreach (UnityEngine.Object o in all)
            {
                if (o != null && o.name.StartsWith(prefix))
                {
                    string suffix = o.name.Substring(prefix.Length);
                    int i;
                    if (Int32.TryParse(suffix, out i) && i > count)
                        count = i;
                }
            }
            return prefix + (count + 1);
        }
    }
}
