using System.Buffers.Binary;
using System.Net;

namespace CompVpn.Common;

public enum MessageType : byte
{
    Hello = 1,
    Assigned = 2,
    Data = 3,
    KeepAlive = 4,
}

public readonly record struct HelloMessage(string ClientName);
public readonly record struct AssignedMessage(IPAddress ClientIp, IPAddress ServerIp, IPAddress DnsIp, byte Prefix);

public static class Protocol
{
    public const int MaxPayload = 1400;

    public static byte[] SerializeHello(HelloMessage message)
    {
        var nameBytes = System.Text.Encoding.UTF8.GetBytes(message.ClientName);
        if (nameBytes.Length > 200)
        {
            throw new ArgumentException("Client name too long.", nameof(message));
        }

        var buffer = new byte[2 + nameBytes.Length];
        buffer[0] = (byte)MessageType.Hello;
        buffer[1] = (byte)nameBytes.Length;
        Buffer.BlockCopy(nameBytes, 0, buffer, 2, nameBytes.Length);
        return buffer;
    }

    public static bool TryParseHello(ReadOnlySpan<byte> buffer, out HelloMessage message)
    {
        message = default;
        if (buffer.Length < 2 || buffer[0] != (byte)MessageType.Hello)
        {
            return false;
        }

        var nameLength = buffer[1];
        if (buffer.Length < 2 + nameLength)
        {
            return false;
        }

        var name = System.Text.Encoding.UTF8.GetString(buffer.Slice(2, nameLength));
        message = new HelloMessage(name);
        return true;
    }

    public static byte[] SerializeAssigned(AssignedMessage message)
    {
        var buffer = new byte[1 + 4 + 4 + 4 + 1];
        buffer[0] = (byte)MessageType.Assigned;
        message.ClientIp.TryWriteBytes(buffer.AsSpan(1, 4));
        message.ServerIp.TryWriteBytes(buffer.AsSpan(5, 4));
        message.DnsIp.TryWriteBytes(buffer.AsSpan(9, 4));
        buffer[13] = message.Prefix;
        return buffer;
    }

    public static bool TryParseAssigned(ReadOnlySpan<byte> buffer, out AssignedMessage message)
    {
        message = default;
        if (buffer.Length < 14 || buffer[0] != (byte)MessageType.Assigned)
        {
            return false;
        }

        var clientIp = new IPAddress(buffer.Slice(1, 4));
        var serverIp = new IPAddress(buffer.Slice(5, 4));
        var dnsIp = new IPAddress(buffer.Slice(9, 4));
        var prefix = buffer[13];
        message = new AssignedMessage(clientIp, serverIp, dnsIp, prefix);
        return true;
    }

    public static byte[] SerializeData(ReadOnlySpan<byte> payload)
    {
        if (payload.Length > MaxPayload)
        {
            throw new ArgumentException("Payload too large.", nameof(payload));
        }

        var buffer = new byte[3 + payload.Length];
        buffer[0] = (byte)MessageType.Data;
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(1, 2), (ushort)payload.Length);
        payload.CopyTo(buffer.AsSpan(3));
        return buffer;
    }

    public static bool TryParseData(ReadOnlySpan<byte> buffer, out ReadOnlySpan<byte> payload)
    {
        payload = default;
        if (buffer.Length < 3 || buffer[0] != (byte)MessageType.Data)
        {
            return false;
        }

        var length = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(1, 2));
        if (buffer.Length < 3 + length)
        {
            return false;
        }

        payload = buffer.Slice(3, length);
        return true;
    }

    public static byte[] SerializeKeepAlive()
        => new[] { (byte)MessageType.KeepAlive };
}
