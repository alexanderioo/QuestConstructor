using System.Text.Json;
using QuestConstructor.Models;

namespace QuestConstructor.Services;

public sealed class QuestRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public QuestRepository(IWebHostEnvironment environment)
    {
        _filePath = Path.Combine(environment.ContentRootPath, "Data", "quests.json");
    }

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        if (File.Exists(_filePath))
        {
            return;
        }

        var sample = SampleQuestFactory.Create();
        await WriteAllAsync([sample]);
    }

    public async Task<IReadOnlyList<Quest>> GetAllAsync()
    {
        await _lock.WaitAsync();
        try
        {
            return await ReadAllAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<Quest?> GetAsync(Guid id) =>
        (await GetAllAsync()).FirstOrDefault(quest => quest.Id == id);

    public async Task SaveAsync(Quest quest)
    {
        await _lock.WaitAsync();
        try
        {
            var quests = await ReadAllAsync();
            var index = quests.FindIndex(item => item.Id == quest.Id);

            if (index >= 0)
            {
                quests[index] = quest;
            }
            else
            {
                quests.Add(quest);
            }

            await WriteAllAsync(quests);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        await _lock.WaitAsync();
        try
        {
            var quests = await ReadAllAsync();
            var deleted = quests.RemoveAll(quest => quest.Id == id) > 0;
            if (deleted)
            {
                await WriteAllAsync(quests);
            }

            return deleted;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<List<Quest>> ReadAllAsync()
    {
        await using var stream = File.OpenRead(_filePath);
        return await JsonSerializer.DeserializeAsync<List<Quest>>(stream, JsonOptions) ?? [];
    }

    private async Task WriteAllAsync(IReadOnlyCollection<Quest> quests)
    {
        var temporaryPath = _filePath + ".tmp";
        await using (var stream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(stream, quests, JsonOptions);
        }

        File.Move(temporaryPath, _filePath, true);
    }
}
