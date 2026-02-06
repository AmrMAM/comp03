using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;

var options = ClientOptions.Parse(args);
var listener = new TcpListener(IPAddress.Parse(options.ListenHost), options.ListenPort);
listener.Start();
Console.WriteLine($"VPN client listening locally on {options.ListenHost}:{options.ListenPort}");
Console.WriteLine($"Forwarding to {options.TargetHost}:{options.TargetPort} via {options.ServerHost}:{options.ServerPort}");

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
        var localClient = await listener.AcceptTcpClientAsync(cts.Token);
        _ = Task.Run(() => HandleLocalClientAsync(localClient, options, cts.Token), cts.Token);
    }
}
catch (OperationCanceledException)
{
}
finally
{
    listener.Stop();
}

static async Task HandleLocalClientAsync(TcpClient localClient, ClientOptions options, CancellationToken token)
{
    await using var localStream = localClient.GetStream();
    try
    {
        using var vpnClient = new TcpClient();
        await vpnClient.ConnectAsync(options.ServerHost, options.ServerPort, token);
        await using var vpnStream = vpnClient.GetStream();

        await WriteDestinationAsync(vpnStream, options.TargetHost, options.TargetPort, token);
        var status = await ReadStatusAsync(vpnStream, token);
        if (status != 0)
        {
            Console.WriteLine("Server rejected tunnel request.");
            return;
        }

        Console.WriteLine($"Tunnel open for {localClient.Client.RemoteEndPoint}");
        await RelayAsync(localStream, vpnStream, token);
    }
    catch (Exception ex) when (ex is IOException or SocketException or InvalidOperationException)
    {
        Console.WriteLine($"Tunnel error: {ex.Message}");
    }
    finally
    {
        localClient.Close();
    }
}

static async Task WriteDestinationAsync(NetworkStream stream, string host, int port, CancellationToken token)
{
    var hostBytes = Encoding.UTF8.GetBytes(host);
    if (hostBytes.Length == 0)
    {
        throw new InvalidOperationException("Target host is required.");
    }

    var lengthBuffer = new byte[2];
    BinaryPrimitives.WriteUInt16BigEndian(lengthBuffer, (ushort)hostBytes.Length);
    await stream.WriteAsync(lengthBuffer, token);
    await stream.WriteAsync(hostBytes, token);

    var portBuffer = new byte[4];
    BinaryPrimitives.WriteInt32BigEndian(portBuffer, port);
    await stream.WriteAsync(portBuffer, token);
}

static async Task<int> ReadStatusAsync(NetworkStream stream, CancellationToken token)
{
    var buffer = new byte[1];
    await ReadExactAsync(stream, buffer, token);
    return buffer[0];
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

static async Task RelayAsync(NetworkStream clientStream, NetworkStream serverStream, CancellationToken token)
{
    var clientToServer = PumpAsync(clientStream, serverStream, token);
    var serverToClient = PumpAsync(serverStream, clientStream, token);
    await Task.WhenAny(clientToServer, serverToClient);
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

record ClientOptions(string ListenHost, int ListenPort, string ServerHost, int ServerPort, string TargetHost, int TargetPort)
{
    public static ClientOptions Parse(string[] args)
    {
        var listenHost = GetArg(args, "--listen-host", "127.0.0.1");
        var listenPort = int.Parse(GetArg(args, "--listen-port", "7000"));
        var serverHost = GetArg(args, "--server-host", "127.0.0.1");
        var serverPort = int.Parse(GetArg(args, "--server-port", "5000"));
        var targetHost = GetArg(args, "--target-host", "example.com");
        var targetPort = int.Parse(GetArg(args, "--target-port", "80"));

        return new ClientOptions(listenHost, listenPort, serverHost, serverPort, targetHost, targetPort);
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
}
