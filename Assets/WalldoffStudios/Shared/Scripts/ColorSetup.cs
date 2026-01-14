using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WalldoffStudios.ToonCharacter
{
    public enum ColorUpdateType
    {
        OnClick = 0,
        Always = 1,
    }
    
    [ExecuteInEditMode, CanEditMultipleObjects]
    public class ColorSetup : MonoBehaviour
    {
        [SerializeField] private ToonColors toonColors = null;
        [SerializeField] private bool destroyAfterApplying = true;
        [SerializeField] private bool applyOnEnable = true;
        [SerializeField] private ColorUpdateType updateType = ColorUpdateType.OnClick;
        public ColorUpdateType UpdateType => updateType;
        public MeshRenderer MeshRenderer { get; private set; }
        public SkinnedMeshRenderer SkinnedMeshRenderer { get; private set; }
        private MaterialPropertyBlock _materialPropertyBlock;
        
        private static readonly int SelfShadingSize = Shader.PropertyToID("_SelfShadingSize");
        private static readonly int HighLightSmoothness = Shader.PropertyToID("_HighlightSmoothness");
        private static readonly int HighlightStrength = Shader.PropertyToID("_HighlightStrength");
        private static readonly int ShadowStrength = Shader.PropertyToID("_ShadowStrength");

        public void SetToonColors(ToonColors newToonColors)
        {
            toonColors = newToonColors;
            ApplyColors();
        }
        
        public Color[] GetColors()
        {
            if (toonColors == null) return null;
            return toonColors.colors;
        }

        public void UpdateColors(Color[] newColors)
        {
            toonColors.OverrideSavedColors(newColors);
            ApplyColors();
        }

        private void Awake()
        {
            if (MeshRenderer == null) MeshRenderer = GetComponent<MeshRenderer>();
            if (SkinnedMeshRenderer == null) SkinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
            if (_materialPropertyBlock == null)
            {
                _materialPropertyBlock = new MaterialPropertyBlock();
            }
        }

        private void OnEnable()
        {
            if(applyOnEnable == false) return;
            if (toonColors == null)
            {
                Debug.LogWarning("Toon colors isn't assigned");
                return;
            }
            //if (Application.isPlaying == true) ApplyColors();
            ApplyColors();
        }

        //Call this from another class if you want to apply automatically
        public void ApplyColors()
        {
            _materialPropertyBlock ??= new MaterialPropertyBlock();
         
            Renderer renderer = GetActiveRenderer();
            if (renderer == null) throw new Exception($"The {gameObject.name} doesn't have a MeshRenderer or SkinnedMeshRenderer component");
            renderer.GetPropertyBlock(_materialPropertyBlock);
         
            if (toonColors != null)
            {
                for (int i = 0; i < toonColors.ColorCount; i++)
                {
                    _materialPropertyBlock.SetVector($"_Color{i+1}", toonColors.GetColorAtIndex(i));
                }
                _materialPropertyBlock.SetFloat(SelfShadingSize, toonColors.selfShadingSize);
                _materialPropertyBlock.SetFloat(HighLightSmoothness, toonColors.highlightSmoothness);
                _materialPropertyBlock.SetFloat(HighlightStrength, toonColors.highlightStrength);
                _materialPropertyBlock.SetFloat(ShadowStrength, toonColors.shadowStrength);
            }
         
            
            renderer.SetPropertyBlock(_materialPropertyBlock);
            
            if (destroyAfterApplying && Application.isPlaying) Destroy(this, 0.1f);
        }
        
        private Renderer GetActiveRenderer()
        {
            Renderer renderer = null;
            if (MeshRenderer == null) MeshRenderer = GetComponent<MeshRenderer>();
            if (MeshRenderer != null) return MeshRenderer;
         
            if (SkinnedMeshRenderer == null) SkinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
            if (SkinnedMeshRenderer != null) return SkinnedMeshRenderer;
            
            return renderer;
        }
    }   
}
