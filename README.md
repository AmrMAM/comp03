# CompVPN (.NET)

This repository contains a minimal, network-level VPN prototype written in .NET 8. It creates a TUN virtual interface on Linux, exchanges raw IP packets with a UDP-based tunnel, and demonstrates how to route client traffic through a server.

> **Note:** This is a learning-oriented implementation and does not include production security features such as mutual authentication, encryption, or replay protection. If you need a production VPN, use WireGuard or OpenVPN.

## Projects

- `CompVpn.Server`: UDP tunnel server with a Linux TUN device.
- `CompVpn.Client`: Client that creates a TUN device and forwards packets to the server.
- `CompVpn.Common`: Shared protocol/TUN utilities.

## Build

```bash
dotnet build
```

## Run (Linux)

### Server

```bash
sudo dotnet run --project src/CompVpn.Server -- --bind 0.0.0.0 --port 51820 --tun cvpn0 --server-ip 10.0.0.1 --prefix 24 --upstream eth0
```

### Client

```bash
sudo dotnet run --project src/CompVpn.Client -- --server <server_public_ip> --port 51820 --tun cvpn0 --name laptop
```

## Run (Windows)

> **Requirement:** Install the [Wintun](https://www.wintun.net/) driver and make sure `wintun.dll` is available on the system path or alongside the executable.

### Server

```powershell
dotnet run --project src/CompVpn.Server -- --bind 0.0.0.0 --port 51820 --tun CompVpn --server-ip 10.0.0.1 --prefix 24
```

### Client

```powershell
dotnet run --project src/CompVpn.Client -- --server <server_public_ip> --port 51820 --tun CompVpn --name laptop
```

### What happens

- Server allocates a client IP from `10.0.0.10-10.0.0.209` and returns it in an `Assigned` message.
- Client configures the TUN interface, sets a default route through the VPN, and pushes DNS via `resolvectl`.
- Client and server exchange raw IP packets over UDP.

## Hosting the server on the public internet

1. **Provision a VM**
   - Any cloud VM (AWS EC2, Azure VM, GCP Compute Engine, DigitalOcean) with a public IPv4 address.
   - Ensure UDP port `51820` is allowed in the cloud firewall/security group.

2. **Enable IP forwarding & NAT** (automated by the server by default)
   - `net.ipv4.ip_forward=1` via `sysctl`.
   - `iptables -t nat -A POSTROUTING -s 10.0.0.0/24 -o eth0 -j MASQUERADE`.

3. **Run server as a systemd service**
   - Use `User=root` or grant `CAP_NET_ADMIN` to allow TUN configuration.

4. **Harden & secure**
   - Add authentication and encryption (for example, Noise or libsodium).
   - Rotate keys and implement replay protection.
   - Add per-client ACLs and traffic accounting.

### Windows hosting notes

- Ensure the Wintun driver is installed and `wintun.dll` is discoverable by the server process.
- Open UDP port `51820` in the Windows Firewall.
- The server will attempt to enable IP forwarding and set up a `New-NetNat` rule named `CompVpnNat`. If it already exists, update or remove it before re-running.

## Security notes

This code intentionally keeps the protocol simple to focus on the TUN + routing flow. Before using it in real environments, you must add:

- Authenticated key exchange and encryption.
- Packet integrity checks (HMAC/AEAD).
- Replay protection and rate limiting.
