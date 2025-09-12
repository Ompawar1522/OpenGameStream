using System.Text.Json.Serialization;

namespace OGS.Core.Common;

public sealed class BitrateValue
{
    public long BitsPerSecond { get; }

    [JsonIgnore] public double BytesPerSecond => BitsPerSecond * 8;
    [JsonIgnore] public double KiloBytesPerSecond => BytesPerSecond / 1000.0;
    [JsonIgnore] public double MegaBytesPerSecond => BytesPerSecond / 1000000.0;
    [JsonIgnore] public double KiloBitsPerSecond => BitsPerSecond / 1000.0;
    [JsonIgnore] public double MegaBitsPerSecond => BitsPerSecond / 1000000.0;

    public BitrateValue(long bitsPerSecond)
    {
        BitsPerSecond = bitsPerSecond;
    }

    public static BitrateValue FromBits(long bps) => new BitrateValue(bps);
    public static BitrateValue FromKiloBits(double kiloBits) => new BitrateValue((long)(kiloBits * 1000));
    public static BitrateValue FromMegaBits(double megaBits) => new BitrateValue((long)(megaBits * 1000000));

    public static BitrateValue FromBytes(long bytes) => new BitrateValue(bytes * 8);
    public static BitrateValue FromKiloBytes(double kiloBytes) => new BitrateValue((long)(kiloBytes * 1000) * 8);
    public static BitrateValue FromMegaBytes(double megaBytes) => new BitrateValue((long)(megaBytes * 1000000) * 8);

    public override string ToString() => $"{MegaBitsPerSecond:F1} Mbps";

    public override int GetHashCode() => BytesPerSecond.GetHashCode();
}