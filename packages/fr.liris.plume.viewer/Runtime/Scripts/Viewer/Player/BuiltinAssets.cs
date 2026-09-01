using UnityEngine;
using UnityEngine.Serialization;

// Script used to force including assets into a build
namespace PLUME.Viewer.Player
{
    public class BuiltinAssets : MonoBehaviour
    {
        public static BuiltinAssets Instance { get; private set; }
        
        public Mesh cube;
        public Mesh sphere;
        public Mesh cylinder;
        public Mesh quad;
        public Mesh plane;
        public Mesh capsule;

        public Material defaultSkybox;
        public Material defaultMaterial;
        public Material defaultDiffuse;
        public Material defaultTerrainStandard;
        public Material defaultLine;
        
        public Sprite background;
        public Sprite checkmark;
        public Sprite dropdownArrow;
        public Sprite inputFieldBackground;
        public Sprite knob;
        public Sprite uiSprite;
        public Sprite uiMask;

        public Font legacyRuntime;

        private void Awake()
        {
            if (Instance != null && !ReferenceEquals(Instance, this))
            {
                Debug.LogWarning($"A BuiltinAssets instance already exists ('{Instance.name}'); destroying the " +
                                 $"duplicate on '{name}'.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(Instance, this))
                Instance = null;
        }
    }
}