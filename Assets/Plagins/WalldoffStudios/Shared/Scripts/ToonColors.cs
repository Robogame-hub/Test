using System.Collections.Generic;
using UnityEngine;

namespace WalldoffStudios.ToonCharacter
{
    [CreateAssetMenu(fileName = "Toon colors", menuName = "WalldoffStudios/Toon Character asset/Toon colors")]
    public class ToonColors : ScriptableObject
    {
        [field: SerializeField] public float selfShadingSize { get; private set; } = 0.2f;
        [field: SerializeField] public float highlightSmoothness { get; private set; } = 0.1f;
        [field: SerializeField] public float highlightStrength { get; private set; } = 0.04f;
        [field: SerializeField] public float shadowStrength { get; private set; } = 0.2f;
        [field: SerializeField] public Color[] colors { get; private set; } = null;
        public int ColorCount => colors.Length;
        
        public void SetSelfShadingSize(float newValue) => selfShadingSize = newValue; 
        public void SetHighlightSmoothness(float newValue) => highlightSmoothness = newValue; 
        public void SetHighlightStrength(float newValue) => highlightStrength = newValue; 
        public void SetShadowStrength(float newValue) => shadowStrength = newValue;

        [ContextMenu("Randomize all colors")]
        public void RandomizeAllColors()
        {
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = GetRandomColor();

                #if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(this);
                #endif
            }
        }
        
        private Color GetRandomColor()
        {
            float red = Random.Range(0f, 1f);
            float green = Random.Range(0f, 1f);
            float blue = Random.Range(0f, 1f);
            return new Color(red, green, blue, 0.0f);
        }
        
        public Color GetColorAtIndex(int index)
        {
            if (index < 0 || index >= colors.Length)
            {
                Debug.LogWarning($"Invalid index: {index} in method: GetColorsAtIndex, returning magenta color");
                return Color.magenta;
            }
            return colors[index];
        }
        
        public void SetColorAtIndex(Color color, int index)
        {
            if (index < 0 || index >= colors.Length)
            {
                Debug.LogWarning($"Invalid index at: {index}");
                return;
            }
            colors[index] = color;
        }
        
        public void AddNewColor()
        {
            Color duplicatedColor = colors[^1];
            List<Color> currentColors = new List<Color>(colors);
            currentColors.Add(duplicatedColor);
            colors = currentColors.ToArray();
            
            SaveChanges();
        }
        
        public void DuplicateColorAtIndex(int index)
        {
            if (index < 0 || index >= colors.Length)
            {
                Debug.LogWarning($"Invalid index: {index} in 'DuplicateColorAtIndex' method");
                return;
            }
            
            Color duplicatedColor = colors[index];
            List<Color> currentColors = new List<Color>(colors);
            currentColors.Add(duplicatedColor);
            colors = currentColors.ToArray();
            
            SaveChanges();
        }
        
        public void RemoveColorAtIndex(int index)
        {
            if (index < 0 || index >= colors.Length)
            {
                Debug.LogWarning($"Invalid index: {index} in RemoveColorAtIndex method");
                return;
            }
            
            List<Color> currentColors = new List<Color>();
            for (int i = 0; i < colors.Length; i++)
            {
                if(i != index) currentColors.Add(colors[i]);
            }
            colors = currentColors.ToArray();
            
            SaveChanges();
        }
        
        public void OverrideSavedColors(Color[] newColors)
        {
            colors = newColors;

            SaveChanges();
        }
        
        public void SaveChanges()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
                //UnityEditor.AssetDatabase.SaveAssets();
            #endif
        }
    }   
}
