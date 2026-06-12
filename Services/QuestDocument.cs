using QuestConstructor.Models;

namespace QuestConstructor.Services;

public static class QuestDocument
{
    public static void PrepareNew(Quest quest)
    {
        quest.Id = Guid.NewGuid();
        quest.CreatedAt = DateTimeOffset.UtcNow;
        quest.UpdatedAt = quest.CreatedAt;
        quest.PlayCount = 0;
        quest.CompletionCount = 0;
        NormalizeNestedIds(quest);
    }

    public static void PrepareForUpdate(Quest quest, Quest existing)
    {
        quest.Id = existing.Id;
        quest.CreatedAt = existing.CreatedAt;
        quest.UpdatedAt = DateTimeOffset.UtcNow;
        quest.PlayCount = existing.PlayCount;
        quest.CompletionCount = existing.CompletionCount;
        NormalizeNestedIds(quest);
    }

    private static void NormalizeNestedIds(Quest quest)
    {
        quest.Title = quest.Title.Trim();
        quest.Description = quest.Description.Trim();
        quest.Scenes ??= [];

        foreach (var scene in quest.Scenes)
        {
            if (scene.Id == Guid.Empty)
            {
                scene.Id = Guid.NewGuid();
            }

            scene.Title = scene.Title.Trim();
            scene.Text = scene.Text.Trim();
            scene.EndingText = scene.EndingText.Trim();
            scene.Choices ??= [];

            foreach (var choice in scene.Choices)
            {
                if (choice.Id == Guid.Empty)
                {
                    choice.Id = Guid.NewGuid();
                }

                choice.Text = choice.Text.Trim();
                choice.RequiredItem = choice.RequiredItem.Trim();
                choice.GrantedItem = choice.GrantedItem.Trim();
            }
        }

        if (quest.StartSceneId == Guid.Empty && quest.Scenes.Count > 0)
        {
            quest.StartSceneId = quest.Scenes[0].Id;
        }
    }
}
