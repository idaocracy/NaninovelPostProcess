using Naninovel.Commands;
using System.Collections.Generic;

namespace Naninovel.PostProcess
{
    [CommandAlias("DepthOfField")]
    public class SpawnDepthOfField : SpawnEffect
    {
        [ParameterAlias("time")]
        public DecimalParameter FadeDuration;
        public DecimalParameter Weight;
        public DecimalParameter FocusDistance;
        public DecimalParameter Aperture;
        public DecimalParameter FocalLength;
        public StringParameter MaxBlurSize;

        protected override string Path => "DoF";
        protected override bool DestroyWhen => Assigned(Weight) && Weight == 0;

        protected override StringListParameter GetSpawnParameters() => new List<string> {
            ToSpawnParam(FadeDuration),
            ToSpawnParam(Weight),
            ToSpawnParam(FocusDistance),
            ToSpawnParam(Aperture),
            ToSpawnParam(FocalLength),
            ToSpawnParam(MaxBlurSize)
        };

        protected override StringListParameter GetDestroyParameters() => new List<string> {
            ToSpawnParam(FadeDuration)
        };
    }
}
