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

namespace NaninovelPostProcess { 

	[RequireComponent(typeof(PostProcessVolume))]
	public class ColorGradingEXT : PostProcessSpawnObject, Spawn.IParameterized, Spawn.IAwaitable, DestroySpawned.IParameterized, DestroySpawned.IAwaitable, PostProcessSpawnObject.ITextureParameterized
	{
		protected string LookUpTexture { get; private set; }

		[Header("Color Grading Settings")]
		[SerializeField] private string defaultLookUpTexture = "None";
		[SerializeField] private List<Texture> lookUpTextures = new List<Texture>();

		private UnityEngine.Rendering.PostProcessing.ColorGrading colorGrading;

		public List<Texture> TextureItems => lookUpTextures;

		public override void SetSpawnParameters(IReadOnlyList<string> parameters, bool asap)
		{
			base.SetSpawnParameters(parameters, asap);
			LookUpTexture = parameters?.ElementAtOrDefault(2) ?? defaultLookUpTexture;
			
		}
		public async UniTask AwaitSpawn(AsyncToken asyncToken = default)
		{
			CompleteTweens();
			var duration = asyncToken.Completed ? 0 : Duration;
			await ChangeColorGradingAsync(duration, VolumeWeight, LookUpTexture, asyncToken);
		}

		public async UniTask ChangeColorGradingAsync(float duration, float volumeWeight, string lookUpTexture, AsyncToken asyncToken = default)
		{
			var tasks = new List<UniTask>();

			if (Volume.weight != volumeWeight) tasks.Add(ChangeVolumeWeightAsync(volumeWeight, duration, asyncToken));
			colorGrading.externalLut.value = ChangeTexture(lookUpTexture);
			
			await UniTask.WhenAll(tasks);
		}

		protected override void CompleteTweens()
		{
			if(volumeWeightTweener.Running) volumeWeightTweener.CompleteInstantly();
		}

		protected override void Awake()
		{
			base.Awake();
			colorGrading = Volume.profile.GetSetting<UnityEngine.Rendering.PostProcessing.ColorGrading>() ?? Volume.profile.AddSettings<UnityEngine.Rendering.PostProcessing.ColorGrading>();
			colorGrading.SetAllOverridesTo(true);
			colorGrading.gradingMode.value = GradingMode.External;
		}

#if NANINOVEL_SCENE_ASSISTANT_AVAILABLE
		public override List<ICommandParameterData> GetParams()
		{
			return new List<ICommandParameterData>()
			{
				{ new CommandParameterData<float>("Time", () => Duration, v => Duration = v, (i,p) => i.FloatField(p), defaultSpawnDuration)},
				{ new CommandParameterData<float>("Weight", () => Volume.weight, v => Volume.weight = v, (i,p) => i.FloatSliderField(p, 0f, 1f), defaultVolumeWeight)},
				{ new CommandParameterData<Texture>("LookUpTexture", () => colorGrading.externalLut.value, v => colorGrading.externalLut.value = v, (i,p) => i.TypeDropdownField<Texture>(p, Textures), Textures.FirstOrDefault(t => t.Key == defaultLookUpTexture).Value) }
			};
		}
#endif

	}

#if UNITY_EDITOR && NANINOVEL_SCENE_ASSISTANT_AVAILABLE
	[CustomEditor(typeof(ColorGradingEXT))]
	public class ColorGradingEXTEditor : SpawnObjectEditor { }
#endif

}



#endif