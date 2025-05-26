using Naninovel;
using Naninovel.Commands;
using System.Collections.Generic;

namespace NaninovelPostProcess
{
    [Alias("LensDistortion")]
    public class SpawnLensDistortion : SpawnPostProcessing
    {
        [Alias("time")]
        public DecimalParameter FadeDuration;
        public DecimalParameter Weight;
        public DecimalParameter Intensity;
        public DecimalParameter XMultiplier;
        public DecimalParameter YMultiplier;
        public DecimalParameter CenterX;
        public DecimalParameter CenterY;
        public DecimalParameter Scale;

        protected override string PostProcessName => "LensDistortion";
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
