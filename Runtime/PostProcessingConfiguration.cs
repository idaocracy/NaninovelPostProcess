using Naninovel;
using UnityEngine;
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif

namespace NaninovelPostProcessFX
{
    [EditInProjectSettings]
    public class PostProcessingConfiguration : Configuration
    {

#if UNITY_POST_PROCESSING_STACK_V2
        [Tooltip("Will override the Layer of the camera controlled by Naninovel and objects that have the Post Processing Object Manager component added.")]
        public bool OverrideObjectsAndCameraLayer = true;

        [Tooltip("The layer used for Post-process rendering. It is recommended to create a dedicated layer for this.")]
        public int Layer = 5;

        [Header("Post Process Layer")]

        [Tooltip("Will add a Post Process Layer component to the camera controlled by Naninovel. If using a custom camera prefab with Post process Layer already added, the settings below will be applied.")]
        public bool AddPostProcessLayerToCamera = true;

        [Tooltip("The layer to which the Post processing layer should be applied. Should be the same layer used by the post processing objects and camera (Enable \"Override Objects and Camera Layer\") to override all at once.")]
        public LayerMask LayerMask = 0;

        [Tooltip("This is needed for the Post Process Layer. You can find the default one by searching \"Post Process Resources\" and using the \"All\" filter.")]
        public PostProcessResources PostProcessResources;

        [Tooltip("Controls the anti-aliasing options on the Post Process Layer.")]
        public int AntiAliasing = 0;

        public bool FastMode = false;

        public bool KeepAlpha = false;

        public int SMAAQuality = 0;

        public float JitterSpread = 0.75f;
        public float StationaryBlending = 0.95f;
        public float MotionBlending = 0.85f;
        public float Sharpness = 0.25f;

#else
        [Header("Test")]
        private const string message = "Post Process V2 has not been installed.";
#endif
    }

}