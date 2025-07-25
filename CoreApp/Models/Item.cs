using System;
using System.Text.Json.Serialization;

namespace CoreApp;

public class Item
{
    public string Name { get; set; }
    public List<string> PhotoFileNames { get; set; }
    public List<Photo> Photos { get; set; }

    [JsonIgnore]
    public List<PhotoWithData> PhotosWithData { get; set; }
}
