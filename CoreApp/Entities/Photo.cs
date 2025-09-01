using System.Text.Json.Serialization;

namespace CoreApp.Entities;

public class Photo
{
    public Photo()
    {
        FileName = $"{Guid.NewGuid()}.jpg";
    }

    [JsonConstructor]
    public Photo(string fileName)
    {
        FileName = fileName;
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
