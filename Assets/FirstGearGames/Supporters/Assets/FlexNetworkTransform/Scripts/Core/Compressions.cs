using FirstGearGames.Utilities.Networks;
using Mirror;
using UnityEngine;

#pragma warning disable CS0618, CS0672
namespace FirstGearGames.Mirrors.Assets.FlexNetworkTransforms
{

    /// <summary>
    /// Indicates how each axes is compressed.
    /// </summary>
    [System.Flags]
    public enum CompressedAxes : byte
    {
        None = 0,
        XUncompressed = 1,
        XCompressed = 2,
        YUncompressed = 4,
        YCompressed = 8,
        ZUncompressed = 16,
        ZCompressed = 32
    }

    public class Compressions : MonoBehaviour
    {
        /// <summary>
        /// Maximum value a float may be, positive or negative, for compression.
        /// </summary>
        private const float MAX_FLOAT_COMPRESSION_VALUE = 326f;

        /// <summary>
        /// Writes a compressed Vector3 to the writer.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="ca"></param>
        /// <param name="v"></param>
        public static void WriteCompressedVector3(NetworkWriter writer, Vector3 v, FlexNetworkTransformBase fnt)
        {
            CompressedAxes ca = CompressedAxes.None;
            int beforePosition = writer.Position;
            //This byte will be replaced with compressedAxes.
            writer.WriteByte(0);

            //X
            if (fnt.CompressSmall && v.x > -MAX_FLOAT_COMPRESSION_VALUE && v.x < MAX_FLOAT_COMPRESSION_VALUE)
            {
                writer.WriteInt16((short)Mathf.Round(v.x * 100f));
                ca |= CompressedAxes.XCompressed;
            }
            else
            {
                writer.WriteSingle(v.x);
                ca |= CompressedAxes.XUncompressed;
            }
            //Y
            if (fnt.CompressSmall && v.y > -MAX_FLOAT_COMPRESSION_VALUE && v.y < MAX_FLOAT_COMPRESSION_VALUE)
            {
                writer.WriteInt16((short)Mathf.Round(v.y * 100f));
                ca |= CompressedAxes.YCompressed;
            }
            else
            {
                writer.WriteSingle(v.y);
                ca |= CompressedAxes.YUncompressed;
            }
            //Z
            if (fnt.CompressSmall && v.z > -MAX_FLOAT_COMPRESSION_VALUE && v.z < MAX_FLOAT_COMPRESSION_VALUE)
            {
                writer.WriteInt16((short)Mathf.Round(v.z * 100f));
                ca |= CompressedAxes.ZCompressed;
            }
            else
            {
                writer.WriteSingle(v.z);
                ca |= CompressedAxes.ZUncompressed;
            }

            int afterPosition = writer.Position;
            writer.Position = beforePosition;
            writer.WriteByte((byte)ca);
            writer.Position = afterPosition;
        }


        /// <summary>
        /// Reads a compressed Vector3.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="ca"></param>
        /// <param name="v"></param>
        public static Vector3 ReadCompressedVector3(NetworkReader reader, FlexNetworkTransformBase fnt)
        {
            CompressedAxes ca = (CompressedAxes)reader.ReadByte();
            //X
            float x = 0f;
            if (EnumContains.CompressedAxesContains(ca, CompressedAxes.XCompressed))
                x = (reader.ReadInt16() / 100f);
            else if (EnumContains.CompressedAxesContains(ca, CompressedAxes.XUncompressed))
                x = reader.ReadSingle();
            //y
            float y = 0f;
            if (EnumContains.CompressedAxesContains(ca, CompressedAxes.YCompressed))
                y = (reader.ReadInt16() / 100f);
            else if (EnumContains.CompressedAxesContains(ca, CompressedAxes.YUncompressed))
                y = reader.ReadSingle();
            //z
            float z = 0f;
            if (EnumContains.CompressedAxesContains(ca, CompressedAxes.ZCompressed))
                z = (reader.ReadInt16() / 100f);
            else if (EnumContains.CompressedAxesContains(ca, CompressedAxes.ZUncompressed))
                z = reader.ReadSingle();

            return new Vector3(x, y, z);
        }
    }


}