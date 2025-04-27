using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace HexTools.Editor
{
    public class PrettyInspectorSettingsProvider : SettingsProvider
    {
        private const string VERSION = "1.0.0";
        private const string OVERRIDES_FOLDOUT_STATE_KEY = "prttyins-overrides-foldout-state";
        private const string DEFAULT_FOLDOUT_STATE_KEY = "prttyins-default-foldout-state";
        private const string PADDING_FOLDOUT_STATE_KEY = "prttyins-padding-foldout-state";
        private const string PADDING_FOLDOUT_STATE_KEY_GLOBAL = "prttyins-padding-foldout-state-global";
        static PrettyInspectorSettings m_Settings;
        private PrettyInspectorSettings.Profile m_LastEditedProfile;
        SerializedProperty m_DefaultProfile;
        SerializedProperty m_TopColor;
        SerializedProperty m_BottomColor;
        SerializedProperty m_EnableOverrides;
        SerializedProperty m_EnablePreview;
        SerializedProperty m_OverrideKeys;
        SerializedProperty m_OverrideValues;
        SerializedObject m_SerializedObject;
        GUIStyle m_Style;
        Dictionary<string, PrettyInspectorSettings.Profile> m_Overrides;
        private SerializedObject serializedObject
        {
            get
            {
                if (m_SerializedObject == null)
                    m_SerializedObject = new SerializedObject(m_Settings);
                return m_SerializedObject;
            }
        }


        public const string PREFERENCES_PATH = "Plugins/Pretty Inspector";
        private static PrettyInspectorSettingsProvider provider;
        private PrettyInspectorSettingsProvider(string path, SettingsScope scope) : base(path, scope) { }

        [SettingsProvider]
        private static SettingsProvider GetSettingsProvider()
        {
            provider ??= new PrettyInspectorSettingsProvider(PREFERENCES_PATH, SettingsScope.User);
            return provider;
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            if (m_Settings == null)
                m_Settings = GetEditorSettings();
            m_DefaultProfile = serializedObject.FindProperty("m_DefaultProfile");
            m_TopColor = serializedObject.FindProperty("m_TopColor");
            m_BottomColor = serializedObject.FindProperty("m_BottomColor");
            m_EnableOverrides = serializedObject.FindProperty("m_EnableOverrides");
            m_OverrideKeys = serializedObject.FindProperty("m_OverrideKeys");
            m_OverrideValues = serializedObject.FindProperty("m_OverrideValues");
            m_EnablePreview = serializedObject.FindProperty("m_EnablePreview");
            m_Overrides = m_Settings.overrides;
            m_LastEditedProfile = m_Settings.defaultProfile;
            m_Style = new GUIStyle(EditorStyles.label);
            m_Style.alignment = TextAnchor.MiddleCenter;
            base.OnActivate(searchContext, rootElement);
        }
        public override void OnGUI(string _)
        {
            bool resetFired = false;
            serializedObject.Update();
            if (m_EnablePreview.boolValue)
                DrawPreview();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(m_EnablePreview);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Reset To Default"))
                resetFired = true;
            if (GUILayout.Button(new GUIContent("Github", "Redirect you to the Official Github page of this plugin.")))
                Application.OpenURL("https://github.com/hexdogstudio/pretty-inspector");
            EditorGUILayout.EndHorizontal();
            bool state = SessionState.GetBool(DEFAULT_FOLDOUT_STATE_KEY, true);
            SessionState.SetBool(DEFAULT_FOLDOUT_STATE_KEY, EditorGUILayout.BeginFoldoutHeaderGroup(state, "Default Profile"));
            if (state)
            {
                EditorGUI.indentLevel++;
                DrawProfile(null, m_DefaultProfile, false, PADDING_FOLDOUT_STATE_KEY);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.PropertyField(m_TopColor);
            EditorGUILayout.PropertyField(m_BottomColor);

            EditorGUILayout.PropertyField(m_EnableOverrides, new GUIContent("Enable Overrides", "Allowing individual styles based on Classes that are inherits from the MonoBehaviour super Class."));
            if (m_EnableOverrides.boolValue)
            {
                GUILayout.Space(10);
                DrawDictionary();
            }
            if (resetFired)
            {
                m_Settings.Reset();
                m_LastEditedProfile = m_Settings.defaultProfile;
                EditorUtility.SetDirty(m_Settings);
            }
            serializedObject.ApplyModifiedProperties();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"v{VERSION}", m_Style);
        }

        private void DrawPreview()
        {
            m_LastEditedProfile ??= m_Settings.defaultProfile;
            Color background = m_LastEditedProfile == m_Settings.defaultProfile ?
                m_Settings.topColor : m_LastEditedProfile.background;
            Rect rect = EditorGUILayout.BeginVertical();
            rect.width += PrettyInspector.EXTRA_WIDTH;
            rect.x -= PrettyInspector.EXTRA_WIDTH / 2;
            rect.height += PrettyInspector.EXTRA_HEIGHT;
            rect.y -= PrettyInspector.EXTRA_HEIGHT / 2;
            EditorGUI.DrawRect(rect, background);
            EditorGUILayout.LabelField("Preview", m_LastEditedProfile.style, GUILayout.Height(m_LastEditedProfile.fontSize + 10));
            EditorGUILayout.EndVertical();
        }
        private void DrawDictionary()
        {
            bool state = SessionState.GetBool(OVERRIDES_FOLDOUT_STATE_KEY, true);
            SessionState.SetBool(OVERRIDES_FOLDOUT_STATE_KEY, EditorGUILayout.BeginFoldoutHeaderGroup(state, "Overrides"));
            if (state)
            {
                Color lineColor = new Color(0, 0, 0, 0.25f);
                int toRemoveIdx = -1;
                int length = m_OverrideKeys.arraySize;
                EditorGUI.indentLevel++;
                for (int i = 0; i < length; i++)
                {
                    if (i != 0)
                    {
                        GUILayout.Space(5);
                        DrawLine(lineColor);
                        GUILayout.Space(5);
                    }
                    EditorGUILayout.BeginHorizontal();
                    SerializedProperty key = m_OverrideKeys.GetArrayElementAtIndex(i);
                    SerializedProperty profile = m_OverrideValues.GetArrayElementAtIndex(i);
                    EditorGUILayout.PropertyField(key, new GUIContent("Class"));
                    if (GUILayout.Button(new GUIContent("-", "Remove Override"), GUILayout.Width(22)))
                        toRemoveIdx = i;
                    EditorGUILayout.EndHorizontal();
                    DrawProfile(key.stringValue, profile, true);
                }
                EditorGUI.indentLevel--;
                if (length > 0)
                    GUILayout.Space(10);
                else
                {
                    EditorGUILayout.LabelField("No Overrides defined");
                    GUILayout.Space(5);
                }
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Create"))
                    AddOverride(length);
                if (length > 0 && GUILayout.Button("Clear"))
                    ClearOverrides();
                EditorGUILayout.EndHorizontal();
                if (toRemoveIdx != -1)
                    RemoveOverride(toRemoveIdx);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        private void DrawProfile(string key, SerializedProperty profile, bool showBackground, string id = null)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(profile.FindPropertyRelative("m_Font"));
            EditorGUILayout.PropertyField(profile.FindPropertyRelative("m_FontSize"));
            EditorGUILayout.PropertyField(profile.FindPropertyRelative("m_FontColor"));
            EditorGUILayout.PropertyField(profile.FindPropertyRelative("m_TextAlignment"));
            DrawPadding(profile, id);
            if (showBackground)
                EditorGUILayout.PropertyField(profile.FindPropertyRelative("m_Background"));
            if (key != null && EditorGUI.EndChangeCheck() && m_Settings.overrides.TryGetValue(key, out PrettyInspectorSettings.Profile settingsProfile))
                m_LastEditedProfile = settingsProfile;
            else if (EditorGUI.EndChangeCheck() && key == null)
                m_LastEditedProfile = m_Settings.defaultProfile;

        }
        private void DrawPadding(SerializedProperty padding, string id = null)
        {
            id ??= PADDING_FOLDOUT_STATE_KEY_GLOBAL;
            bool state = SessionState.GetBool(id, true);
            SessionState.SetBool(id, EditorGUILayout.Foldout(state, new GUIContent("Padding")));
            if (state)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(padding.FindPropertyRelative("m_TextPaddingTop"), new GUIContent("Top"));
                EditorGUILayout.PropertyField(padding.FindPropertyRelative("m_TextPaddingRight"), new GUIContent("Right"));
                EditorGUILayout.PropertyField(padding.FindPropertyRelative("m_TextPaddingBottom"), new GUIContent("Bottom"));
                EditorGUILayout.PropertyField(padding.FindPropertyRelative("m_TextPaddingLeft"), new GUIContent("Left"));
                EditorGUI.indentLevel--;
            }
        }
        private void AddOverride(int idx)
        {
            string key = "ClassName";
            int i = 0;
            while (m_Overrides.ContainsKey(key))
                key = $"ClassName_{++i}";
            var profile = new PrettyInspectorSettings.Profile();
            m_Overrides.Add(key, profile);
            m_OverrideKeys.arraySize++;
            m_OverrideValues.arraySize++;
            m_OverrideKeys.GetArrayElementAtIndex(idx).stringValue = key;
            m_LastEditedProfile = profile;
            EditorUtility.SetDirty(m_Settings);
        }
        private void RemoveOverride(int idx)
        {
            m_Overrides.Remove(m_OverrideKeys.GetArrayElementAtIndex(idx).stringValue);
            m_OverrideKeys.DeleteArrayElementAtIndex(idx);
            m_OverrideValues.DeleteArrayElementAtIndex(idx);
            m_LastEditedProfile = m_Settings.defaultProfile;
            EditorUtility.SetDirty(m_Settings);
        }
        private void ClearOverrides()
        {
            m_Overrides.Clear();
            m_OverrideKeys.ClearArray();
            m_OverrideValues.ClearArray();
            m_LastEditedProfile = m_Settings.defaultProfile;
            EditorUtility.SetDirty(m_Settings);
        }
        private void DrawLine(Color color, int thickness = 2, int offset = 0)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(thickness));
            r.height = thickness;
            r.x -= offset;
            r.width += offset * 1.5f;
            EditorGUI.DrawRect(r, color);
        }
        private PrettyInspectorSettings GetEditorSettings()
        {
            string guid = EditorPrefs.GetString(PrettyInspectorSettings.PREF_KEY, null);
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path != "" && AssetDatabase.AssetPathExists(path))
                return AssetDatabase.LoadAssetAtPath<PrettyInspectorSettings>(path);
            else
            {
                string fallbackPath = FindFileFolderPath(nameof(PrettyInspectorSettings));
                var settings = ScriptableObject.CreateInstance<PrettyInspectorSettings>();
                settings.name = nameof(PrettyInspectorSettings);
                path = $"{fallbackPath}/{settings.name}.asset";
                AssetDatabase.CreateAsset(settings, path);
                EditorPrefs.SetString(PrettyInspectorSettings.PREF_KEY, AssetDatabase.AssetPathToGUID(path));
                return settings;
            }

        }
        public string FindFileFolderPath(string fileName)
        {
            string[] guids = AssetDatabase.FindAssets(fileName);
            if (guids.Length > 0)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                string folderPath = Path.GetDirectoryName(assetPath);
                return folderPath;
            }
            return null;
        }
    }
}
