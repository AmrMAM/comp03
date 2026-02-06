# Dotnet VPN Tunnel

This repository provides a minimal VPN-style tunnel implemented in .NET. It includes a server that enforces an allowlist and a client that forwards a local port through the server to a target host.

## Projects

- `src/VpnServer`: Listens for tunnel requests and proxies traffic to approved destinations.
- `src/VpnClient`: Listens locally and forwards traffic to the server.

## Example usage

```bash
# Start the server
VpnServer --listen-host 0.0.0.0 --listen-port 5000 --allowed-hosts example.com --allowed-ports 80

# Start the client
VpnClient --listen-port 7000 --server-host 127.0.0.1 --server-port 5000 --target-host example.com --target-port 80
```

Connect your local application to `127.0.0.1:7000` and the traffic will be forwarded to `example.com:80` via the server tunnel.
