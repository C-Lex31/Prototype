using UnityEngine;
using UnityEditor;
using Ace.Editor;
using System.Collections.Generic;
using Ace.Utility;

namespace Ace
{
    [CustomEditor(typeof(ACEFreeLook))]
    [CanEditMultipleObjects]
    internal sealed class ACEFreeLookEditor
        : VirtualCameraBaseEditor<ACEFreeLook>
    {
        /// <summary>Get the property names to exclude in the inspector.</summary>
        /// <param name="excluded">Add the names to this list</param>
        protected override void GetExcludedPropertiesInInspector(List<string> excluded)
        {
            base.GetExcludedPropertiesInInspector(excluded);
            excluded.Add(FieldPath(x => x.m_Orbits));
            if (!Target.m_CommonLens)
                excluded.Add(FieldPath(x => x.m_Lens));
            if (Target.m_BindingMode == Transposer.BindingMode.SimpleFollowWithWorldUp)
            {
                excluded.Add(FieldPath(x => x.m_Heading));
                excluded.Add(FieldPath(x => x.m_RecenterToTargetHeading));
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Target.UpdateInputAxisProvider();
        }
        
        protected override void OnDisable()
        {
            base.OnDisable();

            // Must destroy child editors or we get exceptions
            if (m_editors != null)
                foreach (UnityEditor.Editor e in m_editors)
                    if (e != null)
                        UnityEngine.Object.DestroyImmediate(e);
        }

        public override void OnInspectorGUI()
        {
            Target.m_XAxis.ValueRangeLocked
                = (Target.m_BindingMode == Transposer.BindingMode.SimpleFollowWithWorldUp);

            // Ordinary properties
            BeginInspector();
            DrawHeaderInInspector();
            DrawPropertyInInspector(FindProperty(x => x.m_Priority));
            DrawTargetsInInspector(FindProperty(x => x.m_Follow), FindProperty(x => x.m_LookAt));
            DrawRemainingPropertiesInInspector();

            // Orbits
            EditorGUI.BeginChangeCheck();
            SerializedProperty orbits = FindProperty(x => x.m_Orbits);
            for (int i = 0; i < ACEFreeLook.RigNames.Length; ++i)
            {
                Rect rect = EditorGUILayout.GetControlRect(true);
                SerializedProperty orbit = orbits.GetArrayElementAtIndex(i);
                InspectorUtility.MultiPropertyOnLine(rect,
                    new GUIContent(ACEFreeLook.RigNames[i]),
                    new [] { orbit.FindPropertyRelative(() => Target.m_Orbits[i].m_Height),
                            orbit.FindPropertyRelative(() => Target.m_Orbits[i].m_Radius) },
                    null);
            }
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();

            // Rigs
            if (Selection.objects.Length == 1)
            {
                UpdateRigEditors();
                for (int i = 0; i < m_editors.Length; ++i)
                {
                    if (m_editors[i] == null)
                        continue;
                    EditorGUILayout.Separator();
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    EditorGUILayout.LabelField(RigNames[i], EditorStyles.boldLabel);
                    ++EditorGUI.indentLevel;
                    m_editors[i].OnInspectorGUI();
                    --EditorGUI.indentLevel;
                    EditorGUILayout.EndVertical();
                }
            }
            
            // Extensions
            DrawExtensionsWidgetInInspector();
        }

        string[] RigNames;
        VirtualCamBase[] m_rigs;
        UnityEditor.Editor[] m_editors;
        void UpdateRigEditors()
        {
            RigNames = ACEFreeLook.RigNames;
            if (m_rigs == null)
                m_rigs = new VirtualCamBase[RigNames.Length];
            if (m_editors == null)
                m_editors = new UnityEditor.Editor[RigNames.Length];
            for (int i = 0; i < RigNames.Length; ++i)
            {
                ACEVirtualCamera rig = Target.GetRig(i);
                if (rig == null || rig != m_rigs[i])
                {
                    m_rigs[i] = rig;
                    if (m_editors[i] != null)
                        UnityEngine.Object.DestroyImmediate(m_editors[i]);
                    m_editors[i] = null;
                    if (rig != null)
                        CreateCachedEditor(rig, null, ref m_editors[i]);
                }
            }
        }

        /// <summary>
        /// Register with ACEFreeLook to create the pipeline in an undo-friendly manner
        /// </summary>
        [InitializeOnLoad]
        class CreateRigWithUndo
        {
            static CreateRigWithUndo()
            {
                ACEFreeLook.CreateRigOverride
                    = (ACEFreeLook vcam, string name, ACEVirtualCamera copyFrom) =>
                    {
                        // Create a new rig with default components
                        GameObject go = InspectorUtility.CreateGameObject(name);
                        Undo.RegisterCreatedObjectUndo(go, "created rig");
                        Undo.SetTransformParent(go.transform, vcam.transform, "parenting rig");
                        ACEVirtualCamera rig = Undo.AddComponent<ACEVirtualCamera>(go);
                        Undo.RecordObject(rig, "creating rig");
                        if (copyFrom != null)
                            ReflectionHelpers.CopyFields(copyFrom, rig);
                        else
                        {
                            go = rig.GetComponentOwner().gameObject;
                            Undo.RecordObject(Undo.AddComponent<OrbitalTransposer>(go), "creating rig");
                            Undo.RecordObject(Undo.AddComponent<Composer>(go), "creating rig");
                        }
                        return rig;
                    };
                ACEFreeLook.DestroyRigOverride = (GameObject rig) =>
                    {
                        Undo.DestroyObjectImmediate(rig);
                    };
            }
        }

        [DrawGizmo(GizmoType.Active | GizmoType.Selected, typeof(ACEFreeLook))]
        private static void DrawFreeLookGizmos(ACEFreeLook vcam, GizmoType selectionType)
        {
            // Standard frustum and logo
            ACEBrainEditor.DrawVirtualCameraBaseGizmos(vcam, selectionType);

            Color originalGizmoColour = Gizmos.color;
            bool isActiveVirtualCam = ACECore.Instance.IsLive(vcam);
            Gizmos.color = isActiveVirtualCam
                ? ACESettings.ACECoreSettings.ActiveGizmoColour
                : ACESettings.ACECoreSettings.InactiveGizmoColour;

            if (vcam.Follow != null)
            {
                Vector3 pos = vcam.Follow.position;
                Vector3 up = vcam.State.ReferenceUp;

                var MiddleRig = vcam.GetRig(1).GetCinemachineComponent<OrbitalTransposer>();
                if (MiddleRig != null)
                {
                    Quaternion orient = MiddleRig.GetReferenceOrientation(up);
                    up = orient * Vector3.up;
                    float rotation = vcam.m_XAxis.Value + vcam.m_Heading.m_Bias;
                    orient = Quaternion.AngleAxis(rotation, up) * orient;

                    ACEOrbitalTransposerEditor.DrawCircleAtPointWithRadius(
                        pos + up * vcam.m_Orbits[0].m_Height, orient, vcam.m_Orbits[0].m_Radius);
                    ACEOrbitalTransposerEditor.DrawCircleAtPointWithRadius(
                        pos + up * vcam.m_Orbits[1].m_Height, orient, vcam.m_Orbits[1].m_Radius);
                    ACEOrbitalTransposerEditor.DrawCircleAtPointWithRadius(
                        pos + up * vcam.m_Orbits[2].m_Height, orient, vcam.m_Orbits[2].m_Radius);

                    DrawCameraPath(pos, orient, vcam);
                }
            }

            Gizmos.color = originalGizmoColour;
        }

        private static void DrawCameraPath(Vector3 atPos, Quaternion orient, ACEFreeLook vcam)
        {
            Matrix4x4 prevMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(atPos, orient, Vector3.one);

            const int kNumSteps = 20;
            Vector3 currPos = vcam.GetLocalPositionForCameraFromInput(0f);
            for (int i = 1; i < kNumSteps + 1; ++i)
            {
                float t = (float)i / (float)kNumSteps;
                Vector3 nextPos = vcam.GetLocalPositionForCameraFromInput(t);
                Gizmos.DrawLine(currPos, nextPos);
                currPos = nextPos;
            }
            Gizmos.matrix = prevMatrix;
        }
    }
}
