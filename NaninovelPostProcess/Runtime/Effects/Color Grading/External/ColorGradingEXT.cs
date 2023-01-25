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
    public class ColorGradingEXT : PostProcessObject, Spawn.IParameterized, Spawn.IAwaitable, DestroySpawned.IParameterized, DestroySpawned.IAwaitable, PostProcessObject.ITextureParameterized, ISceneAssistant
    {
        protected string LookUpTexture { get; private set; }

        [Header("Color Grading Settings")]
        [SerializeField] private string defaultLookUpTexture = "None";
        [SerializeField] private List<Texture> lookUpTextures = new List<Texture>();

        private UnityEngine.Rendering.PostProcessing.ColorGrading colorGrading;

        public List<Texture> TextureItems() => lookUpTextures;

        public override void SetSpawnParameters(IReadOnlyList<string> parameters, bool asap)
        {
            base.SetSpawnParameters(parameters, asap);
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

            if (Volume.weight != volumeWeight) tasks.Add(ChangeVolumeWeightAsync(volumeWeight, duration, asyncToken));
            if (colorGrading.externalLut.value != null && colorGrading.externalLut.value.ToString() != lookUpTexture) ChangeTexture(lookUpTexture);
            
            await UniTask.WhenAll(tasks);
        }

        protected override void CompleteTweens()
        {
            if(volumeWeightTweener.Running) volumeWeightTweener.CompleteInstantly();
        }

        protected override void Awake()
        {
            base.Awake();
            colorGrading = Volume.profile.GetSetting<UnityEngine.Rendering.PostProcessing.ColorGrading>() ?? Volume.profile.AddSettings<UnityEngine.Rendering.PostProcessing.ColorGrading>();
            colorGrading.SetAllOverridesTo(true);
            colorGrading.gradingMode.value = GradingMode.External;
        }

        private void ChangeTexture(string imageId)
        {
            if (imageId == "None" || String.IsNullOrEmpty(imageId)) colorGrading.ldrLut.value = null;
            else lookUpTextures.Select(t => t != null && t.name == imageId);
        }

    #if UNITY_EDITOR
        public string SceneAssistantParameters()
        {
            Duration = SpawnSceneAssistant.FloatField("Fade-in time", Duration);
            Volume.weight = SpawnSceneAssistant.SliderField("Volume Weight", Volume.weight, 0f, 1f);
            colorGrading.externalLut.value = SpawnSceneAssistant.TextureField("Lookup Texture", colorGrading.externalLut.value, this is PostProcessObject.ITextureParameterized textureParameterized ? textureParameterized.TextureItems() : null);
            return SpawnSceneAssistant.GetSpawnString(ParameterList());
        }

        public IReadOnlyDictionary<string, string> ParameterList()
        {
            if (colorGrading == null) return null;

            return new Dictionary<string, string>()
            {
                { "time", Duration.ToString()},
                { "weight", Volume.weight.ToString()},
                { "lookUpTexture", colorGrading.externalLut.value?.name},
            };
        }
    #endif
    }


    #if UNITY_EDITOR

    [CustomEditor(typeof(ColorGradingEXT))]
    public class ColorGradingEXTEditor : PostProcessObjectEditor
    {
        protected override string Label => "colorGradingEXT";
    }

    #endif

}

#endif