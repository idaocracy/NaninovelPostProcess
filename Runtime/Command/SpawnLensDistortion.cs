using Naninovel.Commands;
using System.Collections.Generic;

namespace Naninovel.PostProcess
{
    [CommandAlias("LensDistortion")]
    public class SpawnLensDistortion : SpawnEffect
    {
        [ParameterAlias("time")]
        public DecimalParameter FadeDuration;
        public DecimalParameter Weight;
        public DecimalParameter Intensity;
        public DecimalParameter XMultiplier;
        public DecimalParameter YMultiplier;
        public DecimalParameter CenterX;
        public DecimalParameter CenterY;
        public DecimalParameter Scale;

        protected override string Path => "LensDistortion";
        protected override bool DestroyWhen => Assigned(Weight) && Weight == 0;

        protected override StringListParameter GetSpawnParameters() => new List<string> {
            ToSpawnParam(FadeDuration),
            ToSpawnParam(Weight),
            ToSpawnParam(Intensity),
            ToSpawnParam(XMultiplier),
            ToSpawnParam(YMultiplier),
            ToSpawnParam(CenterX),
            ToSpawnParam(CenterY),
            ToSpawnParam(Scale)
        };

        protected override StringListParameter GetDestroyParameters() => new List<string> {
            ToSpawnParam(FadeDuration)
        };
    }
}
