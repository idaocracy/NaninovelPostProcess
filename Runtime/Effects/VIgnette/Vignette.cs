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
using NaninovelSceneAssistant;
using UnityEditor;
#endif

namespace NaninovelPostProcess { 

    [RequireComponent(typeof(PostProcessVolume))]
    public class Vignette : PostProcessSpawnObject, Spawn.IParameterized, Spawn.IAwaitable, DestroySpawned.IParameterized, DestroySpawned.IAwaitable, PostProcessSpawnObject.ITextureParameterized
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
        public List<Texture> TextureItems => maskTextures;

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
            maskTextures.Insert(0, null);
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

#if UNITY_EDITOR && NANINOVEL_SCENE_ASSISTANT_AVAILABLE
        public override List<ParameterValue> GetParams()
        {
            return new List<ParameterValue>()
            {
                { new ParameterValue("Time", () => Duration, v => Duration = (float)v, (i,p) => i.FloatField(p), false) },
                { new ParameterValue("Weight", () => Volume.weight, v => Volume.weight = (float)v, (i,p) => i.FloatSliderField(p, 0f, 1f), false) },
                { new ParameterValue("ClassicOrMask", () => vignette.mode.value, v => vignette.mode.value = (VignetteMode)v, (i,p) => i.EnumField(p), false) },
                { new ParameterValue("Color", () => vignette.color.value, v => vignette.color.value = (Color)v, (i,p) => i.ColorField(p), false) },

                { new ParameterValue("Center", () => vignette.center.value, v => vignette.center.value = (Vector2)v, (i,p) => i.Vector2Field(p), isParameter:false, condition: () => vignette.mode.value == VignetteMode.Classic) },
                { new ParameterValue("Intensity", () => vignette.intensity.value, v => vignette.intensity.value = (float)v, (i,p) => i.FloatSliderField(p, 0f,1f), isParameter:false, condition: () => vignette.mode.value == VignetteMode.Classic) },
                { new ParameterValue("Smoothness", () => vignette.smoothness.value, v => vignette.smoothness.value = (float)v, (i,p) => i.FloatSliderField(p, 0.01f,1f), isParameter:false, condition: () => vignette.mode.value == VignetteMode.Classic) },
                { new ParameterValue("Roundness", () => vignette.roundness.value, v => vignette.roundness.value = (float)v, (i,p) => i.FloatSliderField(p, 0f,1f), isParameter:false, condition: () => vignette.mode.value == VignetteMode.Classic) },
                { new ParameterValue("Rounded", () => vignette.rounded.value, v => vignette.rounded.value = (bool)v, (i,p) => i.BoolField(p), isParameter:false, condition: () => vignette.mode.value == VignetteMode.Classic) },
                
                { new ParameterValue("MaskTexture", () => vignette.mask.value, v => vignette.mask.value = (Texture)v, (i,p) => i.TypeListField<Texture>(p, Textures), isParameter:false, condition: () => vignette.mode.value == VignetteMode.Masked) },
                { new ParameterValue("Opacity", () => vignette.opacity.value, v => vignette.opacity.value = (float)v, (i,p) => i.FloatSliderField(p, 0f, 1f), isParameter:false, condition: () => vignette.mode.value == VignetteMode.Masked) },
            };
        }
#endif
    }

#if UNITY_EDITOR && NANINOVEL_SCENE_ASSISTANT_AVAILABLE
    [CustomEditor(typeof(Vignette))]
    public class VignetteEditor : SpawnObjectEditor { }
#endif

}

#endif