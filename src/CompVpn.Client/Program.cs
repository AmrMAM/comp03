using System.Net;
using System.Net.Sockets;
using CompVpn.Common;

var options = ClientOptions.Parse(args);

Console.WriteLine($"Starting client '{options.ClientName}' to {options.ServerHost}:{options.ServerPort}");
using var tun = TunDevice.Create(options.TunName);
Console.WriteLine($"Created TUN device: {tun.Name}");

using var udp = new UdpClient();
udp.Connect(options.ServerHost, options.ServerPort);

await udp.SendAsync(Protocol.SerializeHello(new HelloMessage(options.ClientName)));

var result = await udp.ReceiveAsync();
if (!Protocol.TryParseAssigned(result.Buffer, out var assigned))
{
    throw new InvalidOperationException("Expected Assigned message from server.");
}

Console.WriteLine($"Assigned {assigned.ClientIp}/{assigned.Prefix} with server {assigned.ServerIp}");

if (!options.NoConfigure)
{
    NetworkConfigurator.ConfigureClient(tun.Name, assigned.ClientIp, assigned.Prefix, assigned.ServerIp, assigned.DnsIp);
}

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

var sendTask = Task.Run(async () =>
{
    var buffer = new byte[Protocol.MaxPayload];
    while (!cts.IsCancellationRequested)
    {
        var read = await tun.Stream.ReadAsync(buffer, cts.Token);
        if (read <= 0)
        {
            continue;
        }

        var payload = buffer.AsSpan(0, read);
        var packet = Protocol.SerializeData(payload);
        await udp.SendAsync(packet, packet.Length);
    }
}, cts.Token);

var receiveTask = Task.Run(async () =>
{
    while (!cts.IsCancellationRequested)
    {
        var serverPacket = await udp.ReceiveAsync(cts.Token);
        if (Protocol.TryParseData(serverPacket.Buffer, out var payload))
        {
            await tun.Stream.WriteAsync(payload, cts.Token);
        }
    }
}, cts.Token);

await Task.WhenAll(sendTask, receiveTask);

internal sealed record ClientOptions(
    string ServerHost,
    int ServerPort,
    string ClientName,
    string TunName,
    bool NoConfigure)
{
    public static ClientOptions Parse(string[] args)
    {
        var serverHost = "127.0.0.1";
        var serverPort = 51820;
        var clientName = Environment.MachineName.ToLowerInvariant();
        var tunName = "cvpn0";
        var noConfigure = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--server" when i + 1 < args.Length:
                    serverHost = args[++i];
                    break;
                case "--port" when i + 1 < args.Length:
                    serverPort = int.Parse(args[++i]);
                    break;
                case "--name" when i + 1 < args.Length:
                    clientName = args[++i];
                    break;
                case "--tun" when i + 1 < args.Length:
                    tunName = args[++i];
                    break;
                case "--no-config":
                    noConfigure = true;
                    break;
            }
        }

        return new ClientOptions(serverHost, serverPort, clientName, tunName, noConfigure);
    }
}

internal static class NetworkConfigurator
{
    public static void ConfigureClient(string interfaceName, IPAddress clientIp, byte prefix, IPAddress gatewayIp, IPAddress dnsIp)
    {
        if (!OperatingSystem.IsLinux())
        {
            if (OperatingSystem.IsWindows())
            {
                var mask = PrefixToMask(prefix);
                Run("netsh", $"interface ip set address name=\"{interfaceName}\" static {clientIp} {mask} {gatewayIp}");
                Run("netsh", $"interface ip set dns name=\"{interfaceName}\" static {dnsIp}");
                Run("route", $"add 0.0.0.0 mask 0.0.0.0 {gatewayIp}");
                return;
            }

            Console.WriteLine("Skipping network configuration: unsupported OS.");
            return;
        }

        Run("ip", $"addr add {clientIp}/{prefix} dev {interfaceName}");
        Run("ip", $"link set dev {interfaceName} up");
        Run("ip", $"route add default via {gatewayIp} dev {interfaceName}");
        Run("resolvectl", $"dns {interfaceName} {dnsIp}");
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

    private static string PrefixToMask(byte prefix)
    {
        var mask = prefix == 0 ? 0u : uint.MaxValue << (32 - prefix);
        var bytes = BitConverter.GetBytes(mask).Reverse().ToArray();
        return string.Join('.', bytes);
    }
}
