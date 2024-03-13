using Naninovel;
using Naninovel.Commands;
using System.Collections.Generic;

namespace NaninovelPostProcess
{
    [CommandAlias("Grain")]
    public class SpawnGrain : SpawnEffect
    {
        [ParameterAlias("time")]
        public DecimalParameter FadeDuration;
        public DecimalParameter Weight;
        public BooleanParameter Colored;
        public DecimalParameter Intensity;
        public DecimalParameter Size;
        public DecimalParameter LuminanceContribution;

        protected override string Path => "Grain";
        protected override bool DestroyWhen => Assigned(Weight) && Weight == 0;

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
