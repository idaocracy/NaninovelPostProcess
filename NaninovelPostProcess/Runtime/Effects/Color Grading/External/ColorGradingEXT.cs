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

namespace NaninovelPostProcess { 

    [RequireComponent(typeof(PostProcessVolume))]
    public class ColorGradingEXT : PostProcessObject, Spawn.IParameterized, Spawn.IAwaitable, DestroySpawned.IParameterized, DestroySpawned.IAwaitable, PostProcessObject.ISceneAssistant
    {
        protected string LookUpTexture { get; private set; }
        protected float VolumeWeight { get; private set; }
        protected float Duration { get; private set; }
        protected float FadeOutDuration { get; private set; }

        private readonly Tweener<FloatTween> volumeWeightTweener = new Tweener<FloatTween>();

        [Header("Spawn/Fadein Settings")]
        [SerializeField] private float defaultDuration = 0.35f;
        [Header("Volume Settings")]
        [SerializeField] private float defaultVolumeWeight = 1f;
        [Header("Color Grading Settings")]
        [SerializeField] private string defaultLookUpTexture = "None";
        [SerializeField] private List<Texture> lookUpTextures = new List<Texture>();

        [Header("Despawn/Fadeout Settings")]
        [SerializeField] private float defaultFadeOutDuration = 0.35f;

        private PostProcessVolume volume;
        private UnityEngine.Rendering.PostProcessing.ColorGrading colorGrading;

        private List<string> lookUpTextureIds = new List<string>();

        public virtual void SetSpawnParameters(IReadOnlyList<string> parameters, bool asap)
        {
            Duration = asap ? 0 : Mathf.Abs(parameters?.ElementAtOrDefault(0)?.AsInvariantFloat() ?? defaultDuration);
            VolumeWeight = parameters?.ElementAtOrDefault(1)?.AsInvariantFloat() ?? defaultVolumeWeight;
            LookUpTexture = parameters?.ElementAtOrDefault(2) ?? defaultLookUpTexture;
            
        }

        public async UniTask AwaitSpawnAsync(AsyncToken asyncToken = default)
        {
            CompleteTweens();
            var duration = asyncToken.Completed ? 0 : Duration;
            await ChangeColorGradingAsync(duration, VolumeWeight, LookUpTexture, asyncToken);
        }

        public async UniTask ChangeColorGradingAsync(float duration, float volumeWeight, string lookUpTexture, AsyncToken asyncToken = default)
        {

            var tasks = new List<UniTask>();

            if (volume.weight != volumeWeight) tasks.Add(ChangeVolumeWeightAsync(volumeWeight, duration, asyncToken));
            if (colorGrading.externalLut.value != null && colorGrading.externalLut.value.ToString() != lookUpTexture) ChangeTexture(lookUpTexture);
            
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
            if(volumeWeightTweener.Running) volumeWeightTweener.CompleteInstantly();
        }

        private void Awake()
        {
            volume = GetComponent<PostProcessVolume>();
            colorGrading = volume.profile.GetSetting<UnityEngine.Rendering.PostProcessing.ColorGrading>() ?? volume.profile.AddSettings<UnityEngine.Rendering.PostProcessing.ColorGrading>();
            colorGrading.SetAllOverridesTo(true);
            colorGrading.gradingMode.value = GradingMode.External;
            volume.weight = 0f;
            lookUpTextureIds = lookUpTextures.Select(s => s.name).ToList();
            lookUpTextureIds.Insert(0, "None");
        }

        private async UniTask ChangeVolumeWeightAsync(float volumeWeight, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await volumeWeightTweener.RunAsync(new FloatTween(volume.weight, volumeWeight, duration, x => volume.weight = x), asyncToken, volume);
            else volume.weight = volumeWeight;
        }

        private void ChangeTexture(string imageId)
        {
            if (imageId == "None" || String.IsNullOrEmpty(imageId))
            {
                 colorGrading.externalLut.value = null;
            }
            else
            {
                foreach (var img in lookUpTextures)
                {
                    if (img != null && img.name == imageId)
                    {
                        if (colorGrading.externalLut.value.ToString() == "External") colorGrading.externalLut.value = img;
                    }
                }
            }
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
            EditorGUILayout.LabelField("Lookup Texture", GUILayout.Width(190));
            string[] maskTexturesArray = lookUpTextureIds.ToArray();
            var maskIndex = Array.IndexOf(maskTexturesArray, colorGrading.externalLut.value?.name ?? "None");
            maskIndex = EditorGUILayout.Popup(maskIndex, maskTexturesArray, GUILayout.Height(20), GUILayout.Width(220));
            colorGrading.externalLut.value = lookUpTextures.FirstOrDefault(s => s.name == lookUpTextureIds[maskIndex]) ?? null;
            GUILayout.EndHorizontal();

            return base.GetSpawnString();
        }

        public IReadOnlyDictionary<string, string> ParameterList()
        {
            return new Dictionary<string, string>()
            {
                { "time", Duration.ToString()},
                { "weight", volume.weight.ToString()},
                { "lookUpTexture", colorGrading.externalLut.value?.name},
            };
        }

#endif
    }


#if UNITY_EDITOR

    [CustomEditor(typeof(ColorGradingEXT))]
    public class CopyFXColorGradingEXT : PostProcessObjectEditor
    {
        protected override string label => "colorGradingEXT";
    }
#endif

}

#endif