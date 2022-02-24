using System.Collections.Generic;
using UnityEditor;

namespace Ace.Editor
{
    [CustomEditor(typeof(ACEExternalCamera))]
    [CanEditMultipleObjects]
    internal class ACEExternalCameraEditor 
        : VirtualCameraBaseEditor<ACEExternalCamera>
    {
        /// <summary>Get the property names to exclude in the inspector.</summary>
        /// <param name="excluded">Add the names to this list</param>
        protected override void GetExcludedPropertiesInInspector(List<string> excluded)
        {
            base.GetExcludedPropertiesInInspector(excluded);
            excluded.Add("Extensions");
        }
    }
}
