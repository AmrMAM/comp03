using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace CompVpn.Common;

public sealed class TunDevice : IDisposable
{
    public string Name { get; }
    public Stream Stream { get; }
    private readonly Action _dispose;

    internal TunDevice(string name, Stream stream, Action dispose)
    {
        Name = name;
        Stream = stream;
        _dispose = dispose;
    }

    public static TunDevice Create(string requestedName)
    {
        if (OperatingSystem.IsLinux())
        {
            return CreateLinux(requestedName);
        }

        if (OperatingSystem.IsWindows())
        {
            return WintunDevice.Create(requestedName);
        }

        throw new PlatformNotSupportedException("Only Linux and Windows are implemented.");
    }

    private static TunDevice CreateLinux(string requestedName)
    {
        var fd = open("/dev/net/tun", O_RDWR);
        if (fd < 0)
        {
            throw new IOException("Failed to open /dev/net/tun.");
        }

        var ifr = new Ifreq
        {
            ifr_name = requestedName,
            ifr_flags = IFF_TUN | IFF_NO_PI,
        };

        var result = ioctl(fd, TUNSETIFF, ref ifr);
        if (result < 0)
        {
            close(fd);
            throw new IOException("Failed to configure TUN device.");
        }

        var handle = new SafeFileHandle((IntPtr)fd, ownsHandle: true);
        var stream = new FileStream(handle, FileAccess.ReadWrite, 4096, isAsync: true);
        return new TunDevice(ifr.ifr_name, stream, () => { });
    }

    public void Dispose()
    {
        Stream.Dispose();
        _dispose();
    }

    private const int O_RDWR = 2;
    private const short IFF_TUN = 0x0001;
    private const short IFF_NO_PI = 0x1000;
    private const uint TUNSETIFF = 0x400454ca;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private struct Ifreq
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string ifr_name;

        public short ifr_flags;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
        public byte[] padding;
    }

    [DllImport("libc", SetLastError = true)]
    private static extern int open(string pathname, int flags);

    [DllImport("libc", SetLastError = true)]
    private static extern int ioctl(int fd, uint request, ref Ifreq ifreq);

    [DllImport("libc", SetLastError = true)]
    private static extern int close(int fd);
}
