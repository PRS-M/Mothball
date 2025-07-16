using System;

namespace CoreApp;

public class Photo
{
    public string Id { get; set; }
    public byte[] ImageData { get; set; }
    public DateTime DateTaken { get; set; }
}
