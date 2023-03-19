//2022-2023 idaocracy

#if UNITY_POST_PROCESSING_STACK_V2

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using Naninovel;
using Naninovel.Commands;
#if UNITY_EDITOR && NANINOVEL_SCENE_ASSISTANT_AVAILABLE
using UnityEditor;
using NaninovelSceneAssistant;
#endif

namespace NaninovelPostProcess
{
    [RequireComponent(typeof(PostProcessVolume))]
    public class AutoExposure : PostProcessSpawnObject, Spawn.IParameterized, Spawn.IAwaitable, DestroySpawned.IParameterized, DestroySpawned.IAwaitable
    {
        protected Vector2 Filtering { get; private set; }
        protected float Minimum { get; private set; }
        protected float Maximum { get; private set; }
        protected float ExposureCompensation { get; private set; }
        protected string Type { get; private set; }
        protected float SpeedUp { get; private set; }
        protected float SpeedDown { get; private set; }

        private readonly Tweener<VectorTween> filteringTweener = new Tweener<VectorTween>();
        private readonly Tweener<FloatTween> minimumTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> maximumTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> exposureCompensationTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> speedUpTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> speedDownTweener = new Tweener<FloatTween>();

        [Header("Auto Exposure Settings")]
        [SerializeField, MinMax(1f, 99f)] private Vector2 defaultFiltering = new Vector2(50f,95f);
        [SerializeField, Range(-9f,9f)] private float defaultMinimum = 0f;
        [SerializeField, Range(-9f, 9f)] private float defaultMaximum = 0f;
        [SerializeField, UnityEngine.Min(0f)] private float defaultExposureCompensation = 1f;
        [SerializeField] private EyeAdaptation defaultType = EyeAdaptation.Progressive;
        [SerializeField, UnityEngine.Min(0f)] private float defaultSpeedUp = 2f;
        [SerializeField, UnityEngine.Min(0f)] private float defaultSpeedDown = 1f;

        private UnityEngine.Rendering.PostProcessing.AutoExposure autoExposure;

        public override void SetSpawnParameters(IReadOnlyList<string> parameters, bool asap)
        {
            base.SetSpawnParameters(parameters, asap);
            Filtering = new Vector2(parameters?.ElementAtOrDefault(2)?.AsInvariantFloat() ?? defaultFiltering.x, parameters?.ElementAtOrDefault(3)?.AsInvariantFloat() ?? defaultFiltering.y) ;
            Minimum = parameters?.ElementAtOrDefault(4)?.AsInvariantFloat() ?? defaultMinimum;
            Maximum = parameters?.ElementAtOrDefault(5)?.AsInvariantFloat() ?? defaultMaximum;
            ExposureCompensation = parameters?.ElementAtOrDefault(6)?.AsInvariantFloat() ?? defaultExposureCompensation;
            Type = parameters?.ElementAtOrDefault(7)?.ToString() ?? defaultType.ToString();

            if(Type == "Progressive") { 
                SpeedUp = parameters?.ElementAtOrDefault(8)?.AsInvariantFloat() ?? defaultSpeedUp;
                SpeedDown = parameters?.ElementAtOrDefault(9)?.AsInvariantFloat() ?? defaultSpeedDown;
            }
        }

        public async UniTask AwaitSpawnAsync(AsyncToken asyncToken = default)
        {
            CompleteTweens();
            var duration = asyncToken.Completed ? 0 : Duration;
            await ChangeAutoExposureAsync(duration, VolumeWeight, Filtering, Minimum,  Maximum, ExposureCompensation, Type, SpeedUp, SpeedDown, asyncToken);
        }

        public async UniTask ChangeAutoExposureAsync(float duration, float volumeWeight, Vector2 filtering, float minimum, float maximum, float exposureCompensation, string type, float speedUp, float speedDown, AsyncToken asyncToken = default)
        {
            var tasks = new List<UniTask>();
            if (Volume.weight != volumeWeight) tasks.Add(ChangeVolumeWeightAsync(volumeWeight, duration, asyncToken));
            if (autoExposure.filtering.value != filtering) tasks.Add(ChangeFilteringAsync(filtering, duration, asyncToken));
            if (autoExposure.minLuminance.value != minimum) tasks.Add(ChangeMinimumAsync(minimum, duration, asyncToken));
            if (autoExposure.maxLuminance.value != maximum) tasks.Add(ChangeMaximumAsync(maximum, duration, asyncToken));
            if (autoExposure.keyValue.value != exposureCompensation) tasks.Add(ChangeExposureCompensationAsync(exposureCompensation, duration, asyncToken));
            autoExposure.eyeAdaptation.value = (EyeAdaptation)System.Enum.Parse(typeof(EyeAdaptation), type);

            if(type == "Progressive") { 
                if (autoExposure.speedUp.value != speedUp) tasks.Add(ChangeSpeedUpAsync(speedUp, duration, asyncToken));
                if (autoExposure.speedDown.value != speedDown) tasks.Add(ChangeSpeedDownAsync(speedDown, duration, asyncToken));
            }

            await UniTask.WhenAll(tasks);
        }

        protected override void CompleteTweens()
        {
            if (volumeWeightTweener.Running) volumeWeightTweener.CompleteInstantly();
            if (filteringTweener.Running) filteringTweener.CompleteInstantly();
            if (minimumTweener.Running) minimumTweener.CompleteInstantly();
            if (maximumTweener.Running) maximumTweener.CompleteInstantly();
            if (exposureCompensationTweener.Running) exposureCompensationTweener.CompleteInstantly();
            if (speedUpTweener.Running) speedUpTweener.CompleteInstantly();
            if (speedDownTweener.Running) speedDownTweener.CompleteInstantly();
        }

        protected override void Awake()
        {
            base.Awake();
            autoExposure = Volume.profile.GetSetting<UnityEngine.Rendering.PostProcessing.AutoExposure>() ?? Volume.profile.AddSettings<UnityEngine.Rendering.PostProcessing.AutoExposure>();
            autoExposure.SetAllOverridesTo(true);
        }

        private async UniTask ChangeFilteringAsync(Vector2 filtering, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await filteringTweener.RunAsync(new VectorTween(autoExposure.filtering.value, filtering, duration, x => autoExposure.filtering.value = x), asyncToken, autoExposure);
            else autoExposure.filtering.value = filtering;
        }
        private async UniTask ChangeMinimumAsync(float minimum, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await minimumTweener.RunAsync(new FloatTween(autoExposure.minLuminance.value, minimum, duration, x => autoExposure.minLuminance.value = x), asyncToken, autoExposure);
            else autoExposure.minLuminance.value = minimum;
        }
        private async UniTask ChangeMaximumAsync(float maximum, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await maximumTweener.RunAsync(new FloatTween(autoExposure.maxLuminance.value, maximum, duration, x => autoExposure.maxLuminance.value = x), asyncToken, autoExposure);
            else autoExposure.maxLuminance.value = maximum;
        }
        private async UniTask ChangeExposureCompensationAsync(float exposureCompensation, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await exposureCompensationTweener.RunAsync(new FloatTween(autoExposure.keyValue.value, exposureCompensation, duration, x => autoExposure.keyValue.value = x), asyncToken, autoExposure);
            else autoExposure.keyValue.value = exposureCompensation;
        }
        private async UniTask ChangeSpeedUpAsync(float speedUp, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await speedUpTweener.RunAsync(new FloatTween(autoExposure.speedUp.value, speedUp, duration, x => autoExposure.speedUp.value = x), asyncToken, autoExposure);
            else autoExposure.speedUp.value = speedUp;
        }
        private async UniTask ChangeSpeedDownAsync(float anamorphicRatio, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await speedDownTweener.RunAsync(new FloatTween(autoExposure.speedDown.value, anamorphicRatio, duration, x => autoExposure.speedDown.value = x), asyncToken, autoExposure);
            else autoExposure.speedDown.value = anamorphicRatio;
        }

#if UNITY_EDITOR && NANINOVEL_SCENE_ASSISTANT_AVAILABLE
        public override List<ICommandParameterData> GetParams()
        {
            return new List<ICommandParameterData>()
            {
                { new CommandParameterData<float>("Time", () => Duration, v => Duration = v, (i,p) => i.FloatField(p), defaultSpawnDuration)},
                { new CommandParameterData<float>("Weight", () => Volume.weight, v => Volume.weight = v, (i,p) => i.FloatSliderField(p, 0f, 1f), defaultVolumeWeight)},
                { new CommandParameterData<float>("FilteringX", () => autoExposure.filtering.value.x, v => autoExposure.filtering.value.x = v, (i,p) => i.FloatField(p), defaultFiltering.x)},
                { new CommandParameterData<float>("FilteringY", () => autoExposure.filtering.value.y, v => autoExposure.filtering.value.y = v, (i,p) => i.FloatField(p), defaultFiltering.y)},
                { new CommandParameterData<float>("Minimum", () => autoExposure.minLuminance.value, v => autoExposure.minLuminance.value = v, (i,p) => i.FloatSliderField(p, -9f, 9f), defaultMinimum)},
                { new CommandParameterData<float>("Maximum", () => autoExposure.maxLuminance.value, v => autoExposure.maxLuminance.value = v, (i,p) => i.FloatSliderField(p, -9f, 9f), defaultMaximum)},
                { new CommandParameterData<float>("ExposureCompensation", () => autoExposure.keyValue.value, v => autoExposure.keyValue.value = v, (i,p) => i.FloatField(p), defaultExposureCompensation)},
                { new CommandParameterData<Enum>("ProgressiveOrFixed", () => autoExposure.eyeAdaptation.value, v => autoExposure.eyeAdaptation.value = (EyeAdaptation)v, (i,p) => i.EnumField(p), defaultType)},
                { new CommandParameterData<float>("ProgressiveSpeedUp", () => autoExposure.speedUp.value, v => autoExposure.speedUp.value = v, (i,p) => i.FloatField(p, min:0f), defaultValue: defaultSpeedUp, getCondition: () => autoExposure.eyeAdaptation.value == EyeAdaptation.Progressive)},
                { new CommandParameterData<float>("ProgressiveSpeedDown", () => autoExposure.speedDown.value, v => autoExposure.speedDown.value = v, (i,p) => i.FloatField(p, min:0f), defaultValue: defaultSpeedDown, getCondition: () => autoExposure.eyeAdaptation.value == EyeAdaptation.Progressive)},
            };
        }
#endif
    }

    #if UNITY_EDITOR && NANINOVEL_SCENE_ASSISTANT_AVAILABLE
    [CustomEditor(typeof(AutoExposure))]
    public class AutoExposureEditor : SpawnObjectEditor { }
    #endif
}

#endif