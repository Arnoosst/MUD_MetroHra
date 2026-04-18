namespace MUD_MetroHra;
using System.Text;

public class CommandProcessor
{
    public static string Process(string input, Player player, GameWorld world, GameServer server)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "Zadej prikaz.";

        var parts = input.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var command = parts[0].ToLower();
        var argument = parts.Length > 1 ? parts[1].Trim().ToLower() : "";

        return command switch
        {
            "pomoc" => GetHelp(),
            "prozkoumej" => Look(player, world, server),
            "jdi" => Go(argument, player, world, server),
            "vezmi" => Take(argument, player, world),
            "odloz" => Drop(argument, player, world),
            "inventar" => Inventory(player),
            "mluv" => Talk(argument, player, world),
            "konec" => "Odpojuji te ze hry.",
            _ => "Neznamy prikaz. Napis 'pomoc'."
        };
    }

    private static string GetHelp()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Prikazy:");
        sb.AppendLine("pomoc - seznam prikazu");
        sb.AppendLine("prozkoumej - vypise informace o mistnosti");
        sb.AppendLine("jdi <smer> - presun do jine mistnosti");
        sb.AppendLine("vezmi <predmet> - vezme predmet do inventare");
        sb.AppendLine("odloz <predmet> - odlozi predmet do mistnosti");
        sb.AppendLine("inventar - vypise obsah inventare");
        sb.AppendLine("mluv <npc> - promluvi s NPC");
        sb.AppendLine("konec - odpoji te ze hry");
        return sb.ToString();
    }

    public static string Look(Player player, GameWorld world, GameServer server)
    {
        var room = world.GetRoom(player.CurrentRoomId);
        if (room == null)
            return "Aktualni mistnost nebyla nalezena.";

        var exits = room.Exits.Any() ? string.Join(", ", room.Exits.Keys) : "zadne";
        var items = room.Items.Any() ? string.Join(", ", room.Items.Select(i => i.Name)) : "zadne";
        var npcs = room.Npcs.Any() ? string.Join(", ", room.Npcs.Select(n => n.Name)) : "zadne";
        var players = server.GetOtherPlayersInRoom(room.Id, player.Name);
        var otherPlayers = players.Any() ? string.Join(", ", players) : "zadni";

        var sb = new StringBuilder();
        sb.AppendLine($"[{room.Name}]");
        sb.AppendLine(room.Description);
        sb.AppendLine($"Vychody: {exits}");
        sb.AppendLine($"Predmety: {items}");
        sb.AppendLine($"NPC: {npcs}");
        sb.AppendLine($"Hraci: {otherPlayers}");

        return sb.ToString();
    }

    private static string Go(string direction, Player player, GameWorld world, GameServer server)
    {
        if (string.IsNullOrWhiteSpace(direction))
            return "Pouziti: jdi <smer>";

        var room = world.GetRoom(player.CurrentRoomId);
        if (room == null)
            return "Aktualni mistnost nebyla nalezena.";

        if (!room.Exits.TryGetValue(direction, out var nextRoomId))
            return "Tudy se jit neda.";

        var nextRoom = world.GetRoom(nextRoomId);
        if (nextRoom == null)
            return "Cilova mistnost neexistuje.";

        player.CurrentRoomId = nextRoom.Id;

        var sb = new StringBuilder();
        sb.AppendLine($"Presunul ses do: {nextRoom.Name}");
        sb.AppendLine();
        sb.Append(Look(player, world, server));

        if (!player.HasFinishedGame &&
            player.CurrentRoomId == "posledni" &&
            player.Inventory.Any(i => i.Id == "karta") &&
            player.Inventory.Any(i => i.Id == "artefakt"))
        {
            player.HasFinishedGame = true;
            player.FinishedAt = DateTime.Now;
            sb.AppendLine();
            sb.AppendLine("*** GRATULUJI, dokoncil jsi hru! ***");
        }

        return sb.ToString();
    }

    private static string Take(string itemName, Player player, GameWorld world)
    {
        if (string.IsNullOrWhiteSpace(itemName))
            return "Pouziti: vezmi <predmet>";

        var room = world.GetRoom(player.CurrentRoomId);
        if (room == null)
            return "Aktualni mistnost nebyla nalezena.";

        if (player.Inventory.Count >= player.InventoryCapacity)
            return $"Inventar je plny. Kapacita: {player.InventoryCapacity}";

        var item = room.Items.FirstOrDefault(i =>
            i.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase) ||
            i.Id.Equals(itemName, StringComparison.OrdinalIgnoreCase));

        if (item == null)
            return "Takovy predmet tu neni.";

        room.Items.Remove(item);
        player.Inventory.Add(item);

        return $"Sebral jsi predmet: {item.Name}";
    }

    private static string Drop(string itemName, Player player, GameWorld world)
    {
        if (string.IsNullOrWhiteSpace(itemName))
            return "Pouziti: odloz <predmet>";

        var room = world.GetRoom(player.CurrentRoomId);
        if (room == null)
            return "Aktualni mistnost nebyla nalezena.";

        var item = player.Inventory.FirstOrDefault(i =>
            i.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase) ||
            i.Id.Equals(itemName, StringComparison.OrdinalIgnoreCase));

        if (item == null)
            return "Takovy predmet v inventari nemas.";

        player.Inventory.Remove(item);
        room.Items.Add(item);

        return $"Odlozil jsi predmet: {item.Name}";
    }

    private static string Inventory(Player player)
    {
        var items = player.Inventory.Any()
            ? string.Join(", ", player.Inventory.Select(i => i.Name))
            : "prazdny";

        var sb = new StringBuilder();
        sb.AppendLine($"Inventar: {items}");
        sb.AppendLine($"Kapacita: {player.Inventory.Count}/{player.InventoryCapacity}");
        return sb.ToString();
    }

    private static string Talk(string npcName, Player player, GameWorld world)
    {
        if (string.IsNullOrWhiteSpace(npcName))
            return "Pouziti: mluv <npc>";

        var room = world.GetRoom(player.CurrentRoomId);
        if (room == null)
            return "Aktualni mistnost nebyla nalezena.";

        var npc = room.Npcs.FirstOrDefault(n =>
            n.Name.Equals(npcName, StringComparison.OrdinalIgnoreCase));

        if (npc == null)
            return "Takove NPC tu neni.";

        return $"{npc.Name}: {npc.Dialogue}";
    }
}