using Naninovel;
using Naninovel.Commands;
using System.Collections.Generic;

namespace NaninovelPostProcess
{
    [Alias("DoF")]
    public class SpawnDepthOfField : SpawnPostProcessing
    {
        [Alias("time")]
        public DecimalParameter FadeDuration;
        public DecimalParameter Weight;
        public DecimalParameter FocusDistance;
        public DecimalParameter Aperture;
        public DecimalParameter FocalLength;
        public StringParameter MaxBlurSize;

        protected override string PostProcessName => "DoF";
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
