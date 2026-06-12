namespace QuestConstructor.Models;

// Quest is the root document stored in JSON. Scenes and choices live inside it,
// so one file contains everything required to edit and play a quest.
public sealed class Quest
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public Guid StartSceneId { get; set; }
    public List<QuestScene> Scenes { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public int PlayCount { get; set; }
    public int CompletionCount { get; set; }
}

public sealed class QuestScene
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Text { get; set; } = "";
    public bool IsEnding { get; set; }
    public string EndingText { get; set; } = "";
    public List<QuestChoice> Choices { get; set; } = [];
}

public sealed class QuestChoice
{
    public Guid Id { get; set; }
    public string Text { get; set; } = "";
    public Guid? NextSceneId { get; set; }
    public string RequiredItem { get; set; } = "";
    public string GrantedItem { get; set; } = "";
    public bool ConsumesRequiredItem { get; set; }
    public int HealthChange { get; set; }
}
