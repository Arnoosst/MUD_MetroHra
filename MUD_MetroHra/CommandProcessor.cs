namespace MUD_MetroHra;
using System.Text;

public class CommandProcessor
{
    public static async Task<string> ProcessAsync(string input, Player player, GameWorld world, GameServer server)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "Zadej prikaz.";

        var parts = input.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var command = parts[0].ToLower();
        var argument = parts.Length > 1 ? parts[1].Trim().ToLower() : "";

        TickEffects(player);

        return command switch
        {
            "pomoc" => GetHelp(),
            "prozkoumej" => Look(player, world, server),
            "jdi" => await GoAsync(argument, player, world, server),
            "vezmi" => Take(argument, player, world),
            "odloz" => Drop(argument, player, world),
            "inventar" => Inventory(player),
            "mluv" => Talk(argument, player, world),
            "rekni" => await SayAsync(argument, player, server),
            "krik" => await ShoutAsync(argument, player, server),
            "pouzij" => UseItem(argument, player, world),
            "quest" => ShowQuest(player),
            "zebricek" => ShowLeaderboard(server),
            "stav" => ShowStatus(player),
            "konec" => "Odpojuji te ze hry.",
            _ => "Neznamy prikaz. Napis 'pomoc'."
        };
    }

    public static string Process(string input, Player player, GameWorld world, GameServer server)
    {
        return ProcessAsync(input, player, world, server).GetAwaiter().GetResult();
    }

    private static string GetHelp()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Prikazy:");
        sb.AppendLine("pomoc          - seznam prikazu");
        sb.AppendLine("prozkoumej     - vypise informace o mistnosti");
        sb.AppendLine("jdi <smer>     - presun do jine mistnosti");
        sb.AppendLine("vezmi <predmet>- vezme predmet do inventare");
        sb.AppendLine("odloz <predmet>- odlozi predmet do mistnosti");
        sb.AppendLine("inventar       - vypise obsah inventare");
        sb.AppendLine("mluv <npc>     - promluvi s NPC");
        sb.AppendLine("rekni <zprava> - rekne zpravu hracum v mistnosti (M1)");
        sb.AppendLine("krik <zprava>  - rekne zpravu vsem hracum na serveru (M1)");
        sb.AppendLine("pouzij <predmet> - pouzije predmet z inventare (M8)");
        sb.AppendLine("quest          - zobraz stav svych questu (M10)");
        sb.AppendLine("stav           - zobraz stav hrace vcetne efektu (M12)");
        sb.AppendLine("zebricek       - zobraz hrace co dokoncili hru (P1)");
        sb.AppendLine("konec          - odpoji te ze hry");
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

        if (room.PoisonOnEnter)
            sb.AppendLine("! Vzduch je tady tezky a jedovaty.");

        sb.AppendLine($"Vychody: {exits}");
        sb.AppendLine($"Predmety: {items}");
        sb.AppendLine($"NPC: {npcs}");
        sb.AppendLine($"Hraci: {otherPlayers}");

        return sb.ToString();
    }

    private static async Task<string> GoAsync(string direction, Player player, GameWorld world, GameServer server)
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

        if (nextRoom.RequiredItemId != null)
        {
            bool hasItem = player.Inventory.Any(i => i.Id == nextRoom.RequiredItemId);
            if (!hasItem)
            {
                var requiredItem = nextRoom.RequiredItemId;
                return $"Vstup je zablokovan. Potrebujes predmet: {requiredItem}";
            }
        }

        player.CurrentRoomId = nextRoom.Id;

        var sb = new StringBuilder();
        sb.AppendLine($"Presunul ses do: {nextRoom.Name}");
        sb.AppendLine();
        sb.Append(Look(player, world, server));

        if (nextRoom.PoisonOnEnter && !player.HasEffect("otrava"))
        {
            player.AddEffect(new StatusEffect
            {
                Name = "otrava",
                Description = "Jsi otraveny toxickym vzduchem.",
                RemainingTicks = 5,
                DamagePerTick = 5
            });
            sb.AppendLine("! Toxicky vzduch te otraví! Jsi OTRAVENY.");
        }

        if (!player.HasFinishedGame &&
            player.CurrentRoomId == "posledni" &&
            player.Inventory.Any(i => i.Id == "karta") &&
            player.Inventory.Any(i => i.Id == "artefakt"))
        {
            player.HasFinishedGame = true;
            player.FinishedAt = DateTime.Now;
            sb.AppendLine();
            sb.AppendLine("*** GRATULUJI, dokoncil jsi hru! ***");
            sb.AppendLine($"*** Cas dokonceni: {player.FinishedAt:HH:mm:ss} ***");

            await server.BroadcastToAllAsync($"*** Hrac {player.Name} dokoncil hru! ***");
        }

        CheckQuests(player, sb);

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

        var sb = new StringBuilder();
        sb.AppendLine($"Sebral jsi predmet: {item.Name}");

        CheckQuestItemPickup(player, item.Id, sb);

        return sb.ToString();
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

        var sb = new StringBuilder();
        sb.AppendLine($"{npc.Name}: {npc.Dialogue}");

        if (npc.GivesQuestId != null && !player.HasQuest(npc.GivesQuestId))
        {
            var quest = world.GetQuest(npc.GivesQuestId);
            if (quest != null)
            {
                player.AcceptQuest(npc.GivesQuestId);
                sb.AppendLine($"[QUEST] Prijal jsi ukol: {quest.Name}");
                sb.AppendLine($"[QUEST] {quest.Description}");
            }
        }

        return sb.ToString();
    }

    private static async Task<string> SayAsync(string message, Player player, GameServer server)
    {
        if (string.IsNullOrWhiteSpace(message))
            return "Pouziti: rekni <zprava>";

        await server.BroadcastToRoomAsync(
            player.CurrentRoomId,
            $"{player.Name} rika: {message}",
            player.Name);

        return $"Ty: {message}";
    }

    private static async Task<string> ShoutAsync(string message, Player player, GameServer server)
    {
        if (string.IsNullOrWhiteSpace(message))
            return "Pouziti: krik <zprava>";

        await server.BroadcastToAllAsync($"[KRIK] {player.Name}: {message}", player.Name);
        return $"[KRIK] Ty: {message}";
    }

    private static string UseItem(string itemName, Player player, GameWorld world)
    {
        if (string.IsNullOrWhiteSpace(itemName))
            return "Pouziti: pouzij <predmet>";

        var item = player.Inventory.FirstOrDefault(i =>
            i.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase) ||
            i.Id.Equals(itemName, StringComparison.OrdinalIgnoreCase));

        if (item == null)
            return "Takovy predmet v inventari nemas.";

        if (item.UseEffect == null)
            return $"Predmet '{item.Name}' nejde pouzit.";

        var sb = new StringBuilder();
        var effect = item.UseEffect;

        if (effect.HealAmount > 0)
        {
            int before = player.Health;
            player.Health = Math.Min(player.MaxHealth, player.Health + effect.HealAmount);
            sb.AppendLine($"Pouzil jsi {item.Name}. Zdravi: {before} -> {player.Health}");
        }

        if (effect.CuresPoison)
        {
            player.RemoveEffect("otrava");
            sb.AppendLine("Otrava byla vylecena!");
        }

        if (effect.Consumable)
            player.Inventory.Remove(item);

        return sb.ToString();
    }

    private static string ShowQuest(Player player)
    {
        if (!player.ActiveQuests.Any() && !player.CompletedQuests.Any())
            return "Nemas zadne ukoly. Pomuv s NPC pro ziskani questu.";

        var sb = new StringBuilder();
        sb.AppendLine("=== Tvoje ukoly ===");

        foreach (var q in player.ActiveQuests)
            sb.AppendLine($"[AKTIVNI] {q.QuestId} — {q.Progress}");

        foreach (var qId in player.CompletedQuests)
            sb.AppendLine($"[SPLNENO] {qId}");

        return sb.ToString();
    }

    private static string ShowStatus(Player player)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"=== Stav hrace {player.Name} ===");
        sb.AppendLine($"Zdravi: {player.Health}/{player.MaxHealth}");

        if (player.ActiveEffects.Any())
        {
            sb.AppendLine("Aktivni efekty:");
            foreach (var e in player.ActiveEffects)
                sb.AppendLine($"  - {e.Name}: {e.Description} ({e.RemainingTicks} tahu)");
        }
        else
        {
            sb.AppendLine("Zadne aktivni efekty.");
        }

        return sb.ToString();
    }

    private static string ShowLeaderboard(GameServer server)
    {
        var finished = server.GetLeaderboard();
        if (!finished.Any())
            return "Jeste nikdo hru nedokoncil. Bud prvni!";

        var sb = new StringBuilder();
        sb.AppendLine("=== Zebricek dokonceni ===");
        int rank = 1;
        foreach (var entry in finished)
            sb.AppendLine($"{rank++}. {entry.Name} — {entry.FinishedAt:dd.MM.yyyy HH:mm:ss}");

        return sb.ToString();
    }

    private static void TickEffects(Player player)
    {
        var toRemove = new List<StatusEffect>();

        foreach (var effect in player.ActiveEffects)
        {
            if (effect.DamagePerTick > 0)
            {
                player.Health = Math.Max(0, player.Health - effect.DamagePerTick);
            }

            effect.RemainingTicks--;
            if (effect.RemainingTicks <= 0)
                toRemove.Add(effect);
        }

        foreach (var e in toRemove)
            player.ActiveEffects.Remove(e);
    }

    private static void CheckQuests(Player player, StringBuilder sb)
    {
        foreach (var q in player.ActiveQuests.ToList())
        {
            if (q.QuestId == "quest_zakladna" && player.CurrentRoomId == "zakladna"
                && player.Inventory.Any(i => i.Id == "karta"))
            {
                player.ActiveQuests.Remove(q);
                player.CompletedQuests.Add(q.QuestId);
                player.InventoryCapacity += 2;
                sb.AppendLine("[QUEST SPLNEN] Donesl jsi kartu na zakladnu! Kapacita inventare +2.");
            }
        }
    }

    private static void CheckQuestItemPickup(Player player, string itemId, StringBuilder sb)
    {
        foreach (var q in player.ActiveQuests)
        {
            if (q.QuestId == "quest_zakladna" && itemId == "karta")
            {
                q.Progress = "Mas kartu! Vrat se na zakladnu.";
                sb.AppendLine("[QUEST] Mas kartu! Vrat se na zakladnu.");
            }
        }
    }
}
