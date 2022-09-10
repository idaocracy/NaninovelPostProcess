//2022 idaocracy
#if UNITY_POST_PROCESSING_STACK_V2


using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using Naninovel;
using Naninovel.Commands;
#if UNITY_EDITOR
using UnityEditor;
using System;
#endif

namespace NaninovelPostProcessFX { 

    [RequireComponent(typeof(PostProcessVolume))]
    public class ChromaticAberration : MonoBehaviour, Spawn.IParameterized, Spawn.IAwaitable, DestroySpawned.IParameterized, DestroySpawned.IAwaitable
    {
        protected float Duration { get; private set; }
        protected float VolumeWeight { get; private set; }
        protected string SpectralLut { get; private set; }
        protected float Intensity { get; private set; }
        protected bool FastMode { get; private set; }

        protected float FadeOutDuration { get; private set; }

        public bool logResult { get; set; }

        private readonly Tweener<FloatTween> volumeWeightTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> intensityTweener = new Tweener<FloatTween>();

        [SerializeField] private float defaultDuration = 0.35f;
        [SerializeField] private float defaultVolumeWeight = 1f;
        [SerializeField] private string defaultSpectralLutId = string.Empty;
        [SerializeField] private float defaultIntensity = 1f;
        [SerializeField] private bool defaultFastMode = false;

        [SerializeField] private float defaultFadeOutDuration = 0.35f;

        private PostProcessVolume volume;
        private UnityEngine.Rendering.PostProcessing.ChromaticAberration chromaticAberration;

        [SerializeField] private List<Texture> spectralLuts = new List<Texture>();
        [SerializeField] private List<string> spectralLutsId = new List<string>();

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
            if (intensityTweener.Running) intensityTweener.CompleteInstantly();
            if (volumeWeightTweener.Running) volumeWeightTweener.CompleteInstantly();

            var duration = asyncToken.Completed ? 0 : Duration;
            await ChangeChromaticAberration(duration, VolumeWeight, SpectralLut, Intensity, FastMode, asyncToken);
        }

        public async UniTask ChangeChromaticAberration(float duration, float volumeWeight, string spectralLut, float intensity, bool fastMode, AsyncToken asyncToken = default)
        {
            var tasks = new List<UniTask>();
            if (volume.weight != volumeWeight) tasks.Add(ChangeVolumeWeightAsync(volumeWeight, duration, asyncToken));
            if (chromaticAberration.spectralLut.value?.ToString() != spectralLut) ChangeTexture(spectralLut);
            if (chromaticAberration.intensity.value != intensity) tasks.Add(ChangeIntensityAsync(intensity, duration, asyncToken));

            await UniTask.WhenAll(tasks);
        }

        public void SetDestroyParameters(IReadOnlyList<string> parameters)
        {
            FadeOutDuration = parameters?.ElementAtOrDefault(0)?.AsInvariantFloat() ?? defaultFadeOutDuration;
        }

        public async UniTask AwaitDestroyAsync(AsyncToken asyncToken = default)
        {
            if (intensityTweener.Running) intensityTweener.CompleteInstantly();
            if (volumeWeightTweener.Running) volumeWeightTweener.CompleteInstantly();

            var duration = asyncToken.Completed ? 0 : FadeOutDuration;
            await ChangeVolumeWeightAsync(0f, duration, asyncToken);
        }

        private void OnDestroy()
        {
            volume.profile.RemoveSettings<UnityEngine.Rendering.PostProcessing.ChromaticAberration>();
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

        private async UniTask ChangeIntensityAsync(float shutterAngle, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await intensityTweener.RunAsync(new FloatTween(chromaticAberration.intensity.value, Intensity, duration, x => chromaticAberration.intensity.value = x), asyncToken, chromaticAberration);
            else chromaticAberration.intensity.value = shutterAngle;
        }

        private void ChangeTexture(string imageId)
        {
            if (imageId == "None" || String.IsNullOrEmpty(imageId)) return;

            foreach (var img in spectralLuts)
            {
                if(img != null && img.name == imageId)
                {
                    chromaticAberration.spectralLut.value = img;
                }
            }
        }

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

            return Duration + "," + volume.weight + "," + chromaticAberration.spectralLut.value?.name + "," + chromaticAberration.intensity.value + "," + chromaticAberration.fastMode.value.ToString().ToLower();
        }
    }


#if UNITY_EDITOR

    [CustomEditor(typeof(ChromaticAberration))]
    public class CopyFXChromaticAberration : Editor
    {
        private ChromaticAberration targetObject;
        private UnityEngine.Rendering.PostProcessing.ChromaticAberration chromaticAberration;
        private PostProcessVolume volume;
        public bool logResult;

        private void Awake()
        {
            targetObject = (ChromaticAberration)target;
            volume = targetObject.gameObject.GetComponent<PostProcessVolume>();
            chromaticAberration = volume.profile.GetSetting<UnityEngine.Rendering.PostProcessing.ChromaticAberration>();
            logResult = targetObject.logResult;
        }

        public override void OnInspectorGUI()
        {
            base.DrawDefaultInspector();

            GUILayout.Space(20f);
            if (GUILayout.Button("Copy command and params (@)", GUILayout.Height(50)))
            {
                if (chromaticAberration != null) GUIUtility.systemCopyBuffer = "@spawn " + targetObject.gameObject.name + " params:" + CreateString();
                if (logResult) Debug.Log(GUIUtility.systemCopyBuffer);
            }

            GUILayout.Space(20f);
            if (GUILayout.Button("Copy command and params (@)", GUILayout.Height(50)))
            {
                if (chromaticAberration != null) GUIUtility.systemCopyBuffer = "[spawn " + targetObject.gameObject.name + " params:" + CreateString() + "]"; 
                if (logResult) Debug.Log(GUIUtility.systemCopyBuffer);
            }

            GUILayout.Space(20f);
            if (GUILayout.Button("Copy command and params (@)", GUILayout.Height(50)))
            {
                if (chromaticAberration != null) GUIUtility.systemCopyBuffer = CreateString();
                if (logResult) Debug.Log(GUIUtility.systemCopyBuffer);
            }

            GUILayout.Space(20f);
            if (GUILayout.Toggle(logResult, "Log Results")) logResult = true;
            else logResult = false;
        }

        private string CreateString() => "(time)," + volume.weight + "," + chromaticAberration.spectralLut.value?.name + "," + chromaticAberration.intensity.value + "," + chromaticAberration.fastMode.value.ToString().ToLower();
    }

#endif

}

#endif