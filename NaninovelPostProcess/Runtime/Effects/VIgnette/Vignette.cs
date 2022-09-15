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
    public class Vignette : MonoBehaviour, Spawn.IParameterized, Spawn.IAwaitable, DestroySpawned.IParameterized, DestroySpawned.IAwaitable
    {
        protected float Duration { get; private set; }
        protected float VolumeWeight { get; private set; }
        protected string Mode { get; private set; }
        protected Color Color { get; private set; }

        //Classic parameters
        protected Vector2 Center { get; private set; }
        protected float Intensity { get; private set; }
        protected float Smoothness { get; private set; }
        protected float Roundness { get; private set; }
        protected bool Rounded { get; private set; }

        //Mask parameters
        protected string Mask { get; private set; }
        protected float Opacity { get; private set; }
        protected float FadeOutDuration { get; private set; }

        private readonly Tweener<FloatTween> volumeWeightTweener = new Tweener<FloatTween>();
        private readonly Tweener<ColorTween> colorTweener = new Tweener<ColorTween>();
        private readonly Tweener<VectorTween> centerTweener = new Tweener<VectorTween>();
        private readonly Tweener<FloatTween> intensityTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> smoothnessTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> roundnessTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> opacityTweener = new Tweener<FloatTween>();

        [Header("Spawn/Fadein Settings")]
        [SerializeField] private float defaultDuration = 0.35f;
        [Header("Volume Settings")]
        [SerializeField] private float defaultVolumeWeight = 1f;

        [Header("Vignette Settings")]
        [SerializeField] private string defaultMode = "Masked";
        [SerializeField] private Color defaultColor = Color.black;

        //Classic parameters
        [SerializeField] private Vector2 defaultCenter = new Vector2(0.5f,0.5f);
        [SerializeField] private float defaultIntensity = 0f;
        [SerializeField] private float defaultSmoothness = 0.2f;
        [SerializeField] private float defaultRoundness = 1f;
        [SerializeField] private bool defaultRounded = false;

        //Masked parameters
        [SerializeField] private string defaultMask = string.Empty;
        [SerializeField] private List<Texture> maskTextures = new List<Texture>();
        [SerializeField] private float defaultOpacity = 1f;

        [Header("Despawn/Fadeout Settings")]
        [SerializeField] private float defaultFadeOutDuration = 0.35f;

        private PostProcessVolume volume;
        private UnityEngine.Rendering.PostProcessing.Vignette vignette;

        private List<string> maskTextureIds = new List<string>();

        public virtual void SetSpawnParameters(IReadOnlyList<string> parameters, bool asap)
        {
            Duration = asap ? 0 : Mathf.Abs(parameters?.ElementAtOrDefault(0)?.AsInvariantFloat() ?? defaultDuration);

            VolumeWeight = parameters?.ElementAtOrDefault(1)?.AsInvariantFloat() ?? defaultVolumeWeight;
            Mode = parameters?.ElementAtOrDefault(2)?.ToString() ?? defaultMode;
            Color = ColorUtility.TryParseHtmlString(parameters?.ElementAtOrDefault(3), out var color) ? color: defaultColor;

            //Classic parameters
            Center = new Vector2(parameters?.ElementAtOrDefault(4).AsInvariantFloat() ?? defaultCenter.x, parameters?.ElementAtOrDefault(5).AsInvariantFloat() ?? defaultCenter.y);
            Intensity = parameters?.ElementAtOrDefault(6)?.AsInvariantFloat() ?? defaultIntensity;
            Smoothness = parameters?.ElementAtOrDefault(7)?.AsInvariantFloat() ?? defaultSmoothness;
            Roundness = parameters?.ElementAtOrDefault(8)?.AsInvariantFloat() ?? defaultRoundness;
            Rounded = bool.TryParse(parameters?.ElementAtOrDefault(9)?.ToString(), out var rounded) ? rounded : defaultRounded;

            //Mask parameters
            Mask = parameters?.ElementAtOrDefault(4) ?? defaultMask;
            Opacity = parameters?.ElementAtOrDefault(6)?.AsInvariantFloat() ?? defaultOpacity;
        }

        public async UniTask AwaitSpawnAsync(AsyncToken asyncToken = default)
        {
            CompleteTweens();
            var duration = asyncToken.Completed ? 0 : Duration;
            if (Mode == "Classic") await ChangeVignetteClassicAsync(duration, VolumeWeight, Mode, Color, Center, Intensity, Smoothness, Roundness, Rounded, asyncToken);
            else if (Mode == "Masked") await ChangeVignetteMaskedAsync(duration, VolumeWeight, Mode, Color, Mask, Opacity);
        }

        public async UniTask ChangeVignetteClassicAsync(float duration, float volumeWeight, string mode, Color color, Vector2 center, float intensity, float smoothness, float roundness, bool rounded, AsyncToken asyncToken = default)
        {
            var tasks = new List<UniTask>();
            if (volume.weight != volumeWeight) tasks.Add(ChangeVolumeWeightAsync(volumeWeight, duration, asyncToken));
            vignette.mode.value = (VignetteMode)System.Enum.Parse(typeof(VignetteMode), mode);
            if (vignette.color.value != color) tasks.Add(ChangeColorAsync(color, duration, asyncToken));
            if (vignette.center.value != center) tasks.Add(ChangeCenterAsync(center, duration, asyncToken));
            if (vignette.intensity.value != intensity) tasks.Add(ChangeIntensityAsync(intensity, duration, asyncToken));
            if (vignette.smoothness.value != intensity) tasks.Add(ChangeSmoothnessAsync(smoothness, duration, asyncToken));
            if (vignette.roundness.value != intensity) tasks.Add(ChangeRoundnessAsync(roundness, duration, asyncToken));
            vignette.rounded.value = rounded;

            await UniTask.WhenAll(tasks);
        }

        public async UniTask ChangeVignetteMaskedAsync(float duration, float volumeWeight, string mode, Color color, string mask, float opacity, AsyncToken asyncToken = default)
        {
            var tasks = new List<UniTask>();
            if (volume.weight != volumeWeight) tasks.Add(ChangeVolumeWeightAsync(volumeWeight, duration, asyncToken));
            vignette.mode.value = (VignetteMode)System.Enum.Parse(typeof(VignetteMode), mode);
            if (vignette.color.value != color) tasks.Add(ChangeColorAsync(color, duration, asyncToken));
            if (vignette.mask.value != null && vignette.mask.value.name != mask) ChangeTexture(mask);
            if (vignette.opacity.value != opacity) tasks.Add(ChangeOpacityAsync(opacity, duration, asyncToken));

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
            if (colorTweener.Running) colorTweener.CompleteInstantly();
            if (centerTweener.Running) centerTweener.CompleteInstantly();
            if (intensityTweener.Running) intensityTweener.CompleteInstantly();
            if (smoothnessTweener.Running) smoothnessTweener.CompleteInstantly();
            if (roundnessTweener.Running) roundnessTweener.CompleteInstantly();
            if (opacityTweener.Running) opacityTweener.CompleteInstantly();
            if (volumeWeightTweener.Running) volumeWeightTweener.CompleteInstantly();
        }

        private void Awake()
        {
            volume = GetComponent<PostProcessVolume>();
            vignette = volume.profile.GetSetting<UnityEngine.Rendering.PostProcessing.Vignette>() ?? volume.profile.AddSettings<UnityEngine.Rendering.PostProcessing.Vignette>();
            vignette.SetAllOverridesTo(true);
            volume.weight = 0f;

            maskTextureIds = maskTextures.Select(s => s.name).ToList();
            maskTextureIds.Insert(0, "None");
        }
        private async UniTask ChangeVolumeWeightAsync(float volumeWeight, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await volumeWeightTweener.RunAsync(new FloatTween(volume.weight, volumeWeight, duration, x => volume.weight = x), asyncToken, volume);
            else volume.weight = volumeWeight;
        }

        private async UniTask ChangeColorAsync(Color color, float duration, AsyncToken asyncToken = default)
        {

            if (duration > 0) await colorTweener.RunAsync(new ColorTween(vignette.color.value, color, ColorTweenMode.All, duration, x => vignette.color.value = x), asyncToken, vignette);
            else vignette.color.value = color;
        }
        private async UniTask ChangeCenterAsync(Vector2 center, float duration, AsyncToken asyncToken = default)
        {

            if (duration > 0) await centerTweener.RunAsync(new VectorTween(vignette.center.value, center, duration, x => vignette.center.value = x), asyncToken, vignette);
            else vignette.center.value = center;
        }

        private async UniTask ChangeIntensityAsync(float intensity, float duration, AsyncToken asyncToken = default)
        {

            if (duration > 0) await intensityTweener.RunAsync(new FloatTween(vignette.intensity.value, intensity, duration, x => vignette.intensity.value = x), asyncToken, vignette);
            else vignette.intensity.value = intensity;
        }
        private async UniTask ChangeSmoothnessAsync(float smoothness, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await smoothnessTweener.RunAsync(new FloatTween(vignette.smoothness.value, smoothness, duration, x => vignette.smoothness.value = x), asyncToken, vignette);
            else vignette.smoothness.value = smoothness;
        }
        private async UniTask ChangeRoundnessAsync(float roundness, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await roundnessTweener.RunAsync(new FloatTween(vignette.roundness.value, roundness, duration, x => vignette.roundness.value = x), asyncToken, vignette);
            else vignette.roundness.value = roundness;
        }

        private async UniTask ChangeOpacityAsync(float opacity, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await opacityTweener.RunAsync(new FloatTween(vignette.opacity.value, opacity, duration, x => vignette.opacity.value = x), asyncToken, vignette);
            else vignette.opacity.value = opacity;
        }

        private void ChangeTexture(string imageId)
        {
            if (imageId == "None" || String.IsNullOrEmpty(imageId)) vignette.mask.value = null;
            else
            {
                foreach (var img in maskTextures)
                {
                    if (img != null && img.name == imageId)
                    {
                        vignette.mask.value = img;
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
            EditorGUILayout.LabelField("Mode", GUILayout.Width(190));
            string[] kernelSizeArray = new string[] { "Classic", "Masked" };
            var sizeIndex = Array.IndexOf(kernelSizeArray, vignette.mode.value.ToString());
            sizeIndex = EditorGUILayout.Popup(sizeIndex, kernelSizeArray, GUILayout.Width(220));
            vignette.mode.value = (VignetteMode)sizeIndex;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Color", GUILayout.Width(190));
            vignette.color.value = EditorGUILayout.ColorField(vignette.color.value, GUILayout.Height(20), GUILayout.Width(180));
            GUILayout.EndHorizontal();


            if(vignette.mode.value.ToString() == "Classic") { 

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Center", GUILayout.Width(190));
                vignette.center.value = EditorGUILayout.Vector2Field("",vignette.center.value, GUILayout.Height(20), GUILayout.Width(180));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Intensity", GUILayout.Width(190));
                vignette.intensity.value = EditorGUILayout.Slider(vignette.intensity.value, 0f, 1f, GUILayout.Width(220));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Smoothnes", GUILayout.Width(190));
                vignette.smoothness.value = EditorGUILayout.Slider(vignette.smoothness.value, 0.01f, 1f, GUILayout.Width(220));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Roundness", GUILayout.Width(190));
                vignette.roundness.value = EditorGUILayout.Slider(vignette.roundness.value, 0f, 1f, GUILayout.Width(220));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Rounded", GUILayout.Width(190));
                string[] options = new string[] { "True", "False" };
                var roundedIndex = Array.IndexOf(options, vignette.rounded.value.ToString());
                roundedIndex = EditorGUILayout.Popup(roundedIndex, options, GUILayout.Height(20), GUILayout.Width(220));
                vignette.rounded.value = bool.Parse(options[roundedIndex]);
                GUILayout.EndHorizontal();

            }
            else
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Mask", GUILayout.Width(190));
                string[] maskTexturesArray = maskTextureIds.ToArray();
                var maskIndex = Array.IndexOf(maskTexturesArray, vignette.mask.value?.name ?? "None");
                maskIndex = EditorGUILayout.Popup(maskIndex, maskTexturesArray, GUILayout.Height(20), GUILayout.Width(220));
                vignette.mask.value = maskTextures.FirstOrDefault(s => s.name == maskTextureIds[maskIndex]) ?? null;
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Opacity", GUILayout.Width(190));
                vignette.opacity.value = EditorGUILayout.Slider(vignette.opacity.value, 0f, 1f, GUILayout.Width(220));
                GUILayout.EndHorizontal();
            }

            return Duration + "," + GetString();

        }

        public string GetString()
        {

            if (vignette.mode.value.ToString() == "Classic")
            {
                return volume.weight + "," + vignette.mode.value + "," + "#" + ColorUtility.ToHtmlStringRGBA(vignette.color.value) + "," + vignette.center.value.x + "," + vignette.center.value.y + "," + vignette.intensity.value + "," +
                            vignette.smoothness.value + "," + vignette.roundness.value + "," + vignette.rounded.value.ToString().ToLower();
            }
            else
            {
                return volume.weight + "," + vignette.mode.value + "," + "#" + ColorUtility.ToHtmlStringRGBA(vignette.color.value) + "," + (vignette.mask.value != null ? vignette.mask.value.name : string.Empty) + "," + vignette.opacity.value;
            }
            
        }
#endif
    }


#if UNITY_EDITOR

    [CustomEditor(typeof(Vignette))]
    public class CopyFXVignette : Editor
    {
        private Vignette targetObject;
        private UnityEngine.Rendering.PostProcessing.Vignette vignette;
        private PostProcessVolume volume;
        public bool LogResult;

        private void Awake()
        {
            targetObject = (Vignette)target;
            volume = targetObject.gameObject.GetComponent<PostProcessVolume>();
            vignette = volume.profile.GetSetting<UnityEngine.Rendering.PostProcessing.Vignette>();

        }

        public override void OnInspectorGUI()
        {
            base.DrawDefaultInspector();

            GUILayout.Space(20f);
            if (GUILayout.Button("Copy command and params (@)", GUILayout.Height(50)))
            {
                if (vignette != null) GUIUtility.systemCopyBuffer = "@spawn " + targetObject.gameObject.name + " params:(time)," + targetObject.GetString();
                if (LogResult) Debug.Log(GUIUtility.systemCopyBuffer);
            }

            GUILayout.Space(20f);

            if (GUILayout.Button("Copy command and params ([])", GUILayout.Height(50)))
            {
                if (vignette != null) GUIUtility.systemCopyBuffer = "[spawn " + targetObject.gameObject.name + " params:(time)," + targetObject.GetString() + "]";
                if (LogResult) Debug.Log(GUIUtility.systemCopyBuffer);
            }

            GUILayout.Space(20f);

            if (GUILayout.Button("Copy params", GUILayout.Height(50)))
            {
                if (vignette != null) GUIUtility.systemCopyBuffer = "(time)," + targetObject.GetString();
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