namespace QuestConstructor.Models;

// Sessions are intentionally kept in memory: quest definitions are persistent,
// while an unfinished playthrough disappears after the server restarts.
public sealed class GameSession
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid QuestId { get; init; }
    public Guid CurrentSceneId { get; set; }
    public int Health { get; set; } = 100;
    public HashSet<string> Inventory { get; } = new(StringComparer.OrdinalIgnoreCase);
    public bool IsCompleted { get; set; }
}
