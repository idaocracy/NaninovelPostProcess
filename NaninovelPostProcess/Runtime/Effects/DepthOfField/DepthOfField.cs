//2022-2023 idaocracy

#if UNITY_POST_PROCESSING_STACK_V2

using System.Collections.Generic;
using System.Linq;
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
    public class DepthOfField : PostProcessSpawnObject, Spawn.IParameterized, Spawn.IAwaitable, DestroySpawned.IParameterized, DestroySpawned.IAwaitable
    {
        protected float FocusDistance { get; private set; }
        protected float Aperture { get; private set; }
        protected float FocalLength { get; private set; }
        protected string MaxBlurSize { get; private set; }

        private readonly Tweener<FloatTween> focusDistanceTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> apertureTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> focalLengthTweener = new Tweener<FloatTween>();

        [Header("Depth of Field Settings")]
        [SerializeField, UnityEngine.Min(0.1f)] private float defaultFocusDistance = 0.1f;
        [SerializeField, Range(0.05f, 32f)] private float defaultAperture = 1f;
        [SerializeField, Range(1f, 300f)] private float defaultFocalLength = 1f;
        [SerializeField] private KernelSize defaultMaxBlurSize = KernelSize.Medium;

        private UnityEngine.Rendering.PostProcessing.DepthOfField dof;

        public override void SetSpawnParameters(IReadOnlyList<string> parameters, bool asap)
        {
            base.SetSpawnParameters(parameters, asap);
            FocusDistance = parameters?.ElementAtOrDefault(2)?.AsInvariantFloat() ?? defaultFocusDistance;
            Aperture = parameters?.ElementAtOrDefault(3)?.AsInvariantFloat() ?? defaultAperture;
            FocalLength = parameters?.ElementAtOrDefault(4)?.AsInvariantFloat() ?? defaultFocalLength;
            MaxBlurSize = parameters?.ElementAtOrDefault(5)?.ToString() ?? defaultMaxBlurSize.ToString();
        }

        public async UniTask AwaitSpawnAsync(AsyncToken asyncToken = default)
        {
            CompleteTweens();
            var duration = asyncToken.Completed ? 0 : Duration;
            await ChangeDoFAsync(duration, VolumeWeight, FocusDistance, FocalLength, Aperture, MaxBlurSize, asyncToken);
        }

        public async UniTask ChangeDoFAsync(float duration, float volumeWeight, float focusDistance, float focalLength, float aperture, string blursize, AsyncToken asyncToken = default)
        {
            var tasks = new List<UniTask>();
            if (Volume.weight != volumeWeight) tasks.Add(ChangeVolumeWeightAsync(volumeWeight, duration, asyncToken));
            if (dof.focusDistance.value != focusDistance) tasks.Add(ChangeFocusDistanceAsync(focusDistance, duration, asyncToken));
            if (dof.aperture.value != aperture) tasks.Add(ChangeApertureAsync(aperture, duration, asyncToken));
            if (dof.focalLength.value != focalLength) tasks.Add(ChangeFocalLengthAsync(focalLength, duration, asyncToken));
            dof.kernelSize.value = (KernelSize)System.Enum.Parse(typeof(KernelSize), blursize);

            await UniTask.WhenAll(tasks);
        }

        protected override void CompleteTweens()
        {
            if (focusDistanceTweener.Running) focusDistanceTweener.CompleteInstantly();
            if (apertureTweener.Running) apertureTweener.CompleteInstantly();
            if (focalLengthTweener.Running) focalLengthTweener.CompleteInstantly();
            if (volumeWeightTweener.Running) volumeWeightTweener.CompleteInstantly();
        }
        protected override void Awake()
        {
            base.Awake();
            dof = Volume.profile.GetSetting<UnityEngine.Rendering.PostProcessing.DepthOfField>() ?? Volume.profile.AddSettings<UnityEngine.Rendering.PostProcessing.DepthOfField>();
            dof.SetAllOverridesTo(true);
        }

        private async UniTask ChangeFocusDistanceAsync(float focusDistance, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await focusDistanceTweener.RunAsync(new FloatTween(dof.focusDistance.value, focusDistance, duration, x => dof.focusDistance.value = x), asyncToken, dof);
            else dof.focusDistance.value = focusDistance;
        }
        private async UniTask ChangeApertureAsync(float aperture, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await apertureTweener.RunAsync(new FloatTween(dof.aperture.value, aperture, duration, x => dof.aperture.value = x), asyncToken, dof);
            else dof.aperture.value = aperture;
        }
        private async UniTask ChangeFocalLengthAsync(float focalLength, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await focalLengthTweener.RunAsync(new FloatTween(dof.focalLength.value, focalLength, duration, x => dof.focalLength.value = x), asyncToken, dof);
            else dof.focalLength.value = focalLength;
        }

        public override List<ParameterValue> GetParams()
        {
            return new List<ParameterValue>
            {
                { new ParameterValue("Time", () => Duration, v => Duration = (float)v, (i,p) => i.FloatField(p, minValue:0), false)},
                { new ParameterValue("Weight", () => Volume.weight, v => Volume.weight = (float)v, (i,p) => i.FloatSliderField(p, 0f, 1f), false)},
                { new ParameterValue("FocusDistance", () => dof.focusDistance.value, v => dof.focusDistance.value = (float)v, (i,p) => i.FloatField(p, 0.1f), false)},
                { new ParameterValue("Aperture", () => dof.aperture.value, v => dof.aperture.value = (float)v, (i,p) => i.FloatSliderField(p, 0.1f, 32f), false)},
                { new ParameterValue("FocalLength", () => dof.focalLength.value, v => dof.focalLength.value = (float)v, (i,p) => i.FloatSliderField(p, 1f, 300f), false)},
                { new ParameterValue("MaxBlurSize", () => dof.kernelSize.value, v => dof.kernelSize.value = (KernelSize)v, (i,p) => i.EnumField(p), false)},
            };
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(DepthOfField))]
    public class DepthOfFieldEditor : SpawnObjectEditor { }
#endif

}

#endif