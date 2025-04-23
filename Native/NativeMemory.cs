using System;
using System.Text;

namespace SMOClient.Native;

public static class NativeMemory
{
    public static byte[] StringToFixedBytes(string str, int size)
    {
        var bytes = new byte[size];
        if (string.IsNullOrEmpty(str))
            return bytes;

        var strBytes = Encoding.UTF8.GetBytes(str);
        var copyLength = Math.Min(strBytes.Length, size);
        Array.Copy(strBytes, bytes, copyLength);
        return bytes;
    }

    public static string FixedBytesToString(byte[] bytes)
    {
        var nullTerminator = Array.IndexOf<byte>(bytes, 0);
        var length = nullTerminator < 0 ? bytes.Length : nullTerminator;
        return Encoding.UTF8.GetString(bytes, 0, length);
    }
} 