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
    public class Grain : PostProcessObjectManager, Spawn.IParameterized, Spawn.IAwaitable, DestroySpawned.IParameterized, DestroySpawned.IAwaitable, PostProcessObjectManager.ISceneAssistant
    {
        protected float Duration { get; private set; }
        protected float VolumeWeight { get; private set; }
        protected bool Colored { get; private set; }
        protected float Intensity { get; private set; }
        protected float Size { get; private set; }
        protected float LuminanceContribution { get; private set; }

        protected float FadeOutDuration { get; private set; }



        private readonly Tweener<FloatTween> volumeWeightTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> intensityTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> sizeTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> luminanceContributionTweener = new Tweener<FloatTween>();

        [Header("Spawn/Fadein Settings")]
        [SerializeField] private float defaultDuration = 0.35f;
        [Header("Volume Settings")]
        [SerializeField] private float defaultVolumeWeight = 1f;
        [Header("Grain Settings")]
        [SerializeField] private bool defaultColored = true;
        [SerializeField] private float defaultIntensity = 0f;
        [SerializeField] private float defaultSize = 1f;
        [SerializeField] private float defaultluminanceContribution = 0.8f;
        [Header("Despawn/Fadeout Settings")]
        [SerializeField] private float defaultFadeOutDuration = 0.35f;

        private PostProcessVolume volume;
        private UnityEngine.Rendering.PostProcessing.Grain grain;

        public virtual void SetSpawnParameters(IReadOnlyList<string> parameters, bool asap)
        {
            Duration = asap ? 0 : Mathf.Abs(parameters?.ElementAtOrDefault(0)?.AsInvariantFloat() ?? defaultDuration);

            VolumeWeight = parameters?.ElementAtOrDefault(1)?.AsInvariantFloat() ?? defaultVolumeWeight;
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
            if (volume.weight != volumeWeight) tasks.Add(ChangeVolumeWeightAsync(volumeWeight, duration, asyncToken));
            grain.colored.value = colored;
            if (grain.intensity.value != intensity) tasks.Add(ChangeIntensityAsync(intensity, duration, asyncToken));
            if (grain.size.value != size) tasks.Add(ChangeSizeAsync(size, duration, asyncToken));
            if (grain.lumContrib.value != luminanceContribution) tasks.Add(ChangeLuminanceContributionAsync(luminanceContribution, duration, asyncToken));
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
            if (volumeWeightTweener.Running) volumeWeightTweener.CompleteInstantly();
            if (intensityTweener.Running) intensityTweener.CompleteInstantly();
            if (sizeTweener.Running) sizeTweener.CompleteInstantly();
            if (luminanceContributionTweener.Running) luminanceContributionTweener.CompleteInstantly();
        }

        private void Awake()
        {
            volume = GetComponent<PostProcessVolume>();
            grain = volume.profile.GetSetting<UnityEngine.Rendering.PostProcessing.Grain>() ?? volume.profile.AddSettings<UnityEngine.Rendering.PostProcessing.Grain>();
            grain.SetAllOverridesTo(true);
            volume.weight = 0f;
        }

        private async UniTask ChangeVolumeWeightAsync(float volumeWeight, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await volumeWeightTweener.RunAsync(new FloatTween(volume.weight, volumeWeight, duration, x => volume.weight = x), asyncToken, volume);
            else volume.weight = volumeWeight;
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
            EditorGUIUtility.labelWidth = 190;
            GUILayout.BeginHorizontal();
            Duration = EditorGUILayout.FloatField("Fade-in time", Duration, GUILayout.Width(413));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Volume Weight", GUILayout.Width(190));
            volume.weight = EditorGUILayout.Slider(volume.weight, 0f, 1f, GUILayout.Width(220));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Colored", GUILayout.Width(190));
            string[] options = new string[] { "True", "False" };
            var coloredIndex = Array.IndexOf(options, grain.colored.value.ToString());
            coloredIndex = EditorGUILayout.Popup(coloredIndex, options, GUILayout.Height(20), GUILayout.Width(220));
            grain.colored.value = bool.Parse(options[coloredIndex]);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Intensity", GUILayout.Width(190));
            grain.intensity.value = EditorGUILayout.Slider(grain.intensity.value, 0f, 1f, GUILayout.Width(220));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Size", GUILayout.Width(190));
            grain.size.value = EditorGUILayout.Slider(grain.size.value, 0.3f, 3f, GUILayout.Width(220));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Luminance Contribution", GUILayout.Width(190));
            grain.lumContrib.value = EditorGUILayout.Slider(grain.lumContrib.value, 0f, 1f, GUILayout.Width(220));
            GUILayout.EndHorizontal();

            return GetSpawnString();
        }

        public string GetCommandString()
        {
            return "time:" + Duration + " weight:" + volume.weight + " colored:" + grain.colored.value.ToString().ToLower() + " intensity:" + grain.intensity.value + " size:" + grain.size.value + " luminanceContribution:" + grain.lumContrib.value;
        }

        public string GetSpawnString()
        {
            return Duration + "," + volume.weight + "," + grain.colored.value.ToString().ToLower() + "," + grain.intensity.value + "," + grain.size.value + "," + grain.lumContrib.value;
        }
#endif
    }


#if UNITY_EDITOR

    [CustomEditor(typeof(Grain))]
    public class CopyFXGrain : PostProcessEditor
    {
        protected override string label => "grain";
    }

#endif

}

#endif