namespace MUD_MetroHra;

public class Item
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";

    public ItemUseEffect? UseEffect { get; set; }
}

public class ItemUseEffect
{
    public int HealAmount { get; set; } = 0;
    public bool CuresPoison { get; set; } = false;
    public bool Consumable { get; set; } = true;
}
