#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace WalldoffStudios.ToonCharacter
{
    public enum ColorUpdateType
    {
        OnClick = 0,
        Always = 1,
    }
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ColorSetup))]
    public class ColorSetupEditor : Editor
    {
        private Color _inspectorBackground = new Color(0.07f, 0.2f, 0.16f);

        private Rect _screenRect;
        private Rect _vertRect;

        private ColorSetup _colorSetup;
        private ToonColors _toonColors;
        private SerializedProperty toonColors;
        private SerializedProperty updateType;
        private SerializedProperty destroyAfterApplying;
        private SerializedProperty applyOnEnable;

        private bool displayColorSettings = true;

        private ColorUpdateType _updateType = ColorUpdateType.OnClick;

        private Color[] _colors = Array.Empty<Color>();
        
        private void OnEnable()
        {
            _colorSetup = target as ColorSetup;
            if (_colorSetup == null) return;

            destroyAfterApplying = serializedObject.FindProperty(nameof(destroyAfterApplying));
            applyOnEnable = serializedObject.FindProperty(nameof(applyOnEnable));
            toonColors = serializedObject.FindProperty(nameof(toonColors));
            updateType = serializedObject.FindProperty(nameof(updateType));

            // Only initialize _colors if it hasn't been initialized yet
            if (_colors == null)
            {
                _colors = _colorSetup.GetColors();
            }

            _updateType = (ColorUpdateType)updateType.enumValueIndex;
        }
        
        public override void OnInspectorGUI()
        {
            _colorSetup = target as ColorSetup;
            if(_colorSetup == null) return;
            
            serializedObject.Update();

            _screenRect = GUILayoutUtility.GetRect(1, 1);
            _vertRect = EditorGUILayout.BeginVertical();

            EditorGUI.DrawRect(
                new Rect(_screenRect.x - 18, _screenRect.y - 4, _screenRect.width + 25, _vertRect.height + 17),
                _inspectorBackground);
            EditorGUI.BeginChangeCheck();

            InspectorItems();

            if (EditorGUI.EndChangeCheck() == true)
            {
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.EndVertical();
        }

        protected void InspectorItems()
        {
            DrawVisualsSetting();
            if (toonColors.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("You need to assign a color scheme for this to work", MessageType.Error);
                return;
            }

            EditorGUILayout.PropertyField(destroyAfterApplying,
                new GUIContent("Destroy after setup?",
                    "If this is set to true, this class will be removed after being applied."));
            EditorGUILayout.PropertyField(applyOnEnable,
                new GUIContent("Apply on enable?",
                    "If this is set to true, it will apply the colors when the gameObject with this script is enabled"));
            displayColorSettings = GUILayout.Toggle(displayColorSettings,
                new GUIContent(displayColorSettings == true ? "Hide colors" : "Show colors",
                    "Toggle if you want to see color settings or not"), EditorStyles.popup);
            
            if (displayColorSettings == true) DrawColorSettings();
        }
        
        private void DrawVisualsSetting()
        { 
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(toonColors, new GUIContent("Character visuals"));
            if (EditorGUI.EndChangeCheck() == true)
            {
                serializedObject.ApplyModifiedProperties();
            }
            if(toonColors.objectReferenceValue == null) return;
            _toonColors = toonColors.objectReferenceValue as ToonColors;
            float selfShadingSize = _toonColors.selfShadingSize;
            float _selfShadingSize = selfShadingSize;
            _selfShadingSize = EditorGUILayout.Slider(new GUIContent("Self shading size",
               "How large the shadows it applies to itself are, generally recommended to keep it at lower values for better looking results"),
           _selfShadingSize, 0.0f, 1.0f);

            if (selfShadingSize != _selfShadingSize)
            {
                _toonColors.SetSelfShadingSize(_selfShadingSize);
            }
           
            float highlightSmoothness = _toonColors.highlightSmoothness;
            float _highlightSmoothness = highlightSmoothness;
            _highlightSmoothness = EditorGUILayout.Slider(new GUIContent("Highlight smoothness",
               "How smooth the highlight is, higher values will produce a more sharp distinction"),
               _highlightSmoothness, 0.01f, 0.9f);

            if (highlightSmoothness != _highlightSmoothness)
            {
                _toonColors.SetHighlightSmoothness(_highlightSmoothness);
            }
           
            float highlightStrength = _toonColors.highlightStrength;
            float _highlightStrength = highlightStrength;
            _highlightStrength = EditorGUILayout.Slider(new GUIContent("Highlight strength",
               "How strong the highlight light is"),
               _highlightStrength, 0.0f, 0.2f);

            if (highlightStrength != _highlightStrength)
            {
                _toonColors.SetHighlightStrength(_highlightStrength);
            }
            
            float shadowStrength = _toonColors.shadowStrength;
            float _shadowStrength = shadowStrength;
            _shadowStrength = EditorGUILayout.Slider(new GUIContent("Shadow strength",
               "How strong the shadow is"),
               _shadowStrength, 0.0f, 1.0f);

            if (shadowStrength != _shadowStrength)
            {
                _toonColors.SetShadowStrength(_shadowStrength);
            }
        }
        
        private Color GetRandomColor()
        {
            float red = Random.Range(0f, 1f);
            float green = Random.Range(0f, 1f);
            float blue = Random.Range(0f, 1f);
            return new Color(red, green, blue, 0.0f);
        }
        
        // private GUIContent[] ColorFunctionFields = new GUIContent[2]
        // {
        //     new GUIContent("Apply", "This applies this color to the selected parts of your mesh."),
        //     new GUIContent(EditorGUIUtility.IconContent("d_FilterByType@2x").image, "Randomize this color, will use the currently selected randomization algorithm.")
        // };
        
        private void DrawColorSettings()
        {
            _colors = _toonColors.colors;
            
            for (int i = 0; i < _colors.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();

                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.BeginHorizontal();
                    Color newColor = EditorGUILayout.ColorField(_colors[i], GUILayout.Height(20));
                    if (newColor != _colors[i])
                    {
                        _colors[i] = newColor;
                        _toonColors.SetColorAtIndex(newColor, i);
                    }

                    //if (_updateType == ColorUpdateType.OnClick)
                    //{
                        if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("d_FilterByType@2x").image, "Randomize this color, will use the currently selected randomization algorithm."), GUILayout.Width(25), GUILayout.Height(20)))
                        {
                            _toonColors.SetColorAtIndex(GetRandomColor(), i);
                            _toonColors.SaveChanges();
                            serializedObject.ApplyModifiedProperties();
                        }   
                    //}
                    
                    EditorGUILayout.EndHorizontal(); 
                }

                EditorGUILayout.EndHorizontal();
            }
            
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
             
                EditorGUI.BeginChangeCheck();
                
                GUIStyle popupStyle = new GUIStyle(EditorStyles.popup)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white },
                    fixedHeight = 20
                };
                
                string[] enumNames = Enum.GetNames(typeof(ColorUpdateType));
                int selectedIndex = Array.IndexOf(enumNames, _updateType.ToString());
                selectedIndex = EditorGUILayout.Popup("Update Type", selectedIndex, enumNames, popupStyle);
                _updateType = (ColorUpdateType)Enum.Parse(typeof(ColorUpdateType), enumNames[selectedIndex]);
                
                if (EditorGUI.EndChangeCheck())
                {
                    updateType.enumValueIndex = (int)_updateType;
                    serializedObject.ApplyModifiedProperties();
                }
                if (_updateType == ColorUpdateType.OnClick)
                {
                    if (GUILayout.Button(new GUIContent("Update colors", "Will update colors on the character"),
                            ActiveStyle()))
                    {
                        _colorSetup.UpdateColors(_colors);
                    }
                }
                else if (_updateType == ColorUpdateType.Always)
                {
                    _colorSetup.UpdateColors(_colors);
                }
            }
        }
                
        private GUIStyle ActiveStyle()
        {
            GUIStyle duplicateGuiStyle = new GUIStyle(GUI.skin.button);
            duplicateGuiStyle.fontSize = 13;
            duplicateGuiStyle.padding = new RectOffset(0, 0, 0, 3);
            duplicateGuiStyle.normal.textColor = Color.white;

            return duplicateGuiStyle;
        }
    }   
}
#endif