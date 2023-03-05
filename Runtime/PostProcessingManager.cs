#if UNITY_POST_PROCESSING_STACK_V2
using Naninovel;
using UnityEngine.Rendering.PostProcessing;


namespace NaninovelPostProcess
{
    [InitializeAtRuntime]
    public class PostProcessingManager : IEngineService
    {
        public virtual PostProcessingConfiguration Configuration { get; }
        private readonly ICameraManager cameraManager;

        public PostProcessingManager(PostProcessingConfiguration config, ICameraManager cameraManager)
        {
            Configuration = config;
            this.cameraManager = cameraManager;
        }

        public virtual UniTask InitializeServiceAsync()
        {
            if (Configuration.AddPostProcessLayerToCamera)
            {
                PostProcessLayer layer = cameraManager.Camera.gameObject.GetComponent<PostProcessLayer>() ?? cameraManager.Camera.gameObject.AddComponent<PostProcessLayer>();
                layer.Init(Configuration.PostProcessResources);
                layer.volumeTrigger = cameraManager.Camera.transform;
                layer.volumeLayer = Configuration.LayerMask;
                layer.antialiasingMode = (PostProcessLayer.Antialiasing)Configuration.AntiAliasing;

                SetAntiAlias(layer);
            }

            return UniTask.CompletedTask;
        }

        private void SetAntiAlias(PostProcessLayer layer)
        {
            if (layer.antialiasingMode == PostProcessLayer.Antialiasing.FastApproximateAntialiasing)
            {
                layer.fastApproximateAntialiasing.fastMode = Configuration.FastMode;
                layer.fastApproximateAntialiasing.keepAlpha = Configuration.KeepAlpha;
            }
            else if (layer.antialiasingMode == PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing)
            {
                layer.subpixelMorphologicalAntialiasing.quality = (SubpixelMorphologicalAntialiasing.Quality)Configuration.SMAAQuality;
            }
            else if (layer.antialiasingMode == PostProcessLayer.Antialiasing.TemporalAntialiasing)
            {
                layer.temporalAntialiasing.jitterSpread = Configuration.JitterSpread;
                layer.temporalAntialiasing.stationaryBlending = Configuration.StationaryBlending;
                layer.temporalAntialiasing.motionBlending = Configuration.MotionBlending;
                layer.temporalAntialiasing.sharpness = Configuration.Sharpness;
            }
        }

        public void ResetService()
        {
        }

        public void DestroyService()
        {
        }
    }
}

#endif