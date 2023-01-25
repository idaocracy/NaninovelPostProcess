using Naninovel.Commands;
using System.Collections.Generic;
using System.Linq;

namespace Naninovel.PostProcess
{
    [CommandAlias("ColorGradingLDR")]
    public class SpawnColorGradingLDR : SpawnEffect
    {
        [ParameterAlias("time")]
        public DecimalParameter FadeDuration;
        public DecimalParameter Weight;
        public StringParameter LookUpTexture;
        public DecimalParameter Contribution;
        public DecimalParameter Temperature;
        public DecimalParameter Tint;
        public StringParameter ColorFilter;
        public DecimalParameter HueShift;
        public DecimalParameter Saturation;
        public DecimalParameter Brightness;
        public DecimalParameter Contrast;

        public DecimalListParameter RedChannel;
        public DecimalListParameter GreenChannel;
        public DecimalListParameter BlueChannel;
        public DecimalListParameter Lift;
        public DecimalListParameter Gamma;
        public DecimalListParameter Gain;

        protected override string Path => "ColorGradingLDR";
        protected override bool DestroyWhen => Assigned(Weight) && Weight == 0;

        protected override StringListParameter GetSpawnParameters() =>
            new List<string> {
            ToSpawnParam(FadeDuration),
            ToSpawnParam(Weight),
            ToSpawnParam(Contribution),
            ToSpawnParam(LookUpTexture),
            ToSpawnParam(Temperature),
            ToSpawnParam(Tint),
            ToSpawnParam(ColorFilter),
            ToSpawnParam(HueShift),
            ToSpawnParam(Saturation),
            ToSpawnParam(Brightness),
            ToSpawnParam(Contrast),
            ToSpawnParam(RedChannel[0]),
            ToSpawnParam(RedChannel[1]),
            ToSpawnParam(RedChannel[2]),
            ToSpawnParam(GreenChannel[0]),
            ToSpawnParam(GreenChannel[1]),
            ToSpawnParam(GreenChannel[2]),
            ToSpawnParam(BlueChannel[0]),
            ToSpawnParam(BlueChannel[1]),
            ToSpawnParam(BlueChannel[3]),
            ToSpawnParam(Lift[0]),
            ToSpawnParam(Lift[1]),
            ToSpawnParam(Lift[2]),
            ToSpawnParam(Lift[3]),
            ToSpawnParam(Lift[4]),
            ToSpawnParam(Gamma[0]),
            ToSpawnParam(Gamma[1]),
            ToSpawnParam(Gamma[2]),
            ToSpawnParam(Gamma[3]),
            ToSpawnParam(Gain[0]),
            ToSpawnParam(Gain[1]),
            ToSpawnParam(Gain[2]),
            ToSpawnParam(Gain[3])
            };

        protected override StringListParameter GetDestroyParameters() => new List<string> {
            ToSpawnParam(FadeDuration)
        };
    }
}