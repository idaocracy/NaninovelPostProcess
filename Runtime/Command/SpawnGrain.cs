using Naninovel;
using Naninovel.Commands;
using System.Collections.Generic;

namespace NaninovelPostProcess
{
    [Alias("Grain")]
    public class SpawnGrain : SpawnPostProcessing
    {
        [Alias("time")]
        public DecimalParameter FadeDuration;
        public DecimalParameter Weight;
        public BooleanParameter Colored;
        public DecimalParameter Intensity;
        public DecimalParameter Size;
        public DecimalParameter LuminanceContribution;
        protected override bool DestroyWhen => Assigned(Weight) && Weight == 0;
        protected override string PostProcessName => "Grain";
        protected override StringListParameter GetSpawnParameters() => new List<string> {
            ToSpawnParam(FadeDuration),
            ToSpawnParam(Weight),
            ToSpawnParam(Colored),
            ToSpawnParam(Intensity),
            ToSpawnParam(Size),
            ToSpawnParam(LuminanceContribution),
        };

        protected override StringListParameter GetDestroyParameters() => new List<string> {
            ToSpawnParam(FadeDuration)
        };
    }
}
