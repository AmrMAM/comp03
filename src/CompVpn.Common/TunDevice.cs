using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace CompVpn.Common;

public sealed class TunDevice : IDisposable
{
    private readonly SafeFileHandle _handle;
    private readonly FileStream _stream;

    public string Name { get; }
    public Stream Stream => _stream;

    private TunDevice(SafeFileHandle handle, string name)
    {
        _handle = handle;
        Name = name;
        _stream = new FileStream(_handle, FileAccess.ReadWrite, 4096, isAsync: true);
    }

    public static TunDevice Create(string requestedName)
    {
        if (!OperatingSystem.IsLinux())
        {
            throw new PlatformNotSupportedException("Only Linux /dev/net/tun is implemented.");
        }

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
        return new TunDevice(handle, ifr.ifr_name);
    }

    public void Dispose()
    {
        _stream.Dispose();
        _handle.Dispose();
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
