//2022-2023 idaocracy

#if UNITY_POST_PROCESSING_STACK_V2

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using Naninovel;
using Naninovel.Commands;
using static NaninovelPostProcess.PostProcessSpawnObject;
using NaninovelSceneAssistant;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NaninovelPostProcess { 

    [RequireComponent(typeof(PostProcessVolume))]
    public class ColorGradingLDR : PostProcessSpawnObject, Spawn.IParameterized, Spawn.IAwaitable, DestroySpawned.IParameterized, DestroySpawned.IAwaitable, PostProcessSpawnObject.ITextureParameterized
    {
        protected string LookUpTexture { get; private set; }
        protected float Contribution { get; private set; }
        protected float Temperature { get; private set; }
        protected float Tint { get; private set; }
        protected Color ColorFilter { get; private set; }
        protected float HueShift { get; private set; }
        protected float Saturation { get; private set; }
        protected float Brightness { get; private set; }
        protected float Contrast { get; private set; }
        protected Vector3 RedChannel { get; private set; }
        protected Vector3 GreenChannel { get; private set; }
        protected Vector3 BlueChannel { get; private set; }
        protected Vector4 Lift { get; private set; }
        protected Vector4 Gamma { get; private set; }
        protected Vector4 Gain { get; private set; }

        private readonly Tweener<FloatTween> temperatureTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> tintTweener = new Tweener<FloatTween>();
        private readonly Tweener<ColorTween> colorFilterTweener = new Tweener<ColorTween>();
        private readonly Tweener<FloatTween> hueShiftTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> saturationTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> brightnessTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> contrastTweener = new Tweener<FloatTween>();
        private readonly Tweener<VectorTween> redChannelTweener = new Tweener<VectorTween>();
        private readonly Tweener<VectorTween> greenChannelTweener = new Tweener<VectorTween>();
        private readonly Tweener<VectorTween> blueChannelTweener = new Tweener<VectorTween>();
        private readonly Tweener<VectorTween> liftTweener = new Tweener<VectorTween>();
        private readonly Tweener<VectorTween> gammaTweener = new Tweener<VectorTween>();
        private readonly Tweener<VectorTween> gainTweener = new Tweener<VectorTween>();
        private readonly Tweener<FloatTween> contributionTweener = new Tweener<FloatTween>();

        [Header("Color Grading Settings")]
        [SerializeField] private string defaultLookUpTexture = "None";
        [SerializeField] private List<Texture> lookUpTextures = new List<Texture>();
        [SerializeField, Range(0f, 1f)] private float defaultContribution = 1f;
        [SerializeField, Range(-100f, 100f)] private float defaultTemperature = 0f;
        [SerializeField, Range(-100f, 100f)] private float defaultTint = 0f;
        [SerializeField, ColorUsage(false, true)] private Color defaultColorFilter = Color.white;
        [SerializeField, Range(-180f, 180f)] private float defaultHueShift = 0f;
        [SerializeField, Range(-100f, 100f)] private float defaultSaturation = 0f;
        [SerializeField, Range(-100f, 100f)] private float defaultBrightness = 0f;
        [SerializeField, Range(-100f, 100f)] private float defaultContrast = 0f;
        [SerializeField] private Vector3 defaultRedChannel = new Vector3(100, 0, 0);
        [SerializeField] private Vector3 defaultGreenChannel = new Vector3(0, 100, 0);
        [SerializeField] private Vector3 defaultBlueChannel = new Vector3(0, 0, 100);
        [SerializeField] private Vector4 defaultLift = new Vector4(1f, 1f, 1f, 0f);
        [SerializeField] private Vector4 defaultGamma = new Vector4(1f, 1f, 1f, 0f);
        [SerializeField] private Vector4 defaultGain = new Vector4(1f, 1f, 1f, 0f);

        private UnityEngine.Rendering.PostProcessing.ColorGrading colorGrading;

        public List<Texture> TextureItems => lookUpTextures;

        public override void SetSpawnParameters(IReadOnlyList<string> parameters, bool asap)
        {
            base.SetSpawnParameters(parameters, asap);
            LookUpTexture = parameters?.ElementAtOrDefault(2) ?? defaultLookUpTexture;
            Contribution = parameters?.ElementAtOrDefault(3)?.AsInvariantFloat() ?? defaultContribution;
            Temperature = parameters?.ElementAtOrDefault(4)?.AsInvariantFloat() ?? defaultTemperature;
            Tint = parameters?.ElementAtOrDefault(5)?.AsInvariantFloat() ?? defaultTint;
            ColorFilter = ColorUtility.TryParseHtmlString(parameters?.ElementAtOrDefault(6)?.ToString(), out var colorFilter) ? colorFilter : defaultColorFilter;
            HueShift = parameters?.ElementAtOrDefault(7)?.AsInvariantFloat() ?? defaultHueShift;
            Saturation = parameters?.ElementAtOrDefault(8)?.AsInvariantFloat() ?? defaultSaturation;
            Brightness = parameters?.ElementAtOrDefault(9)?.AsInvariantFloat() ?? defaultBrightness;
            Contrast = parameters?.ElementAtOrDefault(10)?.AsInvariantFloat() ?? defaultContrast;

            RedChannel = new Vector3(parameters?.ElementAtOrDefault(11)?.AsInvariantFloat() ?? defaultRedChannel.x,
                        parameters?.ElementAtOrDefault(12)?.AsInvariantFloat() ?? defaultRedChannel.y,
                        parameters?.ElementAtOrDefault(13)?.AsInvariantFloat() ?? defaultRedChannel.z);

            GreenChannel = new Vector3(parameters?.ElementAtOrDefault(14)?.AsInvariantFloat() ?? defaultGreenChannel.x,
                        parameters?.ElementAtOrDefault(15)?.AsInvariantFloat() ?? defaultGreenChannel.y,
                        parameters?.ElementAtOrDefault(16)?.AsInvariantFloat() ?? defaultGreenChannel.z);

            BlueChannel = new Vector3(parameters?.ElementAtOrDefault(17)?.AsInvariantFloat() ?? defaultBlueChannel.x,
                        parameters?.ElementAtOrDefault(18)?.AsInvariantFloat() ?? defaultBlueChannel.y,
                        parameters?.ElementAtOrDefault(19)?.AsInvariantFloat() ?? defaultBlueChannel.z);

            Lift = new Vector4(parameters?.ElementAtOrDefault(20)?.AsInvariantFloat() ?? defaultLift.x,
                        parameters?.ElementAtOrDefault(21)?.AsInvariantFloat() ?? defaultLift.y,
                        parameters?.ElementAtOrDefault(22)?.AsInvariantFloat() ?? defaultLift.z,
                        parameters?.ElementAtOrDefault(23)?.AsInvariantFloat() ?? defaultLift.w);

            Gamma = new Vector4(parameters?.ElementAtOrDefault(24)?.AsInvariantFloat() ?? defaultGamma.x,
                        parameters?.ElementAtOrDefault(25)?.AsInvariantFloat() ?? defaultGamma.y,
                        parameters?.ElementAtOrDefault(26)?.AsInvariantFloat() ?? defaultGamma.z,
                        parameters?.ElementAtOrDefault(27)?.AsInvariantFloat() ?? defaultGamma.w);

            Gain = new Vector4(parameters?.ElementAtOrDefault(28)?.AsInvariantFloat() ?? defaultGain.x,
                        parameters?.ElementAtOrDefault(29)?.AsInvariantFloat() ?? defaultGain.y,
                        parameters?.ElementAtOrDefault(30)?.AsInvariantFloat() ?? defaultGain.z,
                        parameters?.ElementAtOrDefault(31)?.AsInvariantFloat() ?? defaultGain.w);
        }

        public async UniTask AwaitSpawnAsync(AsyncToken asyncToken = default)
        {
            CompleteTweens();
            var duration = asyncToken.Completed ? 0 : Duration;
            await ChangeColorGradingAsync(duration, VolumeWeight, Contribution, LookUpTexture, Temperature, Tint, ColorFilter, HueShift, Saturation, Brightness, Contrast, RedChannel, GreenChannel, BlueChannel, Lift, Gamma, Gain, asyncToken);
        }

        public async UniTask ChangeColorGradingAsync(float duration, float volumeWeight, float contribution, string lookUpTexture, float temperature, float tint,
                                                    Color colorFilter, float hueShift, float saturation, float brightness, float contrast,
                                                    Vector3 redChannel, Vector3 greenChannel, Vector3 blueChannel,
                                                    Vector4 lift, Vector4 gamma, Vector4 gain,
                                                    AsyncToken asyncToken = default)
        {

            var tasks = new List<UniTask>();

            if (Volume.weight != volumeWeight) tasks.Add(ChangeVolumeWeightAsync(volumeWeight, duration, asyncToken));

            if (colorGrading.ldrLutContribution.value != contribution) tasks.Add(ChangeContributionAsync(volumeWeight, duration, asyncToken));
            if (colorGrading.ldrLut.value != null && colorGrading.ldrLut.value.name != lookUpTexture) ChangeTexture(lookUpTexture);
            if (colorGrading.temperature.value != temperature) tasks.Add(ChangeTemperatureAsync(temperature, duration, asyncToken));
            if (colorGrading.tint.value != tint) tasks.Add(ChangeTintAsync(tint, duration, asyncToken));
            if (colorGrading.colorFilter.value != colorFilter) tasks.Add(ChangeColorFilterAsync(colorFilter, duration, asyncToken));
            if (colorGrading.hueShift.value != hueShift) tasks.Add(ChangeHueShiftAsync(hueShift, duration, asyncToken));
            if (colorGrading.saturation.value != saturation) tasks.Add(ChangeSaturationAsync(saturation, duration, asyncToken));
            if (colorGrading.brightness.value != brightness) tasks.Add(ChangeBrightnessAsync(brightness, duration, asyncToken));
            if (colorGrading.contrast.value != contrast) tasks.Add(ChangeContrastAsync(contrast, duration, asyncToken));
            if (GetRedChannel() != redChannel) tasks.Add(ChangeRedChannelAsync(redChannel, duration, asyncToken));
            if (GetGreenChannel() != greenChannel) tasks.Add(ChangeGreenChannelAsync(greenChannel, duration, asyncToken));
            if (GetBlueChannel() != blueChannel) tasks.Add(ChangeBlueChannelAsync(blueChannel, duration, asyncToken));
            if (colorGrading.lift.value != lift) tasks.Add(ChangeLiftAsync(lift, duration, asyncToken));
            if (colorGrading.gamma.value != gamma) tasks.Add(ChangeGammaAsync(gamma, duration, asyncToken));
            if (colorGrading.gain.value != gain) tasks.Add(ChangeGainAsync(gain, duration, asyncToken));

            await UniTask.WhenAll(tasks);
        }

        protected override void CompleteTweens()
        {
            if(volumeWeightTweener.Running) volumeWeightTweener.CompleteInstantly();
            if(temperatureTweener.Running) temperatureTweener.CompleteInstantly();
            if(tintTweener.Running) tintTweener.CompleteInstantly();
            if(colorFilterTweener.Running) colorFilterTweener.CompleteInstantly();
            if(hueShiftTweener.Running) hueShiftTweener.CompleteInstantly();
            if(saturationTweener.Running) saturationTweener.CompleteInstantly();
            if(brightnessTweener.Running) brightnessTweener.CompleteInstantly();
            if(contrastTweener.Running) contrastTweener.CompleteInstantly();
            if(redChannelTweener.Running) redChannelTweener.CompleteInstantly();
            if(greenChannelTweener.Running) greenChannelTweener.CompleteInstantly();
            if(blueChannelTweener.Running) blueChannelTweener.CompleteInstantly();
            if(liftTweener.Running) liftTweener.CompleteInstantly();
            if(gammaTweener.Running) gammaTweener.CompleteInstantly();
            if(gainTweener.Running) gainTweener.CompleteInstantly();
            if(contributionTweener.Running) contributionTweener.CompleteInstantly();
        }

        protected override void Awake()
        {
            base.Awake();
            colorGrading = Volume.profile.GetSetting<UnityEngine.Rendering.PostProcessing.ColorGrading>() ?? Volume.profile.AddSettings<UnityEngine.Rendering.PostProcessing.ColorGrading>();
            colorGrading.SetAllOverridesTo(true);
            colorGrading.gradingMode.value = GradingMode.LowDefinitionRange;
        }

        private async UniTask ChangeContributionAsync(float contribution, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await contributionTweener.RunAsync(new FloatTween(colorGrading.ldrLutContribution.value, contribution, duration, x => colorGrading.ldrLutContribution.value = x), asyncToken, colorGrading);
            else colorGrading.ldrLutContribution.value = contribution;
        }
        private async UniTask ChangeTemperatureAsync(float temperature, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await temperatureTweener.RunAsync(new FloatTween(colorGrading.temperature.value, temperature, duration, x => colorGrading.temperature.value = x), asyncToken, colorGrading);
            else colorGrading.temperature.value = temperature;
        }
        private async UniTask ChangeTintAsync(float tint, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await tintTweener.RunAsync(new FloatTween(colorGrading.tint.value, tint, duration, x => colorGrading.tint.value = x), asyncToken, colorGrading);
            else colorGrading.tint.value = tint;
        }
        private async UniTask ChangeColorFilterAsync(Color colorFilter, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await colorFilterTweener.RunAsync(new ColorTween(colorGrading.colorFilter.value, colorFilter, ColorTweenMode.All, duration, x => colorGrading.colorFilter.value = x), asyncToken, colorGrading);
            else colorGrading.colorFilter.value = colorFilter;
        }
        private async UniTask ChangeHueShiftAsync(float hueShift, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await hueShiftTweener.RunAsync(new FloatTween(colorGrading.hueShift.value, hueShift, duration, x => colorGrading.hueShift.value = x), asyncToken, colorGrading);
            else colorGrading.hueShift.value = hueShift;
        }
        private async UniTask ChangeSaturationAsync(float saturation, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await saturationTweener.RunAsync(new FloatTween(colorGrading.saturation.value, saturation, duration, x => colorGrading.saturation.value = x), asyncToken, colorGrading);
            else colorGrading.saturation.value = saturation;
        }
        private async UniTask ChangeBrightnessAsync(float brightness, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await brightnessTweener.RunAsync(new FloatTween(colorGrading.brightness.value, brightness, duration, x => colorGrading.brightness.value = x), asyncToken, colorGrading);
            else colorGrading.brightness.value = brightness;
        }
        private async UniTask ChangeContrastAsync(float contrast, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await contrastTweener.RunAsync(new FloatTween(colorGrading.contrast.value, contrast, duration, x => colorGrading.contrast.value = x), asyncToken, colorGrading);
            else colorGrading.contrast.value = contrast;
        }
        private async UniTask ChangeRedChannelAsync(Vector3 red, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await redChannelTweener.RunAsync(new VectorTween(GetRedChannel(), red, duration, ApplyRedChannel), asyncToken, colorGrading);
            else ApplyRedChannel(red);
        }
        private async UniTask ChangeGreenChannelAsync(Vector3 green, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await greenChannelTweener.RunAsync(new VectorTween(GetGreenChannel(), green, duration, ApplyGreenChannel), asyncToken, colorGrading);
            else ApplyGreenChannel(green);
        }
        private async UniTask ChangeBlueChannelAsync(Vector3 blue, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await blueChannelTweener.RunAsync(new VectorTween(GetBlueChannel(), blue, duration, ApplyBlueChannel), asyncToken, colorGrading);
            else ApplyBlueChannel(blue);
        }
        private async UniTask ChangeLiftAsync(Vector4 lift, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await liftTweener.RunAsync(new VectorTween(colorGrading.lift.value, lift, duration, x => colorGrading.lift.value = x), asyncToken, colorGrading);
            else colorGrading.lift.value = lift;
        }
        private async UniTask ChangeGammaAsync(Vector4 gamma, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await gammaTweener.RunAsync(new VectorTween(colorGrading.gamma.value, gamma, duration, x => colorGrading.gamma.value = x), asyncToken, colorGrading);
            else colorGrading.gamma.value = gamma;
        }
        private async UniTask ChangeGainAsync(Vector4 gain, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await gainTweener.RunAsync(new VectorTween(colorGrading.gain.value, gain, duration, x => colorGrading.gain.value = x), asyncToken, colorGrading);
            else colorGrading.gain.value = gain;
        }

        private void ChangeTexture(string imageId)
        {
            if (imageId == "None" || String.IsNullOrEmpty(imageId)) colorGrading.ldrLut.value = null;
            else lookUpTextures.Select(t => t != null && t.name == imageId);
        }

        public Vector3 GetRedChannel() => new Vector3(colorGrading.mixerRedOutRedIn.value, colorGrading.mixerRedOutGreenIn.value, colorGrading.mixerRedOutBlueIn.value);
        public Vector3 GetGreenChannel() => new Vector3(colorGrading.mixerGreenOutRedIn.value, colorGrading.mixerGreenOutGreenIn.value, colorGrading.mixerGreenOutBlueIn.value);
        public Vector3 GetBlueChannel() => new Vector3(colorGrading.mixerBlueOutRedIn.value, colorGrading.mixerBlueOutGreenIn.value, colorGrading.mixerBlueOutBlueIn.value);


        public void ApplyRedChannel(Vector3 red)
        {
            colorGrading.mixerRedOutRedIn.value = red.x;
            colorGrading.mixerRedOutGreenIn.value = red.y;
            colorGrading.mixerRedOutBlueIn.value = red.z;
        }

        public void ApplyGreenChannel(Vector3 green)
        {
            colorGrading.mixerGreenOutRedIn.value = green.x;
            colorGrading.mixerGreenOutGreenIn.value = green.y;
            colorGrading.mixerGreenOutBlueIn.value = green.z;
        }

        public void ApplyBlueChannel(Vector3 blue)
        {
            colorGrading.mixerBlueOutRedIn.value = blue.x;
            colorGrading.mixerBlueOutGreenIn.value = blue.y;
            colorGrading.mixerBlueOutBlueIn.value = blue.z;
        }

        public override List<ParameterValue> GetParams()
        {
            return new List<ParameterValue>()
            {
                { new ParameterValue("Time", () => Duration, v => Duration = (float)v, (i,p) => i.FloatField(p), false) },
                { new ParameterValue("Weight", () => Volume.weight, v => Volume.weight = (float)v, (i,p) => i.FloatSliderField(p, 0f,1f), false) },
                { new ParameterValue("LookUpTexture", () => colorGrading.ldrLut.value, v => colorGrading.ldrLut.value = (Texture)v, (i,p) => i.TypeListField<Texture>(p, Textures), false) },
                { new ParameterValue("Contribution", () => colorGrading.ldrLutContribution.value, v => colorGrading.ldrLutContribution.value = (float)v, (i,p) => i.FloatSliderField(p, 0f,1f), false) },
                { new ParameterValue("Temperature", () => colorGrading.temperature.value, v => colorGrading.temperature.value = (float)v, (i,p) => i.FloatField(p), false) },
                { new ParameterValue("Tint", () => colorGrading.tint.value, v => colorGrading.tint.value = (float)v, (i,p) => i.FloatField(p), false) },
                { new ParameterValue("ColorFilter", () => colorGrading.colorFilter.value, v => colorGrading.colorFilter.value = (Color)v, (i,p) => i.ColorField(p), false) },
                { new ParameterValue("HueShift", () => colorGrading.hueShift.value, v => colorGrading.hueShift.value = (float)v, (i,p) => i.FloatField(p), false) },
                { new ParameterValue("Saturation", () => colorGrading.saturation.value, v => colorGrading.saturation.value = (float)v, (i,p) => i.FloatField(p), false) },
                { new ParameterValue("Brightness", () => colorGrading.brightness.value, v => colorGrading.brightness.value = (float)v, (i,p) => i.FloatField(p), false) },
                { new ParameterValue("Contrast", () => colorGrading.contrast.value, v => colorGrading.contrast.value = (float)v, (i,p) => i.FloatField(p), false) },
                { new ParameterValue("RedChannel", () => GetRedChannel(), v => ApplyRedChannel((Vector3)v), (i,p) => i.Vector3Field(p), false) },
                { new ParameterValue("GreenChannel", () => GetGreenChannel(), v => ApplyGreenChannel((Vector3)v), (i,p) => i.Vector3Field(p), false) },
                { new ParameterValue("BlueChannel", () => GetBlueChannel(), v => ApplyBlueChannel((Vector3)v), (i,p) => i.Vector3Field(p), false) },
                { new ParameterValue("Lift", () => colorGrading.lift.value, v => colorGrading.lift.value = (Vector4)v, (i,p) => i.Vector4Field(p), false) },
                { new ParameterValue("Gamma", () => colorGrading.gamma.value, v => colorGrading.gamma.value = (Vector4)v, (i,p) => i.Vector4Field(p), false) },
                { new ParameterValue("Gain", () => colorGrading.gain.value, v => colorGrading.gain.value = (Vector4)v, (i,p) => i.Vector4Field(p), false) },
            };
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ColorGradingLDR))]
    public class ColorGradingLDREditor : SpawnObjectEditor { }
#endif

}

#endif