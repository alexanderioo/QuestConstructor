using System.Text.Json;
using QuestConstructor.Contracts;
using QuestConstructor.Models;
using QuestConstructor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.WriteIndented = true;
});

builder.Services.AddSingleton<QuestRepository>();
builder.Services.AddSingleton<QuestValidator>();
builder.Services.AddSingleton<GameService>();

var app = builder.Build();

var repository = app.Services.GetRequiredService<QuestRepository>();
await repository.InitializeAsync();

app.UseDefaultFiles();
app.UseStaticFiles();

var api = app.MapGroup("/api");

api.MapGet("/health", () => Results.Ok(new { status = "ok" }));

api.MapGet("/quests", async (QuestRepository quests) =>
{
    var items = await quests.GetAllAsync();
    return Results.Ok(items.OrderByDescending(quest => quest.UpdatedAt));
});

api.MapGet("/quests/{id:guid}", async (Guid id, QuestRepository quests) =>
{
    var quest = await quests.GetAsync(id);
    return quest is null ? Results.NotFound() : Results.Ok(quest);
});

api.MapPost("/quests", async (Quest quest, QuestRepository quests) =>
{
    QuestDocument.PrepareNew(quest);
    await quests.SaveAsync(quest);
    return Results.Created($"/api/quests/{quest.Id}", quest);
});

api.MapPut("/quests/{id:guid}", async (Guid id, Quest quest, QuestRepository quests) =>
{
    var existing = await quests.GetAsync(id);
    if (existing is null)
    {
        return Results.NotFound();
    }

    QuestDocument.PrepareForUpdate(quest, existing);
    await quests.SaveAsync(quest);
    return Results.Ok(quest);
});

api.MapDelete("/quests/{id:guid}", async (Guid id, QuestRepository quests) =>
{
    var deleted = await quests.DeleteAsync(id);
    return deleted ? Results.NoContent() : Results.NotFound();
});

api.MapPost("/quests/{id:guid}/validate", async (
    Guid id,
    QuestRepository quests,
    QuestValidator validator) =>
{
    var quest = await quests.GetAsync(id);
    return quest is null
        ? Results.NotFound()
        : Results.Ok(validator.Validate(quest));
});

api.MapPost("/games/start", async (
    StartGameRequest request,
    QuestRepository quests,
    QuestValidator validator,
    GameService games) =>
{
    var quest = await quests.GetAsync(request.QuestId);
    if (quest is null)
    {
        return Results.NotFound(new { message = "Квест не найден." });
    }

    var validation = validator.Validate(quest);
    if (!validation.CanStart)
    {
        return Results.BadRequest(new
        {
            message = "Сначала исправьте ошибки в структуре квеста.",
            validation
        });
    }

    var game = games.Start(quest);
    quest.PlayCount++;
    await quests.SaveAsync(quest);
    return Results.Ok(game);
});

api.MapPost("/games/{sessionId:guid}/choices/{choiceId:guid}", async (
    Guid sessionId,
    Guid choiceId,
    QuestRepository quests,
    GameService games) =>
{
    var session = games.Find(sessionId);
    if (session is null)
    {
        return Results.NotFound(new { message = "Игровая сессия не найдена." });
    }

    var quest = await quests.GetAsync(session.QuestId);
    if (quest is null)
    {
        return Results.NotFound(new { message = "Квест был удалён." });
    }

    var result = games.MakeChoice(session, quest, choiceId);
    if (!result.Success)
    {
        return Results.BadRequest(new { message = result.Error });
    }

    if (result.JustCompleted)
    {
        quest.CompletionCount++;
        await quests.SaveAsync(quest);
    }

    return Results.Ok(result.View);
});

app.MapFallbackToFile("index.html");
app.Run();
