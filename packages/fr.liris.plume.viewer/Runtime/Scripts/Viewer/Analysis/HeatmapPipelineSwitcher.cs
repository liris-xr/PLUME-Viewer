using UnityEngine;
using UnityEngine.Rendering;

namespace PLUME.Viewer.Analysis
{
    /// <summary>
    /// Temporarily switches the active render pipeline to a heatmap-friendly one (URP) while a heatmap is being
    /// generated or displayed, then restores the record's pipeline afterwards.
    ///
    /// The heatmap generation (segmented object depth pre-pass) and display shaders are built-in (CGPROGRAM)
    /// shaders. HDRP's render loop does not execute them (no HDRP pass), so the depth pre-pass produces no usable
    /// data and the heatmap stays blue. URP runs these legacy shaders correctly.
    ///
    /// Reference counted so multiple analysis modules can be active simultaneously; the pipeline is only restored
    /// once the last one releases.
    /// </summary>
    public class HeatmapPipelineSwitcher : MonoBehaviour
    {
        public static HeatmapPipelineSwitcher Instance { get; private set; }

        /// <summary>
        /// True while the heatmap pipeline override is in effect. While active, replay modules (e.g.
        /// QualitySettingsPlayerModule) must NOT change the render pipeline, otherwise they would reset it back to
        /// the record's HDRP asset mid-heatmap.
        /// </summary>
        public static bool IsActive => Instance != null && Instance._refCount > 0;

        [Tooltip("Render pipeline used while a heatmap is generating or visible (e.g. the project's URP asset). " +
                 "Leave as None to use the built-in render pipeline, the native target for the heatmap shaders.")]
        public RenderPipelineAsset targetPipeline;

        private RenderPipelineAsset _savedDefaultPipeline;
        private RenderPipelineAsset _savedQualityPipeline;
        private int _refCount;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>Switch to the heatmap pipeline (no-op after the first acquire until fully released).</summary>
        public void Acquire()
        {
            if (_refCount++ > 0)
                return;

            _savedDefaultPipeline = GraphicsSettings.defaultRenderPipeline;
            _savedQualityPipeline = QualitySettings.renderPipeline;

            // targetPipeline == null => built-in render pipeline (no SRP).
            GraphicsSettings.defaultRenderPipeline = targetPipeline;
            QualitySettings.renderPipeline = targetPipeline;
        }

        /// <summary>Restore the record's pipeline once the last holder releases.</summary>
        public void Release()
        {
            if (_refCount == 0)
                return;

            if (--_refCount > 0)
                return;

            GraphicsSettings.defaultRenderPipeline = _savedDefaultPipeline;
            QualitySettings.renderPipeline = _savedQualityPipeline;
        }
    }
}
