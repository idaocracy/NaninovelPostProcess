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
    public class LensDistortion : MonoBehaviour, Spawn.IParameterized, Spawn.IAwaitable, DestroySpawned.IParameterized, DestroySpawned.IAwaitable
    {
        protected float Duration { get; private set; }
        protected float VolumeWeight { get; private set; }
        protected float Intensity { get; private set; }
        protected float XMultiplier { get; private set; }
        protected float YMultiplier { get; private set; }
        protected float CenterX { get; private set; }
        protected float CenterY { get; private set; }
        protected float Scale { get; private set; }

        protected float FadeOutDuration { get; private set; }

        private readonly Tweener<FloatTween> volumeWeightTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> intensityTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> xMultiplierTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> yMultiplierTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> centerXTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> centerYTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> scaleTweener = new Tweener<FloatTween>();

        [Header("Spawn/Fadein Settings")]
        [SerializeField] private float defaultDuration = 0.35f;
        [Header("Volume Settings")]
        [SerializeField] private float defaultVolumeWeight = 1f;
        [Header("Lens Distortion Settings")]
        [SerializeField] private float defaultIntensity = 0f;
        [SerializeField] private float defaultXMultiplier = 1f;
        [SerializeField] private float defaultYMultiplier = 1f;
        [SerializeField] private float defaultCenterX = 0f;
        [SerializeField] private float defaultCenterY = 0f;
        [SerializeField] private float defaultScale = 1f;

        [Header("Despawn/Fadeout Settings")]
        [SerializeField] private float defaultFadeOutDuration = 0.35f;

        private PostProcessVolume volume;
        private UnityEngine.Rendering.PostProcessing.LensDistortion lensDistortion;

        public virtual void SetSpawnParameters(IReadOnlyList<string> parameters, bool asap)
        {
            Duration = asap ? 0 : Mathf.Abs(parameters?.ElementAtOrDefault(0)?.AsInvariantFloat() ?? defaultDuration);

            VolumeWeight = parameters?.ElementAtOrDefault(1)?.AsInvariantFloat() ?? defaultVolumeWeight;
            Intensity = parameters?.ElementAtOrDefault(2)?.AsInvariantFloat() ?? defaultIntensity;
            XMultiplier = parameters?.ElementAtOrDefault(3)?.AsInvariantFloat() ?? defaultYMultiplier;
            YMultiplier = parameters?.ElementAtOrDefault(4)?.AsInvariantFloat() ?? defaultXMultiplier;
            CenterX = parameters?.ElementAtOrDefault(5)?.AsInvariantFloat() ?? defaultCenterX;
            CenterY = parameters?.ElementAtOrDefault(6)?.AsInvariantFloat() ?? defaultCenterY;
            Scale = parameters?.ElementAtOrDefault(7)?.AsInvariantFloat() ?? defaultScale;
        }

        public async UniTask AwaitSpawnAsync(AsyncToken asyncToken = default)
        {
            if (intensityTweener.Running) intensityTweener.CompleteInstantly();
            if (xMultiplierTweener.Running) xMultiplierTweener.CompleteInstantly();
            if (yMultiplierTweener.Running) yMultiplierTweener.CompleteInstantly();
            if (centerXTweener.Running) centerXTweener.CompleteInstantly();
            if (centerYTweener.Running) centerYTweener.CompleteInstantly();
            if (scaleTweener.Running) scaleTweener.CompleteInstantly();

            if (volumeWeightTweener.Running) volumeWeightTweener.CompleteInstantly();

            var duration = asyncToken.Completed ? 0 : Duration;
            await ChangeLensDistortionAsync(duration, VolumeWeight, Intensity, XMultiplier, YMultiplier, CenterX, CenterY, Scale, asyncToken);
        }

        public async UniTask ChangeLensDistortionAsync(float duration, float volumeWeight, float intensity, float xMultiplier, float yMultiplier, float centerX, float centerY, float scale, AsyncToken asyncToken = default)
        {
            var tasks = new List<UniTask>();
            if (volume.weight != volumeWeight) tasks.Add(ChangeVolumeWeightAsync(volumeWeight, duration, asyncToken));
            if (lensDistortion.intensity.value != intensity) tasks.Add(ChangeIntensityAsync(intensity, duration, asyncToken));
            if (lensDistortion.intensityX.value != xMultiplier) tasks.Add(ChangeXMultiplierAsync(xMultiplier, duration, asyncToken));
            if (lensDistortion.intensityY.value != yMultiplier) tasks.Add(ChangeYMultiplierAsync(yMultiplier, duration, asyncToken));
            if (lensDistortion.centerX.value != centerX) tasks.Add(ChangeCenterXAsync(centerX, duration, asyncToken));
            if (lensDistortion.centerY.value != centerY) tasks.Add(ChangeCenterYAsync(centerX, duration, asyncToken));
            if (lensDistortion.scale.value != centerY) tasks.Add(ChangeScaleAsync(scale, duration, asyncToken));

            await UniTask.WhenAll(tasks);
        }

        public void SetDestroyParameters(IReadOnlyList<string> parameters)
        {
            FadeOutDuration = parameters?.ElementAtOrDefault(0)?.AsInvariantFloat() ?? defaultFadeOutDuration;
        }

        public async UniTask AwaitDestroyAsync(AsyncToken asyncToken = default)
        {
            if (intensityTweener.Running) intensityTweener.CompleteInstantly();
            if (xMultiplierTweener.Running) xMultiplierTweener.CompleteInstantly();
            if (yMultiplierTweener.Running) yMultiplierTweener.CompleteInstantly();
            if (centerXTweener.Running) centerXTweener.CompleteInstantly();
            if (centerYTweener.Running) centerYTweener.CompleteInstantly();
            if (scaleTweener.Running) scaleTweener.CompleteInstantly();
            if (volumeWeightTweener.Running) volumeWeightTweener.CompleteInstantly();

            var duration = asyncToken.Completed ? 0 : FadeOutDuration;
            await ChangeVolumeWeightAsync(0f, duration, asyncToken);
        }

        private void OnDestroy()
        {
            volume.profile.RemoveSettings<UnityEngine.Rendering.PostProcessing.LensDistortion>();
        }

        private void Awake()
        {
            volume = GetComponent<PostProcessVolume>();
            lensDistortion = volume.profile.GetSetting<UnityEngine.Rendering.PostProcessing.LensDistortion>() ?? volume.profile.AddSettings<UnityEngine.Rendering.PostProcessing.LensDistortion>();
            lensDistortion.SetAllOverridesTo(true);
            volume.weight = 0f;
        }


        private async UniTask ChangeVolumeWeightAsync(float volumeWeight, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await volumeWeightTweener.RunAsync(new FloatTween(volume.weight, volumeWeight, duration, x => volume.weight = x), asyncToken, volume);
            else volume.weight = volumeWeight;
        }

        private async UniTask ChangeIntensityAsync(float intensity, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await intensityTweener.RunAsync(new FloatTween(lensDistortion.intensity.value, intensity, duration, x => lensDistortion.intensity.value = x), asyncToken, lensDistortion);
            else lensDistortion.intensity.value = intensity;
        }
        private async UniTask ChangeXMultiplierAsync(float xMultiplier, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await xMultiplierTweener.RunAsync(new FloatTween(lensDistortion.intensityX.value, xMultiplier, duration, x => lensDistortion.intensityX.value = x), asyncToken, lensDistortion);
            else lensDistortion.intensityX.value = xMultiplier;
        }    
        private async UniTask ChangeYMultiplierAsync(float yMultiplier, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await yMultiplierTweener.RunAsync(new FloatTween(lensDistortion.intensityY.value, yMultiplier, duration, x => lensDistortion.intensityY.value = x), asyncToken, lensDistortion);
            else lensDistortion.intensityY.value = yMultiplier;
        }    
    
        private async UniTask ChangeCenterXAsync(float centerX, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await centerXTweener.RunAsync(new FloatTween(lensDistortion.centerX.value, centerX, duration, x => lensDistortion.centerX.value = x), asyncToken, lensDistortion);
            else lensDistortion.centerX.value = centerX;
        }   
        private async UniTask ChangeCenterYAsync(float centerY, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await centerYTweener.RunAsync(new FloatTween(lensDistortion.centerY.value, centerY, duration, x => lensDistortion.centerY.value = x), asyncToken, lensDistortion);
            else lensDistortion.centerY.value = centerY;
        }    
        private async UniTask ChangeScaleAsync(float scale, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await scaleTweener.RunAsync(new FloatTween(lensDistortion.scale.value, scale, duration, x => lensDistortion.scale.value = x), asyncToken, lensDistortion);
            else lensDistortion.scale.value = scale;
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
            EditorGUILayout.LabelField("Intensity", GUILayout.Width(190));
            lensDistortion.intensity.value = EditorGUILayout.Slider(lensDistortion.intensity.value, -100f, 100f, GUILayout.Width(220));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("X Multiplier", GUILayout.Width(190));
            lensDistortion.intensityX.value = EditorGUILayout.Slider(lensDistortion.intensityX.value, 0, 1f, GUILayout.Width(220));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Y Multiplier", GUILayout.Width(190));
            lensDistortion.intensityY.value = EditorGUILayout.Slider(lensDistortion.intensityY.value, 0f, 1f, GUILayout.Width(220));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Center X", GUILayout.Width(190));
            lensDistortion.centerX.value = EditorGUILayout.Slider(lensDistortion.centerX.value, -1f, 1f, GUILayout.Width(220));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Center Y", GUILayout.Width(190));
            lensDistortion.centerY.value = EditorGUILayout.Slider(lensDistortion.centerY.value, -1f, 1f, GUILayout.Width(220));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Scale", GUILayout.Width(190));
            lensDistortion.scale.value = EditorGUILayout.Slider(lensDistortion.scale.value, 0.01f, 5f, GUILayout.Width(220));
            GUILayout.EndHorizontal();

            return Duration + "," + GetString();
        }

        public string GetString()
        {
            return volume.weight + "," + lensDistortion.intensity.value + "," + lensDistortion.intensityX.value + "," + lensDistortion.intensityY.value + "," + lensDistortion.centerX.value + "," + lensDistortion.centerY.value + "," + lensDistortion.scale.value;
        }

#endif
    }


#if UNITY_EDITOR

    [CustomEditor(typeof(LensDistortion))]
    public class CopyFXLensDistortion : Editor
    {
        private LensDistortion targetObject;
        private UnityEngine.Rendering.PostProcessing.LensDistortion lensDistortion;
        private PostProcessVolume volume;
        public bool LogResult;

        private void Awake()
        {
            targetObject = (LensDistortion)target;
            volume = targetObject.gameObject.GetComponent<PostProcessVolume>();
            lensDistortion = volume.profile.GetSetting<UnityEngine.Rendering.PostProcessing.LensDistortion>();

        }

        public override void OnInspectorGUI()
        {
            base.DrawDefaultInspector();

            GUILayout.Space(20f);
            if (GUILayout.Button("Copy command and params (@)", GUILayout.Height(50)))
            {
                if (lensDistortion != null) GUIUtility.systemCopyBuffer = "@spawn " + targetObject.gameObject.name + " params:(time)" + targetObject.GetString();
                if (LogResult) Debug.Log(GUIUtility.systemCopyBuffer);
            }

            GUILayout.Space(20f);

            if (GUILayout.Button("Copy command and params ([])", GUILayout.Height(50)))
            {
                if (lensDistortion != null) GUIUtility.systemCopyBuffer = "[spawn " + targetObject.gameObject.name + " params:(time)" + targetObject.GetString() + "]";
                if (LogResult) Debug.Log(GUIUtility.systemCopyBuffer);
            }

            GUILayout.Space(20f);

            if (GUILayout.Button("Copy params", GUILayout.Height(50)))
            {
                if (lensDistortion != null) GUIUtility.systemCopyBuffer = "(time)," + targetObject.GetString();
                if (LogResult) Debug.Log(GUIUtility.systemCopyBuffer);
            }

            GUILayout.Space(20f);
            if (GUILayout.Toggle(LogResult, "Log Results")) LogResult = true;
            else LogResult = false;
        }
    }

#endif

}

#endif