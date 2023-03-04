#if UNITY_POST_PROCESSING_STACK_V2

using Naninovel;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine;
using NaninovelSceneAssistant;

namespace NaninovelPostProcess { 

    public abstract class PostProcessSpawnObject : SceneAssistantSpawnObject
    {
        public interface ITextureParameterized
        {
            List<Texture> TextureItems { get; }
        }

        public override bool IsTransformable => false;

        protected Dictionary<string, Texture> Textures;
        public override string CommandId => this.name;
        public float Duration { get; protected set; }
        protected float VolumeWeight { get; private set; }
        protected float FadeOutDuration { get; private set; }

        protected readonly Tweener<FloatTween> volumeWeightTweener = new Tweener<FloatTween>();

        private PostProcessingConfiguration postProcessingConfiguration;
        protected PostProcessVolume Volume;

        [Header("Spawn/Despawn settings")]
        [SerializeField, UnityEngine.Min(0f)] private float defaultSpawnDuration = 0.35f;
        [SerializeField, UnityEngine.Min(0f)] private float defaultDespawnDuration = 0.35f;
        [Header("Volume Settings")]
        [SerializeField, Range(0f, 1f)] private float defaultVolumeWeight = 1f;

        protected override void Awake()
        {
            base.Awake();

            postProcessingConfiguration = Engine.GetConfiguration<PostProcessingConfiguration>();
            if (postProcessingConfiguration.OverrideObjectsLayer) gameObject.layer = postProcessingConfiguration.PostProcessingLayer;

            Volume = GetComponent<PostProcessVolume>();
            Volume.weight = 0f;

            if (this is ITextureParameterized textureParameterized)
            {
                Textures = new Dictionary<string, Texture>
                {
                    { "None", null }
                };

                textureParameterized.TextureItems.ForEach(x => Textures.Add(x.name, x));
            }
        }

        public virtual void SetSpawnParameters(IReadOnlyList<string> parameters, bool asap)
        {
            Duration = asap ? 0 : Mathf.Abs(parameters?.ElementAtOrDefault(0)?.AsInvariantFloat() ?? defaultSpawnDuration);
            VolumeWeight = parameters?.ElementAtOrDefault(1)?.AsInvariantFloat() ?? defaultVolumeWeight;
        }

        protected async UniTask ChangeVolumeWeightAsync(float volumeWeight, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await volumeWeightTweener.RunAsync(new FloatTween(Volume.weight, volumeWeight, duration, x => Volume.weight = x), asyncToken, Volume);
            else Volume.weight = volumeWeight;
        }

        public void SetDestroyParameters(IReadOnlyList<string> parameters)
        {
            FadeOutDuration = parameters?.ElementAtOrDefault(0)?.AsInvariantFloat() ?? defaultDespawnDuration;
        }

        public async UniTask AwaitDestroyAsync(AsyncToken asyncToken = default)
        {
            CompleteTweens();
            var duration = asyncToken.Completed ? 0 : FadeOutDuration;
            await ChangeVolumeWeightAsync(0f, duration, asyncToken);
        }

        protected abstract void CompleteTweens();
    }

}

#endif