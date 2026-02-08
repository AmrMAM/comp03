using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using CompVpn.Common;

var options = ServerOptions.Parse(args);

Console.WriteLine($"Starting VPN server on {options.BindAddress}:{options.Port}");
using var tun = TunDevice.Create(options.TunName);
Console.WriteLine($"Created TUN device: {tun.Name}");

if (!options.NoConfigure)
{
    NetworkConfigurator.ConfigureServer(tun.Name, options.ServerIp, options.Prefix, options.UpstreamInterface);
}

using var udp = new UdpClient(new IPEndPoint(options.BindAddress, options.Port));

var leases = new ConcurrentDictionary<IPAddress, IPEndPoint>();
var endpointToIp = new ConcurrentDictionary<IPEndPoint, IPAddress>(new IPEndPointComparer());
var ipPool = new Queue<IPAddress>(Enumerable.Range(10, 200).Select(i => IPAddress.Parse($"10.0.0.{i}")));

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

var tunToClientTask = Task.Run(async () =>
{
    var buffer = new byte[Protocol.MaxPayload];
    while (!cts.IsCancellationRequested)
    {
        var read = await tun.Stream.ReadAsync(buffer, cts.Token);
        if (read <= 0)
        {
            continue;
        }

        if (!PacketUtils.TryGetIpv4Destination(buffer.AsSpan(0, read), out var destination))
        {
            continue;
        }

        if (leases.TryGetValue(destination, out var endpoint))
        {
            var packet = Protocol.SerializeData(buffer.AsSpan(0, read));
            await udp.SendAsync(packet, packet.Length, endpoint);
        }
    }
}, cts.Token);

var clientToTunTask = Task.Run(async () =>
{
    while (!cts.IsCancellationRequested)
    {
        var packet = await udp.ReceiveAsync(cts.Token);
        if (Protocol.TryParseHello(packet.Buffer, out var hello))
        {
            var assignedIp = endpointToIp.GetOrAdd(packet.RemoteEndPoint, _ =>
            {
                if (ipPool.Count == 0)
                {
                    throw new InvalidOperationException("IP pool exhausted.");
                }

                var ip = ipPool.Dequeue();
                leases[ip] = packet.RemoteEndPoint;
                Console.WriteLine($"Assigned {ip} to {hello.ClientName} ({packet.RemoteEndPoint})");
                return ip;
            });

            var assigned = new AssignedMessage(assignedIp, options.ServerIp, options.DnsIp, options.Prefix);
            var response = Protocol.SerializeAssigned(assigned);
            await udp.SendAsync(response, response.Length, packet.RemoteEndPoint);
            continue;
        }

        if (Protocol.TryParseData(packet.Buffer, out var payload))
        {
            var buffer = payload.ToArray();
            await tun.Stream.WriteAsync(buffer, cts.Token);
        }
    }
}, cts.Token);

await Task.WhenAll(tunToClientTask, clientToTunTask);

internal sealed record ServerOptions(
    IPAddress BindAddress,
    int Port,
    string TunName,
    IPAddress ServerIp,
    IPAddress DnsIp,
    byte Prefix,
    string UpstreamInterface,
    bool NoConfigure)
{
    public static ServerOptions Parse(string[] args)
    {
        var bind = IPAddress.Any;
        var port = 51820;
        var tun = "cvpn0";
        var serverIp = IPAddress.Parse("10.0.0.1");
        var dnsIp = IPAddress.Parse("1.1.1.1");
        var prefix = (byte)24;
        var upstream = "eth0";
        var noConfigure = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--bind" when i + 1 < args.Length:
                    bind = IPAddress.Parse(args[++i]);
                    break;
                case "--port" when i + 1 < args.Length:
                    port = int.Parse(args[++i]);
                    break;
                case "--tun" when i + 1 < args.Length:
                    tun = args[++i];
                    break;
                case "--server-ip" when i + 1 < args.Length:
                    serverIp = IPAddress.Parse(args[++i]);
                    break;
                case "--dns" when i + 1 < args.Length:
                    dnsIp = IPAddress.Parse(args[++i]);
                    break;
                case "--prefix" when i + 1 < args.Length:
                    prefix = byte.Parse(args[++i]);
                    break;
                case "--upstream" when i + 1 < args.Length:
                    upstream = args[++i];
                    break;
                case "--no-config":
                    noConfigure = true;
                    break;
            }
        }

        return new ServerOptions(bind, port, tun, serverIp, dnsIp, prefix, upstream, noConfigure);
    }
}

internal static class NetworkConfigurator
{
    public static void ConfigureServer(string interfaceName, IPAddress serverIp, byte prefix, string upstreamInterface)
    {
        if (!OperatingSystem.IsLinux())
        {
            if (OperatingSystem.IsWindows())
            {
                var mask = PrefixToMask(prefix);
                Run("netsh", $"interface ip set address name=\"{interfaceName}\" static {serverIp} {mask}");
                RunPowerShell($"Set-NetIPInterface -InterfaceAlias \"{interfaceName}\" -Forwarding Enabled");
                var cidr = $"{NetworkPrefix(serverIp, prefix)}/{prefix}";
                RunPowerShell($"New-NetNat -Name \"CompVpnNat\" -InternalIPInterfaceAddressPrefix \"{cidr}\"");
                return;
            }

            Console.WriteLine("Skipping network configuration: unsupported OS.");
            return;
        }

        Run("ip", $"addr add {serverIp}/{prefix} dev {interfaceName}");
        Run("ip", $"link set dev {interfaceName} up");
        Run("sysctl", "-w net.ipv4.ip_forward=1");
        Run("iptables", $"-t nat -A POSTROUTING -s {serverIp}/{prefix} -o {upstreamInterface} -j MASQUERADE");
    }

    private static void Run(string file, string args)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo(file, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        using var process = System.Diagnostics.Process.Start(startInfo);
        process!.WaitForExit();
        if (process.ExitCode != 0)
        {
            var error = process.StandardError.ReadToEnd();
            throw new InvalidOperationException($"Command '{file} {args}' failed: {error}");
        }
    }

    private static void RunPowerShell(string command)
        => Run("powershell", $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"");

    private static string PrefixToMask(byte prefix)
    {
        var mask = prefix == 0 ? 0u : uint.MaxValue << (32 - prefix);
        var bytes = BitConverter.GetBytes(mask).Reverse().ToArray();
        return string.Join('.', bytes);
    }

    private static string NetworkPrefix(IPAddress address, byte prefix)
    {
        var bytes = address.GetAddressBytes();
        if (bytes.Length != 4)
        {
            throw new InvalidOperationException("Only IPv4 is supported.");
        }

        var mask = prefix == 0 ? 0u : uint.MaxValue << (32 - prefix);
        var ip = BitConverter.ToUInt32(bytes.Reverse().ToArray(), 0);
        var network = ip & mask;
        var networkBytes = BitConverter.GetBytes(network).Reverse().ToArray();
        return string.Join('.', networkBytes);
    }
}

internal sealed class IPEndPointComparer : IEqualityComparer<IPEndPoint>
{
    public bool Equals(IPEndPoint? x, IPEndPoint? y)
    {
        if (x is null || y is null)
        {
            return false;
        }

        return x.Address.Equals(y.Address) && x.Port == y.Port;
    }

    public int GetHashCode(IPEndPoint obj)
        => HashCode.Combine(obj.Address, obj.Port);
}
