using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace VtuberBot.Core.Extensions
{
    public static class BinaryExtensions
    {
        public static void BeWrite(this BinaryWriter bw, ushort v)
        {
            bw.Write(BitConverter.GetBytes(v).Reverse().ToArray());
        }

        public static void BeWrite(this BinaryWriter bw, char v)
        {
            bw.Write(BitConverter.GetBytes((ushort)v).Reverse().ToArray());
        }

        public static void BeWrite(this BinaryWriter bw, int v)
        {
            bw.Write(BitConverter.GetBytes(v).Reverse().ToArray());
        }

        public static void BeUshortWrite(this BinaryWriter bw, ushort v)
        {
            bw.BeWrite(v);
        }

        // 注意: 此处的long和ulong均为四个字节，而不是八个。
        public static void BeWrite(this BinaryWriter bw, long v)
        {
            bw.Write(BitConverter.GetBytes((uint)v).Reverse().ToArray());
        }

        public static void BeWrite(this BinaryWriter bw, ulong v)
        {
            bw.Write(BitConverter.GetBytes((uint)v).Reverse().ToArray());
        }

        public static ushort BeReadUInt16(this BinaryReader br)
        {
            return (ushort)((br.ReadByte() << 8) + br.ReadByte());
        }

        public static int BeReadInt32(this BinaryReader br)
        {
            return (br.ReadByte() << 24) | (br.ReadByte() << 16) | (br.ReadByte() << 8) | br.ReadByte();
        }

        public static uint BeReadUInt32(this BinaryReader br)
        {
            return (uint)((br.ReadByte() << 24) | (br.ReadByte() << 16) | (br.ReadByte() << 8) | br.ReadByte());
        }

        public static long BeReadLong32(this BinaryReader br)
        {
            return ((long)br.ReadByte() << 24) | (br.ReadByte() << 16) | (br.ReadByte() << 8) | br.ReadByte();
        }

        public static string BeReadString(this BinaryReader br, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;
            return encoding.GetString(br.ReadBytes(br.BeReadUInt16()));
        }
    }
}
