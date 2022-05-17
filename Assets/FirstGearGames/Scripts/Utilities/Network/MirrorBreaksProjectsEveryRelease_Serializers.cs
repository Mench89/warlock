#if MIRROR_53_0_OR_NEWER

using Mirror;

namespace FirstGearGames.Utilities.Networks
{

    public static class MirrorBreaksProjectsEveryRelease_Serializers
    {
        public static void WriteBoolean(this NetworkWriter writer, bool value) => writer.WriteBool(value);
        public static bool ReadBoolean(this NetworkReader reader) => reader.ReadBool();

        public static void WriteInt64(this NetworkWriter writer, long value) => writer.WriteLong(value);
        public static long ReadInt64(this NetworkReader reader) => reader.ReadLong();
        public static void WriteUInt64(this NetworkWriter writer, ulong value) => writer.WriteULong(value);
        public static ulong ReadUInt64(this NetworkReader reader) => reader.ReadULong();

        public static void WriteInt32(this NetworkWriter writer, int value) => writer.WriteInt(value);
        public static int ReadInt32(this NetworkReader reader) => reader.ReadInt();
        public static void WriteUInt32(this NetworkWriter writer, uint value) => writer.WriteUInt(value);
        public static uint ReadUInt32(this NetworkReader reader) => reader.ReadUInt();

        public static void WriteInt16(this NetworkWriter writer, short value) => writer.WriteShort(value);
        public static short ReadInt16(this NetworkReader reader) => reader.ReadShort();
        public static void WriteUInt16(this NetworkWriter writer, ushort value) => writer.WriteUShort(value);
        public static ushort ReadUInt16(this NetworkReader reader) => reader.ReadUShort();

        public static void WriteSingle(this NetworkWriter writer, float value) => writer.WriteFloat(value);
        public static float ReadSingle(this NetworkReader reader) => reader.ReadFloat();

    }


}

#endif