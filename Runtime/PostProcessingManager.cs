#if UNITY_POST_PROCESSING_STACK_V2
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Naninovel;
using UnityEngine.Rendering.PostProcessing;


namespace NaninovelPostProcessFX
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
            }

            if (Configuration.OverrideObjectsAndCameraLayer)
            {
                cameraManager.Camera.gameObject.layer = Configuration.Layer;
            }

            return UniTask.CompletedTask;
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