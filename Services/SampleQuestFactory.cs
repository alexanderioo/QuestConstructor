using QuestConstructor.Models;

namespace QuestConstructor.Services;

public static class SampleQuestFactory
{
    public static IReadOnlyList<Quest> CreateAll() =>
    [
        CreateOrionQuest(),
        CreateLighthouseQuest(),
        CreateLastTrainQuest(),
        CreateArchiveQuest(),
        CreateSunlessCityQuest()
    ];

    private static Quest CreateOrionQuest()
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

        return NewQuest(
            "Сигнал с «Ориона»",
            "Научно-фантастический квест об аварии на орбитальной станции.",
            airlock,
            [airlock, corridor, storage, bridge, escape, trapped],
            playCount: 24,
            completionCount: 15,
            ageInDays: 18);
    }

    private static Quest CreateLighthouseQuest()
    {
        var pier = NewScene("Затопленный причал",
            "Шторм отрезал остров от материка. Маяк погас, а в тумане приближается корабль.");
        var house = NewScene("Дом смотрителя",
            "На столе лежит журнал смотрителя, а на стене висит связка старых ключей.");
        var generator = NewScene("Генераторная",
            "Двигатель исправен, но топливный кран закрыт ржавым замком.");
        var shore = NewScene("Каменный берег",
            "Волны выбрасывают на берег ящики с аварийным снаряжением.");
        var tower = NewScene("Башня маяка",
            "Линза цела. Для сигнала нужен источник света или питание генератора.");
        var rescue = NewScene("Свет в тумане",
            "Луч маяка разрезает туман, и корабль меняет курс.", true,
            "Вы спасли экипаж и нашли пропавшего смотрителя на борту спасательного катера.");
        var wreck = NewScene("Последняя волна",
            "В темноте слышится удар корпуса о рифы.", true,
            "Маяк так и не загорелся. Утром море возвращает на берег обломки.");

        pier.Choices.Add(NewChoice("Искать подсказки в доме смотрителя", house.Id));
        pier.Choices.Add(NewChoice("Спуститься к каменному берегу", shore.Id, healthChange: -10));
        house.Choices.Add(NewChoice("Взять ключ от генераторной", generator.Id, grantedItem: "Ключ от генераторной"));
        house.Choices.Add(NewChoice("Подняться в башню без подготовки", tower.Id));
        generator.Choices.Add(NewChoice(
            "Открыть замок и запустить генератор",
            tower.Id,
            requiredItem: "Ключ от генераторной",
            grantedItem: "Работающий генератор",
            consumesItem: true));
        shore.Choices.Add(NewChoice("Забрать сигнальную ракету", tower.Id, grantedItem: "Сигнальная ракета"));
        shore.Choices.Add(NewChoice("Попытаться уплыть на старой лодке", wreck.Id, healthChange: -40));
        tower.Choices.Add(NewChoice(
            "Включить электрическую лампу",
            rescue.Id,
            requiredItem: "Работающий генератор"));
        tower.Choices.Add(NewChoice(
            "Запустить сигнальную ракету",
            rescue.Id,
            requiredItem: "Сигнальная ракета",
            consumesItem: true));
        tower.Choices.Add(NewChoice("Ждать рассвета", wreck.Id));

        return NewQuest(
            "Тайна погасшего маяка",
            "Мистическая история о шторме, пропавшем смотрителе и корабле среди рифов.",
            pier,
            [pier, house, generator, shore, tower, rescue, wreck],
            playCount: 41,
            completionCount: 29,
            ageInDays: 12);
    }

    private static Quest CreateLastTrainQuest()
    {
        var hall = NewScene("Пустой вокзал",
            "Часы показывают 23:57. Последний поезд отправится через три минуты, но кассы закрыты.");
        var office = NewScene("Кабинет дежурного",
            "На столе лежит забытый билет, рядом мигает служебный телефон.");
        var platform = NewScene("Платформа № 4",
            "Поезд уже подан. Проводник проверяет документы у каждого пассажира.");
        var tunnel = NewScene("Служебный тоннель",
            "Тоннель выводит прямо к хвостовому вагону, но путь затоплен.");
        var carriage = NewScene("Ночной вагон",
            "Внутри нет пассажиров. На каждом месте лежит газета с завтрашней датой.");
        var destination = NewScene("Город на рассвете",
            "Поезд останавливается у незнакомого города, которого нет на карте.", true,
            "Вы сходите на платформу и понимаете, что получили шанс изменить завтрашний день.");
        var missed = NewScene("После полуночи",
            "Поезд исчезает за поворотом, а часы на вокзале начинают идти назад.", true,
            "Вы остались на станции. Следующий поезд придёт только через двадцать лет.");

        hall.Choices.Add(NewChoice("Проверить кабинет дежурного", office.Id));
        hall.Choices.Add(NewChoice("Бежать прямо на платформу", platform.Id));
        office.Choices.Add(NewChoice("Взять забытый билет", platform.Id, grantedItem: "Билет"));
        office.Choices.Add(NewChoice("Ответить на служебный телефон", tunnel.Id, healthChange: -5));
        platform.Choices.Add(NewChoice("Показать билет проводнику", carriage.Id, requiredItem: "Билет"));
        platform.Choices.Add(NewChoice("Попытаться пройти без билета", missed.Id));
        tunnel.Choices.Add(NewChoice("Пробраться через воду к вагону", carriage.Id, healthChange: -30));
        tunnel.Choices.Add(NewChoice("Вернуться в зал ожидания", hall.Id));
        carriage.Choices.Add(NewChoice("Остаться в поезде до конечной", destination.Id));
        carriage.Choices.Add(NewChoice("Испугаться и выйти до отправления", missed.Id));

        return NewQuest(
            "Последний поезд",
            "Мистический квест о вокзале, где расписание связывает настоящее и будущее.",
            hall,
            [hall, office, platform, tunnel, carriage, destination, missed],
            playCount: 37,
            completionCount: 21,
            ageInDays: 9);
    }

    private static Quest CreateArchiveQuest()
    {
        var lobby = NewScene("Вестибюль корпорации",
            "Ночью здание пустует. В архиве на 47-м этаже хранится доказательство эксперимента.");
        var security = NewScene("Пост охраны",
            "Камеры следят за лифтами. На столе охранника лежит временный пропуск.");
        var maintenance = NewScene("Технический этаж",
            "Здесь можно отключить камеры, но система требует код инженера.");
        var archive = NewScene("Архив 47",
            "Серверная стойка защищена биометрическим замком и резервной сигнализацией.");
        var lab = NewScene("Скрытая лаборатория",
            "В терминале открыт журнал экспериментов. Данные можно скопировать на носитель.");
        var published = NewScene("Прямая трансляция",
            "Документы уходят журналистам и появляются в открытом доступе.", true,
            "Расследование становится главной новостью. Корпорация больше не сможет скрыть проект.");
        var captured = NewScene("Красный протокол",
            "Сигнализация блокирует этаж, а из лифта выходит группа безопасности.", true,
            "Операция провалена. Однако часть данных успела сохраниться в облаке.");

        lobby.Choices.Add(NewChoice("Проникнуть на пост охраны", security.Id));
        lobby.Choices.Add(NewChoice("Подняться по технической лестнице", maintenance.Id, healthChange: -15));
        security.Choices.Add(NewChoice("Забрать временный пропуск", archive.Id, grantedItem: "Пропуск"));
        security.Choices.Add(NewChoice("Изучить журнал инженера", maintenance.Id, grantedItem: "Код инженера"));
        maintenance.Choices.Add(NewChoice(
            "Отключить камеры",
            archive.Id,
            requiredItem: "Код инженера",
            grantedItem: "Камеры отключены"));
        maintenance.Choices.Add(NewChoice("Вскрыть щиток вручную", archive.Id, healthChange: -35));
        archive.Choices.Add(NewChoice("Открыть архив пропуском", lab.Id, requiredItem: "Пропуск"));
        archive.Choices.Add(NewChoice(
            "Войти, пока камеры отключены",
            lab.Id,
            requiredItem: "Камеры отключены"));
        archive.Choices.Add(NewChoice("Взломать дверь напрямую", captured.Id));
        lab.Choices.Add(NewChoice("Передать материалы журналистам", published.Id));
        lab.Choices.Add(NewChoice("Удалить следы и уйти", captured.Id));

        return NewQuest(
            "Архив 47",
            "Кибердетектив о тайной лаборатории, цифровых уликах и рискованном проникновении.",
            lobby,
            [lobby, security, maintenance, archive, lab, published, captured],
            playCount: 53,
            completionCount: 34,
            ageInDays: 6);
    }

    private static Quest CreateSunlessCityQuest()
    {
        var gate = NewScene("Ворота Авроры",
            "Над городом уже десять лет не восходит солнце. Сегодня во дворце проводят ритуал затмения.");
        var market = NewScene("Ночной рынок",
            "Торговка предлагает зеркало, способное удержать первый луч рассвета.");
        var catacombs = NewScene("Катакомбы",
            "Под городом спрятана печать прежней династии и древний проход во дворец.");
        var palace = NewScene("Зал затмения",
            "Регент начинает ритуал. Над троном вращается чёрный кристалл.");
        var observatory = NewScene("Королевская обсерватория",
            "Через раскрытый купол виден единственный луч солнца.");
        var sunrise = NewScene("Первый рассвет",
            "Зеркало отражает луч в кристалл, и вечная ночь рассыпается.", true,
            "Жители впервые за десять лет видят голубое небо. Город встречает новый день.");
        var exile = NewScene("Город теней",
            "Ритуал завершается, и тьма становится вечной.", true,
            "Вы покидаете Аврору, унося историю о городе, который отказался от солнца.");

        gate.Choices.Add(NewChoice("Отправиться на ночной рынок", market.Id));
        gate.Choices.Add(NewChoice("Найти тайный вход в катакомбы", catacombs.Id, healthChange: -10));
        market.Choices.Add(NewChoice("Обменять медальон на солнечное зеркало", palace.Id, grantedItem: "Солнечное зеркало"));
        market.Choices.Add(NewChoice("Расспросить торговку о дворце", observatory.Id));
        catacombs.Choices.Add(NewChoice("Забрать королевскую печать", palace.Id, grantedItem: "Королевская печать"));
        catacombs.Choices.Add(NewChoice("Следовать за голосами в темноте", exile.Id, healthChange: -45));
        palace.Choices.Add(NewChoice(
            "Приказать страже остановить ритуал",
            observatory.Id,
            requiredItem: "Королевская печать"));
        palace.Choices.Add(NewChoice(
            "Направить зеркало на кристалл",
            sunrise.Id,
            requiredItem: "Солнечное зеркало"));
        palace.Choices.Add(NewChoice("Попытаться разбить кристалл", exile.Id, healthChange: -50));
        observatory.Choices.Add(NewChoice("Забрать зеркало у хранителя", palace.Id, grantedItem: "Солнечное зеркало"));
        observatory.Choices.Add(NewChoice("Открыть купол и поймать первый луч", sunrise.Id, requiredItem: "Солнечное зеркало"));

        return NewQuest(
            "Город без солнца",
            "Фэнтезийное приключение о вечной ночи, дворцовом ритуале и возвращении рассвета.",
            gate,
            [gate, market, catacombs, palace, observatory, sunrise, exile],
            playCount: 68,
            completionCount: 46,
            ageInDays: 3);
    }

    private static Quest NewQuest(
        string title,
        string description,
        QuestScene startScene,
        List<QuestScene> scenes,
        int playCount,
        int completionCount,
        int ageInDays)
    {
        var updatedAt = DateTimeOffset.UtcNow.AddDays(-ageInDays);
        return new Quest
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            StartSceneId = startScene.Id,
            Scenes = scenes,
            CreatedAt = updatedAt.AddDays(-14),
            UpdatedAt = updatedAt,
            PlayCount = playCount,
            CompletionCount = completionCount
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
