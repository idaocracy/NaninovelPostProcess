//2022 idaocracy

#if UNITY_POST_PROCESSING_STACK_V2

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using Naninovel;
using Naninovel.Commands;
using Codice.CM.Client.Differences.Graphic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NaninovelPostProcess
{
    [RequireComponent(typeof(PostProcessVolume))]
    public class Bloom : PostProcessObject, Spawn.IParameterized, Spawn.IAwaitable, DestroySpawned.IParameterized, DestroySpawned.IAwaitable, PostProcessObject.ISceneAssistant
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
        protected float VolumeWeight { get; private set; }
        protected float Duration { get; private set; }
        protected float FadeOutDuration { get; private set; }

        private readonly Tweener<FloatTween> volumeWeightTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> intensityTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> thresholdTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> softKneeTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> clampTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> diffusionTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> anamorphicRatioTweener = new Tweener<FloatTween>();
        private readonly Tweener<ColorTween> tintTweener = new Tweener<ColorTween>();
        private readonly Tweener<FloatTween> dirtIntensityTweener = new Tweener<FloatTween>();

        [Header("Spawn/Fadein Settings")]
        [SerializeField] private float defaultDuration = 0.35f;

        [Header("Volume Settings")]
        [SerializeField] private float defaultVolumeWeight = 1f;

        [Header("Bloom Settings")]
        [SerializeField] private float defaultIntensity = 10f;
        [SerializeField] private float defaultThreshold = 1f;
        [SerializeField] private float defaultSoftKnee = 0.5f;
        [SerializeField] private float defaultClamp = 65472f;
        [SerializeField] private float defaultDiffusion = 7f;
        [SerializeField] private float defaultAnamorphicRatio = 0f;
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private bool defaultFastMode= false;

        [SerializeField] private string defaultDirtTextureId = string.Empty;
        [SerializeField] private List<Texture> dirtTextures = new List<Texture>();
        [SerializeField] private float defaultDirtIntensity = 0f;

        [Header("Despawn/Fadeout Settings")]
        [SerializeField] private float defaultFadeOutDuration = 0.35f;

        private PostProcessVolume volume;
        private UnityEngine.Rendering.PostProcessing.Bloom bloom;

        private List<string> dirtTextureIds = new List<string>();

        public virtual void SetSpawnParameters(IReadOnlyList<string> parameters, bool asap)
        {
            Duration = asap ? 0 : Mathf.Abs(parameters?.ElementAtOrDefault(0)?.AsInvariantFloat() ?? defaultDuration);

            VolumeWeight = parameters?.ElementAtOrDefault(1)?.AsInvariantFloat() ?? defaultVolumeWeight;
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

        public async UniTask AwaitSpawnAsync(AsyncToken asyncToken = default)
        {
            CompleteTweens();
            var duration = asyncToken.Completed ? 0 : Duration;
            await ChangeBloomAsync(duration, VolumeWeight, Intensity, Threshold,  SoftKnee, Clamp, Diffusion, AnamorphicRatio, BloomColor, FastMode, DirtTexture, DirtIntensity, asyncToken);
        }

        public async UniTask ChangeBloomAsync(float duration, float volumeWeight, float intensity, float threshold, float softKnee, float clamp, float diffusion, float anamorphicRatio, Color tint, bool fastMode, string dirtTexture, float dirtIntensity, AsyncToken asyncToken = default)
        {
            var tasks = new List<UniTask>();
            if (volume.weight != volumeWeight) tasks.Add(ChangeVolumeWeightAsync(volumeWeight, duration, asyncToken));
            if (bloom.intensity.value != intensity) tasks.Add(ChangeIntensityAsync(intensity, duration, asyncToken));
            if (bloom.threshold.value != threshold) tasks.Add(ChangeThresholdAsync(threshold, duration, asyncToken));
            if (bloom.softKnee.value != softKnee) tasks.Add(ChangeSoftKneeAsync(softKnee, duration, asyncToken));
            if (bloom.clamp.value != clamp) tasks.Add(ChangeClampAsync(clamp, duration, asyncToken));
            if (bloom.diffusion.value != diffusion) tasks.Add(ChangeDiffusionAsync(diffusion, duration, asyncToken));
            if (bloom.anamorphicRatio.value != anamorphicRatio) tasks.Add(ChangeAnamorphicRatioAsync(anamorphicRatio, duration, asyncToken));
            if (bloom.color.value != tint) tasks.Add(ChangeTintAsync(tint, duration, asyncToken));
            if (bloom.dirtTexture.value != null && bloom.dirtTexture.value.name != dirtTexture) ChangeTexture(dirtTexture);
            if (bloom.dirtIntensity.value != dirtIntensity) tasks.Add(ChangeDirtIntensityAsync(dirtIntensity, duration, asyncToken));
            bloom.fastMode.value = FastMode;

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
            if (intensityTweener.Running) intensityTweener.CompleteInstantly();
            if (thresholdTweener.Running) thresholdTweener.CompleteInstantly();
            if (softKneeTweener.Running) softKneeTweener.CompleteInstantly();
            if (clampTweener.Running) clampTweener.CompleteInstantly();
            if (diffusionTweener.Running) diffusionTweener.CompleteInstantly();
            if (anamorphicRatioTweener.Running) anamorphicRatioTweener.CompleteInstantly();
            if (tintTweener.Running) tintTweener.CompleteInstantly();
            if (dirtIntensityTweener.Running) dirtIntensityTweener.CompleteInstantly();
        }

        private void Awake()
        {
            volume = GetComponent<PostProcessVolume>();
            bloom = volume.profile.GetSetting<UnityEngine.Rendering.PostProcessing.Bloom>() ?? volume.profile.AddSettings<UnityEngine.Rendering.PostProcessing.Bloom>();
            bloom.SetAllOverridesTo(true);
            volume.weight = 0f;

            dirtTextureIds = dirtTextures.Select(s => s.name).ToList();
            dirtTextureIds.Insert(0, "None");

        }

        private async UniTask ChangeVolumeWeightAsync(float volumeWeight, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await volumeWeightTweener.RunAsync(new FloatTween(volume.weight, volumeWeight, duration, x => volume.weight = x), asyncToken, volume);
            else volume.weight = volumeWeight;
        }
        private async UniTask ChangeIntensityAsync(float intensity, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await intensityTweener.RunAsync(new FloatTween(bloom.intensity.value, intensity, duration, x => bloom.intensity.value = x), asyncToken, bloom);
            else bloom.intensity.value = intensity;
        }
        private async UniTask ChangeThresholdAsync(float threshold, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await thresholdTweener.RunAsync(new FloatTween(bloom.threshold.value, threshold, duration, x => bloom.threshold.value = x), asyncToken, bloom);
            else bloom.threshold.value = threshold;
        }

        private async UniTask ChangeSoftKneeAsync(float softKnee, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await softKneeTweener.RunAsync(new FloatTween(bloom.softKnee.value, softKnee, duration, x => bloom.softKnee.value = x), asyncToken, bloom);
            else bloom.softKnee.value = softKnee;
        }
        private async UniTask ChangeClampAsync(float clamp, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await clampTweener.RunAsync(new FloatTween(bloom.clamp.value, clamp, duration, x => bloom.clamp.value = x), asyncToken, bloom);
            else bloom.clamp.value = clamp;
        }
        private async UniTask ChangeDiffusionAsync(float diffusion, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await diffusionTweener.RunAsync(new FloatTween(bloom.diffusion.value, diffusion, duration, x => bloom.diffusion.value = x), asyncToken, bloom);
            else bloom.diffusion.value = diffusion;
        }
        private async UniTask ChangeAnamorphicRatioAsync(float anamorphicRatio, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await anamorphicRatioTweener.RunAsync(new FloatTween(bloom.anamorphicRatio.value, anamorphicRatio, duration, x => bloom.anamorphicRatio.value = x), asyncToken, bloom);
            else bloom.anamorphicRatio.value = anamorphicRatio;
        }
        private async UniTask ChangeTintAsync(Color tint, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await tintTweener.RunAsync(new ColorTween(bloom.color.value, tint, ColorTweenMode.All, duration, x => bloom.color.value = x), asyncToken, bloom);
            else bloom.color.value = tint;
        }
        private async UniTask ChangeDirtIntensityAsync(float dirtIntensity, float duration, AsyncToken asyncToken = default)
        {
            if (duration > 0) await dirtIntensityTweener.RunAsync(new FloatTween(bloom.dirtIntensity.value, dirtIntensity, duration, x => bloom.dirtIntensity.value = x), asyncToken, bloom);
            else bloom.dirtIntensity.value = dirtIntensity;
        }

        private void ChangeTexture(string imageId)
        {
            if (imageId == "None" || String.IsNullOrEmpty(imageId)) bloom.dirtTexture.value = null;
            else
            {
                foreach (var img in dirtTextures)
                {
                    if (img != null && img.name == imageId)
                    {
                        bloom.dirtTexture.value = img;
                    }
                }
            }
        }

#if UNITY_EDITOR

        public string SceneAssistantParameters()
        {
            EditorGUIUtility.labelWidth = 190;

            Duration = FloatField("Duration", Duration);
            volume.weight = SliderField("Volume Weight", volume.weight, 0f, 1f);
            bloom.intensity.value = FloatField("Intensity", bloom.intensity.value);
            bloom.threshold.value = FloatField("Threshold", bloom.threshold.value);
            bloom.softKnee.value = SliderField("Soft Knee", bloom.softKnee.value, 0f, 1f);
            bloom.clamp.value = FloatField("Clamp", bloom.clamp.value);
            bloom.diffusion.value = SliderField("Diffusion", bloom.diffusion.value, 1f, 10f);
            bloom.anamorphicRatio.value = SliderField("Anamorphic Ratio", bloom.anamorphicRatio.value, -1f, 1f);
            bloom.color.value = ColorField("Color", bloom.color.value);
            bloom.fastMode.value = BooleanField("Fast Mode", bloom.fastMode.value);
            bloom.dirtTexture.value = TextureField("Dirt Texture", bloom.dirtTexture.value, dirtTextures, dirtTextureIds) ?? null;
            bloom.dirtIntensity.value = EditorGUILayout.FloatField("Dirt Intensity", bloom.dirtIntensity.value);

            return base.GetSpawnString();
        }

        public IReadOnlyDictionary<string, string> ParameterList()
        {
            return new Dictionary<string, string>()
            {
                { "time", Duration.ToString()},
                { "weight", volume.weight.ToString()},
                { "intensity", bloom.intensity.value.ToString()},
                { "threshold", bloom.threshold.value.ToString()},
                { "softKnee", bloom.softKnee.value.ToString()},
                { "clamp", bloom.clamp.value.ToString()},
                { "diffusion", bloom.diffusion.value.ToString()},
                { "anamorphicRatio", bloom.diffusion.value.ToString()},
                { "color", "#" + ColorUtility.ToHtmlStringRGBA(bloom.color.value)},
                { "fastMode", bloom.fastMode.value.ToString().ToLower()},
                { "dirtTexture", bloom.dirtTexture.value?.name},
                { "dirtIntensity", bloom.dirtIntensity.value.ToString()}
            };
        }
#endif
    }


#if UNITY_EDITOR

    [CustomEditor(typeof(Bloom))]
    public class CopyFXBloom : PostProcessObjectEditor
    {
        protected override string label => "bloom";
    }

#endif

}

#endif