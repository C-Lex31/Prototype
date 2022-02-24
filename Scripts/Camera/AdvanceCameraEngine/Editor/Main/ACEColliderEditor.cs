#if !UNITY_2019_3_OR_NEWER
#define CINEMACHINE_PHYSICS
#define CINEMACHINE_PHYSICS_2D
#endif

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Ace.Editor
{
#if CINEMACHINE_PHYSICS
    [CustomEditor(typeof(ACECollider))]
    [CanEditMultipleObjects]
    internal sealed class ACEColliderEditor : BaseEditor<ACECollider>
    {
        /// <summary>Get the property names to exclude in the inspector.</summary>
        /// <param name="excluded">Add the names to this list</param>
        protected override void GetExcludedPropertiesInInspector(List<string> excluded)
        {
            base.GetExcludedPropertiesInInspector(excluded);
            if (!Target.m_AvoidObstacles)
            {
                excluded.Add(FieldPath(x => x.m_DistanceLimit));
                excluded.Add(FieldPath(x => x.m_CameraRadius));
                excluded.Add(FieldPath(x => x.m_Strategy));
                excluded.Add(FieldPath(x => x.m_MaximumEffort));
                excluded.Add(FieldPath(x => x.m_Damping));
                excluded.Add(FieldPath(x => x.m_DampingWhenOccluded));
                excluded.Add(FieldPath(x => x.m_SmoothingTime));
            }
            else if (Target.m_Strategy == ACECollider.ResolutionStrategy.PullCameraForward)
            {
                excluded.Add(FieldPath(x => x.m_MaximumEffort));
            }
        }

        public override void OnInspectorGUI()
        {
            BeginInspector();

            if (Target.m_AvoidObstacles && Target.VirtualCamera != null
                    && !Target.VirtualCamera.State.HasLookAt)
                EditorGUILayout.HelpBox(
                    "Avoid Obstacles requires a LookAt target.",
                    MessageType.Warning);

            DrawRemainingPropertiesInInspector();
        }

        [DrawGizmo(GizmoType.Active | GizmoType.Selected, typeof(ACECollider))]
        private static void DrawColliderGizmos(ACECollider collider, GizmoType type)
        {
            VirtualCamBase vcam = (collider != null) ? collider.VirtualCamera : null;
            if (vcam != null && collider.enabled)
            {
                Color oldColor = Gizmos.color;
                Vector3 pos = vcam.State.FinalPosition;
                if (collider.m_AvoidObstacles && vcam.State.HasLookAt)
                {
                    Gizmos.color = ACEColliderPrefs.FeelerColor;
                    if (collider.m_CameraRadius > 0)
                        Gizmos.DrawWireSphere(pos, collider.m_CameraRadius);

                    Vector3 forwardFeelerVector = (vcam.State.ReferenceLookAt - pos).normalized;
                    float distance = collider.m_DistanceLimit;
                    Gizmos.DrawLine(pos, pos + forwardFeelerVector * distance);

                    // Show the avoidance path, for debugging
                    List<List<Vector3>> debugPaths = collider.DebugPaths;
                    foreach (var path in debugPaths)
                    {
                        Gizmos.color = ACEColliderPrefs.FeelerHitColor;
                        Vector3 p0 = vcam.State.ReferenceLookAt;
                        foreach (var p in path)
                        {
                            Gizmos.DrawLine(p0, p);
                            p0 = p;
                        }
                        Gizmos.DrawLine(p0, pos);
                    }
                }
                Gizmos.color = oldColor;
            }
        }
    }
#endif
}
