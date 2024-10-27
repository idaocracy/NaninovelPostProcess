//2022-2023 idaocracy

#if UNITY_POST_PROCESSING_STACK_V2

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using Naninovel;
using Naninovel.Commands;
#if NANINOVEL_SCENE_ASSISTANT_AVAILABLE
using NaninovelSceneAssistant;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NaninovelPostProcess
{
    [RequireComponent(typeof(PostProcessVolume))]
    public class Bloom : PostProcessSpawnObject, Spawn.IParameterized, Spawn.IAwaitable, DestroySpawned.IParameterized, DestroySpawned.IAwaitable, PostProcessSpawnObject.ITextureParameterized
    {
        protected float Intensity { get; private set; }
        protected float Threshold { get; private set; }
        protected float SoftKnee { get; private set; }
        protected float Clamp { get; private set; }
        protected float Diffusion { get; private set; }
        protected float AnamorphicRatio { get; private set; }
        protected Color BloomColor { get; private set; }
        protected bool FastMode { get; private set; }
        protected string DirtTexture { get; private set; }
        protected float DirtIntensity { get; private set; }

        public List<Texture> TextureItems => dirtTextures;

        private readonly Tweener<FloatTween> intensityTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> thresholdTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> softKneeTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> clampTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> diffusionTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> anamorphicRatioTweener = new Tweener<FloatTween>();
        private readonly Tweener<ColorTween> tintTweener = new Tweener<ColorTween>();
        private readonly Tweener<FloatTween> dirtIntensityTweener = new Tweener<FloatTween>();

        [Header("Bloom Settings")]
        [SerializeField, UnityEngine.Min(0f)] private float defaultIntensity = 10f;
        [SerializeField, UnityEngine.Min(0f)] private float defaultThreshold = 1f;
        [SerializeField, Range (0f, 1f)] private float defaultSoftKnee = 0.5f;
        [SerializeField] private float defaultClamp = 65472f;
        [SerializeField, Range(1f, 10f)] private float defaultDiffusion = 7f;
        [SerializeField, Range(-1f, 1f)] private float defaultAnamorphicRatio = 0f;
        [SerializeField, ColorUsage(false, true)] private Color defaultColor = Color.white;
        [SerializeField] private bool defaultFastMode = false;

        [SerializeField] private string defaultDirtTextureId = string.Empty;
        [SerializeField] private List<Texture> dirtTextures = new List<Texture>();
        [SerializeField, UnityEngine.Min(0f)] private float defaultDirtIntensity = 0f;

        private UnityEngine.Rendering.PostProcessing.Bloom bloom;

        public override void SetSpawnParameters(IReadOnlyList<string> parameters, bool asap)
        {
            base.SetSpawnParameters(parameters, asap);

            Intensity = parameters?.ElementAtOrDefault(2)?.AsInvariantFloat() ?? defaultIntensity;
            Threshold = parameters?.ElementAtOrDefault(3)?.AsInvariantFloat() ?? defaultThreshold;
            SoftKnee = parameters?.ElementAtOrDefault(4)?.AsInvariantFloat() ?? defaultSoftKnee;
            Clamp = parameters?.ElementAtOrDefault(5)?.AsInvariantFloat() ?? defaultClamp;
            Diffusion = parameters?.ElementAtOrDefault(6)?.AsInvariantFloat() ?? defaultDiffusion;
            AnamorphicRatio = parameters?.ElementAtOrDefault(7)?.AsInvariantFloat() ?? defaultAnamorphicRatio;
            BloomColor = ColorUtility.TryParseHtmlString(parameters?.ElementAtOrDefault(8), out var tint) ? tint : defaultColor;
            FastMode = bool.Parse(parameters?.ElementAtOrDefault(9) ?? defaultFastMode.ToString());
            DirtTexture = parameters?.ElementAtOrDefault(10) ?? defaultDirtTextureId;
            DirtIntensity = parameters?.ElementAtOrDefault(11)?.AsInvariantFloat() ?? defaultDirtIntensity;
        }

        public async UniTask AwaitSpawn(AsyncToken asyncToken = default)
        {
            CompleteTweens();
            var duration = asyncToken.Completed ? 0 : Duration;
            await ChangeBloomAsync(duration, VolumeWeight, Intensity, Threshold,  SoftKnee, Clamp, Diffusion, AnamorphicRatio, BloomColor, FastMode, DirtTexture, DirtIntensity, asyncToken);
        }

        public async UniTask ChangeBloomAsync(float duration, float volumeWeight, float intensity, float threshold, float softKnee, float clamp, float diffusion, float anamorphicRatio, Color tint, bool fastMode, string dirtTexture, float dirtIntensity, AsyncToken asyncToken = default)
        {
            var tasks = new List<UniTask>();
            if (Volume.weight != volumeWeight) tasks.Add(ChangeVolumeWeightAsync(volumeWeight, duration, asyncToken));
            if (bloom.intensity.value != intensity) tasks.Add(ChangeIntensityAsync(intensity, duration, asyncToken));
            if (bloom.threshold.value != threshold) tasks.Add(ChangeThresholdAsync(threshold, duration, asyncToken));
            if (bloom.softKnee.value != softKnee) tasks.Add(ChangeSoftKneeAsync(softKnee, duration, asyncToken));
            if (bloom.clamp.value != clamp) tasks.Add(ChangeClampAsync(clamp, duration, asyncToken));
            if (bloom.diffusion.value != diffusion) tasks.Add(ChangeDiffusionAsync(diffusion, duration, asyncToken));
            if (bloom.anamorphicRatio.value != anamorphicRatio) tasks.Add(ChangeAnamorphicRatioAsync(anamorphicRatio, duration, asyncToken));
            if (bloom.color.value != tint) tasks.Add(ChangeTintAsync(tint, duration, asyncToken));
            bloom.dirtTexture.value = ChangeTexture(dirtTexture);
            if (bloom.dirtIntensity.value != dirtIntensity) tasks.Add(ChangeDirtIntensityAsync(dirtIntensity, duration, asyncToken));
            bloom.fastMode.value = FastMode;

            await UniTask.WhenAll(tasks);
        }

        protected override void CompleteTweens()
        {
            if (volumeWeightTweener.Running) volumeWeightTweener.CompleteInstantly();
            if (intensityTweener.Running) intensityTweener.CompleteInstantly();
            if (thresholdTweener.Running) thresholdTweener.CompleteInstantly();
            if (softKneeTweener.Running) softKneeTweener.CompleteInstantly();
            if (clampTweener.Running) clampTweener.CompleteInstantly();
            if (diffusionTweener.Running) diffusionTweener.CompleteInstantly();
            if (anamorphicRatioTweener.Running) anamorphicRatioTweener.CompleteInstantly();
            if (tintTweener.Running) tintTweener.CompleteInstantly();
            if (dirtIntensityTweener.Running) dirtIntensityTweener.CompleteInstantly();
        }

        protected override void Awake()
        {
            base.Awake();
            bloom = Volume.profile.GetSetting<UnityEngine.Rendering.PostProcessing.Bloom>() ?? Volume.profile.AddSettings<UnityEngine.Rendering.PostProcessing.Bloom>();
            bloom.SetAllOverridesTo(true);
        }

        private async UniTask ChangeIntensityAsync(float intensity, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await intensityTweener.RunAwaitable(new FloatTween(bloom.intensity.value, intensity, new(duration), x => bloom.intensity.value = x/*, IgnoreTimescale*/), asyncToken, bloom);
            else bloom.intensity.value = intensity;
        }
        private async UniTask ChangeThresholdAsync(float threshold, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await thresholdTweener.RunAwaitable(new FloatTween(bloom.threshold.value, threshold, new(duration), x => bloom.threshold.value = x/*, IgnoreTimescale*/), asyncToken, bloom);
            else bloom.threshold.value = threshold;
        }
        private async UniTask ChangeSoftKneeAsync(float softKnee, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await softKneeTweener.RunAwaitable(new FloatTween(bloom.softKnee.value, softKnee, new(duration), x => bloom.softKnee.value = x/*, IgnoreTimescale*/), asyncToken, bloom);
            else bloom.softKnee.value = softKnee;
        }
        private async UniTask ChangeClampAsync(float clamp, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await clampTweener.RunAwaitable(new FloatTween(bloom.clamp.value, clamp, new(duration), x => bloom.clamp.value = x/*, IgnoreTimescale*/), asyncToken, bloom);
            else bloom.clamp.value = clamp;
        }
        private async UniTask ChangeDiffusionAsync(float diffusion, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await diffusionTweener.RunAwaitable(new FloatTween(bloom.diffusion.value, diffusion, new(duration), x => bloom.diffusion.value = x/*, IgnoreTimescale*/), asyncToken, bloom);
            else bloom.diffusion.value = diffusion;
        }
        private async UniTask ChangeAnamorphicRatioAsync(float anamorphicRatio, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await anamorphicRatioTweener.RunAwaitable(new FloatTween(bloom.anamorphicRatio.value, anamorphicRatio, new(duration), x => bloom.anamorphicRatio.value = x/*, IgnoreTimescale*/), asyncToken, bloom);
            else bloom.anamorphicRatio.value = anamorphicRatio;
        }
        private async UniTask ChangeTintAsync(Color tint, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await tintTweener.RunAwaitable(new ColorTween(bloom.color.value, tint, new(duration), ColorTweenMode.All, x => bloom.color.value = x/*, IgnoreTimescale*/), asyncToken, bloom);
            else bloom.color.value = tint;
        }
        private async UniTask ChangeDirtIntensityAsync(float dirtIntensity, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await dirtIntensityTweener.RunAwaitable(new FloatTween(bloom.dirtIntensity.value, dirtIntensity, new(duration), x => bloom.dirtIntensity.value = x/*, IgnoreTimescale*/), asyncToken, bloom);
            else bloom.dirtIntensity.value = dirtIntensity;
        }
        
#if NANINOVEL_SCENE_ASSISTANT_AVAILABLE

        public override List<ICommandParameterData> GetParams()
        {
            return new List<ICommandParameterData>()
            {
                { new CommandParameterData<float>("Time", () => Duration, v => Duration = v, (i,p) => i.FloatField(p), defaultSpawnDuration)},
                { new CommandParameterData<float>("Weight", () => Volume.weight, v => Volume.weight = v, (i,p) => i.FloatSliderField(p, 0f, 1f), defaultVolumeWeight)},
                { new CommandParameterData<float>("Intensity", () => bloom.intensity.value, v => bloom.intensity.value = v, (i,p) => i.FloatField(p, min:0f), defaultIntensity)},
                { new CommandParameterData<float>("Threshold", () => bloom.threshold.value, v => bloom.threshold.value = v, (i,p) => i.FloatField(p, min:0f), defaultThreshold)},
                { new CommandParameterData<float>("SoftKnee", () => bloom.softKnee.value, v => bloom.softKnee.value = v, (i,p) => i.FloatSliderField(p, 0f, 1f), defaultSoftKnee)},
                { new CommandParameterData<float>("Clamp", () => bloom.clamp.value, v => bloom.clamp.value = v, (i,p) => i.FloatField(p), defaultClamp)},
                { new CommandParameterData<float>("Diffusion", () => bloom.diffusion.value, v => bloom.diffusion.value = v, (i,p) => i.FloatSliderField(p, 1f, 10f), defaultDiffusion)},
                { new CommandParameterData<float>("AnamorphicRatio", () => bloom.anamorphicRatio.value, v => bloom.anamorphicRatio.value = v, (i,p) => i.FloatSliderField(p, -1f, 1f), defaultAnamorphicRatio)},
                { new CommandParameterData<Color>("Color", () => bloom.color.value, v => bloom.color.value = (Color)v, (i,p) => i.ColorField(p), defaultColor)},
                { new CommandParameterData<bool>("FastMode", () => bloom.fastMode.value, v => bloom.fastMode.value = (bool)v, (i,p) => i.BoolField(p), defaultFastMode)},
                { new CommandParameterData<Texture>("DirtTexture", () => bloom.dirtTexture.value, v => bloom.dirtTexture.value = (Texture)v, (i,p) => i.TypeDropdownField(p, Textures), dirtTextures.FirstOrDefault(t => t.name == defaultDirtTextureId))},
                { new CommandParameterData<float>("DirtIntensity", () => bloom.dirtIntensity.value, v => bloom.dirtIntensity.value = v, (i,p) => i.FloatField(p), defaultDirtIntensity)}
            };
        }

#endif
    }

    #if UNITY_EDITOR && NANINOVEL_SCENE_ASSISTANT_AVAILABLE
    [CustomEditor(typeof(Bloom))]
    public class BloomEditor : SpawnObjectEditor { }
    #endif

}

#endif