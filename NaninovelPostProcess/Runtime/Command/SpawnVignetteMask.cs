using Naninovel.Commands;
using System.Collections.Generic;

namespace Naninovel.PostProcess
{
    [CommandAlias("VignetteMask")]
    public class SpawnVignetteMask : SpawnEffect
    {
        [ParameterAlias("time")]
        public DecimalParameter FadeDuration;
        public DecimalParameter Weight;
        public StringParameter Color;
        public StringParameter Mask;
        public DecimalParameter Opacity;

        protected override string Path => "Vignette";
        protected override bool DestroyWhen => Assigned(Weight) && Weight == 0;

        protected override StringListParameter GetSpawnParameters() => 
            new List<string> {
            ToSpawnParam(FadeDuration),
            ToSpawnParam(Weight),
            ToSpawnParam("Mask"),
            ToSpawnParam(Color),
            ToSpawnParam(Mask),
            ToSpawnParam(Opacity)
            };

        protected override StringListParameter GetDestroyParameters() => new List<string> {
            ToSpawnParam(FadeDuration)
        };
    }
}
