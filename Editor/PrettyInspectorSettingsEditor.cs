using UnityEngine;
using UnityEditor;

namespace HexTools.Editor
{
    [CustomEditor(typeof(PrettyInspectorSettings))]
    public class PrettyInspectorSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox($"{nameof(PrettyInspectorSettings)} should be edited from Preferences > Plugins", MessageType.Warning);
            if (GUILayout.Button("Open Preferences > Plugins > ..."))
                SettingsService.OpenUserPreferences(PrettyInspectorSettingsProvider.PREFERENCES_PATH);
        }
    }
}
