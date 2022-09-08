//2022 idaocracy

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

namespace NaninovelPostProcessFX { 

    [RequireComponent(typeof(PostProcessVolume))]
    public class ColorGrading : MonoBehaviour, Spawn.IParameterized, Spawn.IAwaitable, DestroySpawned.IParameterized, DestroySpawned.IAwaitable
    {
        protected string Mode { get; private set; }

        //Non-external parameters
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

        //External parameters

        protected string LookUpTexture { get; private set; }

        protected float VolumeWeight { get; private set; }
        protected float Duration { get; private set; }
        protected float FadeOutDuration { get; private set; }

        public bool logResult;

        private readonly Tweener<FloatTween> volumeWeightTweener = new Tweener<FloatTween>();
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

        [SerializeField] private float defaultDuration = 0.35f;
        [SerializeField] private float defaultVolumeWeight = 1f;
        [SerializeField] private string defaultMode = "Low Definition Range";
        [SerializeField] private float defaultTemperature = 0f;
        [SerializeField] private float defaultTint = 0f;
        [SerializeField] private float defaultPostExposure = 0f;
        [SerializeField] private Color defaultColorFilter = Color.white;
        [SerializeField] private float defaultHueShift = 0f;
        [SerializeField] private float defaultSaturation = 0f;
        [SerializeField] private float defaultContrast = 0f;
        [SerializeField] private Vector3 defaultRedChannel = new Vector3(100, 0, 0);
        [SerializeField] private Vector3 defaultGreenChannel = new Vector3(0, 100, 0);
        [SerializeField] private Vector3 defaultBlueChannel = new Vector3(0, 0, 100);
        [SerializeField] private Vector4 defaultLift = new Vector4(1f, 1f, 1f, 0f);
        [SerializeField] private Vector4 defaultGamma = new Vector4(1f, 1f, 1f, 0f);
        [SerializeField] private Vector4 defaultGain = new Vector4(1f, 1f, 1f, 0f);

        [SerializeField] private string defaultLookUpTexture = "None";

        [SerializeField] private float defaultFadeOutDuration = 0.35f;

        private PostProcessVolume volume;
        private UnityEngine.Rendering.PostProcessing.ColorGrading colorGrading;

        [SerializeField] private List<Texture> lookUpTextures = new List<Texture>();
        private List<string> lookUpTextureIds = new List<string>();

        public virtual void SetSpawnParameters(IReadOnlyList<string> parameters, bool asap)
        {
            Duration = asap ? 0 : Mathf.Abs(parameters?.ElementAtOrDefault(0)?.AsInvariantFloat() ?? defaultDuration);

            VolumeWeight = parameters?.ElementAtOrDefault(1)?.AsInvariantFloat() ?? defaultVolumeWeight;
            Mode = parameters?.ElementAtOrDefault(2)?.ToString() ?? defaultMode;

            if (Mode != "External") 
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
            else
            {
                LookUpTexture = parameters?.ElementAtOrDefault(3) ?? defaultLookUpTexture;
            }
        }

        public async UniTask AwaitSpawnAsync(AsyncToken asyncToken = default)
        {
            if (volumeWeightTweener.Running) volumeWeightTweener.CompleteInstantly();

            var duration = asyncToken.Completed ? 0 : Duration;
            if (Mode != "External") await ChangeColorGradingAsync(duration, VolumeWeight, Mode, Temperature, Tint, PostExposure, ColorFilter, HueShift, Saturation, Contrast, RedChannel, GreenChannel, BlueChannel, Lift, Gamma, Gain, asyncToken);
            else await ChangeLookUpTextureAsync(duration, VolumeWeight, Mode, LookUpTexture, asyncToken);
        }

        public async UniTask ChangeColorGradingAsync(float duration, float volumeWeight, string mode, float temperature, float tint,
                                                    float postExposure, Color colorFilter, float hueShift, float saturation, float contrast,
                                                    Vector3 redChannel, Vector3 blueChannel, Vector3 greenChannel,
                                                    Vector4 lift, Vector4 gamma, Vector4 gain,
                                                    AsyncToken asyncToken = default)
        {

            var tasks = new List<UniTask>();
            if (volume.weight != volumeWeight) tasks.Add(ChangeVolumeWeightAsync(volumeWeight, duration, asyncToken));
            colorGrading.gradingMode.value = (GradingMode)System.Enum.Parse(typeof(GradingMode), mode.Replace(" ", ""));
            if (colorGrading.temperature.value != temperature) tasks.Add(ChangeTemperatureAsync(temperature, duration, asyncToken));
            if (colorGrading.tint.value != tint) tasks.Add(ChangeTintAsync(tint, duration, asyncToken));
            if (colorGrading.postExposure.value != postExposure) tasks.Add(ChangePostExposureAsync(postExposure, duration, asyncToken));
            if (colorGrading.colorFilter.value != colorFilter) tasks.Add(ChangeColorFilterAsync(colorFilter, duration, asyncToken));
            if (colorGrading.hueShift.value != hueShift) tasks.Add(ChangeHueShiftAsync(hueShift, duration, asyncToken));
            if (colorGrading.saturation.value != saturation) tasks.Add(ChangeSaturationAsync(saturation, duration, asyncToken));
            if (colorGrading.contrast.value != tint) tasks.Add(ChangeContrastAsync(contrast, duration, asyncToken));
            if (GetRedChannel() != redChannel) tasks.Add(ChangeRedChannelAsync(redChannel, duration, asyncToken));
            if (GetGreenChannel() != redChannel) tasks.Add(ChangeGreenChannelAsync(blueChannel, duration, asyncToken));
            if (GetBlueChannel() != redChannel) tasks.Add(ChangeBlueChannelAsync(blueChannel, duration, asyncToken));
            if (colorGrading.lift.value != lift) tasks.Add(ChangeLiftAsync(lift, duration, asyncToken));
            if (colorGrading.gamma.value != lift) tasks.Add(ChangeGammaAsync(gamma, duration, asyncToken));
            if (colorGrading.gain.value != gain) tasks.Add(ChangeGainAsync(gain, duration, asyncToken));

            await UniTask.WhenAll(tasks);
        }

        public async UniTask ChangeLookUpTextureAsync(float duration, float volumeWeight, string mode, string lookUpTexture, AsyncToken asyncToken)
        {
            var tasks = new List<UniTask>();
            if (volume.weight != volumeWeight) tasks.Add(ChangeVolumeWeightAsync(volumeWeight, duration, asyncToken));
            colorGrading.gradingMode.value = (GradingMode)System.Enum.Parse(typeof(GradingMode), mode);
            if (colorGrading.externalLut.value.ToString() != mode) ChangeTexture(lookUpTexture);

            await UniTask.WhenAll(tasks);
        }

        public void SetDestroyParameters(IReadOnlyList<string> parameters)
        {
            FadeOutDuration = parameters?.ElementAtOrDefault(0)?.AsInvariantFloat() ?? defaultFadeOutDuration;
        }

        public async UniTask AwaitDestroyAsync(AsyncToken asyncToken = default)
        {
            if (volumeWeightTweener.Running) volumeWeightTweener.CompleteInstantly();

            var duration = asyncToken.Completed ? 0 : FadeOutDuration;
            await ChangeVolumeWeightAsync(0f, duration, asyncToken);

        }

        public void OnDestroy()
        {
                volume.profile.RemoveSettings<UnityEngine.Rendering.PostProcessing.ColorGrading>();
        }

        private void Awake()
        {
            volume = GetComponent<PostProcessVolume>();
                colorGrading = volume.profile.GetSetting<UnityEngine.Rendering.PostProcessing.ColorGrading>() ?? volume.profile.AddSettings<UnityEngine.Rendering.PostProcessing.ColorGrading>();
            colorGrading.SetAllOverridesTo(true);
            volume.weight = 0f;

            lookUpTextureIds = lookUpTextures.Select(s => s.name).ToList();
            lookUpTextureIds.Insert(0, "None");
        }

        private async UniTask ChangeVolumeWeightAsync(float volumeWeight, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await volumeWeightTweener.RunAsync(new FloatTween(volume.weight, volumeWeight, duration, x => volume.weight = x), asyncToken, volume);
            else volume.weight = volumeWeight;
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
            if (duration > 0) await greenChannelTweener.RunAsync(new VectorTween(GetBlueChannel(), green, duration, ApplyBlueChannel), asyncToken, colorGrading);
            else ApplyBlueChannel(green);
        }

        private async UniTask ChangeBlueChannelAsync(Vector3 blue, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await blueChannelTweener.RunAsync(new VectorTween(GetGreenChannel(), blue, duration, ApplyChannelMixerB), asyncToken, colorGrading);
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
            if (imageId == "None" || String.IsNullOrEmpty(imageId)) return;

            foreach (var img in lookUpTextures)
            {
                if (img != null && img.name == imageId)
                {
                    colorGrading.externalLut.value = img;
                }
            }
        }

        private Vector3 GetRedChannel() => new Vector3(colorGrading.mixerRedOutRedIn.value, colorGrading.mixerRedOutGreenIn.value, colorGrading.mixerRedOutBlueIn.value);
        private Vector3 GetBlueChannel() => new Vector3(colorGrading.mixerRedOutRedIn.value, colorGrading.mixerRedOutGreenIn.value, colorGrading.mixerRedOutBlueIn.value);
        private Vector3 GetGreenChannel() => new Vector3(colorGrading.mixerRedOutRedIn.value, colorGrading.mixerRedOutGreenIn.value, colorGrading.mixerRedOutBlueIn.value);

        private void ApplyRedChannel(Vector3 red)
        {
            colorGrading.mixerRedOutRedIn.value = red.x;
            colorGrading.mixerRedOutGreenIn.value = red.y;
            colorGrading.mixerRedOutBlueIn.value = red.z;
        }

        private void ApplyBlueChannel(Vector3 green)
        {
            colorGrading.mixerGreenOutRedIn.value = green.x;
            colorGrading.mixerGreenOutGreenIn.value = green.y;
            colorGrading.mixerGreenOutBlueIn.value = green.z;
        }

        private void ApplyChannelMixerB(Vector3 blue)
        {
            colorGrading.mixerBlueOutRedIn.value = blue.x;
            colorGrading.mixerBlueOutGreenIn.value = blue.y;
            colorGrading.mixerBlueOutBlueIn.value = blue.z;
        }


        public string SceneAssistantParameters()
        {

            GUILayout.BeginHorizontal();
            Duration = EditorGUILayout.FloatField("Fade-in time", Duration, GUILayout.Width(413));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Volume Weight", GUILayout.Width(190));
            volume.weight = EditorGUILayout.Slider(volume.weight, 0f, 1f, GUILayout.Width(220));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Mode", GUILayout.Width(190));
            string[] kernelSizeArray = new string[] { "LowDefinitionRange", "HighDefinitionRange", "External" };
            var modeIndex = Array.IndexOf(kernelSizeArray, colorGrading.gradingMode.value.ToString());
            modeIndex = EditorGUILayout.Popup(modeIndex, kernelSizeArray, GUILayout.Width(220));
            colorGrading.gradingMode.value = (GradingMode)modeIndex;
            GUILayout.EndHorizontal();

            if(colorGrading.gradingMode.value.ToString() != "External") { 

                GUILayout.BeginHorizontal();
                colorGrading.temperature.value = EditorGUILayout.FloatField("Temperature", colorGrading.temperature.value, GUILayout.Width(413));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                colorGrading.tint.value = EditorGUILayout.FloatField("Tint", colorGrading.tint.value, GUILayout.Width(413));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                colorGrading.postExposure.value = EditorGUILayout.FloatField("Post Exposure", colorGrading.postExposure.value, GUILayout.Width(413));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Color", GUILayout.Width(190));
                colorGrading.colorFilter.value = EditorGUILayout.ColorField(colorGrading.colorFilter.value, GUILayout.Width(220));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                colorGrading.hueShift.value = EditorGUILayout.FloatField("Hue Shift", colorGrading.hueShift.value, GUILayout.Width(413));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                colorGrading.saturation.value = EditorGUILayout.FloatField("Saturation", colorGrading.saturation.value, GUILayout.Width(413));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                colorGrading.contrast.value = EditorGUILayout.FloatField("Contrast", colorGrading.contrast.value, GUILayout.Width(413));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Red Channel", GUILayout.Width(190));
                colorGrading.mixerRedOutRedIn.value = EditorGUILayout.FloatField(" ", colorGrading.mixerRedOutRedIn.value, GUILayout.Width(70));
                colorGrading.mixerRedOutGreenIn.value = EditorGUILayout.FloatField(" ", colorGrading.mixerRedOutGreenIn.value, GUILayout.Width(70));
                colorGrading.mixerRedOutBlueIn.value = EditorGUILayout.FloatField(" ", colorGrading.mixerRedOutBlueIn.value, GUILayout.Width(70));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Green Channel", GUILayout.Width(190));
                colorGrading.mixerGreenOutRedIn.value = EditorGUILayout.FloatField("", colorGrading.mixerGreenOutRedIn.value, GUILayout.Width(70));
                colorGrading.mixerGreenOutGreenIn.value = EditorGUILayout.FloatField("", colorGrading.mixerGreenOutGreenIn.value, GUILayout.Width(70));
                colorGrading.mixerGreenOutBlueIn.value = EditorGUILayout.FloatField("", colorGrading.mixerGreenOutBlueIn.value, GUILayout.Width(70));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Blue Channel", GUILayout.Width(190));
                colorGrading.mixerBlueOutRedIn.value = EditorGUILayout.FloatField("", colorGrading.mixerBlueOutRedIn.value, GUILayout.Width(70));
                colorGrading.mixerBlueOutGreenIn.value = EditorGUILayout.FloatField("", colorGrading.mixerBlueOutGreenIn.value, GUILayout.Width(70));
                colorGrading.mixerBlueOutBlueIn.value = EditorGUILayout.FloatField("", colorGrading.mixerBlueOutBlueIn.value, GUILayout.Width(70));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Lift", GUILayout.Width(190));
                colorGrading.lift.value = EditorGUILayout.Vector4Field("", colorGrading.lift.value, GUILayout.Width(220));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Gamma", GUILayout.Width(190));
                colorGrading.gamma.value = EditorGUILayout.Vector4Field("", colorGrading.gamma.value, GUILayout.Width(220));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Gain", GUILayout.Width(190));
                colorGrading.gain.value = EditorGUILayout.Vector4Field("", colorGrading.gain.value, GUILayout.Width(220));
                GUILayout.EndHorizontal();

            }
            else
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Lookup Texture", GUILayout.Width(190));
                string[] maskTexturesArray = lookUpTextureIds.ToArray();
                var maskIndex = Array.IndexOf(maskTexturesArray, colorGrading.externalLut.value?.name ?? "None");
                maskIndex = EditorGUILayout.Popup(maskIndex, maskTexturesArray, GUILayout.Height(20), GUILayout.Width(220));
                colorGrading.externalLut.value = lookUpTextures.FirstOrDefault(s => s.name == lookUpTextureIds[maskIndex]) ?? null;
                GUILayout.EndHorizontal();
            }

            return Duration + "," + volume.weight + "," + colorGrading.gradingMode.value + "," + colorGrading.temperature.value + "," + colorGrading.tint.value + "," + colorGrading.postExposure.value + "," +
                "#" + ColorUtility.ToHtmlStringRGBA(colorGrading.colorFilter.value) + "," + colorGrading.hueShift.value + "," + colorGrading.saturation.value + "," + colorGrading.contrast.value + "," +
                colorGrading.mixerRedOutRedIn.value + "," + colorGrading.mixerRedOutGreenIn.value + "," + colorGrading.mixerRedOutBlueIn.value + "," +
                colorGrading.mixerGreenOutRedIn.value + "," + colorGrading.mixerGreenOutGreenIn.value + "," + colorGrading.mixerGreenOutBlueIn.value + "," +
                colorGrading.mixerBlueOutRedIn.value + "," + colorGrading.mixerBlueOutGreenIn.value + "," + colorGrading.mixerBlueOutBlueIn.value + "," +
                colorGrading.lift.value.x + "," + colorGrading.lift.value.y + "," + colorGrading.lift.value.z + "," + colorGrading.lift.value.w + "," +
                colorGrading.gamma.value.x + "," + colorGrading.gamma.value.y + "," + colorGrading.gamma.value.z + "," + colorGrading.gamma.value.w + "," +
                colorGrading.gain.value.x + "," + colorGrading.gain.value.y + "," + colorGrading.gain.value.z + "," + colorGrading.gain.value.w;
        }
    }


    #if UNITY_EDITOR

    [CustomEditor(typeof(ColorGrading))]
    public class CopyFXColorGrading : Editor
    {
        private ColorGrading targetObject;
        private UnityEngine.Rendering.PostProcessing.ColorGrading colorGrading;
        private PostProcessVolume volume;
        public bool logResult;

        private void Awake()
        {
            targetObject = (ColorGrading)target;
            volume = targetObject.gameObject.GetComponent<PostProcessVolume>();
                colorGrading = volume.profile.GetSetting<UnityEngine.Rendering.PostProcessing.ColorGrading>();
            logResult = targetObject.logResult;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(20f);
            if (GUILayout.Button("Copy command and params (@)", GUILayout.Height(50)))
            {
                if (colorGrading != null) GUIUtility.systemCopyBuffer = "@spawn " + targetObject.gameObject.name + " params:" + CreateString();
                if (logResult) Debug.Log(GUIUtility.systemCopyBuffer);
            }

            GUILayout.Space(20f);
            GUILayout.Space(20f);
            if (GUILayout.Button("Copy command and params ([])", GUILayout.Height(50)))
            {
                if (colorGrading != null) GUIUtility.systemCopyBuffer = "[spawn " + targetObject.gameObject.name + " params:" + CreateString() + "]";
                if (logResult) Debug.Log(GUIUtility.systemCopyBuffer);
            }

            GUILayout.Space(20f);
            GUILayout.Space(20f);
            if (GUILayout.Button("Copy command and params ([])", GUILayout.Height(50)))
            {
                if (colorGrading != null) GUIUtility.systemCopyBuffer = CreateString();
                if (logResult) Debug.Log(GUIUtility.systemCopyBuffer);
            }

            GUILayout.Space(20f);
            if (GUILayout.Toggle(logResult, "Log Results")) targetObject.logResult = true;
            else targetObject.logResult = false;
        }

        private string CreateString() => "(time)," + "," + volume.weight + "," + colorGrading.gradingMode.value + "," + colorGrading.temperature.value + "," + colorGrading.tint.value + "," + colorGrading.postExposure.value + "," +
                "#" + ColorUtility.ToHtmlStringRGBA(colorGrading.colorFilter.value) + "," + colorGrading.hueShift.value + "," + colorGrading.saturation.value + "," + colorGrading.contrast.value + "," +
                colorGrading.mixerRedOutRedIn.value + "," + colorGrading.mixerRedOutGreenIn.value + "," + colorGrading.mixerRedOutBlueIn.value + "," +
                colorGrading.mixerGreenOutRedIn.value + "," + colorGrading.mixerGreenOutGreenIn.value + "," + colorGrading.mixerGreenOutBlueIn.value + "," +
                colorGrading.mixerBlueOutRedIn.value + "," + colorGrading.mixerBlueOutGreenIn.value + "," + colorGrading.mixerBlueOutBlueIn.value + "," +
                colorGrading.lift.value.x + "," + colorGrading.lift.value.y + "," + colorGrading.lift.value.z + "," + colorGrading.lift.value.w + "," +
                colorGrading.gamma.value.x + "," + colorGrading.gamma.value.y + "," + colorGrading.gamma.value.z + "," + colorGrading.gamma.value.w + "," +
                colorGrading.gain.value.x + "," + colorGrading.gain.value.y + "," + colorGrading.gain.value.z + "," + colorGrading.gain.value.w;
    }
    #endif

}