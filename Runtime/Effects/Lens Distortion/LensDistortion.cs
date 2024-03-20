//2022-2023 idaocracy

#if UNITY_POST_PROCESSING_STACK_V2

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using Naninovel;
using Naninovel.Commands;
#if NANINOVEL_SCENE_ASSISTANT_AVAILABLE
using NaninovelSceneAssistant;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NaninovelPostProcess { 

    [RequireComponent(typeof(PostProcessVolume))]
    public class LensDistortion : PostProcessSpawnObject, Spawn.IParameterized, Spawn.IAwaitable, DestroySpawned.IParameterized, DestroySpawned.IAwaitable
    {
        protected float Intensity { get; private set; }
        protected float XMultiplier { get; private set; }
        protected float YMultiplier { get; private set; }
        protected float CenterX { get; private set; }
        protected float CenterY { get; private set; }
        protected float Scale { get; private set; }

        private readonly Tweener<FloatTween> intensityTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> xMultiplierTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> yMultiplierTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> centerXTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> centerYTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> scaleTweener = new Tweener<FloatTween>();

        [Header("Lens Distortion Settings")]
        [SerializeField, Range(-100f, 100f)] private float defaultIntensity = 0f;
        [SerializeField, Range(0f, 1f)] private float defaultXMultiplier = 1f;
        [SerializeField, Range(0f, 1f)] private float defaultYMultiplier = 1f;
        [SerializeField, Range(-1f, 1f)] private float defaultCenterX = 0f;
        [SerializeField, Range(-1f, 1f)] private float defaultCenterY = 0f;
        [SerializeField, Range(0.01f, 5f)] private float defaultScale = 1f;

        private UnityEngine.Rendering.PostProcessing.LensDistortion lensDistortion;

        public override void SetSpawnParameters(IReadOnlyList<string> parameters, bool asap)
        {
            base.SetSpawnParameters(parameters, asap);
            Intensity = parameters?.ElementAtOrDefault(2)?.AsInvariantFloat() ?? defaultIntensity;
            XMultiplier = parameters?.ElementAtOrDefault(3)?.AsInvariantFloat() ?? defaultYMultiplier;
            YMultiplier = parameters?.ElementAtOrDefault(4)?.AsInvariantFloat() ?? defaultXMultiplier;
            CenterX = parameters?.ElementAtOrDefault(5)?.AsInvariantFloat() ?? defaultCenterX;
            CenterY = parameters?.ElementAtOrDefault(6)?.AsInvariantFloat() ?? defaultCenterY;
            Scale = parameters?.ElementAtOrDefault(7)?.AsInvariantFloat() ?? defaultScale;
        }

        public async UniTask AwaitSpawnAsync(AsyncToken asyncToken = default)
        {
            CompleteTweens();
            var duration = asyncToken.Completed ? 0 : Duration;
            await ChangeLensDistortionAsync(duration, VolumeWeight, Intensity, XMultiplier, YMultiplier, CenterX, CenterY, Scale, asyncToken);
        }

        public async UniTask ChangeLensDistortionAsync(float duration, float volumeWeight, float intensity, float xMultiplier, float yMultiplier, float centerX, float centerY, float scale, AsyncToken asyncToken = default)
        {
            var tasks = new List<UniTask>();
            if (Volume.weight != volumeWeight) tasks.Add(ChangeVolumeWeightAsync(volumeWeight, duration, asyncToken));
            if (lensDistortion.intensity.value != intensity) tasks.Add(ChangeIntensityAsync(intensity, duration, asyncToken));
            if (lensDistortion.intensityX.value != xMultiplier) tasks.Add(ChangeXMultiplierAsync(xMultiplier, duration, asyncToken));
            if (lensDistortion.intensityY.value != yMultiplier) tasks.Add(ChangeYMultiplierAsync(yMultiplier, duration, asyncToken));
            if (lensDistortion.centerX.value != centerX) tasks.Add(ChangeCenterXAsync(centerX, duration, asyncToken));
            if (lensDistortion.centerY.value != centerY) tasks.Add(ChangeCenterYAsync(centerX, duration, asyncToken));
            if (lensDistortion.scale.value != centerY) tasks.Add(ChangeScaleAsync(scale, duration, asyncToken));

            await UniTask.WhenAll(tasks);
        }

        protected override void CompleteTweens()
        {
            if (intensityTweener.Running) intensityTweener.CompleteInstantly();
            if (xMultiplierTweener.Running) xMultiplierTweener.CompleteInstantly();
            if (yMultiplierTweener.Running) yMultiplierTweener.CompleteInstantly();
            if (centerXTweener.Running) centerXTweener.CompleteInstantly();
            if (centerYTweener.Running) centerYTweener.CompleteInstantly();
            if (scaleTweener.Running) scaleTweener.CompleteInstantly();
            if (volumeWeightTweener.Running) volumeWeightTweener.CompleteInstantly();
        }

        private void OnDestroy()
        {
            Volume.profile.RemoveSettings<UnityEngine.Rendering.PostProcessing.LensDistortion>();
        }

        protected override void Awake()
        {
            base.Awake();
            lensDistortion = Volume.profile.GetSetting<UnityEngine.Rendering.PostProcessing.LensDistortion>() ?? Volume.profile.AddSettings<UnityEngine.Rendering.PostProcessing.LensDistortion>();
            lensDistortion.SetAllOverridesTo(true);
        }

        private async UniTask ChangeIntensityAsync(float intensity, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await intensityTweener.RunAsync(new FloatTween(lensDistortion.intensity.value, intensity, duration, x => lensDistortion.intensity.value = x, IgnoreTimescale), asyncToken, lensDistortion);
            else lensDistortion.intensity.value = intensity;
        }
        private async UniTask ChangeXMultiplierAsync(float xMultiplier, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await xMultiplierTweener.RunAsync(new FloatTween(lensDistortion.intensityX.value, xMultiplier, duration, x => lensDistortion.intensityX.value = x, IgnoreTimescale), asyncToken, lensDistortion);
            else lensDistortion.intensityX.value = xMultiplier;
        }    
        private async UniTask ChangeYMultiplierAsync(float yMultiplier, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await yMultiplierTweener.RunAsync(new FloatTween(lensDistortion.intensityY.value, yMultiplier, duration, x => lensDistortion.intensityY.value = x, IgnoreTimescale), asyncToken, lensDistortion);
            else lensDistortion.intensityY.value = yMultiplier;
        }    
        private async UniTask ChangeCenterXAsync(float centerX, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await centerXTweener.RunAsync(new FloatTween(lensDistortion.centerX.value, centerX, duration, x => lensDistortion.centerX.value = x, IgnoreTimescale), asyncToken, lensDistortion);
            else lensDistortion.centerX.value = centerX;
        }   
        private async UniTask ChangeCenterYAsync(float centerY, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await centerYTweener.RunAsync(new FloatTween(lensDistortion.centerY.value, centerY, duration, x => lensDistortion.centerY.value = x, IgnoreTimescale), asyncToken, lensDistortion);
            else lensDistortion.centerY.value = centerY;
        }    
        private async UniTask ChangeScaleAsync(float scale, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await scaleTweener.RunAsync(new FloatTween(lensDistortion.scale.value, scale, duration, x => lensDistortion.scale.value = x, IgnoreTimescale), asyncToken, lensDistortion);
            else lensDistortion.scale.value = scale;
        }

#if NANINOVEL_SCENE_ASSISTANT_AVAILABLE
        public override List<ICommandParameterData> GetParams()
        {
            return new List<ICommandParameterData>
            {
                { new CommandParameterData<float>("Time", () => Duration, v => Duration = v, (i,p) => i.FloatField(p), defaultSpawnDuration)},
                { new CommandParameterData<float>("Weight", () => Volume.weight, v => Volume.weight = v, (i,p) => i.FloatSliderField(p, 0f, 1f), defaultVolumeWeight)},
                { new CommandParameterData<float>("Intensity", () => lensDistortion.intensity.value, v => lensDistortion.intensity.value = v, (i,p) => i.FloatSliderField(p, -100f, 100f), defaultIntensity)},
                { new CommandParameterData<float>("XMultiplier", () => lensDistortion.intensityX.value, v => lensDistortion.intensityX.value = v, (i,p) => i.FloatSliderField(p, 0f, 1f), defaultXMultiplier)},
                { new CommandParameterData<float>("YMultiplier", () => lensDistortion.intensityY.value, v => lensDistortion.intensityY.value = v, (i,p) => i.FloatSliderField(p, 0f, 1f), defaultYMultiplier)},
                { new CommandParameterData<float>("CenterX", () => lensDistortion.centerX.value, v => lensDistortion.centerX.value = v, (i,p) => i.FloatSliderField(p, -1f, 1f), defaultCenterX)},
                { new CommandParameterData<float>("CenterY", () => lensDistortion.centerY.value, v => lensDistortion.centerY.value = v, (i,p) => i.FloatSliderField(p, -1f, 1f), defaultCenterY)},
                { new CommandParameterData<float>("Scale", () => lensDistortion.scale.value, v => lensDistortion.scale.value = v, (i,p) => i.FloatSliderField(p, 0.01f, 5f), defaultScale)},
            };
        }
#endif
    }

#if UNITY_EDITOR && NANINOVEL_SCENE_ASSISTANT_AVAILABLE
    [CustomEditor(typeof(LensDistortion))]
    public class LensDistortionEditor : SpawnObjectEditor { }
#endif

}

#endif