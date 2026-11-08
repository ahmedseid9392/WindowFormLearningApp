using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace WinFormsLearningGuide.Services;

public class UserData
{
    public bool DarkTheme { get; set; } = true;
    public string LastControl { get; set; } = "";
    public List<string> Favorites { get; set; } = new List<string>();
    public List<string> Recent { get; set; } = new List<string>();
    public List<string> Viewed { get; set; } = new List<string>();
}

/// <summary>Persists theme, favorites, recent items and progress in %AppData%.</summary>
public class UserDataService
{
    private const int MaxRecent = 15;
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { WriteIndented = true };

    private readonly string _directory;
    private readonly string _path;

    public UserData Data { get; private set; } = new UserData();

    public UserDataService()
    {
        _directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinFormsLearningGuide");
        _path = Path.Combine(_directory, "userdata.json");
    }

    public int ViewedCount => Data.Viewed.Count;

    public void Load()
    {
        try
        {
            if (File.Exists(_path))
                Data = JsonSerializer.Deserialize<UserData>(File.ReadAllText(_path)) ?? new UserData();
        }
        catch (Exception ex) when (ex is IOException || ex is JsonException || ex is UnauthorizedAccessException)
        {
            Data = new UserData();
        }

        Data.Favorites ??= new List<string>();
        Data.Recent ??= new List<string>();
        Data.Viewed ??= new List<string>();
        Data.LastControl ??= "";
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(_path, JsonSerializer.Serialize(Data, JsonOptions));
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            // Not being able to save preferences must never crash the app.
        }
    }

    public bool IsFavorite(string name) =>
        Data.Favorites.Contains(name, StringComparer.OrdinalIgnoreCase);

    /// <returns>The new favorite state.</returns>
    public bool ToggleFavorite(string name)
    {
        bool nowFavorite;
        if (IsFavorite(name))
        {
            Data.Favorites.RemoveAll(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase));
            nowFavorite = false;
        }
        else
        {
            Data.Favorites.Add(name);
            nowFavorite = true;
        }
        Save();
        return nowFavorite;
    }

    public void MarkViewed(string name)
    {
        Data.Recent.RemoveAll(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase));
        Data.Recent.Insert(0, name);
        if (Data.Recent.Count > MaxRecent)
            Data.Recent.RemoveRange(MaxRecent, Data.Recent.Count - MaxRecent);

        if (!Data.Viewed.Contains(name, StringComparer.OrdinalIgnoreCase))
            Data.Viewed.Add(name);
    }
}
