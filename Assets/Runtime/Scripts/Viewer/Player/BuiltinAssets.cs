using UnityEngine;

// Script used to force including assets into a build
namespace PLUME.Viewer.Player
{
    public class BuiltinAssets : MonoBehaviour
    {
        public Sprite background;
        public Mesh capsule;
        public Sprite checkmark;

        public Mesh cube;
        public Mesh cylinder;
        public Material defaultDiffuse;
        public Material defaultLine;
        public Material defaultMaterial;

        public Material defaultSkybox;
        public Material defaultTerrainStandard;
        public Sprite dropdownArrow;
        public Sprite inputFieldBackground;
        public Sprite knob;

        public Font legacyRuntime;
        public Mesh plane;
        public Mesh quad;
        public Mesh sphere;
        public Sprite uiMask;
        public Sprite uiSprite;
        public static BuiltinAssets Instance { get; private set; }

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