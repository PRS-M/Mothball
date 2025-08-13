using System;
using System.Text.Json.Serialization;

namespace CoreApp;

public class Photo
{
    public Photo()
    {
        FileName = $"photo_{Guid.NewGuid()}.jpg";
    }

    public Photo(string fileName, byte[]? imageData)
    {
        FileName = fileName;
        ImageData = imageData;
    }

    public string FileName { get; set; }

    [JsonIgnore]
    public byte[]? ImageData { get; set; }
}
