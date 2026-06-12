using QuestConstructor.Contracts;
using QuestConstructor.Models;

namespace QuestConstructor.Services;

public sealed class QuestValidator
{
    public QuestValidationResult Validate(Quest quest)
    {
        var issues = new List<ValidationIssue>();

        if (string.IsNullOrWhiteSpace(quest.Title))
        {
            issues.Add(new("error", "У квеста должно быть название."));
        }

        if (quest.Scenes.Count == 0)
        {
            issues.Add(new("error", "Добавьте хотя бы одну сцену."));
            return new(false, 0, 0, issues);
        }

        var scenesById = quest.Scenes
            .GroupBy(scene => scene.Id)
            .ToDictionary(group => group.Key, group => group.First());

        if (!scenesById.ContainsKey(quest.StartSceneId))
        {
            issues.Add(new("error", "Начальная сцена не выбрана или удалена."));
        }

        foreach (var duplicate in quest.Scenes.GroupBy(scene => scene.Id).Where(group => group.Count() > 1))
        {
            issues.Add(new("error", "Обнаружены сцены с одинаковым идентификатором.", duplicate.Key));
        }

        foreach (var scene in quest.Scenes)
        {
            if (string.IsNullOrWhiteSpace(scene.Title))
            {
                issues.Add(new("error", "У сцены отсутствует название.", scene.Id));
            }

            if (!scene.IsEnding && scene.Choices.Count == 0)
            {
                issues.Add(new("error", $"Сцена «{scene.Title}» не имеет вариантов выбора.", scene.Id));
            }

            if (scene.IsEnding && scene.Choices.Count > 0)
            {
                issues.Add(new("warning", $"Финальная сцена «{scene.Title}» содержит лишние переходы.", scene.Id));
            }

            foreach (var choice in scene.Choices)
            {
                if (string.IsNullOrWhiteSpace(choice.Text))
                {
                    issues.Add(new("error", $"В сцене «{scene.Title}» есть вариант без текста.", scene.Id));
                }

                if (choice.NextSceneId is null || !scenesById.ContainsKey(choice.NextSceneId.Value))
                {
                    issues.Add(new("error", $"Переход «{choice.Text}» ведёт в несуществующую сцену.", scene.Id));
                }
            }
        }

        var reachable = FindReachableScenes(quest.StartSceneId, scenesById);
        foreach (var scene in quest.Scenes.Where(scene => !reachable.Contains(scene.Id)))
        {
            issues.Add(new("warning", $"Сцена «{scene.Title}» недостижима из начала квеста.", scene.Id));
        }

        if (!quest.Scenes.Any(scene => scene.IsEnding && reachable.Contains(scene.Id)))
        {
            issues.Add(new("error", "Из начальной сцены нельзя добраться ни до одной концовки."));
        }

        return new(
            issues.All(issue => issue.Level != "error"),
            quest.Scenes.Count,
            reachable.Count,
            issues);
    }

    private static HashSet<Guid> FindReachableScenes(
        Guid startSceneId,
        IReadOnlyDictionary<Guid, QuestScene> scenes)
    {
        var reachable = new HashSet<Guid>();
        var queue = new Queue<Guid>();

        if (scenes.ContainsKey(startSceneId))
        {
            queue.Enqueue(startSceneId);
        }

        // Breadth-first search walks the quest graph from its starting node.
        while (queue.TryDequeue(out var sceneId))
        {
            if (!reachable.Add(sceneId))
            {
                continue;
            }

            foreach (var nextId in scenes[sceneId].Choices
                         .Select(choice => choice.NextSceneId)
                         .Where(id => id.HasValue)
                         .Select(id => id!.Value))
            {
                if (scenes.ContainsKey(nextId) && !reachable.Contains(nextId))
                {
                    queue.Enqueue(nextId);
                }
            }
        }

        return reachable;
    }
}
