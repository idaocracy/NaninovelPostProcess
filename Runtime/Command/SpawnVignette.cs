using Naninovel;
using Naninovel.Commands;
using System.Collections.Generic;

namespace NaninovelPostProcess
{
    [CommandAlias("Vignette")]
    public class SpawnVignette : SpawnPostProcessing
    {
        [ParameterAlias("time")]
        public DecimalParameter FadeDuration;
        public DecimalParameter Weight;
        public StringParameter ClassicOrMask;

        //Classic parameters
        public StringParameter Color;
        public DecimalParameter CenterX;
        public DecimalParameter CenterY;
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
            ToSpawnParam(CenterX),
            ToSpawnParam(CenterY),
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
