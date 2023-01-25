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
    public class Vignette : PostProcessObject, Spawn.IParameterized, Spawn.IAwaitable, DestroySpawned.IParameterized, DestroySpawned.IAwaitable, PostProcessObject.ITextureParameterized, ISceneAssistant
    {
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

        private readonly Tweener<ColorTween> colorTweener = new Tweener<ColorTween>();
        private readonly Tweener<VectorTween> centerTweener = new Tweener<VectorTween>();
        private readonly Tweener<FloatTween> intensityTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> smoothnessTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> roundnessTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> opacityTweener = new Tweener<FloatTween>();

        [Header("Vignette Settings")]
        [SerializeField] private VignetteMode defaultMode = VignetteMode.Classic;
        [SerializeField] private Color defaultColor = Color.black;

        //Classic parameters
        [SerializeField] private Vector2 defaultCenter = new Vector2(0.5f,0.5f);
        [SerializeField, Range(0f, 1f)] private float defaultIntensity = 0f;
        [SerializeField, Range(0.01f, 1f)] private float defaultSmoothness = 0.2f;
        [SerializeField, Range(0f, 1f)] private float defaultRoundness = 1f;
        [SerializeField] private bool defaultRounded = false;

        //Masked parameters
        [SerializeField] private string defaultMask = string.Empty;
        [SerializeField] private List<Texture> maskTextures = new List<Texture>();
        [SerializeField, Range(0f, 1f)] private float defaultOpacity = 1f;

        private UnityEngine.Rendering.PostProcessing.Vignette vignette;

        public List<Texture> TextureItems() => maskTextures;

        public override void SetSpawnParameters(IReadOnlyList<string> parameters, bool asap)
        {
            base.SetSpawnParameters(parameters, asap);
            Mode = parameters?.ElementAtOrDefault(2)?.ToString() ?? defaultMode.ToString();
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
            if (Volume.weight != volumeWeight) tasks.Add(ChangeVolumeWeightAsync(volumeWeight, duration, asyncToken));
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
            if (Volume.weight != volumeWeight) tasks.Add(ChangeVolumeWeightAsync(volumeWeight, duration, asyncToken));
            vignette.mode.value = (VignetteMode)System.Enum.Parse(typeof(VignetteMode), mode);
            if (vignette.color.value != color) tasks.Add(ChangeColorAsync(color, duration, asyncToken));
            if (vignette.mask.value != null && vignette.mask.value.name != mask) ChangeTexture(mask);
            if (vignette.opacity.value != opacity) tasks.Add(ChangeOpacityAsync(opacity, duration, asyncToken));

            await UniTask.WhenAll(tasks);
        }

        protected override void CompleteTweens()
        {
            if (colorTweener.Running) colorTweener.CompleteInstantly();
            if (centerTweener.Running) centerTweener.CompleteInstantly();
            if (intensityTweener.Running) intensityTweener.CompleteInstantly();
            if (smoothnessTweener.Running) smoothnessTweener.CompleteInstantly();
            if (roundnessTweener.Running) roundnessTweener.CompleteInstantly();
            if (opacityTweener.Running) opacityTweener.CompleteInstantly();
            if (volumeWeightTweener.Running) volumeWeightTweener.CompleteInstantly();
        }
        protected override void Awake()
        {
            base.Awake();
            vignette = Volume.profile.GetSetting<UnityEngine.Rendering.PostProcessing.Vignette>() ?? Volume.profile.AddSettings<UnityEngine.Rendering.PostProcessing.Vignette>();
            vignette.SetAllOverridesTo(true);
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
            else maskTextures.Select(t => t != null && t.name == imageId);
        }

    #if UNITY_EDITOR

        public string SceneAssistantParameters()
        {
            Duration = SpawnSceneAssistant.FloatField("Fade-in time", Duration);
            Volume.weight = SpawnSceneAssistant.SliderField("Volume Weight", Volume.weight, 0f, 1f);
            vignette.mode.value = SpawnSceneAssistant.EnumField("Mode", vignette.mode.value);
            vignette.color.value = SpawnSceneAssistant.ColorField("Color", vignette.color.value);

            if (vignette.mode.value == VignetteMode.Classic)
            {
                vignette.center.value = SpawnSceneAssistant.Vector2Field("Center", vignette.center.value);
                vignette.intensity.value = SpawnSceneAssistant.SliderField("Intensity", vignette.intensity.value, 0f, 1f);
                vignette.smoothness.value = SpawnSceneAssistant.SliderField("Smoothness", vignette.smoothness.value, 0.01f, 1f);
                vignette.roundness.value = SpawnSceneAssistant.SliderField("Roundness", vignette.roundness.value, 0f, 1f);
                vignette.rounded.value = SpawnSceneAssistant.BooleanField("Rounded", vignette.rounded.value);
            }
            else if (vignette.mode.value == VignetteMode.Masked)
            {
                vignette.mask.value = SpawnSceneAssistant.TextureField("Mask", vignette.mask.value, this is PostProcessObject.ITextureParameterized textureParameterized ? textureParameterized.TextureItems() : null);
                vignette.opacity.value = EditorGUILayout.Slider("Dirt Mask Opacity", vignette.opacity.value, 0f, 1f, GUILayout.Width(220));
            }

            return SpawnSceneAssistant.GetSpawnString(ParameterList());
        }

        public IReadOnlyDictionary<string, string> ParameterList()
        {
            if (vignette == null) return null;

            return new Dictionary<string, string>()
            {
                { "time", Duration.ToString()},
                { "weight", Volume.weight.ToString()},
                { "classicOrMasked", vignette.mode.value.ToString()},
                { "color",  "#" + ColorUtility.ToHtmlStringRGBA(vignette.color.value)},
                { "center", vignette.center.value.ToString().Remove("(").Remove(")")},
                { "intensity", vignette.intensity.value.ToString()},
                { "smoothness", vignette.smoothness.value.ToString()},
                { "roundness", vignette.roundness.value.ToString()},
                { "rounded", vignette.rounded.value.ToString().ToLower()},
                { "maskTexture", vignette.mask.value?.ToString()},
                { "maskOpacity", vignette.opacity.value.ToString()}
            };
        }
    #endif

    }

    #if UNITY_EDITOR

    [CustomEditor(typeof(Vignette))]
    public class VignetteEditor : PostProcessObjectEditor
    {
        protected override string Label => "vignette";
    }

    #endif

}

#endif