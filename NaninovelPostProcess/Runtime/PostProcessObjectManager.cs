#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine;
using Naninovel;

namespace NaninovelPostProcess
{
    public class PostProcessObjectManager : MonoBehaviour
    {
        private PostProcessingConfiguration postProcessingConfiguration;

        private void Awake()
        {
            postProcessingConfiguration = Engine.GetConfiguration<PostProcessingConfiguration>();

            if (postProcessingConfiguration.OverrideObjectsLayer) gameObject.layer = postProcessingConfiguration.PostProcessingLayer;
        }

    }
}
#endif
