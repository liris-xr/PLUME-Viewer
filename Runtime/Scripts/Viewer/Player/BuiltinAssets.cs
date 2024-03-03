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
            if (Instance != null)
            {
                Destroy(gameObject);
                Debug.LogWarning("BuiltinAssets already exists, destroying duplicate");
                return;
            }

            Instance = this;
        }
    }
}