using QuestConstructor.Models;

namespace QuestConstructor.Services;

public static class SampleQuestFactory
{
    public static Quest Create()
    {
        var airlock = NewScene("Шлюз", "Вы приходите в себя у аварийного шлюза станции «Орион».");
        var corridor = NewScene("Тёмный коридор", "Свет не работает. Впереди слышен металлический скрежет.");
        var storage = NewScene("Склад", "На полках лежит аварийное оборудование.");
        var bridge = NewScene("Командный мостик", "Главный компьютер запрашивает ключ-карту.");
        var escape = NewScene("Эвакуация", "Вы запускаете спасательную капсулу.", true,
            "Станция остаётся позади. Вы успели передать сигнал бедствия и выжили.");
        var trapped = NewScene("Ложный путь", "Дверь блокируется, а запас кислорода заканчивается.", true,
            "Экспедиция закончилась, но журнал станции сохранил вашу историю.");

        airlock.Choices.Add(NewChoice("Осмотреть аварийный шкаф", storage.Id));
        airlock.Choices.Add(NewChoice("Идти по коридору в темноте", corridor.Id, healthChange: -25));

        storage.Choices.Add(NewChoice("Взять фонарь", corridor.Id, grantedItem: "Фонарь"));
        storage.Choices.Add(NewChoice("Взять ключ-карту", bridge.Id, grantedItem: "Ключ-карта"));

        corridor.Choices.Add(NewChoice("Осветить дорогу и пройти к мостику", bridge.Id, requiredItem: "Фонарь"));
        corridor.Choices.Add(NewChoice("Открыть подозрительный боковой люк", trapped.Id));

        bridge.Choices.Add(NewChoice(
            "Активировать протокол эвакуации",
            escape.Id,
            requiredItem: "Ключ-карта",
            consumesItem: true));
        bridge.Choices.Add(NewChoice("Вернуться к шлюзу", airlock.Id));

        var now = DateTimeOffset.UtcNow;
        return new Quest
        {
            Id = Guid.NewGuid(),
            Title = "Сигнал с «Ориона»",
            Description = "Демонстрационный научно-фантастический квест с предметами и несколькими концовками.",
            StartSceneId = airlock.Id,
            Scenes = [airlock, corridor, storage, bridge, escape, trapped],
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static QuestScene NewScene(
        string title,
        string text,
        bool isEnding = false,
        string endingText = "") =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Text = text,
            IsEnding = isEnding,
            EndingText = endingText
        };

    private static QuestChoice NewChoice(
        string text,
        Guid nextSceneId,
        string requiredItem = "",
        string grantedItem = "",
        bool consumesItem = false,
        int healthChange = 0) =>
        new()
        {
            Id = Guid.NewGuid(),
            Text = text,
            NextSceneId = nextSceneId,
            RequiredItem = requiredItem,
            GrantedItem = grantedItem,
            ConsumesRequiredItem = consumesItem,
            HealthChange = healthChange
        };
}
