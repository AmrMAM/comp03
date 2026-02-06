using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;

var options = ServerOptions.Parse(args);
var listener = new TcpListener(IPAddress.Parse(options.ListenHost), options.ListenPort);
listener.Start();
Console.WriteLine($"VPN server listening on {options.ListenHost}:{options.ListenPort}");

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cts.Cancel();
};

try
{
    while (!cts.IsCancellationRequested)
    {
        var client = await listener.AcceptTcpClientAsync(cts.Token);
        _ = Task.Run(() => HandleClientAsync(client, options, cts.Token), cts.Token);
    }
}
catch (OperationCanceledException)
{
}
finally
{
    listener.Stop();
}

static async Task HandleClientAsync(TcpClient client, ServerOptions options, CancellationToken token)
{
    await using var clientStream = client.GetStream();
    try
    {
        var (host, port) = await ReadDestinationAsync(clientStream, token);
        if (!options.IsAllowed(host, port))
        {
            await clientStream.WriteAsync(new byte[] { 1 }, token);
            Console.WriteLine($"Denied tunnel request to {host}:{port} from {client.Client.RemoteEndPoint}");
            return;
        }

        using var targetClient = new TcpClient();
        await targetClient.ConnectAsync(host, port, token);
        await clientStream.WriteAsync(new byte[] { 0 }, token);

        Console.WriteLine($"Established tunnel {client.Client.RemoteEndPoint} -> {host}:{port}");
        await RelayAsync(clientStream, targetClient.GetStream(), token);
    }
    catch (Exception ex) when (ex is IOException or SocketException or InvalidOperationException)
    {
        Console.WriteLine($"Tunnel failed from {client.Client.RemoteEndPoint}: {ex.Message}");
    }
    finally
    {
        client.Close();
    }
}

static async Task<(string Host, int Port)> ReadDestinationAsync(NetworkStream stream, CancellationToken token)
{
    var lengthBuffer = new byte[2];
    await ReadExactAsync(stream, lengthBuffer, token);
    var hostLength = BinaryPrimitives.ReadUInt16BigEndian(lengthBuffer);
    if (hostLength == 0)
    {
        throw new InvalidOperationException("Host length must be greater than zero.");
    }

    var hostBuffer = new byte[hostLength];
    await ReadExactAsync(stream, hostBuffer, token);
    var host = Encoding.UTF8.GetString(hostBuffer);

    var portBuffer = new byte[4];
    await ReadExactAsync(stream, portBuffer, token);
    var port = BinaryPrimitives.ReadInt32BigEndian(portBuffer);
    if (port <= 0 || port > 65535)
    {
        throw new InvalidOperationException("Invalid destination port.");
    }

    return (host, port);
}

static async Task ReadExactAsync(NetworkStream stream, byte[] buffer, CancellationToken token)
{
    var offset = 0;
    while (offset < buffer.Length)
    {
        var read = await stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), token);
        if (read == 0)
        {
            throw new IOException("Unexpected end of stream.");
        }

        offset += read;
    }
}

static async Task RelayAsync(NetworkStream clientStream, NetworkStream targetStream, CancellationToken token)
{
    var clientToTarget = PumpAsync(clientStream, targetStream, token);
    var targetToClient = PumpAsync(targetStream, clientStream, token);
    await Task.WhenAny(clientToTarget, targetToClient);
}

static async Task PumpAsync(NetworkStream source, NetworkStream destination, CancellationToken token)
{
    try
    {
        await source.CopyToAsync(destination, token);
    }
    catch (IOException)
    {
    }
    catch (ObjectDisposedException)
    {
    }
}

record ServerOptions(string ListenHost, int ListenPort, HashSet<string> AllowedHosts, HashSet<int> AllowedPorts)
{
    public static ServerOptions Parse(string[] args)
    {
        var listenHost = GetArg(args, "--listen-host", "0.0.0.0");
        var listenPort = int.Parse(GetArg(args, "--listen-port", "5000"));
        var allowedHosts = ParseList(GetArg(args, "--allowed-hosts", ""));
        var allowedPorts = ParseList(GetArg(args, "--allowed-ports", ""))
            .Select(port => int.TryParse(port, out var parsed) ? parsed : 0)
            .Where(port => port > 0)
            .ToHashSet();

        return new ServerOptions(listenHost, listenPort, allowedHosts, allowedPorts);
    }

    public bool IsAllowed(string host, int port)
    {
        var hostAllowed = AllowedHosts.Count == 0 || AllowedHosts.Contains(host);
        var portAllowed = AllowedPorts.Count == 0 || AllowedPorts.Contains(port);
        return hostAllowed && portAllowed;
    }

    private static string GetArg(string[] args, string key, string defaultValue)
    {
        var index = Array.IndexOf(args, key);
        if (index >= 0 && index < args.Length - 1)
        {
            return args[index + 1];
        }

        return defaultValue;
    }

    private static HashSet<string> ParseList(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
