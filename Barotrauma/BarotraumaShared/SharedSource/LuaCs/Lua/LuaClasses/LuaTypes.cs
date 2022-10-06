using System;

namespace Barotrauma
{
    public struct LuaNone
    {

    }

    public struct LuaSByte
    {
        private readonly sbyte value;

        public LuaSByte(double v)
        {
            value = (sbyte)v;
        }

        public LuaSByte(string v, int radix = 10)
        {
            value = Convert.ToSByte(v, radix);
        }

        public static implicit operator sbyte(LuaSByte luaValue) => luaValue.value;
    }

    public struct LuaByte
    {
        private readonly byte value;

        public LuaByte(double v)
        {
            value = (byte)v;
        }

        public LuaByte(string v, int radix = 10)
        {
            value = Convert.ToByte(v, radix);
        }

        public static implicit operator byte(LuaByte luaValue) => luaValue.value;
    }

    public struct LuaInt16
    {
        private readonly short value;

        public LuaInt16(double v)
        {
            value = (short)v;
        }

        public LuaInt16(string v, int radix = 10)
        {
            value = Convert.ToInt16(v, radix);
        }

        public static implicit operator short(LuaInt16 luaValue) => luaValue.value;
    }

    public struct LuaUInt16
    {
        private readonly ushort value;

        public LuaUInt16(double v)
        {
            value = (ushort)v;
        }

        public LuaUInt16(string v, int radix = 10)
        {
            value = Convert.ToUInt16(v, radix);
        }

        public static implicit operator ushort(LuaUInt16 luaValue) => luaValue.value;
    }

    public struct LuaInt32
    {
        private readonly int value;

        public LuaInt32(double v)
        {
            value = (int)v;
        }

        public LuaInt32(string v, int radix = 10)
        {
            value = Convert.ToInt32(v, radix);
        }

        public static implicit operator int(LuaInt32 luaValue) => luaValue.value;
    }

    public struct LuaUInt32
    {
        private readonly uint value;

        public LuaUInt32(double v)
        {
            value = (uint)v;
        }

        public LuaUInt32(string v, int radix = 10)
        {
            value = Convert.ToUInt32(v, radix);
        }

        public static implicit operator uint(LuaUInt32 luaValue) => luaValue.value;
    }

    public struct LuaInt64
    {
        private readonly long value;

        public LuaInt64(double v)
        {
            value = (long)v;
        }

        public LuaInt64(double lo, double hi)
        {
            value = Convert.ToUInt32(lo) | (long)Convert.ToInt32(hi) << 32;
        }

        public LuaInt64(string v, int radix = 10)
        {
            value = Convert.ToInt64(v, radix);
        }

        public static implicit operator long(LuaInt64 luaValue) => luaValue.value;
    }

    public struct LuaUInt64
    {
        private readonly ulong value;

        public LuaUInt64(double v)
        {
            value = (ulong)v;
        }

        public LuaUInt64(double lo, double hi)
        {
            value = Convert.ToUInt32(lo) | (ulong)Convert.ToUInt32(hi) << 32;
        }

        public LuaUInt64(string v, int radix = 10)
        {
            value = Convert.ToUInt64(v, radix);
        }

        public static implicit operator ulong(LuaUInt64 luaValue) => luaValue.value;
    }

    public struct LuaSingle
    {
        private readonly float value;

        public LuaSingle(double v)
        {
            value = (float)v;
        }

        public LuaSingle(string v)
        {
            value = float.Parse(v);
        }

        public static implicit operator float(LuaSingle luaValue) => luaValue.value;
    }

    public struct LuaDouble
    {
        private readonly double value;

        public LuaDouble(double v)
        {
            value = v;
        }

        public LuaDouble(string v)
        {
            value = double.Parse(v);
        }

        public static implicit operator double(LuaDouble luaValue) => luaValue.value;
    }
}
