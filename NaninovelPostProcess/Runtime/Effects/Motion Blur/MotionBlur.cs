//2022 idaocracy

#if UNITY_POST_PROCESSING_STACK_V2

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
    public class MotionBlur : PostProcessObject, Spawn.IParameterized, Spawn.IAwaitable, DestroySpawned.IParameterized, DestroySpawned.IAwaitable, ISceneAssistant
    {
        protected float ShutterAngle { get; private set; }
        protected float SampleCount { get; private set; }

        private readonly Tweener<FloatTween> shutterAngleTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> sampleCountTweener = new Tweener<FloatTween>();

        [Header("Motion Blur Settings")]
        [SerializeField, Range(0f, 360f)] private float defaultShutterAngle = 270f;
        [SerializeField, Range(4, 32)] private float defaultSampleCount = 10f;

        private UnityEngine.Rendering.PostProcessing.MotionBlur motionBlur;

        public override void SetSpawnParameters(IReadOnlyList<string> parameters, bool asap)
        {
            base.SetSpawnParameters(parameters, asap);
            ShutterAngle = parameters?.ElementAtOrDefault(2)?.AsInvariantFloat() ?? defaultShutterAngle;
            SampleCount = parameters?.ElementAtOrDefault(2)?.AsInvariantFloat() ?? defaultSampleCount;
        }

        public async UniTask AwaitSpawnAsync(AsyncToken asyncToken = default)
        {
            CompleteTweens();
            var duration = asyncToken.Completed ? 0 : Duration;
            await ChangeDoFAsync(duration, VolumeWeight, ShutterAngle, SampleCount, asyncToken);
        }

        public async UniTask ChangeDoFAsync(float duration, float volumeWeight, float focusDistance, float focalLength, AsyncToken asyncToken = default)
        {
            var tasks = new List<UniTask>();
            if (Volume.weight != volumeWeight) tasks.Add(ChangeVolumeWeightAsync(volumeWeight, duration, asyncToken));
            if (motionBlur.shutterAngle.value != focusDistance) tasks.Add(ChangeShutterAngleAsync(focusDistance, duration, asyncToken));
            if (motionBlur.sampleCount.value != focalLength) tasks.Add(ChangeSampleCountAsync(focalLength, duration, asyncToken));

            await UniTask.WhenAll(tasks);
        }

        protected override void CompleteTweens()
        {
            if (shutterAngleTweener.Running) shutterAngleTweener.CompleteInstantly();
            if (sampleCountTweener.Running) sampleCountTweener.CompleteInstantly();
            if (volumeWeightTweener.Running) volumeWeightTweener.CompleteInstantly();
        }

        protected override void Awake()
        {
            base.Awake();
            motionBlur = Volume.profile.GetSetting<UnityEngine.Rendering.PostProcessing.MotionBlur>() ?? Volume.profile.AddSettings<UnityEngine.Rendering.PostProcessing.MotionBlur>();
            motionBlur.SetAllOverridesTo(true);
        }
        private async UniTask ChangeShutterAngleAsync(float shutterAngle, float duration, AsyncToken asyncToken = default)
        {

            if (duration > 0) await shutterAngleTweener.RunAsync(new FloatTween(motionBlur.shutterAngle.value, shutterAngle, duration, x => motionBlur.shutterAngle.value = x), asyncToken, motionBlur);
            else motionBlur.shutterAngle.value = shutterAngle;
        }
        private async UniTask ChangeSampleCountAsync(float sampleCount, float duration, AsyncToken asyncToken = default)
        {

            if (duration > 0) await sampleCountTweener.RunAsync(new FloatTween((int)motionBlur.sampleCount.value, sampleCount, duration, x => motionBlur.sampleCount.value = (int)x), asyncToken, motionBlur);
            else motionBlur.sampleCount.value = (int)sampleCount;
        }

    #if UNITY_EDITOR

        public string SceneAssistantParameters()
        {
            Duration = SpawnSceneAssistant.FloatField("Fade-in time", Duration);
            Volume.weight = SpawnSceneAssistant.SliderField("Volume Weight", Volume.weight, 0f, 1f);
            motionBlur.shutterAngle.value = SpawnSceneAssistant.SliderField("Shutter Angle", motionBlur.shutterAngle.value, 0f, 360f);
            motionBlur.sampleCount.value = (int)SpawnSceneAssistant.SliderField("Sample Count", motionBlur.sampleCount.value, 4, 32);

            return SpawnSceneAssistant.GetSpawnString(ParameterList());
        }

        public IReadOnlyDictionary<string, string> ParameterList()
        {
            if (motionBlur == null) return null;

            return new Dictionary<string, string>()
            {
                { "time", Duration.ToString()},
                { "weight", Volume.weight.ToString()},
                { "shutterAngle", motionBlur.shutterAngle.value.ToString()},
                { "sampleCount", motionBlur.sampleCount.value.ToString()},
            };
        }

    #endif
    }


    #if UNITY_EDITOR

    [CustomEditor(typeof(MotionBlur))]
    public class MotionBlurEditor : PostProcessObjectEditor
    {
        protected override string Label => "motionBlur";
    }

    #endif

}

#endif