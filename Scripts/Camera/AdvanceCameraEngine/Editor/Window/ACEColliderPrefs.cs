using UnityEngine;
using UnityEditor;

using Ace.Editor;

namespace Ace
{
    [InitializeOnLoad]
    internal static class ACEColliderPrefs
    {
        private static bool SettingsFoldedOut
        {
            get { return EditorPrefs.GetBool(kColliderSettingsFoldoutKey, false); }
            set
            {
                if (value != SettingsFoldedOut)
                {
                    EditorPrefs.SetBool(kColliderSettingsFoldoutKey, value);
                }
            }
        }

        public static Color FeelerHitColor
        {
            get
            {
                return ACESettings.UnpackColour(EditorPrefs.GetString(kFeelerHitColourKey, ACESettings.PackColor(Color.yellow)));
            }

            set
            {
                if (value != FeelerHitColor)
                {
                    EditorPrefs.SetString(kFeelerHitColourKey, ACESettings.PackColor(value));
                }
            }
        }

        public static Color FeelerColor
        {
            get
            {
                return ACESettings.UnpackColour(EditorPrefs.GetString(kFeelerColourKey, ACESettings.PackColor(Color.gray)));
            }

            set
            {
                if (value != FeelerColor)
                {
                    EditorPrefs.SetString(kFeelerColourKey, ACESettings.PackColor(value));
                }
            }
        }

        private const string kColliderSettingsFoldoutKey  = "CNMCN_Collider_Foldout";
        private const string kFeelerHitColourKey          = "CNMCN_Collider_FeelerHit_Colour";
        private const string kFeelerColourKey             = "CNMCN_Collider_Feeler_Colour";

        static ACEColliderPrefs()
        {
            Ace.Editor.ACESettings.AdditionalCategories += DrawColliderSettings;
        }

        private static void DrawColliderSettings()
        {
            SettingsFoldedOut = EditorGUILayout.Foldout(SettingsFoldedOut, "Collider Settings", true);
            if (SettingsFoldedOut)
            {
                EditorGUI.indentLevel++;

                EditorGUI.BeginChangeCheck();

                FeelerHitColor   = EditorGUILayout.ColorField("Feeler Hit", FeelerHitColor);
                FeelerColor = EditorGUILayout.ColorField("Feeler", FeelerColor);

                if (EditorGUI.EndChangeCheck())
                {
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                }

                EditorGUI.indentLevel--;
            }
        }
    }
}
