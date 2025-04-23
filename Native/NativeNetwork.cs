using System;
using System.Runtime.InteropServices;

namespace SMOClient.Native;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct NativeNetworkConfig
{
    public int Port;
    public int MaxConnections;
    public float ConnectionTimeout;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    public byte[] IpAddress;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
    public byte[] AppIdentifier;
}

public static class NativeNetwork
{
    [DllImport("SMONative", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr CreateNetworkClient(ref NativeNetworkConfig config);

    [DllImport("SMONative", CallingConvention = CallingConvention.Cdecl)]
    public static extern void DestroyNetworkClient(IntPtr client);

    [DllImport("SMONative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool ConnectToServer(IntPtr client, string serverIp, int serverPort);

    [DllImport("SMONative", CallingConvention = CallingConvention.Cdecl)]
    public static extern void DisconnectFromServer(IntPtr client);

    [DllImport("SMONative", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr CreatePacket(IntPtr client, int initialSize);

    [DllImport("SMONative", CallingConvention = CallingConvention.Cdecl)]
    public static extern void DestroyPacket(IntPtr packet);

    [DllImport("SMONative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool SendPacket(IntPtr client, IntPtr packet, long connectionId, byte deliveryMethod);

    [DllImport("SMONative", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ReceivePacket(IntPtr client);

    [DllImport("SMONative", CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetPacketLength(IntPtr packet);

    [DllImport("SMONative", CallingConvention = CallingConvention.Cdecl)]
    public static extern long GetPacketConnectionId(IntPtr packet);

    [DllImport("SMONative", CallingConvention = CallingConvention.Cdecl)]
    public static extern byte GetPacketChannel(IntPtr packet);

    [DllImport("SMONative", CallingConvention = CallingConvention.Cdecl)]
    private static extern void WritePacketDataNative(IntPtr packet, IntPtr data, int length);

    [DllImport("SMONative", CallingConvention = CallingConvention.Cdecl)]
    private static extern void ReadPacketDataNative(IntPtr packet, IntPtr data, int length);

    public static unsafe void WritePacketData(IntPtr packet, byte[] data, int offset, int length)
    {
        fixed (byte* pData = data)
        {
            WritePacketDataNative(packet, (IntPtr)pData + offset, length);
        }
    }

    public static unsafe void ReadPacketData(IntPtr packet, byte[] data, int offset, int length)
    {
        fixed (byte* pData = data)
        {
            ReadPacketDataNative(packet, (IntPtr)pData + offset, length);
        }
    }
} 