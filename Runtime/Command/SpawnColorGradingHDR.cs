using Naninovel;
using Naninovel.Commands;
using System.Collections.Generic;

namespace NaninovelPostProcess
{
    [CommandAlias("ColorGradingHDR")]
    public class SpawnColorGradingHDR : SpawnPostProcessing
    {
        [ParameterAlias("time")]
        public DecimalParameter FadeDuration;
        public DecimalParameter Weight;
        public StringParameter TonemapperMode;

        public DecimalParameter ToeStrength;
        public DecimalParameter ToeLength;
        public DecimalParameter ShoulderStrength;
        public DecimalParameter ShoulderLength;
        public DecimalParameter ShoulderAngle;
        public DecimalParameter ToneGamma;

        public DecimalParameter Temperature;
        public DecimalParameter Tint;
        public DecimalParameter PostExposure;
        public StringParameter ColorFilter;
        public DecimalParameter HueShift;
        public DecimalParameter Saturation;
        public DecimalParameter Contrast;

        public DecimalListParameter RedChannel;
        public DecimalListParameter GreenChannel;
        public DecimalListParameter BlueChannel;
        public DecimalListParameter Lift;
        public DecimalListParameter Gamma;
        public DecimalListParameter Gain;

        protected override string PostProcessName => "ColorGradingHDR";
        protected override bool DestroyWhen => Assigned(Weight) && Weight == 0;

        protected override StringListParameter GetSpawnParameters() =>
            new List<string> {
            ToSpawnParam(FadeDuration),
            ToSpawnParam(Weight),
            ToSpawnParam(TonemapperMode),
            ToSpawnParam(ToeStrength),
            ToSpawnParam(ToeLength),
            ToSpawnParam(ShoulderStrength),
            ToSpawnParam(ShoulderLength),
            ToSpawnParam(ShoulderAngle),
            ToSpawnParam(Temperature),
            ToSpawnParam(Tint),
            ToSpawnParam(PostExposure),
            ToSpawnParam(ColorFilter),
            ToSpawnParam(HueShift),
            ToSpawnParam(Saturation),
            ToSpawnParam(Contrast),
            (Assigned(RedChannel) ? ToSpawnParam(RedChannel[0]) : ToSpawnParam(string.Empty)),
            (Assigned(RedChannel) ? ToSpawnParam(RedChannel[1]) : ToSpawnParam(string.Empty)),
            (Assigned(RedChannel) ? ToSpawnParam(RedChannel[2]) : ToSpawnParam(string.Empty)),
            (Assigned(GreenChannel) ? ToSpawnParam(GreenChannel[0]) : ToSpawnParam(string.Empty)),
            (Assigned(GreenChannel) ? ToSpawnParam(GreenChannel[1]) : ToSpawnParam(string.Empty)),
            (Assigned(GreenChannel) ? ToSpawnParam(GreenChannel[2]) : ToSpawnParam(string.Empty)),
            (Assigned(BlueChannel) ? ToSpawnParam(BlueChannel[0]) : ToSpawnParam(string.Empty)),
            (Assigned(BlueChannel) ? ToSpawnParam(BlueChannel[1]) : ToSpawnParam(string.Empty)),
            (Assigned(BlueChannel) ? ToSpawnParam(BlueChannel[2]) : ToSpawnParam(string.Empty)),
            (Assigned(Lift) ? ToSpawnParam(Lift[0]) : ToSpawnParam(string.Empty)),
            (Assigned(Lift) ? ToSpawnParam(Lift[1]) : ToSpawnParam(string.Empty)),
            (Assigned(Lift) ? ToSpawnParam(Lift[2]) : ToSpawnParam(string.Empty)),
            (Assigned(Lift) ? ToSpawnParam(Lift[3]) : ToSpawnParam(string.Empty)),
            (Assigned(Gamma) ? ToSpawnParam(Gamma[0]) : ToSpawnParam(string.Empty)),
            (Assigned(Gamma) ? ToSpawnParam(Gamma[1]) : ToSpawnParam(string.Empty)),
            (Assigned(Gamma) ? ToSpawnParam(Gamma[2]) : ToSpawnParam(string.Empty)),
            (Assigned(Gamma) ? ToSpawnParam(Gamma[3]) : ToSpawnParam(string.Empty)),
            (Assigned(Gain) ? ToSpawnParam(Gain[0]) : ToSpawnParam(string.Empty)),
            (Assigned(Gain) ? ToSpawnParam(Gain[1]) : ToSpawnParam(string.Empty)),
            (Assigned(Gain) ? ToSpawnParam(Gain[2]) : ToSpawnParam(string.Empty)),
            (Assigned(Gain) ? ToSpawnParam(Gain[3]) : ToSpawnParam(string.Empty)),
            };

        protected override StringListParameter GetDestroyParameters() => new List<string> {
            ToSpawnParam(FadeDuration)
        };
    }
}