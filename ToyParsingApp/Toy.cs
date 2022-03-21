namespace ToyParsingApp;

public class Toy
{
    public string? RegionName { get; set; }
    public string? ToyName { get; set; }
    public string? Price { get; set; }
    public string? OldPrice { get; set; }
    public string? Availability { get; set; }
    public string? Sections { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public string? ItemUrl { get; set; }
}