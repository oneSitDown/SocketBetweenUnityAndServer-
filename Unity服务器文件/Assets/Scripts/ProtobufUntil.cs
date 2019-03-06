//========================================================
//作者:#AuthorName#
//创建时间:#CreateTime#
//备注:
//========================================================
using ProtoBuf;
using System.IO;

/// <summary>
/// 序列化和反序列化工具类
/// </summary>
public class ProtobufUntil{

    public static byte[] Serialize(IExtensible msg)
    {
        byte[] result;
        using (var stream = new MemoryStream())
        {
            Serializer.Serialize(stream, msg);
            result = stream.ToArray();
        }
        return result;
    }

    public static IExtensible Deserialize<IExtensible>(byte[] message)
    {
        IExtensible result;
        using (var stream = new MemoryStream(message))
        {
            result = Serializer.Deserialize<IExtensible>(stream);
        }
        return result;
    }
}
