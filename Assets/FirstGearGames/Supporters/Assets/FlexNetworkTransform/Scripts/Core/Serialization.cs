using Mirror;
using FirstGearGames.Utilities.Networks;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
#pragma warning disable CS0618, CS0672
namespace FirstGearGames.Mirrors.Assets.FlexNetworkTransforms
{
    public static class Serialization
    {
        /// <summary>
        /// Serializes a TransformData into the returned writer.
        /// </summary>
        /// <param name="datas"></param>
        /// <param name="index"></param>
        public static NetworkWriterPooled SerializeTransformData(List<TransformData> datas, int index, FlexNetworkTransformBase fnt)
        {
            using (NetworkWriterPooled writer = NetworkWriterPool.Get())
            {
                SerializeTransformData(writer, datas[index], fnt);
                return writer;
            }
        }

        /// <summary>
        /// Serializes a TransformData into the returned writer.
        /// </summary>
        /// <param name="datas"></param>
        /// <param name="index"></param>
        public static NetworkWriterPooled SerializeTransformData(TransformData data, FlexNetworkTransformBase fnt)
        {
            using (NetworkWriterPooled writer = NetworkWriterPool.GetWriter())
            {
                SerializeTransformData(writer, data, fnt);
                return writer;
            }
        }


        /// <summary>
        /// Serializes a TransformData into the returned writer.
        /// </summary>
        /// <param name="datas"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SerializeTransformData(NetworkWriter writer, TransformData data, FlexNetworkTransformBase fnt)
        {
            //feature send deltas if using reliable.
            //SyncProperties.
            SyncProperties sp = (SyncProperties)data.SyncProperties;
            writer.WriteByte(data.SyncProperties);

            //NetworkIdentity.
            //Get compression level for netIdentity.
            if (EnumContains.SyncPropertiesContains(sp, SyncProperties.Id1))
                writer.WriteByte((byte)data.NetworkIdentity);
            else if (EnumContains.SyncPropertiesContains(sp, SyncProperties.Id2))
                writer.WriteUInt16((ushort)data.NetworkIdentity);
            else
                writer.WriteUInt32(data.NetworkIdentity);
            //ComponentIndex.
            writer.WriteByte(data.ComponentIndex);

            //Position.
            if (EnumContains.SyncPropertiesContains(sp, SyncProperties.Position))
                Compressions.WriteCompressedVector3(writer, data.Position, fnt);
            //Rotation.
            if (EnumContains.SyncPropertiesContains(sp, SyncProperties.Rotation))
                writer.WriteUInt32(Quaternions.Compress(data.Rotation));
            //Scale.
            if (EnumContains.SyncPropertiesContains(sp, SyncProperties.Scale))
                Compressions.WriteCompressedVector3(writer, data.Scale, fnt);

            //If attached.
            if (EnumContains.SyncPropertiesContains(sp, SyncProperties.Attached))
                WriteAttached(writer, data.Attached);
        }


        /// <summary>
        /// Deserializes a TransformData from data.
        /// </summary>
        public static void DeserializeComponent(ref int readPosition, ref ArraySegment<byte> packetData, out uint netId, out byte componentIndex, out SyncProperties syncProperties)
        {
            using (NetworkReaderPooled reader = NetworkReaderPool.GetReader(packetData))
            {
                DeserializeComponent(reader, ref readPosition, ref packetData, out netId, out componentIndex, out syncProperties);
            }
        }
        /// <summary>
        /// Deserializes a TransformData from data.
        /// </summary>
        public static void DeserializeComponent(NetworkReader reader, ref int readPosition, ref ArraySegment<byte> packetData, out uint netId, out byte componentIndex, out SyncProperties syncProperties)
        {
            reader.Position = readPosition;

            //Sync properties.
            syncProperties = (SyncProperties)reader.ReadByte();

            //NetworkIdentity.
            if (EnumContains.SyncPropertiesContains(syncProperties, SyncProperties.Id1))
                netId = reader.ReadByte();
            else if (EnumContains.SyncPropertiesContains(syncProperties, SyncProperties.Id2))
                netId = reader.ReadUInt16();
            else
                netId = reader.ReadUInt32();

            componentIndex = reader.ReadByte();
            //Set readPosition to new reader position.
            readPosition = reader.Position;
        }

        /// <summary>
        /// Deserializes a TransformData from data.
        /// </summary>
        public static void DeserializeTransformData(ref int readPosition, ref ArraySegment<byte> packetData, ref TransformData transformData, FlexNetworkTransformBase fnt, uint netId, byte componentIndex, SyncProperties syncProperties)
        {
            using (NetworkReaderPooled reader = NetworkReaderPool.GetReader(packetData))
            {
                DeserializeTransformData(reader, ref readPosition, ref packetData, ref transformData, fnt, netId, componentIndex, syncProperties);
            }
        }

        /// <summary>
        /// Deserializes a TransformData from data.
        /// </summary>
        public static void DeserializeTransformData(NetworkReader reader, ref int readPosition, ref ArraySegment<byte> packetData, ref TransformData transformData, FlexNetworkTransformBase fnt, uint netId, byte componentIndex, SyncProperties syncProeprties)
        {
            reader.Position = readPosition;

            transformData.SyncProperties = (byte)syncProeprties;
            transformData.NetworkIdentity = netId;
            transformData.ComponentIndex = componentIndex;

            //Position.
            if (EnumContains.SyncPropertiesContains(syncProeprties, SyncProperties.Position))
                transformData.Position = Compressions.ReadCompressedVector3(reader, fnt);
            //Rotation.
            if (EnumContains.SyncPropertiesContains(syncProeprties, SyncProperties.Rotation))
                transformData.Rotation = Quaternions.Decompress(reader.ReadUInt32());
            //scale.
            if (EnumContains.SyncPropertiesContains(syncProeprties, SyncProperties.Scale))
                transformData.Scale = Compressions.ReadCompressedVector3(reader, fnt);
            //If attached.
            if (EnumContains.SyncPropertiesContains(syncProeprties, SyncProperties.Attached))
                transformData.Attached = ReadAttached(reader);

            //if forced.
            if (EnumContains.SyncPropertiesContains(syncProeprties, SyncProperties.Forced))
                transformData.Forced = true;
            //Data should be marked as set given we just populated it.
            transformData.IsSet = true;

            //Set readPosition to new reader position.
            readPosition = reader.Position;
        }


        /// <summary>
        /// Writes an attached to writer.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="attached"></param>
        public static void WriteAttached(NetworkWriter writer, AttachedData attached)
        {
            uint netId;
            sbyte componentIndex;
            if (!attached.IsSet)
            {
                netId = 0;
                componentIndex = -1;
            }
            else
            {
                netId = attached.NetId;
                componentIndex = attached.AttachedTargetIndex;
            }

            writer.WriteUInt32(netId);
            writer.WriteSByte(componentIndex);
        }

        /// <summary>
        /// Returns an AttachedData.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static AttachedData ReadAttached(NetworkReader reader)
        {
            AttachedData ad = new AttachedData();
            ad.SetData(reader.ReadUInt32(), reader.ReadSByte());
            return ad;
        }

    }


}