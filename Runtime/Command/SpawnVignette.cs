using Naninovel;
using UnityEngine;
using Naninovel.Commands;
using System.Collections.Generic;

namespace NaninovelPostProcess
{
    [Alias("Vignette")]
    public class SpawnVignette : SpawnPostProcessing
    {
        [Alias("time")]
        public DecimalParameter FadeDuration;
        public DecimalParameter Weight;
        public StringParameter ClassicOrMask;

        //Classic parameters
        public StringParameter Color;
        public DecimalListParameter Center;
        public DecimalParameter Intensity;
        public DecimalParameter Smoothness;
        public DecimalParameter Roundness;
        public BooleanParameter Rounded;

        //Mask parameters
        public StringParameter MaskTexture;
        public StringParameter MaskOpacity;
        protected override bool DestroyWhen => Assigned(Weight) && Weight == 0;
        protected override string PostProcessName => "Vignette";

        protected override StringListParameter GetSpawnParameters() => 
            new List<string> {
            ToSpawnParam(FadeDuration),
            ToSpawnParam(Weight),
            ToSpawnParam(ClassicOrMask),
            ToSpawnParam(Color),
            ToSpawnParam(Assigned(Center) ? Center[0].ToString() : string.Empty),
            ToSpawnParam(Assigned(Center) ? Center[1].ToString() : string.Empty),
            ToSpawnParam(Intensity),
            ToSpawnParam(Smoothness),
            ToSpawnParam(Roundness),
            ToSpawnParam(Rounded),
            };

        protected override StringListParameter GetDestroyParameters() => new List<string> {
            ToSpawnParam(FadeDuration)
        };
    }
}
