//2022 idaocracy

#if UNITY_POST_PROCESSING_STACK_V2

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using Naninovel;
using Naninovel.Commands;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NaninovelPostProcess { 

    [RequireComponent(typeof(PostProcessVolume))]
    public class ChromaticAberration : PostProcessObject, Spawn.IParameterized, Spawn.IAwaitable, DestroySpawned.IParameterized, DestroySpawned.IAwaitable, PostProcessObject.ITextureParameterized, ISceneAssistant
    {
        protected string SpectralLut { get; private set; }
        protected float Intensity { get; private set; }
        protected bool FastMode { get; private set; }

        private readonly Tweener<FloatTween> intensityTweener = new Tweener<FloatTween>();

        [Header("Chromatic Aberration Settings")]
        [SerializeField] private string defaultSpectralLutId = string.Empty;
        [SerializeField] private List<Texture> spectralLuts = new List<Texture>();
        [SerializeField, Range(0f, 1f)] private float defaultIntensity = 1f;
        [SerializeField] private bool defaultFastMode = false;

        private UnityEngine.Rendering.PostProcessing.ChromaticAberration chromaticAberration;

        public List<Texture> TextureItems() => spectralLuts; 

        public override void SetSpawnParameters(IReadOnlyList<string> parameters, bool asap)
        {
            base.SetSpawnParameters(parameters, asap);
            SpectralLut = parameters?.ElementAtOrDefault(2) ?? defaultSpectralLutId;
            Intensity = parameters?.ElementAtOrDefault(3)?.AsInvariantFloat() ?? defaultIntensity;
            FastMode = bool.Parse(parameters?.ElementAtOrDefault(4) ?? defaultFastMode.ToString()); 
        }

        public async UniTask AwaitSpawnAsync(AsyncToken asyncToken = default)
        {
            CompleteTweens();
            var duration = asyncToken.Completed ? 0 : Duration;
            await ChangeChromaticAberration(duration, VolumeWeight, SpectralLut, Intensity, FastMode, asyncToken);
        }

        public async UniTask ChangeChromaticAberration(float duration, float volumeWeight, string spectralLut, float intensity, bool fastMode, AsyncToken asyncToken = default)
        {
            var tasks = new List<UniTask>();
            if (Volume.weight != volumeWeight) tasks.Add(ChangeVolumeWeightAsync(volumeWeight, duration, asyncToken));
            if (chromaticAberration.spectralLut.value != null && chromaticAberration.spectralLut.value.name != spectralLut) ChangeTexture(spectralLut);
            if (chromaticAberration.intensity.value != intensity) tasks.Add(ChangeIntensityAsync(intensity, duration, asyncToken));

            await UniTask.WhenAll(tasks);
        }

        protected override void CompleteTweens()
        {
            if (intensityTweener.Running) intensityTweener.CompleteInstantly();
            if (volumeWeightTweener.Running) volumeWeightTweener.CompleteInstantly();
        }
        protected override void Awake()
        {
            base.Awake();
            chromaticAberration = Volume.profile.GetSetting<UnityEngine.Rendering.PostProcessing.ChromaticAberration>() ?? Volume.profile.AddSettings<UnityEngine.Rendering.PostProcessing.ChromaticAberration>();
            chromaticAberration.SetAllOverridesTo(true);
        }

        private async UniTask ChangeIntensityAsync(float intensity, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await intensityTweener.RunAsync(new FloatTween(chromaticAberration.intensity.value, Intensity, duration, x => chromaticAberration.intensity.value = x), asyncToken, chromaticAberration);
            else chromaticAberration.intensity.value = intensity;
        }

        private void ChangeTexture(string imageId)
        {
            if (imageId == "None" || String.IsNullOrEmpty(imageId)) chromaticAberration.spectralLut.value = null;
            else spectralLuts.Select(t => t != null && t.name == imageId);
        }

    #if UNITY_EDITOR

        public string SceneAssistantParameters()
        {
            Duration = SpawnSceneAssistant.FloatField("Fade-in time", Duration);
            Volume.weight = SpawnSceneAssistant.SliderField("Volume Weight", Volume.weight, 0f, 1f);
            chromaticAberration.spectralLut.value = SpawnSceneAssistant.TextureField("Spectral LUT", chromaticAberration.spectralLut.value, spectralLuts);
            chromaticAberration.intensity.value = SpawnSceneAssistant.SliderField("Intensity", chromaticAberration.intensity.value, 0f, 1f);
            chromaticAberration.fastMode.value = SpawnSceneAssistant.BooleanField("Fast Mode", chromaticAberration.fastMode.value);

            return SpawnSceneAssistant.GetSpawnString(ParameterList());
        }

        public IReadOnlyDictionary<string, string> ParameterList()
        {
            if (chromaticAberration == null) return null;

            return new Dictionary<string, string>()
            {
                { "time", Duration.ToString()},
                { "weight", Volume.weight.ToString()},
                { "spectralLut", chromaticAberration.spectralLut.value?.name},
                { "intensity", chromaticAberration.intensity.value.ToString()},
                { "fastMode", chromaticAberration.fastMode.value.ToString().ToLower()},
            };
        }

    #endif
    }

    #if UNITY_EDITOR

    [CustomEditor(typeof(ChromaticAberration))]
    public class ChromaticAberrationEditor : PostProcessObjectEditor
    {
        protected override string Label => "chromaticAberration";
    }

    #endif

}

#endif