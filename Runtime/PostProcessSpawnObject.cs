//2022-2023 idaocracy

#if UNITY_POST_PROCESSING_STACK_V2

using Naninovel;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine;
using System;
#if NANINOVEL_SCENE_ASSISTANT_AVAILABLE 
using NaninovelSceneAssistant;
#endif

namespace NaninovelPostProcess
{
#if NANINOVEL_SCENE_ASSISTANT_AVAILABLE

    public abstract class PostProcessSpawnObject : SceneAssistantSpawnObject
    {
        public override bool IsTransformable => false;
        public override bool IsSpawnEffect => true;
        public override string CommandId => this.name;

        protected override void Awake()
        {
            base.Awake();
            Initialize();
        }

#else

    public abstract class PostProcessSpawnObject : MonoBehaviour
    {
        protected virtual void Awake()
        {
            Initialize();
        }

#endif
        public interface ITextureParameterized
        {
            List<Texture> TextureItems { get; }
        }

        protected Dictionary<string, Texture> Textures;

        public float Duration { get; protected set; }
        protected float VolumeWeight { get; private set; }
        protected float FadeOutDuration { get; private set; }

        protected readonly Tweener<FloatTween> volumeWeightTweener = new Tweener<FloatTween>();

        private PostProcessingConfiguration postProcessingConfiguration;
        protected PostProcessVolume Volume;

        [SerializeField] private bool ignoreTimescale;
        protected bool IgnoreTimescale => ignoreTimescale;

        [Header("Spawn/Despawn settings")]
        [SerializeField, UnityEngine.Min(0f)] protected float defaultSpawnDuration = 0.35f;
        [SerializeField, UnityEngine.Min(0f)] protected float defaultDespawnDuration = 0.35f;
        [Header("Volume Settings")]
        [SerializeField, Range(0f, 1f)] protected float defaultVolumeWeight = 1f;

        private void Initialize()
        {
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
            if (duration > 0) await volumeWeightTweener.RunAwaitable(new FloatTween(Volume.weight, volumeWeight, new(duration), x => Volume.weight = x/*, IgnoreTimescale*/), asyncToken, Volume);
            else Volume.weight = volumeWeight;
        }

        public void SetDestroyParameters(IReadOnlyList<string> parameters)
        {
            FadeOutDuration = parameters?.ElementAtOrDefault(0)?.AsInvariantFloat() ?? defaultDespawnDuration;
        }

        public async UniTask AwaitDestroy(AsyncToken asyncToken = default)
        {
            CompleteTweens();
            var duration = asyncToken.Completed ? 0 : FadeOutDuration;
            await ChangeVolumeWeightAsync(0f, duration, asyncToken);
        }

        protected abstract void CompleteTweens();

        protected Texture ChangeTexture(string imageId)
        {
            if (imageId == "None" || String.IsNullOrEmpty(imageId)) return null;
            else if (!Textures.ContainsKey(imageId))
            {
                Debug.LogWarning($"{imageId} was not found in texture list.");
                return null;
            }
            else return Textures.FirstOrDefault(t => t.Key == imageId).Value;
        }
    }
}

#endif