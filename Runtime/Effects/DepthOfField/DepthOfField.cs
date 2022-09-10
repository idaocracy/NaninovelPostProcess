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
    public class DepthOfField : MonoBehaviour, Spawn.IParameterized, Spawn.IAwaitable, DestroySpawned.IParameterized, DestroySpawned.IAwaitable
    {
        protected float Duration { get; private set; }
        protected float VolumeWeight { get; private set; }
        protected float FocusDistance { get; private set; }
        protected float Aperture { get; private set; }
        protected float FocalLength { get; private set; }
        protected string MaxBlurSize { get; private set; }

        protected float FadeOutDuration { get; private set; }

        public bool logResult { get; set; }

        private readonly Tweener<FloatTween> volumeWeightTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> focusDistanceTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> apertureTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> focalLengthTweener = new Tweener<FloatTween>();

        [SerializeField] private float defaultDuration = 0.35f;
        [SerializeField] private float defaultVolumeWeight = 1f;
        [SerializeField] private float defaultFocusDistance = 0.1f;
        [SerializeField] private float defaultAperture = 1f;
        [SerializeField] private float defaultFocalLength = 1f;
        [SerializeField] private string defaultMaxBlurSize = "Medium";

        [SerializeField] private float defaultFadeOutDuration = 0.35f;

        private PostProcessVolume volume;
        private UnityEngine.Rendering.PostProcessing.DepthOfField dof;

        public virtual void SetSpawnParameters(IReadOnlyList<string> parameters, bool asap)
        {
            Duration = asap ? 0 : Mathf.Abs(parameters?.ElementAtOrDefault(0)?.AsInvariantFloat() ?? defaultDuration);

            VolumeWeight = parameters?.ElementAtOrDefault(1)?.AsInvariantFloat() ?? defaultVolumeWeight;
            FocusDistance = parameters?.ElementAtOrDefault(2)?.AsInvariantFloat() ?? defaultFocusDistance;
            Aperture = parameters?.ElementAtOrDefault(3)?.AsInvariantFloat() ?? defaultAperture;
            FocalLength = parameters?.ElementAtOrDefault(4)?.AsInvariantFloat() ?? defaultFocalLength;
            MaxBlurSize = parameters?.ElementAtOrDefault(5)?.ToString() ?? defaultMaxBlurSize;

        }

        public async UniTask AwaitSpawnAsync(AsyncToken asyncToken = default)
        {
            if (focusDistanceTweener.Running) focusDistanceTweener.CompleteInstantly();
            if (apertureTweener.Running) apertureTweener.CompleteInstantly();
            if (focalLengthTweener.Running) focalLengthTweener.CompleteInstantly();

            if (volumeWeightTweener.Running) volumeWeightTweener.CompleteInstantly();

            var duration = asyncToken.Completed ? 0 : Duration;
            await ChangeDoFAsync(duration, VolumeWeight, FocusDistance, FocalLength, Aperture, MaxBlurSize, asyncToken);
        }

        public async UniTask ChangeDoFAsync(float duration, float volumeWeight, float focusDistance, float focalLength, float aperture, string blursize, AsyncToken asyncToken = default)
        {
            var tasks = new List<UniTask>();
            if (volume.weight != volumeWeight) tasks.Add(ChangeVolumeWeightAsync(volumeWeight, duration, asyncToken));
            if (dof.focusDistance.value != focusDistance) tasks.Add(ChangeFocusDistanceAsync(focusDistance, duration, asyncToken));
            if (dof.aperture.value != aperture) tasks.Add(ChangeApertureAsync(aperture, duration, asyncToken));
            if (dof.focalLength.value != focalLength) tasks.Add(ChangeFocalLengthAsync(focalLength, duration, asyncToken));
            dof.kernelSize.value = (KernelSize)System.Enum.Parse(typeof(KernelSize), blursize);

            await UniTask.WhenAll(tasks);
        }

        public void SetDestroyParameters(IReadOnlyList<string> parameters)
        {
            FadeOutDuration = parameters?.ElementAtOrDefault(0)?.AsInvariantFloat() ?? defaultFadeOutDuration;
        }

        public async UniTask AwaitDestroyAsync(AsyncToken asyncToken = default)
        {
            if (focusDistanceTweener.Running) focusDistanceTweener.CompleteInstantly();
            if (apertureTweener.Running) apertureTweener.CompleteInstantly();
            if (focalLengthTweener.Running) focalLengthTweener.CompleteInstantly();
            if (volumeWeightTweener.Running) volumeWeightTweener.CompleteInstantly();

            var duration = asyncToken.Completed ? 0 : FadeOutDuration;
            await ChangeVolumeWeightAsync(0f, duration, asyncToken);
        }

        private void OnDestroy()
        {
            volume.profile.RemoveSettings<UnityEngine.Rendering.PostProcessing.DepthOfField>();
        }

        private void Awake()
        {
            volume = GetComponent<PostProcessVolume>();
            dof = volume.profile.GetSetting<UnityEngine.Rendering.PostProcessing.DepthOfField>() ?? volume.profile.AddSettings<UnityEngine.Rendering.PostProcessing.DepthOfField>();
            dof.SetAllOverridesTo(true);
            volume.weight = 0f;
        }


        private async UniTask ChangeVolumeWeightAsync(float volumeWeight, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await volumeWeightTweener.RunAsync(new FloatTween(volume.weight, volumeWeight, duration, x => volume.weight = x), asyncToken, volume);
            else volume.weight = volumeWeight;
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
            dof.focusDistance.value = EditorGUILayout.FloatField("Focus Distance", dof.focusDistance.value, GUILayout.Width(413));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Aperture", GUILayout.Width(190));
            dof.aperture.value = EditorGUILayout.Slider(dof.aperture.value, 0.1f, 32f, GUILayout.Width(220));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Focal Length", GUILayout.Width(190));
            dof.focalLength.value = EditorGUILayout.Slider(dof.focalLength.value, 1, 300, GUILayout.Width(220));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Max Blur Size", GUILayout.Width(190));
            string[] kernelSizeArray = new string[] { "Low", "Medium", "Large", "Very Large" };
            var sizeIndex = Array.IndexOf(kernelSizeArray, dof.kernelSize.value.ToString());
            sizeIndex = EditorGUILayout.Popup(sizeIndex, kernelSizeArray, GUILayout.Height(20), GUILayout.Width(220));
            dof.kernelSize.value = (KernelSize)sizeIndex;
            GUILayout.EndHorizontal();

            return Duration + "," + volume.weight + "," + dof.focusDistance.value + "," + dof.aperture.value + "," + dof.focalLength.value + "," + dof.kernelSize.value;
        }
    }


#if UNITY_EDITOR

    [CustomEditor(typeof(DepthOfField))]
    public class CopyFXDoF : Editor
    {
        private DepthOfField targetObject;
        private UnityEngine.Rendering.PostProcessing.DepthOfField dof;
        private PostProcessVolume volume;
        public bool logResult;

        private void Awake()
        {
            targetObject = (DepthOfField)target;
            volume = targetObject.gameObject.GetComponent<PostProcessVolume>();
            dof = volume.profile.GetSetting<UnityEngine.Rendering.PostProcessing.DepthOfField>();
            logResult = targetObject.logResult;
        }

        public override void OnInspectorGUI()
        {
            base.DrawDefaultInspector();

            GUILayout.Space(20f);
            if (GUILayout.Button("Copy command and params (@)", GUILayout.Height(50)))
            {
                if (dof != null) GUIUtility.systemCopyBuffer = "@spawn " + targetObject.gameObject.name + " params:" + CreateString();
                if (logResult) Debug.Log(GUIUtility.systemCopyBuffer);
            }

            GUILayout.Space(20f);

            if (GUILayout.Button("Copy command and params ([])", GUILayout.Height(50)))
            {
                if (dof != null) GUIUtility.systemCopyBuffer = "[spawn " + targetObject.gameObject.name + " params:" + CreateString() + "]";
                if (logResult) Debug.Log(GUIUtility.systemCopyBuffer);
            }

            GUILayout.Space(20f);

            if (GUILayout.Button("Copy params", GUILayout.Height(50)))
            {
                if (dof != null) CreateString();
                if (logResult) Debug.Log(GUIUtility.systemCopyBuffer);
            }

            GUILayout.Space(20f);
            if (GUILayout.Toggle(logResult, "Log Results")) logResult = true;
            else logResult = false;
        }

        private string CreateString() => "(time)," + volume.weight + "," + dof.focusDistance.value + "," + dof.aperture.value + "," + dof.focalLength.value + "," + dof.kernelSize.value;
    }

#endif

}

#endif