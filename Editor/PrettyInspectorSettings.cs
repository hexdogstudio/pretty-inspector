#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HexTools.Editor
{
    public class PrettyInspectorSettings : ScriptableObject, ISerializationCallbackReceiver
    {
        [Serializable]
        public class Profile
        {
            [SerializeField] private Font m_Font;
            [SerializeField] private FontStyle m_FontStyle;
            [SerializeField] private int m_FontSize = 14;
            [SerializeField] private Color m_FontColor = Color.white;
            [SerializeField] private TextAnchor m_TextAlignment = TextAnchor.MiddleLeft;
            [SerializeField] private int m_TextPaddingTop = 0;
            [SerializeField] private int m_TextPaddingRight = 0;
            [SerializeField] private int m_TextPaddingBottom = 0;
            [SerializeField] private int m_TextPaddingLeft = 0;
            [SerializeField] private Color m_Background = Color.white;

            public Font font { get => m_Font; set => m_Font = value; }
            public int fontSize { get => m_FontSize; }
            public Color fontColor { get => m_FontColor; set => m_FontColor = value; }
            public TextAnchor textAlignment { get => m_TextAlignment; }
            public RectOffset textPadding
            {
                get => new RectOffset(
                m_TextPaddingLeft,
                m_TextPaddingRight,
                m_TextPaddingTop,
                m_TextPaddingBottom
            );
            }
            public GUIStyle style
            {
                get
                {
                    var normalState = new GUIStyleState
                    {
                        textColor = m_FontColor
                    };
                    var style = new GUIStyle
                    {
                        fontSize = m_FontSize,
                        fontStyle = m_FontStyle,
                        alignment = m_TextAlignment,
                        padding = textPadding,
                        normal = normalState
                    };
                    if (font != null)
                        style.font = font;
                    return style;
                }
            }

            public Color background { get => m_Background; set => m_Background = value; }
            public FontStyle fontStyle { get => m_FontStyle; set => m_FontStyle = value; }
        }

        public const string PREF_KEY = "prttyins-settings-guid";
        [SerializeField] private Profile m_DefaultProfile;
        [SerializeField] private Color m_TopColor = new Color(0.2f, 0.4f, 1f);
        [SerializeField] private Color m_BottomColor = new Color(0.6f, 0.175f, 0.75f);
        [SerializeField] private bool m_EnableOverrides;
        [SerializeField] private bool m_EnablePreview = true;
        [SerializeField] private int m_SuperClasses = 1;
        [SerializeField, HideInInspector] private List<string> m_OverrideKeys = new List<string>();
        [SerializeField, HideInInspector] private List<Profile> m_OverrideValues = new List<Profile>();
        [NonSerialized] private Dictionary<string, Profile> m_Overrides = new Dictionary<string, Profile>();

        public Color topColor { get => m_TopColor; }
        public Color bottomColor { get => m_BottomColor; }
        public Profile defaultProfile { get => m_DefaultProfile; }
        public Dictionary<string, Profile> overrides { get => m_Overrides; }
        public bool enableOverrides { get => m_EnableOverrides; }
        public bool enablePreview { get => m_EnablePreview; }
        public int superClasses { get => m_SuperClasses; }

        public void OnBeforeSerialize()
        {
            m_OverrideKeys.Clear();
            m_OverrideValues.Clear();

            foreach (var kvp in m_Overrides)
            {
                m_OverrideKeys.Add(kvp.Key);
                m_OverrideValues.Add(kvp.Value);
            }
        }
        public void AddOverride(string key, Profile profile)
        {
            m_OverrideKeys.Add(key);
            m_OverrideValues.Add(profile);
            m_Overrides.Add(key, profile);
        }
        public void Reset()
        {
            m_DefaultProfile = new Profile();
            m_TopColor = new Color(0.2f, 0.4f, 1f);
            m_BottomColor = new Color(0.6f, 0.175f, 0.75f);
            m_EnableOverrides = false;
            m_EnablePreview = true;
            m_SuperClasses = 1;
            m_OverrideKeys.Clear();
            m_OverrideValues.Clear();
            m_Overrides.Clear();
        }

        public void OnAfterDeserialize()
        {
            m_Overrides = new Dictionary<string, Profile>();

            for (int i = 0; i != Math.Min(m_OverrideKeys.Count, m_OverrideValues.Count); i++)
                m_Overrides.Add(m_OverrideKeys[i], m_OverrideValues[i]);
        }
    }
}
#endif
