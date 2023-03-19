//2022-2023 idaocracy

#if UNITY_POST_PROCESSING_STACK_V2

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using Naninovel;
using Naninovel.Commands;
#if UNITY_EDITOR && NANINOVEL_SCENE_ASSISTANT_AVAILABLE
using NaninovelSceneAssistant;
using UnityEditor;
#endif

namespace NaninovelPostProcess { 

    [RequireComponent(typeof(PostProcessVolume))]
    public class ChromaticAberration : PostProcessSpawnObject, Spawn.IParameterized, Spawn.IAwaitable, DestroySpawned.IParameterized, DestroySpawned.IAwaitable, PostProcessSpawnObject.ITextureParameterized
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
            if (chromaticAberration.spectralLut.value != null && chromaticAberration.spectralLut.value.name != spectralLut) 
                chromaticAberration.spectralLut.value = ChangeTexture(spectralLut);
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

#if UNITY_EDITOR && NANINOVEL_SCENE_ASSISTANT_AVAILABLE
        public override List<ICommandParameterData> GetParams()
        {
            return new List<ICommandParameterData>
            {
                { new CommandParameterData<float>("Time", () => Duration, v => Duration = v, (i,p) => i.FloatField(p), defaultSpawnDuration)},
                { new CommandParameterData<float>("Weight", () => Volume.weight, v => Volume.weight = v, (i,p) => i.FloatSliderField(p, 0f, 1f), defaultVolumeWeight)},
                { new CommandParameterData<Texture>("SpectralLut", () => chromaticAberration.spectralLut.value, v => chromaticAberration.spectralLut.value = v, (i,p) => i.TypeListField<Texture>(p, Textures), Textures.FirstOrDefault(t => t.Key == defaultSpectralLutId).Value)},
                { new CommandParameterData<float>("Intensity", () => chromaticAberration.intensity.value, v => chromaticAberration.intensity.value = v, (i,p) => i.FloatSliderField(p, 0f, 1f), defaultIntensity)},
                { new CommandParameterData<bool>("FastMode", () => chromaticAberration.fastMode.value, v => chromaticAberration.fastMode.value = v, (i,p) => i.BoolField(p), defaultFastMode)},
            };
        }
#endif
    }

#if UNITY_EDITOR && NANINOVEL_SCENE_ASSISTANT_AVAILABLE
    [CustomEditor(typeof(ChromaticAberration))]
    public class ChromaticAberrationEditor : SpawnObjectEditor { }
#endif

}

#endif