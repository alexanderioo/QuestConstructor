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
    private readonly string _sampleDataMarkerPath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public QuestRepository(IWebHostEnvironment environment)
    {
        _filePath = Path.Combine(environment.ContentRootPath, "Data", "quests.json");
        _sampleDataMarkerPath = Path.Combine(environment.ContentRootPath, "Data", ".sample-data-v2");
    }

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        var samples = SampleQuestFactory.CreateAll();

        if (!File.Exists(_filePath))
        {
            await WriteAllAsync(samples);
            File.WriteAllText(_sampleDataMarkerPath, "");
            return;
        }

        if (File.Exists(_sampleDataMarkerPath))
        {
            return;
        }

        var quests = await ReadAllAsync();
        var existingTitles = quests
            .Select(quest => quest.Title)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var sample in samples)
        {
            if (!existingTitles.Contains(sample.Title))
            {
                quests.Add(sample);
            }
        }

        await WriteAllAsync(quests);
        File.WriteAllText(_sampleDataMarkerPath, "");
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
