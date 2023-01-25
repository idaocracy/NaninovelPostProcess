//2022 idaocracy

#if UNITY_POST_PROCESSING_STACK_V2

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using Naninovel;
using Naninovel.Commands;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NaninovelPostProcess 
{ 

    [RequireComponent(typeof(PostProcessVolume))]
    public class Grain : PostProcessObject, Spawn.IParameterized, Spawn.IAwaitable, DestroySpawned.IParameterized, DestroySpawned.IAwaitable
    {
        protected bool Colored { get; private set; }
        protected float Intensity { get; private set; }
        protected float Size { get; private set; }
        protected float LuminanceContribution { get; private set; }

        private readonly Tweener<FloatTween> intensityTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> sizeTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> luminanceContributionTweener = new Tweener<FloatTween>();

        [Header("Grain Settings")]
        [SerializeField] private bool defaultColored = true;
        [SerializeField, Range(0f, 1f)] private float defaultIntensity = 0f;
        [SerializeField, Range(0.3f, 3f)] private float defaultSize = 1f;
        [SerializeField, Range(0f, 1f)] private float defaultluminanceContribution = 0.8f;

        private UnityEngine.Rendering.PostProcessing.Grain grain;

        public override void SetSpawnParameters(IReadOnlyList<string> parameters, bool asap)
        {
            base.SetSpawnParameters(parameters, asap);
            Colored = Boolean.TryParse(parameters?.ElementAtOrDefault(2), out var colored) ? colored : defaultColored;
            Intensity = parameters?.ElementAtOrDefault(3)?.AsInvariantFloat() ?? defaultIntensity;
            Size = parameters?.ElementAtOrDefault(4)?.AsInvariantFloat() ?? defaultSize;
            LuminanceContribution = parameters?.ElementAtOrDefault(5).AsInvariantFloat() ?? defaultluminanceContribution;
        }

        public async UniTask AwaitSpawnAsync(AsyncToken asyncToken = default)
        {
            CompleteTweens();
            var duration = asyncToken.Completed ? 0 : Duration;
            await ChangeGrainAsync(duration, VolumeWeight, Colored, Intensity, Size, LuminanceContribution, asyncToken);
        }

        public async UniTask ChangeGrainAsync(float duration, float volumeWeight, bool colored, float intensity, float size, float luminanceContribution, AsyncToken asyncToken = default)
        {
            var tasks = new List<UniTask>();
            if (Volume.weight != volumeWeight) tasks.Add(ChangeVolumeWeightAsync(volumeWeight, duration, asyncToken));
            grain.colored.value = colored;
            if (grain.intensity.value != intensity) tasks.Add(ChangeIntensityAsync(intensity, duration, asyncToken));
            if (grain.size.value != size) tasks.Add(ChangeSizeAsync(size, duration, asyncToken));
            if (grain.lumContrib.value != luminanceContribution) tasks.Add(ChangeLuminanceContributionAsync(luminanceContribution, duration, asyncToken));
            await UniTask.WhenAll(tasks);
        }

        protected override void CompleteTweens()
        {
            if (volumeWeightTweener.Running) volumeWeightTweener.CompleteInstantly();
            if (intensityTweener.Running) intensityTweener.CompleteInstantly();
            if (sizeTweener.Running) sizeTweener.CompleteInstantly();
            if (luminanceContributionTweener.Running) luminanceContributionTweener.CompleteInstantly();
        }

        protected override void Awake()
        {
            base.Awake();
            grain = Volume.profile.GetSetting<UnityEngine.Rendering.PostProcessing.Grain>() ?? Volume.profile.AddSettings<UnityEngine.Rendering.PostProcessing.Grain>();
            grain.SetAllOverridesTo(true);
        }

        private async UniTask ChangeIntensityAsync(float intensity, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await intensityTweener.RunAsync(new FloatTween(grain.intensity.value, intensity, duration, x => grain.intensity.value = x), asyncToken, grain);
            else grain.intensity.value = intensity;
        }
        private async UniTask ChangeSizeAsync(float size, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await sizeTweener.RunAsync(new FloatTween(grain.size.value, size, duration, x => grain.size.value = x), asyncToken, grain);
            else grain.size.value = size;
        }
        private async UniTask ChangeLuminanceContributionAsync(float luminanceContribution, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await luminanceContributionTweener.RunAsync(new FloatTween(grain.lumContrib.value, luminanceContribution, duration, x => grain.lumContrib.value = x), asyncToken, grain);
            else grain.lumContrib.value = luminanceContribution;
        }

    #if UNITY_EDITOR

        public string SceneAssistantParameters()
        {
            Duration = SpawnSceneAssistant.FloatField("Fade-in time", Duration);
            Volume.weight = SpawnSceneAssistant.SliderField("Volume Weight", Volume.weight, 0f, 1f);
            grain.colored.value = SpawnSceneAssistant.BooleanField("Colored", grain.colored.value);
            grain.intensity.value = SpawnSceneAssistant.SliderField("Intensity", grain.intensity.value, 0f, 1f);
            grain.size.value = SpawnSceneAssistant.SliderField("Size", grain.size.value, 0.3f, 3f);
            grain.lumContrib.value = SpawnSceneAssistant.SliderField("Luminance Contribution", grain.lumContrib.value, 0f, 1f);

            return SpawnSceneAssistant.GetSpawnString(ParameterList());
        }

        public IReadOnlyDictionary<string, string> ParameterList()
        {
            if (grain == null) return null;

            return new Dictionary<string, string>()
            {
                { "time", Duration.ToString()},
                { "weight", Volume.weight.ToString()},
                { "colored", grain.colored.value.ToString().ToLower()},
                { "intensity", grain.intensity.value.ToString()},
                { "size", grain.size.value.ToString()},
                { "luminanceContribution", grain.lumContrib.value.ToString()},
            };
        }

    #endif
    }


    #if UNITY_EDITOR

    [CustomEditor(typeof(Grain))]
    public class GrainEditor : PostProcessObjectEditor
    {
        protected override string Label => "grain";
        private UnityEngine.Rendering.PostProcessing.Grain grain;

        protected override void Awake()
        {
            base.Awake();
            grain = spawnObject.GetComponent<UnityEngine.Rendering.PostProcessing.Grain>();
        }

    }

    #endif

}

#endif