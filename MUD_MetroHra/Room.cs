namespace MUD_MetroHra;

public class Room
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";

    public Dictionary<string, string> Exits { get; set; } = new();
    public List<Item> Items { get; set; } = new();
    public List<Npc> Npcs { get; set; } = new();

    public string? RequiredItemId { get; set; }

    public bool PoisonOnEnter { get; set; } = false;
}
