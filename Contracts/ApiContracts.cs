namespace QuestConstructor.Contracts;

public sealed record StartGameRequest(Guid QuestId);

public sealed record ValidationIssue(string Level, string Message, Guid? SceneId = null);

public sealed record QuestValidationResult(
    bool CanStart,
    int SceneCount,
    int ReachableSceneCount,
    IReadOnlyList<ValidationIssue> Issues);

public sealed record GameChoiceView(
    Guid Id,
    string Text,
    bool CanChoose,
    string UnavailableReason);

public sealed record GameView(
    Guid SessionId,
    Guid QuestId,
    string QuestTitle,
    Guid SceneId,
    string SceneTitle,
    string SceneText,
    int Health,
    IReadOnlyList<string> Inventory,
    bool IsCompleted,
    string EndingText,
    IReadOnlyList<GameChoiceView> Choices);

public sealed record ChoiceResult(
    bool Success,
    string Error,
    bool JustCompleted,
    GameView? View);
