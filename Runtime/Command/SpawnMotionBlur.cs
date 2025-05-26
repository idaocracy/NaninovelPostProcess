using Naninovel;
using Naninovel.Commands;
using System.Collections.Generic;

namespace NaninovelPostProcess
{
    [Alias("MotionBlur")]
    public class SpawnMotionBlur : SpawnPostProcessing
    {
        [Alias("time")]
        public DecimalParameter FadeDuration;
        public DecimalParameter Weight;
        public DecimalParameter ShutterAngle;
        public DecimalParameter SampleCount;

        protected override bool DestroyWhen => Assigned(Weight) && Weight == 0;
        protected override string PostProcessName => "MotionBlur";

        protected override StringListParameter GetSpawnParameters() => new List<string> {
            ToSpawnParam(FadeDuration),
            ToSpawnParam(Weight),
            ToSpawnParam(ShutterAngle),
            ToSpawnParam(SampleCount)
        };

        protected override StringListParameter GetDestroyParameters() => new List<string> {
            ToSpawnParam(FadeDuration)
        };
    }
}
