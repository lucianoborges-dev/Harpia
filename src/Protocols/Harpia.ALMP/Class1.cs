using System.Runtime.InteropServices;

namespace Harpia.ALMP;


[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ALMPHeader
{
    public byte Source;
    public byte Dest;
    public byte Func;
    public ushort PresenceVector; // O "Short" de 2 bytes
}


public class ALMPProcessor
{
    // Definição dos bits para um comando específico (Ex: Telemetria)
    [Flags]
    public enum TelemetryPresence : ushort
    {
        HasLatitude = 1 << 0,  // 0x0001
        HasLongitude = 1 << 1,  // 0x0002
        HasAltitude = 1 << 2,  // 0x0004
        HasSpeed = 1 << 3   // 0x0008
    }

    public void Decode(ReadOnlySpan<byte> data)
    {
        // 1. Extrair o header fixo (5 bytes)
        var header = MemoryMarshal.Read<ALMPHeader>(data);
        var payload = data.Slice(Marshal.SizeOf<ALMPHeader>());

        int currentOffset = 0;

        // 2. Checar campos via Presence Vector
        if ((header.PresenceVector & (ushort)TelemetryPresence.HasLatitude) != 0)
        {
            float lat = MemoryMarshal.Read<float>(payload.Slice(currentOffset));
            currentOffset += sizeof(float);
            Console.WriteLine($"Lat: {lat}");
        }

        if ((header.PresenceVector & (ushort)TelemetryPresence.HasLongitude) != 0)
        {
            float lon = MemoryMarshal.Read<float>(payload.Slice(currentOffset));
            currentOffset += sizeof(float);
            Console.WriteLine($"Lon: {lon}");
        }

        // E assim por diante...
    }
}
