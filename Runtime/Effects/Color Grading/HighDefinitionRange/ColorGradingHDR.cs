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
    public class ColorGradingHDR : PostProcessSpawnObject, Spawn.IParameterized, Spawn.IAwaitable, DestroySpawned.IParameterized, DestroySpawned.IAwaitable
    {
        protected Tonemapper TonemapperMode { get; private set; }
        protected float ToeStrength { get; private set; }
        protected float ToeLength { get; private set; }
        protected float ShoulderStrength { get; private set; }
        protected float ShoulderLength { get; private set; }
        protected float ShoulderAngle { get; private set; }
        protected float ToneGamma { get; private set; }
        protected float Temperature { get; private set; }
        protected float Tint { get; private set; }
        protected float PostExposure { get; private set; }
        protected Color ColorFilter { get; private set; }
        protected float HueShift { get; private set; }
        protected float Saturation { get; private set; }
        protected float Contrast { get; private set; }
        protected Vector3 RedChannel { get; private set; }
        protected Vector3 GreenChannel { get; private set; }
        protected Vector3 BlueChannel { get; private set; }
        protected Vector4 Lift { get; private set; }
        protected Vector4 Gamma { get; private set; }
        protected Vector4 Gain { get; private set; }

        private readonly Tweener<FloatTween> temperatureTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> tintTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> postExposureTweener = new Tweener<FloatTween>();
        private readonly Tweener<ColorTween> colorFilterTweener = new Tweener<ColorTween>();
        private readonly Tweener<FloatTween> hueShiftTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> saturationTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> contrastTweener = new Tweener<FloatTween>();
        private readonly Tweener<VectorTween> redChannelTweener = new Tweener<VectorTween>();
        private readonly Tweener<VectorTween> greenChannelTweener = new Tweener<VectorTween>();
        private readonly Tweener<VectorTween> blueChannelTweener = new Tweener<VectorTween>();
        private readonly Tweener<VectorTween> liftTweener = new Tweener<VectorTween>();
        private readonly Tweener<VectorTween> gammaTweener = new Tweener<VectorTween>();
        private readonly Tweener<VectorTween> gainTweener = new Tweener<VectorTween>();
        private readonly Tweener<FloatTween> toeStrengthTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> toeLengthTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> shoulderStrengthTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> shoulderLengthTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> shoulderAngleTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> toneGammaTweener = new Tweener<FloatTween>();

        [Header("Color Grading Settings")]
        [SerializeField] private Tonemapper defaultTonemapperMode = Tonemapper.None;

        [SerializeField, Range(0f, 1f)] private float defaultToeStrength = 0f;
        [SerializeField, Range(0f, 1f)] private float defaultToeLength = 0.5f;
        [SerializeField, Range(0f, 1f)] private float defaultShoulderStrength = 0f;
        [SerializeField, UnityEngine.Min(0f)] private float defaultShoulderLength = 0.5f;
        [SerializeField, Range(0f, 1f)] private float defaultShoulderAngle = 0f;
        [SerializeField, UnityEngine.Min(0.001f)] private float defaultToneGamma = 1f;

        [SerializeField, Range(-100f, 100f)] private float defaultTemperature = 0f;
        [SerializeField, Range(-100f, 100f)] private float defaultTint = 0f;
        [SerializeField] private float defaultPostExposure = 0f;
        [SerializeField, ColorUsage(false, true)] private Color defaultColorFilter = Color.white;
        [SerializeField, Range(-180f, 180f)] private float defaultHueShift = 0f;
        [SerializeField, Range(-100f, 100f)] private float defaultSaturation = 0f;
        [SerializeField, Range(-100f, 100f)] private float defaultContrast = 0f;
        [SerializeField] private Vector3 defaultRedChannel = new Vector3(100, 0, 0);
        [SerializeField] private Vector3 defaultGreenChannel = new Vector3(0, 100, 0);
        [SerializeField] private Vector3 defaultBlueChannel = new Vector3(0, 0, 100);
        [SerializeField] private Vector4 defaultLift = new Vector4(1f, 1f, 1f, 0f);
        [SerializeField] private Vector4 defaultGamma = new Vector4(1f, 1f, 1f, 0f);
        [SerializeField] private Vector4 defaultGain = new Vector4(1f, 1f, 1f, 0f);

        private UnityEngine.Rendering.PostProcessing.ColorGrading colorGrading;

        public override void SetSpawnParameters(IReadOnlyList<string> parameters, bool asap)
        {
            base.SetSpawnParameters(parameters, asap);  
            TonemapperMode = (Tonemapper)System.Enum.Parse(typeof(Tonemapper), parameters?.ElementAtOrDefault(2).ToString() ?? defaultTonemapperMode.ToString());

            if (TonemapperMode == Tonemapper.Custom)
            {
                ToeStrength = parameters?.ElementAtOrDefault(3)?.AsInvariantFloat() ?? defaultToeStrength;
                ToeLength = parameters?.ElementAtOrDefault(4)?.AsInvariantFloat() ?? defaultToeLength;
                ShoulderStrength = parameters?.ElementAtOrDefault(5)?.AsInvariantFloat() ?? defaultShoulderStrength;
                ShoulderLength = parameters?.ElementAtOrDefault(6)?.AsInvariantFloat() ?? defaultShoulderLength;
                ShoulderAngle = parameters?.ElementAtOrDefault(7)?.AsInvariantFloat() ?? defaultShoulderAngle;
                ToneGamma = parameters?.ElementAtOrDefault(8)?.AsInvariantFloat() ?? defaultToneGamma;

                Temperature = parameters?.ElementAtOrDefault(9)?.AsInvariantFloat() ?? defaultTemperature;
                Tint = parameters?.ElementAtOrDefault(10)?.AsInvariantFloat() ?? defaultTint;
                PostExposure = parameters?.ElementAtOrDefault(11)?.AsInvariantFloat() ?? defaultPostExposure;
                ColorFilter = ColorUtility.TryParseHtmlString(parameters?.ElementAtOrDefault(12), out var colorFilterc) ? colorFilterc : defaultColorFilter;
                HueShift = parameters?.ElementAtOrDefault(13)?.AsInvariantFloat() ?? defaultHueShift;
                Saturation = parameters?.ElementAtOrDefault(14)?.AsInvariantFloat() ?? defaultSaturation;
                Contrast = parameters?.ElementAtOrDefault(15)?.AsInvariantFloat() ?? defaultContrast;

                RedChannel = new Vector3(parameters?.ElementAtOrDefault(16)?.AsInvariantFloat() ?? defaultRedChannel.x,
                            parameters?.ElementAtOrDefault(17)?.AsInvariantFloat() ?? defaultRedChannel.y,
                            parameters?.ElementAtOrDefault(18)?.AsInvariantFloat() ?? defaultRedChannel.z);

                GreenChannel = new Vector3(parameters?.ElementAtOrDefault(19)?.AsInvariantFloat() ?? defaultGreenChannel.x,
                            parameters?.ElementAtOrDefault(20)?.AsInvariantFloat() ?? defaultGreenChannel.y,
                            parameters?.ElementAtOrDefault(21)?.AsInvariantFloat() ?? defaultGreenChannel.z);

                BlueChannel = new Vector3(parameters?.ElementAtOrDefault(22)?.AsInvariantFloat() ?? defaultBlueChannel.x,
                            parameters?.ElementAtOrDefault(23)?.AsInvariantFloat() ?? defaultBlueChannel.y,
                            parameters?.ElementAtOrDefault(24)?.AsInvariantFloat() ?? defaultBlueChannel.z);

                Lift = new Vector4(parameters?.ElementAtOrDefault(25)?.AsInvariantFloat() ?? defaultLift.x,
                            parameters?.ElementAtOrDefault(26)?.AsInvariantFloat() ?? defaultLift.y,
                            parameters?.ElementAtOrDefault(27)?.AsInvariantFloat() ?? defaultLift.z,
                            parameters?.ElementAtOrDefault(28)?.AsInvariantFloat() ?? defaultLift.w);

                Gamma = new Vector4(parameters?.ElementAtOrDefault(29)?.AsInvariantFloat() ?? defaultGamma.x,
                            parameters?.ElementAtOrDefault(30)?.AsInvariantFloat() ?? defaultGamma.y,
                            parameters?.ElementAtOrDefault(31)?.AsInvariantFloat() ?? defaultGamma.z,
                            parameters?.ElementAtOrDefault(32)?.AsInvariantFloat() ?? defaultGamma.w);

                Gain = new Vector4(parameters?.ElementAtOrDefault(33)?.AsInvariantFloat() ?? defaultGain.x,
                            parameters?.ElementAtOrDefault(34)?.AsInvariantFloat() ?? defaultGain.y,
                            parameters?.ElementAtOrDefault(35)?.AsInvariantFloat() ?? defaultGain.z,
                            parameters?.ElementAtOrDefault(36)?.AsInvariantFloat() ?? defaultGain.w);
            }
            else
            {
                Temperature = parameters?.ElementAtOrDefault(3)?.AsInvariantFloat() ?? defaultTemperature;
                Tint = parameters?.ElementAtOrDefault(4)?.AsInvariantFloat() ?? defaultTint;
                PostExposure = parameters?.ElementAtOrDefault(5)?.AsInvariantFloat() ?? defaultPostExposure;
                ColorFilter = ColorUtility.TryParseHtmlString(parameters?.ElementAtOrDefault(6)?.ToString(), out var colorFilter) ? colorFilter : defaultColorFilter;
                HueShift = parameters?.ElementAtOrDefault(7)?.AsInvariantFloat() ?? defaultHueShift;
                Saturation = parameters?.ElementAtOrDefault(8)?.AsInvariantFloat() ?? defaultSaturation;
                Contrast = parameters?.ElementAtOrDefault(9)?.AsInvariantFloat() ?? defaultContrast;

                RedChannel = new Vector3(parameters?.ElementAtOrDefault(10)?.AsInvariantFloat() ?? defaultRedChannel.x,
                            parameters?.ElementAtOrDefault(11)?.AsInvariantFloat() ?? defaultRedChannel.y,
                            parameters?.ElementAtOrDefault(12)?.AsInvariantFloat() ?? defaultRedChannel.z);

                GreenChannel = new Vector3(parameters?.ElementAtOrDefault(13)?.AsInvariantFloat() ?? defaultGreenChannel.x,
                            parameters?.ElementAtOrDefault(14)?.AsInvariantFloat() ?? defaultGreenChannel.y,
                            parameters?.ElementAtOrDefault(15)?.AsInvariantFloat() ?? defaultGreenChannel.z);

                BlueChannel = new Vector3(parameters?.ElementAtOrDefault(16)?.AsInvariantFloat() ?? defaultBlueChannel.x,
                            parameters?.ElementAtOrDefault(17)?.AsInvariantFloat() ?? defaultBlueChannel.y,
                            parameters?.ElementAtOrDefault(18)?.AsInvariantFloat() ?? defaultBlueChannel.z);

                Lift = new Vector4(parameters?.ElementAtOrDefault(19)?.AsInvariantFloat() ?? defaultLift.x,
                            parameters?.ElementAtOrDefault(20)?.AsInvariantFloat() ?? defaultLift.y,
                            parameters?.ElementAtOrDefault(21)?.AsInvariantFloat() ?? defaultLift.z,
                            parameters?.ElementAtOrDefault(22)?.AsInvariantFloat() ?? defaultLift.w);

                Gamma = new Vector4(parameters?.ElementAtOrDefault(23)?.AsInvariantFloat() ?? defaultGamma.x,
                            parameters?.ElementAtOrDefault(24)?.AsInvariantFloat() ?? defaultGamma.y,
                            parameters?.ElementAtOrDefault(25)?.AsInvariantFloat() ?? defaultGamma.z,
                            parameters?.ElementAtOrDefault(26)?.AsInvariantFloat() ?? defaultGamma.w);

                Gain = new Vector4(parameters?.ElementAtOrDefault(27)?.AsInvariantFloat() ?? defaultGain.x,
                            parameters?.ElementAtOrDefault(28)?.AsInvariantFloat() ?? defaultGain.y,
                            parameters?.ElementAtOrDefault(29)?.AsInvariantFloat() ?? defaultGain.z,
                            parameters?.ElementAtOrDefault(30)?.AsInvariantFloat() ?? defaultGain.w);
            }

        }
        public async UniTask AwaitSpawnAsync(AsyncToken asyncToken = default)
        {
            CompleteTweens();
            var duration = asyncToken.Completed ? 0 : Duration;
            await ChangeColorGradingAsync(duration, VolumeWeight, TonemapperMode, ToeStrength, ToeLength, 
                ShoulderStrength, ShoulderLength, ShoulderAngle, ToneGamma, Temperature, Tint, PostExposure, 
                ColorFilter, HueShift, Saturation, Contrast, RedChannel, GreenChannel, BlueChannel, Lift, 
                Gamma, Gain, asyncToken);
        }

        public async UniTask ChangeColorGradingAsync(float duration, float volumeWeight, Tonemapper tonemapperMode,
                                                    float toeStrength, float toeLength, float shoulderStrength, float shoulderLength, float shoulderAngle, float toneGamma, 
                                                    float temperature, float tint, float postExposure, Color colorFilter, float hueShift, float saturation, float contrast,
                                                    Vector3 redChannel, Vector3 greenChannel, Vector3 blueChannel, Vector4 lift, Vector4 gamma, Vector4 gain,
                                                    AsyncToken asyncToken = default)
        {

            var tasks = new List<UniTask>();

            if (Volume.weight != volumeWeight) tasks.Add(ChangeVolumeWeightAsync(volumeWeight, duration, asyncToken));
            colorGrading.tonemapper.value = tonemapperMode;

            if (colorGrading.tonemapper.value == Tonemapper.Custom)
            {
                if (colorGrading.toneCurveToeStrength.value != defaultToeStrength) tasks.Add(ChangeToeStrengthAsync(toeStrength, duration, asyncToken));
                if (colorGrading.toneCurveToeLength.value != defaultToeLength) tasks.Add(ChangeToeLengthAsync(toeLength, duration, asyncToken));
                if (colorGrading.toneCurveShoulderStrength.value != defaultShoulderStrength) tasks.Add(ChangeShoulderStrengthAsync(shoulderStrength, duration, asyncToken));
                if (colorGrading.toneCurveShoulderLength.value != defaultShoulderLength) tasks.Add(ChangeShoulderLengthAsync(shoulderLength, duration, asyncToken));
                if (colorGrading.toneCurveShoulderAngle.value != defaultShoulderAngle) tasks.Add(ChangeShoulderAngleAsync(shoulderAngle, duration, asyncToken));
                if (colorGrading.toneCurveGamma.value != defaultToneGamma) tasks.Add(ChangeToneGammaAsync(toneGamma, duration, asyncToken));
            }

            if (colorGrading.temperature.value != temperature) tasks.Add(ChangeTemperatureAsync(temperature, duration, asyncToken));
            if (colorGrading.tint.value != tint) tasks.Add(ChangeTintAsync(tint, duration, asyncToken));
            if (colorGrading.postExposure.value != postExposure) tasks.Add(ChangePostExposureAsync(postExposure, duration, asyncToken));
            if (colorGrading.colorFilter.value != colorFilter) tasks.Add(ChangeColorFilterAsync(colorFilter, duration, asyncToken));
            if (colorGrading.hueShift.value != hueShift) tasks.Add(ChangeHueShiftAsync(hueShift, duration, asyncToken));
            if (colorGrading.saturation.value != saturation) tasks.Add(ChangeSaturationAsync(saturation, duration, asyncToken));
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
            if(postExposureTweener.Running) postExposureTweener.CompleteInstantly();
            if(colorFilterTweener.Running) colorFilterTweener.CompleteInstantly();
            if(hueShiftTweener.Running) hueShiftTweener.CompleteInstantly();
            if(saturationTweener.Running) saturationTweener.CompleteInstantly();
            if(contrastTweener.Running) contrastTweener.CompleteInstantly();
            if(redChannelTweener.Running) redChannelTweener.CompleteInstantly();
            if(greenChannelTweener.Running) greenChannelTweener.CompleteInstantly();
            if(blueChannelTweener.Running) blueChannelTweener.CompleteInstantly();
            if(liftTweener.Running) liftTweener.CompleteInstantly();
            if(gammaTweener.Running) gammaTweener.CompleteInstantly();
            if(gainTweener.Running) gainTweener.CompleteInstantly();
            if(toeStrengthTweener.Running) toeStrengthTweener.CompleteInstantly();
            if(toeLengthTweener.Running) toeLengthTweener.CompleteInstantly();
            if(shoulderStrengthTweener.Running) shoulderStrengthTweener.CompleteInstantly();
            if(shoulderLengthTweener.Running) shoulderLengthTweener.CompleteInstantly();
            if(shoulderAngleTweener.Running) shoulderAngleTweener.CompleteInstantly();
            if(toneGammaTweener.Running) toneGammaTweener.CompleteInstantly();
        }

        protected override void Awake()
        {
            base.Awake();
            colorGrading = Volume.profile.GetSetting<UnityEngine.Rendering.PostProcessing.ColorGrading>() ?? Volume.profile.AddSettings<UnityEngine.Rendering.PostProcessing.ColorGrading>();
            colorGrading.SetAllOverridesTo(true);
            colorGrading.gradingMode.value = GradingMode.HighDefinitionRange;
        }

        private async UniTask ChangeToeStrengthAsync(float toeStrength, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await toeStrengthTweener.RunAsync(new FloatTween(colorGrading.toneCurveToeStrength.value, toeStrength, duration, x => colorGrading.toneCurveToeStrength.value = x), asyncToken, colorGrading);
            else colorGrading.toneCurveToeStrength.value = toeStrength;
        }
        private async UniTask ChangeToeLengthAsync(float toeLength, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await toeLengthTweener.RunAsync(new FloatTween(colorGrading.toneCurveToeLength.value, toeLength, duration, x => colorGrading.toneCurveToeLength.value = x), asyncToken, colorGrading);
            else colorGrading.toneCurveToeLength.value = toeLength;
        }
        private async UniTask ChangeShoulderStrengthAsync(float shoulderStrength, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await shoulderStrengthTweener.RunAsync(new FloatTween(colorGrading.toneCurveShoulderStrength.value, shoulderStrength, duration, x => colorGrading.toneCurveShoulderStrength.value = x), asyncToken, colorGrading);
            else colorGrading.toneCurveShoulderStrength.value = shoulderStrength;
        }
        private async UniTask ChangeShoulderLengthAsync(float shoulderLength, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await shoulderLengthTweener.RunAsync(new FloatTween(colorGrading.toneCurveShoulderLength.value, shoulderLength, duration, x => colorGrading.toneCurveShoulderLength.value = x), asyncToken, colorGrading);
            else colorGrading.toneCurveShoulderLength.value = shoulderLength;
        }
        private async UniTask ChangeShoulderAngleAsync(float shoulderAngle, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await shoulderAngleTweener.RunAsync(new FloatTween(colorGrading.toneCurveShoulderAngle.value, shoulderAngle, duration, x => colorGrading.toneCurveShoulderAngle.value = x), asyncToken, colorGrading);
            else colorGrading.toneCurveShoulderAngle.value = shoulderAngle;
        }
        private async UniTask ChangeToneGammaAsync(float toneGamma, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await toneGammaTweener.RunAsync(new FloatTween(colorGrading.toneCurveGamma.value, toneGamma, duration, x => colorGrading.toneCurveGamma.value = x), asyncToken, colorGrading);
            else colorGrading.toneCurveGamma.value = toneGamma;
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
        private async UniTask ChangePostExposureAsync(float postExposure, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await postExposureTweener.RunAsync(new FloatTween(colorGrading.postExposure.value, postExposure, duration, x => colorGrading.postExposure.value = x), asyncToken, colorGrading);
            else colorGrading.postExposure.value = postExposure;
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

#if UNITY_EDITOR && NANINOVEL_SCENE_ASSISTANT_AVAILABLE
        public override List<ICommandParameterData> GetParams()
        {
            return new List<ICommandParameterData>()
            {
                { new CommandParameterData<float>("Time", () => Duration, v => Duration = v, (i,p) => i.FloatField(p), defaultSpawnDuration)},
                { new CommandParameterData<float>("Weight", () => Volume.weight, v => Volume.weight = v, (i,p) => i.FloatSliderField(p, 0f, 1f), defaultVolumeWeight)},
                { new CommandParameterData<Enum>("TonemapperMode", () => colorGrading.tonemapper.value, v => colorGrading.tonemapper.value = (Tonemapper)v, (i,p) => i.EnumField(p), defaultTonemapperMode)},
                
                { new CommandParameterData<float>("ToeStrength", () => colorGrading.toneCurveToeStrength.value, v => colorGrading.toneCurveToeStrength.value = v, (i,p) => i.FloatSliderField(p, 0f, 1f), defaultValue:defaultToeStrength, getCondition: () => colorGrading.tonemapper.value == Tonemapper.Custom) },
                { new CommandParameterData<float>("ToeLength", () => colorGrading.toneCurveToeLength.value, v => colorGrading.toneCurveToeLength.value = v, (i,p) => i.FloatSliderField(p, 0f, 1f), defaultValue:defaultToeLength, getCondition:() => colorGrading.tonemapper.value == Tonemapper.Custom) },
                { new CommandParameterData<float>("ShoulderStrength", () => colorGrading.toneCurveShoulderStrength.value, v => colorGrading.toneCurveShoulderStrength.value = v, (i,p) => i.FloatSliderField(p, 0f, 1f), defaultValue:defaultShoulderStrength, getCondition: () => colorGrading.tonemapper.value == Tonemapper.Custom) },
                { new CommandParameterData<float>("ShoulderLength", () => colorGrading.toneCurveShoulderLength.value, v => colorGrading.toneCurveShoulderLength.value = v, (i,p) => i.FloatField(p, min:0f), defaultValue:defaultShoulderLength, getCondition:() => colorGrading.tonemapper.value == Tonemapper.Custom) },
                { new CommandParameterData<float>("ShoulderAngle", () => colorGrading.toneCurveShoulderAngle.value, v => colorGrading.toneCurveShoulderAngle.value = v, (i,p) => i.FloatField(p, 0.01f), defaultValue:defaultShoulderAngle, getCondition: () => colorGrading.tonemapper.value == Tonemapper.Custom) },

                { new CommandParameterData<float>("Temperature", () => colorGrading.temperature.value, v => colorGrading.temperature.value = v, (i,p) => i.FloatField(p), defaultTemperature)},
                { new CommandParameterData<float>("Tint", () => colorGrading.tint.value, v => colorGrading.tint.value = v, (i,p) => i.FloatField(p), defaultTint)},
                { new CommandParameterData<float>("PostExposure", () => colorGrading.postExposure.value, v => colorGrading.postExposure.value = v, (i,p) => i.FloatField(p), defaultPostExposure)},
                { new CommandParameterData<Color>("ColorFilter", () => colorGrading.colorFilter.value, v => colorGrading.colorFilter.value = (Color)v, (i,p) => i.ColorField(p), defaultColorFilter)},
                { new CommandParameterData<float>("HueShift", () => colorGrading.hueShift.value, v => colorGrading.hueShift.value = v, (i,p) => i.FloatField(p), defaultHueShift)},
                { new CommandParameterData<float>("Saturation", () => colorGrading.saturation.value, v => colorGrading.saturation.value = v, (i,p) => i.FloatField(p), defaultSaturation)},
                { new CommandParameterData<float>("Contrast", () => colorGrading.contrast.value, v => colorGrading.contrast.value = v, (i,p) => i.FloatField(p), defaultContrast)},
                { new CommandParameterData<Vector3>("RedChannel", () => GetRedChannel(), v => ApplyRedChannel(v), (i,p) => i.Vector3Field(p), defaultRedChannel)},
                { new CommandParameterData<Vector3>("GreenChannel", () => GetGreenChannel(), v => ApplyGreenChannel(v), (i,p) => i.Vector3Field(p), defaultGreenChannel)},
                { new CommandParameterData<Vector3>("BlueChannel", () => GetBlueChannel(), v => ApplyBlueChannel(v), (i,p) => i.Vector3Field(p), defaultBlueChannel)},
                { new CommandParameterData<Vector4>("Lift", () => colorGrading.lift.value, v => colorGrading.lift.value = v, (i,p) => i.Vector4Field(p), defaultLift)},
                { new CommandParameterData<Vector4>("Gamma", () => colorGrading.gamma.value, v => colorGrading.gamma.value = v, (i,p) => i.Vector4Field(p), defaultGamma)},
                { new CommandParameterData<Vector4>("Gain", () => colorGrading.gain.value, v => colorGrading.gain.value = v, (i,p) => i.Vector4Field(p), defaultGain)},
            };
        }
#endif
    }

#if UNITY_EDITOR && NANINOVEL_SCENE_ASSISTANT_AVAILABLE
    [CustomEditor(typeof(ColorGradingHDR))]
    public class ColorGradingHDREditor : SpawnObjectEditor { }
#endif

}

#endif