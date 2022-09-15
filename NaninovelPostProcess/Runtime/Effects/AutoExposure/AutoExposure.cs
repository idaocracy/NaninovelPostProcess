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
    public class AutoExposure : MonoBehaviour, Spawn.IParameterized, Spawn.IAwaitable, DestroySpawned.IParameterized, DestroySpawned.IAwaitable
    {
        protected Vector2 Filtering { get; private set; }
        protected float Minimum { get; private set; }
        protected float Maximum { get; private set; }
        protected float ExposureCompensation { get; private set; }
        protected string Type { get; private set; }
        protected float SpeedUp { get; private set; }
        protected float SpeedDown { get; private set; }
        protected float VolumeWeight { get; private set; }
        protected float Duration { get; private set; }
        protected float FadeOutDuration { get; private set; }

        private readonly Tweener<FloatTween> volumeWeightTweener = new Tweener<FloatTween>();
        private readonly Tweener<VectorTween> filteringTweener = new Tweener<VectorTween>();
        private readonly Tweener<FloatTween> minimumTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> maximumTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> exposureCompensationTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> speedUpTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> speedDownTweener = new Tweener<FloatTween>();

        [Header("Spawn/Fadein settings")]
        [SerializeField] private float defaultDuration = 0.35f;
        [Header("Volume Settings")]
        [SerializeField] private float defaultVolumeWeight = 1f;

        [Header("Auto Exposure Settings")]
        [SerializeField] private Vector2 defaultFiltering = new Vector2(50f,95f);
        [SerializeField] private float defaultMinimum = 0f;
        [SerializeField] private float defaultMaximum = 0f;
        [SerializeField] private float defaultExposureCompensation = 1f;
        [SerializeField] private string defaultType = "Progressive";
        [SerializeField] private float defaultSpeedUp = 2f;
        [SerializeField] private float defaultSpeedDown = 1f;

        [Header("Despawn/Fadeout settings")]
        [SerializeField] private float defaultFadeOutDuration = 0.35f;

        private PostProcessVolume volume;
        private UnityEngine.Rendering.PostProcessing.AutoExposure autoExposure;

        public virtual void SetSpawnParameters(IReadOnlyList<string> parameters, bool asap)
        {
            Duration = asap ? 0 : Mathf.Abs(parameters?.ElementAtOrDefault(0)?.AsInvariantFloat() ?? defaultDuration);

            VolumeWeight = parameters?.ElementAtOrDefault(1)?.AsInvariantFloat() ?? defaultVolumeWeight;
            Filtering = new Vector2(parameters?.ElementAtOrDefault(2)?.AsInvariantFloat() ?? defaultFiltering.x, parameters?.ElementAtOrDefault(3)?.AsInvariantFloat() ?? defaultFiltering.y) ;
            Minimum = parameters?.ElementAtOrDefault(4)?.AsInvariantFloat() ?? defaultMinimum;
            Maximum = parameters?.ElementAtOrDefault(5)?.AsInvariantFloat() ?? defaultMaximum;
            ExposureCompensation = parameters?.ElementAtOrDefault(6)?.AsInvariantFloat() ?? defaultExposureCompensation;
            Type = parameters?.ElementAtOrDefault(7)?.ToString() ?? defaultType;

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
            if (volume.weight != volumeWeight) tasks.Add(ChangeVolumeWeightAsync(volumeWeight, duration, asyncToken));
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
            if (filteringTweener.Running) filteringTweener.CompleteInstantly();
            if (minimumTweener.Running) minimumTweener.CompleteInstantly();
            if (maximumTweener.Running) maximumTweener.CompleteInstantly();
            if (exposureCompensationTweener.Running) exposureCompensationTweener.CompleteInstantly();
            if (speedUpTweener.Running) speedUpTweener.CompleteInstantly();
            if (speedDownTweener.Running) speedDownTweener.CompleteInstantly();
        }

        private void Awake()
        {
            volume = GetComponent<PostProcessVolume>();
            autoExposure = volume.profile.GetSetting<UnityEngine.Rendering.PostProcessing.AutoExposure>() ?? volume.profile.AddSettings<UnityEngine.Rendering.PostProcessing.AutoExposure>();
            autoExposure.SetAllOverridesTo(true);
            volume.weight = 0f;

            foreach (var item in GetComponentsInChildren<SpriteRenderer>()) item.enabled = false;
        }

        private async UniTask ChangeVolumeWeightAsync(float volumeWeight, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await volumeWeightTweener.RunAsync(new FloatTween(volume.weight, volumeWeight, duration, x => volume.weight = x), asyncToken, volume);
            else volume.weight = volumeWeight;
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
            EditorGUILayout.LabelField("Filtering", GUILayout.Width(190));
            autoExposure.filtering.value = EditorGUILayout.Vector2Field("", autoExposure.filtering.value, GUILayout.Width(220));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Minimum (EV)", GUILayout.Width(190));
            autoExposure.minLuminance.value = EditorGUILayout.Slider(autoExposure.minLuminance.value, -9f, 9f, GUILayout.Width(220));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Maximum (EV)", GUILayout.Width(190));
            autoExposure.maxLuminance.value = EditorGUILayout.Slider(autoExposure.maxLuminance.value, -9f, 9f, GUILayout.Width(220));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            autoExposure.keyValue.value = EditorGUILayout.FloatField("Exposure Compensation", autoExposure.keyValue.value, GUILayout.Width(413));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Type", GUILayout.Width(190));
            string[] typeArray = new string[] { "Progressive", "Fixed" };
            var typeIndex = Array.IndexOf(typeArray, autoExposure.eyeAdaptation.value.ToString());
            typeIndex = EditorGUILayout.Popup(typeIndex, typeArray, GUILayout.Width(220));
            autoExposure.eyeAdaptation.value = (EyeAdaptation)typeIndex;
            GUILayout.EndHorizontal();

            if(autoExposure.eyeAdaptation.value.ToString() == "Progressive") { 
                GUILayout.BeginHorizontal();
                autoExposure.speedUp.value = EditorGUILayout.FloatField("Speed Up", autoExposure.speedUp.value, GUILayout.Width(413));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                autoExposure.speedDown.value = EditorGUILayout.FloatField("Speed Down", autoExposure.speedDown.value, GUILayout.Width(413));
                GUILayout.EndHorizontal();
            }

            return Duration + "," + GetString();
        }

        public string GetString()
        {
            return volume.weight + "," + autoExposure.filtering.value.x + "," + autoExposure.filtering.value.y + "," + autoExposure.minLuminance.value + "," + autoExposure.maxLuminance.value + "," +
                  autoExposure.keyValue.value + "," + autoExposure.eyeAdaptation.value + (autoExposure.eyeAdaptation.value.ToString() == "Progressive" ? "," + autoExposure.speedUp.value + "," + autoExposure.speedDown.value : String.Empty);
        }

#endif
    }


    #if UNITY_EDITOR

    [CustomEditor(typeof(AutoExposure))]
    public class CopyFXAutoExposure : Editor
    {
        private AutoExposure targetObject;
        private UnityEngine.Rendering.PostProcessing.AutoExposure autoExposure;
        private PostProcessVolume volume;
        public bool LogResult;

        private void Awake()
        {
            targetObject = (AutoExposure)target;
            volume = targetObject.gameObject.GetComponent<PostProcessVolume>();
            autoExposure = volume.profile.GetSetting<UnityEngine.Rendering.PostProcessing.AutoExposure>();

        }

        public override void OnInspectorGUI()
        {
            base.DrawDefaultInspector();

            GUILayout.Space(20f);
            if (GUILayout.Button("Copy command and params (@)", GUILayout.Height(50)))
            {
                if (autoExposure != null) GUIUtility.systemCopyBuffer = "@spawn " + targetObject.gameObject.name + " params:(time)," + targetObject.GetString();
                if (LogResult) Debug.Log(GUIUtility.systemCopyBuffer);
            }

            GUILayout.Space(20f);
            if (GUILayout.Button("Copy command and params ([])", GUILayout.Height(50)))
            {
                if (autoExposure != null) GUIUtility.systemCopyBuffer = "[spawn " + targetObject.gameObject.name + " params:(time)," + targetObject.GetString() + "]";
                if (LogResult) Debug.Log(GUIUtility.systemCopyBuffer);
            }

            GUILayout.Space(20f);
            if (GUILayout.Button("Copy params", GUILayout.Height(50)))
            {
                if (autoExposure != null) GUIUtility.systemCopyBuffer = "(time)," + targetObject.GetString();
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