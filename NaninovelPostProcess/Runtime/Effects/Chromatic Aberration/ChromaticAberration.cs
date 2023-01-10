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
    public class ChromaticAberration : PostProcessObject, Spawn.IParameterized, Spawn.IAwaitable, DestroySpawned.IParameterized, DestroySpawned.IAwaitable, PostProcessObject.ISceneAssistant
    {
        protected float Duration { get; private set; }
        protected float VolumeWeight { get; private set; }
        protected string SpectralLut { get; private set; }
        protected float Intensity { get; private set; }
        protected bool FastMode { get; private set; }

        protected float FadeOutDuration { get; private set; }

        private readonly Tweener<FloatTween> volumeWeightTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> intensityTweener = new Tweener<FloatTween>();

        [Header("Spawn/Fadein Settings")]
        [SerializeField] private float defaultDuration = 0.35f;
        [Header("Volume Settings")]
        [SerializeField] private float defaultVolumeWeight = 1f;
        [Header("Chromatic Aberration Settings")]
        [SerializeField] private string defaultSpectralLutId = string.Empty;
        [SerializeField] private List<Texture> spectralLuts = new List<Texture>();
        [SerializeField] private float defaultIntensity = 1f;
        [SerializeField] private bool defaultFastMode = false;
        [Header("Despawn/Fadeout Settings")]
        [SerializeField] private float defaultFadeOutDuration = 0.35f;

        private PostProcessVolume volume;
        private UnityEngine.Rendering.PostProcessing.ChromaticAberration chromaticAberration;

        private List<string> spectralLutsId = new List<string>();

        public virtual void SetSpawnParameters(IReadOnlyList<string> parameters, bool asap)
        {
            Duration = asap ? 0 : Mathf.Abs(parameters?.ElementAtOrDefault(0)?.AsInvariantFloat() ?? defaultDuration);
            VolumeWeight = parameters?.ElementAtOrDefault(1)?.AsInvariantFloat() ?? defaultVolumeWeight;
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
            if (volume.weight != volumeWeight) tasks.Add(ChangeVolumeWeightAsync(volumeWeight, duration, asyncToken));
            if (chromaticAberration.spectralLut.value != null && chromaticAberration.spectralLut.value.name != spectralLut) ChangeTexture(spectralLut);
            if (chromaticAberration.intensity.value != intensity) tasks.Add(ChangeIntensityAsync(intensity, duration, asyncToken));

            await UniTask.WhenAll(tasks);
        }

        public void SetDestroyParameters(IReadOnlyList<string> parameters)
        {
            FadeOutDuration = parameters?.ElementAtOrDefault(0)?.AsInvariantFloat() ?? defaultFadeOutDuration;
        }

        public async UniTask AwaitDestroyAsync(AsyncToken asyncToken = default)
        {
            CompleteTweens();
            var duration = asyncToken.Completed ? 0 : FadeOutDuration;
            await ChangeVolumeWeightAsync(0f, duration, asyncToken);
        }

        private void CompleteTweens()
        {
            if (intensityTweener.Running) intensityTweener.CompleteInstantly();
            if (volumeWeightTweener.Running) volumeWeightTweener.CompleteInstantly();
        }

        private void Awake()
        {
            volume = GetComponent<PostProcessVolume>();
            chromaticAberration = volume.profile.GetSetting<UnityEngine.Rendering.PostProcessing.ChromaticAberration>() ?? volume.profile.AddSettings<UnityEngine.Rendering.PostProcessing.ChromaticAberration>();
            chromaticAberration.SetAllOverridesTo(true);
            volume.weight = 0f;

            spectralLutsId = spectralLuts.Select(s => s.name).ToList();
            spectralLutsId.Insert(0, "None");
        }

        private async UniTask ChangeVolumeWeightAsync(float volumeWeight, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await volumeWeightTweener.RunAsync(new FloatTween(volume.weight, volumeWeight, duration, x => volume.weight = x), asyncToken, volume);
            else volume.weight = volumeWeight;
        }

        private async UniTask ChangeIntensityAsync(float intensity, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await intensityTweener.RunAsync(new FloatTween(chromaticAberration.intensity.value, Intensity, duration, x => chromaticAberration.intensity.value = x), asyncToken, chromaticAberration);
            else chromaticAberration.intensity.value = intensity;
        }

        private void ChangeTexture(string imageId)
        {
            if (imageId == "None" || String.IsNullOrEmpty(imageId)) chromaticAberration.spectralLut.value = null;
            else
            {
                foreach (var img in spectralLuts)
                {
                    if (img != null && img.name == imageId)
                    {
                        chromaticAberration.spectralLut.value = img;
                    }
                }
            }
        }

#if UNITY_EDITOR

        public string SceneAssistantParameters()
        {
            EditorGUIUtility.labelWidth = 190;

            Duration = FloatField("Fade-in time", Duration);

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Volume Weight", GUILayout.Width(190));
            volume.weight = EditorGUILayout.Slider(volume.weight, 0f, 1f, GUILayout.Width(220));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Spectral LUT", GUILayout.Width(190));
            string[] lutsArray = spectralLutsId.ToArray();
            var textureIndex = Array.IndexOf(lutsArray, chromaticAberration.spectralLut.value?.name ?? "None");
            textureIndex = EditorGUILayout.Popup(textureIndex, lutsArray, GUILayout.Height(20), GUILayout.Width(220));
            chromaticAberration.spectralLut.value = spectralLuts.FirstOrDefault(s => s.name == spectralLutsId[textureIndex]) ?? null;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Intensity", GUILayout.Width(190));
            chromaticAberration.intensity.value = EditorGUILayout.Slider(chromaticAberration.intensity.value, 0f, 1f, GUILayout.Width(220));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Fast Mode", GUILayout.Width(190));
            string[] options = new string[] { "True", "False" };
            var fastIndex = Array.IndexOf(options, chromaticAberration.fastMode.value.ToString());
            fastIndex = EditorGUILayout.Popup(fastIndex, options, GUILayout.Height(20), GUILayout.Width(220));
            chromaticAberration.fastMode.value = bool.Parse(options[fastIndex]);
            GUILayout.EndHorizontal();

            return base.GetSpawnString();
        }

        public Dictionary<string, string> ParameterList()
        {
            return new Dictionary<string, string>()
            {
                { "time", Duration.ToString()},
                { "weight", volume.weight.ToString()},
                { "spectralLut", chromaticAberration.spectralLut.value.name},
                { "intensity", chromaticAberration.intensity.value.ToString()},
                { "fastMode", chromaticAberration.fastMode.value.ToString().ToLower()},
            };
        }

#endif
    }


#if UNITY_EDITOR

    [CustomEditor(typeof(ChromaticAberration))]
    public class CopyFXChromaticAberration : PostProcessObjectEditor
    {
        protected override string label => "chromaticAberration";
    }

#endif

}

#endif