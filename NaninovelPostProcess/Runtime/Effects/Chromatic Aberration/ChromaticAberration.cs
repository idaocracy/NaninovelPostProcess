//2022-2023 idaocracy

#if UNITY_POST_PROCESSING_STACK_V2

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using Naninovel;
using Naninovel.Commands;
using NaninovelSceneAssistant;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NaninovelPostProcess { 

    [RequireComponent(typeof(PostProcessVolume))]
    public class ChromaticAberration : PostProcessSpawnObject, Spawn.IParameterized, Spawn.IAwaitable, DestroySpawned.IParameterized, DestroySpawned.IAwaitable, PostProcessSpawnObject.ITextureParameterized
    {
        protected string SpectralLut { get; private set; }
        protected float Intensity { get; private set; }
        protected bool FastMode { get; private set; }
        public override bool IsTransformable => false;
        public override string CommandId => "ChromaticAberration";

        private readonly Tweener<FloatTween> intensityTweener = new Tweener<FloatTween>();

        [Header("Chromatic Aberration Settings")]
        [SerializeField] private string defaultSpectralLutId = string.Empty;
        [SerializeField] private List<Texture> spectralLuts = new List<Texture>();
        [SerializeField, Range(0f, 1f)] private float defaultIntensity = 1f;
        [SerializeField] private bool defaultFastMode = false;

        private UnityEngine.Rendering.PostProcessing.ChromaticAberration chromaticAberration;

        public List<Texture> TextureItems => spectralLuts; 

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

        public override List<ParameterValue> GetParams()
        {
            return new List<ParameterValue>
            {
                { new ParameterValue("Time", () => Duration, v => Duration = (float)v, (i,p) => i.FloatField(p, 0), false)},
                { new ParameterValue("Weight", () => Volume.weight, v => Volume.weight = (float)v, (i,p) => i.FloatSliderField(p, 0f, 1f), false)},
                { new ParameterValue("SpectralLut", () => chromaticAberration.spectralLut.value, v => chromaticAberration.spectralLut.value = (Texture)v, (i,p) => i.TypeListField<Texture>(p, Textures), false)},
                { new ParameterValue("Intensity", () => chromaticAberration.intensity.value, v => chromaticAberration.intensity.value = (float)v, (i,p) => i.FloatSliderField(p, 0f, 1f), false)},
                { new ParameterValue("FastMode", () => chromaticAberration.fastMode.value, v => chromaticAberration.fastMode.value = (bool)v, (i,p) => i.BoolField(p), false)},
            };
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ChromaticAberration))]
    public class ChromaticAberrationEditor : SpawnObjectEditor { }
#endif

}

#endif