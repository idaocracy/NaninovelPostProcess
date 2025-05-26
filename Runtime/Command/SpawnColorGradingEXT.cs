using Naninovel;
using Naninovel.Commands;
using System.Collections.Generic;

namespace NaninovelPostProcess
{
    [Alias("ColorGradingEXT")]
    public class SpawnColorGradingEXT : SpawnEffect
    {
        [Alias("time")]
        public DecimalParameter FadeDuration;
        public DecimalParameter Weight;
        public StringParameter LookUpTexture;

        protected override string Path => "ColorGradingEXT";
        protected override bool DestroyWhen => Assigned(Weight) && Weight == 0;

        protected override StringListParameter GetSpawnParameters() =>
            new List<string> {
            ToSpawnParam(FadeDuration),
            ToSpawnParam(Weight),
            ToSpawnParam(LookUpTexture)
            };

        protected override StringListParameter GetDestroyParameters() => new List<string> {
            ToSpawnParam(FadeDuration)
        };
    }
}