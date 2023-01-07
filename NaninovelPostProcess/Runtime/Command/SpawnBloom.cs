using Naninovel.Commands;
using System.Collections.Generic;

namespace Naninovel.PostProcess
{
    [CommandAlias("bloom")]
    public class SpawnBloom : SpawnEffect
    {
        [ParameterAlias("time")]
        public DecimalParameter FadeDuration;
        public DecimalParameter Weight;
        public DecimalParameter Intensity;
        public DecimalParameter Threshold;
        public DecimalParameter SoftKnee;
        public DecimalParameter Clamp;
        public DecimalParameter Diffusion;
        public DecimalParameter AnamorphicRatio;
        public StringParameter Color;
        public BooleanParameter FastMode;

        protected override string Path => $"DoF";
        protected override bool DestroyWhen => Assigned(Weight) && Weight == 0;

        protected override StringListParameter GetSpawnParameters() => new List<string> {
            ToSpawnParam(FadeDuration),
            ToSpawnParam(Weight),
            ToSpawnParam(Intensity),
            ToSpawnParam(Threshold),
            ToSpawnParam(SoftKnee),
            ToSpawnParam(Clamp),
            ToSpawnParam(Diffusion),
            ToSpawnParam(AnamorphicRatio),
            ToSpawnParam(Color),
            ToSpawnParam(FastMode)
        };

        protected override StringListParameter GetDestroyParameters() => new List<string> {
            ToSpawnParam(FadeDuration)
        };
    }
}
