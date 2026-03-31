using System.IO;
using System.Xml.Serialization;

/// <summary>
/// Serialization utility
/// </summary>
public class Serialization
{
    /// <summary>
    /// Serialize to string
    /// </summary>
    /// <typeparam name="T">Type of instance to serialize</typeparam>
    /// <param name="data">Instance to serialize</param>
    /// <returns>Serialized instance</returns>
    public static string Serialize<T>(T data)
    {
        XmlSerializer xmlSerializer = new(typeof(T));
        using StringWriter textWriter = new();
        xmlSerializer.Serialize(textWriter, data);
        return textWriter.ToString();
    }

    /// <summary>
    /// Serialize to file
    /// </summary>
    /// <typeparam name="T">Type of instance to serialize</typeparam>
    /// <param name="data">Instance to serialize</param>
    /// <param name="path">File to create</param>
    public static void Serialize<T>(T data, string path)
    {
        System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
        XmlSerializer xml = new(typeof(T));
        FileStream stream = new(path, FileMode.Create);
        xml.Serialize(stream, data);
        stream.Close();
    }

    /// <summary>
    /// Deserialization.
    /// </summary>
    /// <typeparam name="T">Type of instance to deserialize</typeparam>
    /// <param name="path">Path from which to deserialize instance.</param>
    /// <returns>Deserialized instance</returns>
    public static T Deserialize<T>(string path)
    {
        System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
        XmlSerializer xml = new(typeof(T));
        using FileStream stream = new(path, FileMode.Open);
        return (T)xml.Deserialize(stream);
    }

    /// <summary>
    /// Deserialization of instance represented as a string.
    /// </summary>
    /// <typeparam name="T">Type of instance to deserialize</typeparam>
    /// <param name="data">Instance to deserialize from a string</param>
    /// <returns>Deserialized instance</returns>
    public static T DeserializeString<T>(string data)
    {
        XmlSerializer xml = new(typeof(T));
        using StringReader stream = new(data);
        return (T)xml.Deserialize(stream);
    }
}
