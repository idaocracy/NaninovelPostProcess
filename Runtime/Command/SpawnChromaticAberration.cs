using Naninovel.Commands;
using System.Collections.Generic;

namespace Naninovel.PostProcess
{
    [CommandAlias("ChromaticAberration")]
    public class SpawnChromaticAberration : SpawnEffect
    {
        [ParameterAlias("time")]
        public DecimalParameter FadeDuration;
        public DecimalParameter Weight;
        public StringParameter SpectralLut;
        public DecimalParameter Intensity;
        public BooleanParameter FastMode;

        protected override string Path => "ChromaticAberration";
        protected override bool DestroyWhen => Assigned(Weight) && Weight == 0;

        protected override StringListParameter GetSpawnParameters() => new List<string> {
            ToSpawnParam(FadeDuration),
            ToSpawnParam(Weight),
            ToSpawnParam(SpectralLut),
            ToSpawnParam(Intensity),
            ToSpawnParam(FastMode)
        };

        protected override StringListParameter GetDestroyParameters() => new List<string> {
            ToSpawnParam(FadeDuration)
        };
    }
}
