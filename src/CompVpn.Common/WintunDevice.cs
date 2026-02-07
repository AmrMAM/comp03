using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace CompVpn.Common;

internal static class WintunDevice
{
    public static TunDevice Create(string requestedName)
    {
        var adapterName = string.IsNullOrWhiteSpace(requestedName) ? "CompVpn" : requestedName;
        var adapter = WintunOpenAdapter(adapterName);
        if (adapter == IntPtr.Zero)
        {
            adapter = WintunCreateAdapter(adapterName, "CompVpn", IntPtr.Zero);
        }

        if (adapter == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to create or open Wintun adapter. Ensure wintun.dll is available.");
        }

        var session = WintunStartSession(adapter, 0x400000);
        if (session == IntPtr.Zero)
        {
            WintunCloseAdapter(adapter);
            throw new InvalidOperationException("Failed to start Wintun session.");
        }

        var stream = new WintunStream(adapter, session);
        return new TunDevice(adapterName, stream, () => { });
    }

    private sealed class WintunStream : Stream
    {
        private readonly IntPtr _adapter;
        private readonly IntPtr _session;
        private bool _disposed;

        public WintunStream(IntPtr adapter, IntPtr session)
        {
            _adapter = adapter;
            _session = session;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WintunStream));
            }

            if (buffer is null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0 || count < 0 || offset + count > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            while (true)
            {
                var packetPtr = WintunReceivePacket(_session, out var packetSize);
                if (packetPtr != IntPtr.Zero)
                {
                    if (packetSize > count)
                    {
                        WintunReleaseReceivePacket(_session, packetPtr);
                        throw new InvalidOperationException("Receive buffer too small for packet.");
                    }

                    Marshal.Copy(packetPtr, buffer, offset, (int)packetSize);
                    WintunReleaseReceivePacket(_session, packetPtr);
                    return (int)packetSize;
                }

                var error = Marshal.GetLastWin32Error();
                if (error != ERROR_NO_MORE_ITEMS)
                {
                    throw new InvalidOperationException($"Wintun receive failed: {error}.");
                }

                var readEvent = WintunGetReadWaitEvent(_session);
                WaitForSingleObject(readEvent, 1000);
            }
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => await Task.Run(() => Read(buffer, offset, count), cancellationToken);

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WintunStream));
            }

            if (buffer is null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0 || count < 0 || offset + count > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            var packetPtr = WintunAllocateSendPacket(_session, (uint)count);
            if (packetPtr == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to allocate Wintun send packet.");
            }

            Marshal.Copy(buffer, offset, packetPtr, count);
            WintunSendPacket(_session, packetPtr);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => await Task.Run(() => Write(buffer, offset, count), cancellationToken);

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            WintunEndSession(_session);
            WintunCloseAdapter(_adapter);
            base.Dispose(disposing);
        }
    }

    private const int ERROR_NO_MORE_ITEMS = 259;

    [DllImport("wintun.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr WintunCreateAdapter(string name, string tunnelType, IntPtr requestedGuid);

    [DllImport("wintun.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr WintunOpenAdapter(string name);

    [DllImport("wintun.dll", SetLastError = true)]
    private static extern void WintunCloseAdapter(IntPtr adapter);

    [DllImport("wintun.dll", SetLastError = true)]
    private static extern IntPtr WintunStartSession(IntPtr adapter, uint capacity);

    [DllImport("wintun.dll", SetLastError = true)]
    private static extern void WintunEndSession(IntPtr session);

    [DllImport("wintun.dll", SetLastError = true)]
    private static extern IntPtr WintunReceivePacket(IntPtr session, out uint packetSize);

    [DllImport("wintun.dll", SetLastError = true)]
    private static extern void WintunReleaseReceivePacket(IntPtr session, IntPtr packet);

    [DllImport("wintun.dll", SetLastError = true)]
    private static extern IntPtr WintunAllocateSendPacket(IntPtr session, uint packetSize);

    [DllImport("wintun.dll", SetLastError = true)]
    private static extern void WintunSendPacket(IntPtr session, IntPtr packet);

    [DllImport("wintun.dll", SetLastError = true)]
    private static extern IntPtr WintunGetReadWaitEvent(IntPtr session);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);
}
