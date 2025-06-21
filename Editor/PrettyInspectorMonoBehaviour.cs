#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;
using System.IO;

namespace HexTools.Editor
{
    [CustomEditor(typeof(MonoBehaviour), true), CanEditMultipleObjects]
    public class PrettyInspectorMonoBehaviour : UnityEditor.Editor
    {
        public const int EXTRA_WIDTH = 100;
        public const int EXTRA_HEIGHT = 0;
        readonly float DOUBLE_CLICK_THRESHOLD = 0.2f;
        string m_ClassName;
        GUIStyle m_Style;
        PrettyInspectorSettings m_Settings;
        SerializedProperty m_Script;
        string[] m_IgnoredProperties;
        double m_LastClickTime;
        Color m_Background;

        public override void OnInspectorGUI()
        {
            if (!IsEnabled())
            {
                base.OnInspectorGUI();
                return;
            }

            serializedObject.Update();
            DrawTitleProperty();
            EditorGUILayout.Space(3);
            DrawPropertiesExcluding(serializedObject, m_IgnoredProperties);
            serializedObject.ApplyModifiedProperties();
        }
        void OnEnable()
        {
            m_ClassName = ToTitleCase(target.GetType().Name).ToUpper();
            m_LastClickTime = 0;
            m_Script = serializedObject.FindProperty("m_Script");
            m_IgnoredProperties = new string[] { m_Script.name };
            m_Settings = GetEditorSettings();
            LoadStyle();
        }

        private bool IsEnabled()
        {
            return (m_Settings.superClasses & (1 << 0)) != 0;
        }
        private PrettyInspectorSettings GetEditorSettings()
        {
            string guid = EditorPrefs.GetString(PrettyInspectorSettings.PREF_KEY, null);
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path != "" && AssetDatabase.AssetPathExists(path))
                return AssetDatabase.LoadAssetAtPath<PrettyInspectorSettings>(path);
            else
            {
                string fallbackPath = FindFileFolderPath(GetType().Name);
                var settings = CreateInstance<PrettyInspectorSettings>();
                settings.name = nameof(PrettyInspectorSettings);
                path = $"{fallbackPath}/{settings.name}.asset";
                AssetDatabase.CreateAsset(settings, path);
                EditorPrefs.SetString(PrettyInspectorSettings.PREF_KEY, AssetDatabase.AssetPathToGUID(path));
                return settings;
            }

        }
        private void LoadStyle()
        {
            m_Style = m_Settings.defaultProfile.style;
            m_Background = GetBackgroundColor(m_Settings.topColor, m_Settings.bottomColor);
            if (m_Settings.enableOverrides &&
                m_Settings.overrides.TryGetValue(target.GetType().Name, out PrettyInspectorSettings.Profile profile))
            {
                m_Style = profile.style;
                m_Background = profile.background;
            }
        }
        private void DrawTitleProperty()
        {
            Rect rect = EditorGUILayout.BeginVertical();
            rect.width += EXTRA_WIDTH;
            rect.x -= EXTRA_WIDTH / 2;
            rect.height += EXTRA_HEIGHT;
            rect.y -= EXTRA_HEIGHT / 2;
            EditorGUI.DrawRect(rect, m_Background);
            if (m_Script.objectReferenceValue != null)
                AddDoubleClickListener(rect);
            AddRightClickListener(rect);
            EditorGUILayout.LabelField(m_ClassName, m_Style, GUILayout.Height(m_Style.fontSize + 10));
            EditorGUILayout.EndVertical();
        }
        private void AddDoubleClickListener(Rect rect)
        {
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Arrow);
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                double timeSinceLastClick = EditorApplication.timeSinceStartup - m_LastClickTime;
                if (timeSinceLastClick < DOUBLE_CLICK_THRESHOLD)
                    ShowScript();
                m_LastClickTime = EditorApplication.timeSinceStartup;
                Event.current.Use();
            }
        }
        private void AddRightClickListener(Rect rect)
        {
            if (Event.current.type == EventType.ContextClick && rect.Contains(Event.current.mousePosition))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Show Script"), false, ShowScript);
                menu.AddItem(new GUIContent("Show Settings"), false, ShowSettings);
                if (m_Settings.enableOverrides && !m_Settings.overrides.ContainsKey(target.GetType().Name))
                    menu.AddItem(new GUIContent("Add Override"), false, CreateOverride);
                else if (m_Settings.enableOverrides)
                    menu.AddItem(new GUIContent("Remove Override"), false, RemoveOverride);
                menu.ShowAsContext();
                Event.current.Use();
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
        private string ToTitleCase(string text)
        {
            return Regex.Replace(text, "[a-z][A-Z]", m => m.Value[0] + " " + char.ToUpper(m.Value[1]));
        }
        private Color GetBackgroundColor(Color top, Color bottom)
        {
            MonoBehaviour monoBehaviour = target as MonoBehaviour;
            MonoBehaviour[] monoBehaviours = monoBehaviour.GetComponents<MonoBehaviour>();
            int length = monoBehaviours.Length;
            int idx = System.Array.IndexOf(monoBehaviours, monoBehaviour);
            return Color.Lerp(top, bottom, idx / Mathf.Max(1, length - 1.0f));
        }
        private void ShowScript()
        {
            Selection.activeObject = m_Script.objectReferenceValue;
        }
        private void ShowSettings()
        {
            SettingsService.OpenUserPreferences(PrettyInspectorSettingsProvider.PREFERENCES_PATH);
        }
        private void CreateOverride()
        {
            var profile = new PrettyInspectorSettings.Profile();
            profile.font = m_Style.font;
            profile.background = m_Background;
            m_Settings.overrides.Add(target.GetType().Name, profile);
            EditorUtility.SetDirty(m_Settings);
            ShowSettings();
        }
        private void RemoveOverride()
        {
            m_Settings.overrides.Remove(target.GetType().Name);
            EditorUtility.SetDirty(m_Settings);
            LoadStyle();
        }
    }
}
#endif
