using Naninovel.Commands;
using System.Collections.Generic;

namespace Naninovel.PostProcess
{
    [CommandAlias("AutoExposure")]
    public class SpawnAutoExposure : SpawnEffect
    {
        [ParameterAlias("time")]
        public DecimalParameter FadeDuration;
        public DecimalParameter Weight;
        public DecimalParameter FilteringX;
        public DecimalParameter FilteringY;
        public DecimalParameter Minimum;
        public DecimalParameter Maximum;
        public DecimalParameter ExposureCompensation;
        public StringParameter ProgressiveOrFixed;
        public DecimalParameter ProgressiveSpeedUp;
        public DecimalParameter ProgressiveSpeedDown;

        protected override string Path => "AutoExposure";
        protected override bool DestroyWhen => Assigned(Weight) && Weight == 0;

        protected override StringListParameter GetSpawnParameters() => new List<string> {
            ToSpawnParam(FadeDuration),
            ToSpawnParam(Weight),
            ToSpawnParam(FilteringX),
            ToSpawnParam(FilteringY),
            ToSpawnParam(Minimum),
            ToSpawnParam(Maximum),
            ToSpawnParam(ExposureCompensation),
            ToSpawnParam(ProgressiveOrFixed),
            ToSpawnParam(ProgressiveSpeedUp),
            ToSpawnParam(ProgressiveSpeedDown)
        };

        protected override StringListParameter GetDestroyParameters() => new List<string> {
            ToSpawnParam(FadeDuration)
        };
    }
}
