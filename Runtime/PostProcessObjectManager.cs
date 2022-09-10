using UnityEngine;
using Naninovel;
using NaninovelPostProcessFX;

public class PostProcessObjectManager : MonoBehaviour
{
    private PostProcessingConfiguration postProcessingConfiguration;

    private void Awake()
    {
        #if UNITY_POST_PROCESSING_STACK_V2
        postProcessingConfiguration = Engine.GetConfiguration<PostProcessingConfiguration>();

        if (postProcessingConfiguration.OverrideObjectsAndCameraLayer) gameObject.layer = postProcessingConfiguration.Layer;
        #endif
    }

}
