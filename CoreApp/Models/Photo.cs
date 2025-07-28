using System;
using System.Text.Json.Serialization;

namespace CoreApp;

public class Photo
{
    public string FileName { get; set; } = string.Empty;
    public DateTime DateTaken { get; set; }

    [JsonIgnore]
    public byte[]? ImageData { get; set; }
}
