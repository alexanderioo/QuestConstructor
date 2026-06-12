using System.Collections.Concurrent;
using QuestConstructor.Contracts;
using QuestConstructor.Models;

namespace QuestConstructor.Services;

public sealed class GameService
{
    private readonly ConcurrentDictionary<Guid, GameSession> _sessions = new();

    public GameSession? Find(Guid id) =>
        _sessions.TryGetValue(id, out var session) ? session : null;

    public GameView Start(Quest quest)
    {
        var session = new GameSession
        {
            QuestId = quest.Id,
            CurrentSceneId = quest.StartSceneId
        };

        _sessions[session.Id] = session;
        UpdateCompletionState(session, quest);
        return CreateView(session, quest);
    }

    public ChoiceResult MakeChoice(GameSession session, Quest quest, Guid choiceId)
    {
        if (session.IsCompleted)
        {
            return new(false, "Этот квест уже завершён.", false, null);
        }

        var scene = quest.Scenes.FirstOrDefault(item => item.Id == session.CurrentSceneId);
        var choice = scene?.Choices.FirstOrDefault(item => item.Id == choiceId);
        if (choice is null)
        {
            return new(false, "Такого варианта выбора в текущей сцене нет.", false, null);
        }

        if (!string.IsNullOrWhiteSpace(choice.RequiredItem) &&
            !session.Inventory.Contains(choice.RequiredItem))
        {
            return new(false, $"Для этого действия нужен предмет: {choice.RequiredItem}.", false, null);
        }

        if (choice.ConsumesRequiredItem && !string.IsNullOrWhiteSpace(choice.RequiredItem))
        {
            session.Inventory.Remove(choice.RequiredItem);
        }

        if (!string.IsNullOrWhiteSpace(choice.GrantedItem))
        {
            session.Inventory.Add(choice.GrantedItem);
        }

        session.Health = Math.Clamp(session.Health + choice.HealthChange, 0, 100);
        if (choice.NextSceneId.HasValue)
        {
            session.CurrentSceneId = choice.NextSceneId.Value;
        }

        var justCompleted = UpdateCompletionState(session, quest);
        return new(true, "", justCompleted, CreateView(session, quest));
    }

    private static bool UpdateCompletionState(GameSession session, Quest quest)
    {
        var wasCompleted = session.IsCompleted;
        var scene = quest.Scenes.First(scene => scene.Id == session.CurrentSceneId);
        session.IsCompleted = scene.IsEnding || session.Health == 0;
        return !wasCompleted && session.IsCompleted;
    }

    private static GameView CreateView(GameSession session, Quest quest)
    {
        var scene = quest.Scenes.First(item => item.Id == session.CurrentSceneId);
        var choices = session.IsCompleted
            ? []
            : scene.Choices.Select(choice =>
            {
                var canChoose = string.IsNullOrWhiteSpace(choice.RequiredItem) ||
                                session.Inventory.Contains(choice.RequiredItem);
                var reason = canChoose ? "" : $"Нужен предмет: {choice.RequiredItem}";
                return new GameChoiceView(choice.Id, choice.Text, canChoose, reason);
            }).ToList();

        var endingText = session.Health == 0
            ? "Вы потеряли все очки здоровья. Попробуйте пройти квест иначе."
            : scene.EndingText;

        return new(
            session.Id,
            quest.Id,
            quest.Title,
            scene.Id,
            scene.Title,
            scene.Text,
            session.Health,
            session.Inventory.OrderBy(item => item).ToList(),
            session.IsCompleted,
            endingText,
            choices);
    }
}
