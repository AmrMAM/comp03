using System.Net;

namespace CompVpn.Common;

public static class PacketUtils
{
    public static bool TryGetIpv4Destination(ReadOnlySpan<byte> packet, out IPAddress destination)
    {
        destination = IPAddress.None;
        if (packet.Length < 20)
        {
            return false;
        }

        var version = packet[0] >> 4;
        if (version != 4)
        {
            return false;
        }

        destination = new IPAddress(packet.Slice(16, 4));
        return true;
    }
}
