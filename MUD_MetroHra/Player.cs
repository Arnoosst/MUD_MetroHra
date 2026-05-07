namespace MUD_MetroHra;

public class Player
{
    public string Name { get; set; } = "";
    public string CurrentRoomId { get; set; } = "zakladna";
    public List<Item> Inventory { get; set; } = new();
    public int InventoryCapacity { get; set; } = 5;

    public bool HasFinishedGame { get; set; } = false;
    public DateTime? FinishedAt { get; set; }

    public int Health { get; set; } = 100;
    public int MaxHealth { get; set; } = 100;
    public List<StatusEffect> ActiveEffects { get; set; } = new();

    public List<QuestProgress> ActiveQuests { get; set; } = new();
    public List<string> CompletedQuests { get; set; } = new();

    public bool HasEffect(string effectName)
        => ActiveEffects.Any(e => e.Name.Equals(effectName, StringComparison.OrdinalIgnoreCase));

    public void AddEffect(StatusEffect effect)
    {
        if (!HasEffect(effect.Name))
            ActiveEffects.Add(effect);
    }

    public void RemoveEffect(string effectName)
    {
        ActiveEffects.RemoveAll(e => e.Name.Equals(effectName, StringComparison.OrdinalIgnoreCase));
    }

    public bool HasQuest(string questId)
        => ActiveQuests.Any(q => q.QuestId == questId) || CompletedQuests.Contains(questId);

    public void AcceptQuest(string questId)
    {
        if (!HasQuest(questId))
            ActiveQuests.Add(new QuestProgress { QuestId = questId, Progress = "Prijat." });
    }
}

public class StatusEffect
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int RemainingTicks { get; set; } = 0;
    public int DamagePerTick { get; set; } = 0;
}

public class QuestProgress
{
    public string QuestId { get; set; } = "";
    public string Progress { get; set; } = "";
}
