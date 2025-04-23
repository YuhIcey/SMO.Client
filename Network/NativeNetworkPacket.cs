using System;
using SMOClient.Native;

namespace SMOClient.Network;

public class NativeNetworkPacket : IDisposable
{
    private bool _disposed;
    private IntPtr _nativeHandle;

    public byte[] Data { get; private set; }
    public int Length { get; private set; }
    public long ConnectionId { get; private set; }
    public byte Channel { get; private set; }

    public NativeNetworkPacket(IntPtr nativeHandle, byte[] data, int length, long connectionId, byte channel)
    {
        _nativeHandle = nativeHandle;
        Data = data;
        Length = length;
        ConnectionId = connectionId;
        Channel = channel;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            if (_nativeHandle != IntPtr.Zero)
            {
                NativeNetwork.DestroyPacket(_nativeHandle);
                _nativeHandle = IntPtr.Zero;
            }
        }

        _disposed = true;
    }

    ~NativeNetworkPacket()
    {
        Dispose(false);
    }
} 