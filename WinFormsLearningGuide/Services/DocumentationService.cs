using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using WinFormsLearningGuide.Models;

namespace WinFormsLearningGuide.Services;

/// <summary>Loads the JSON learning content and exposes it to the UI.</summary>
public class DocumentationService
{
    private static readonly string[] DataFiles =
    {
        "CommonControls.json", "Containers.json", "Menus.json",
        "DataControls.json", "Dialogs.json", "Components.json"
    };

    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly List<ControlDocumentation> _all = new List<ControlDocumentation>();
    private readonly Dictionary<string, ControlDocumentation> _byName =
        new Dictionary<string, ControlDocumentation>(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _categories = new List<string>();

    public IReadOnlyList<ControlDocumentation> All => _all;
    public IReadOnlyList<string> Categories => _categories;

    public void Load(string dataFolder)
    {
        _all.Clear();
        _byName.Clear();
        _categories.Clear();

        foreach (string file in DataFiles)
        {
            string path = Path.Combine(dataFolder, file);
            if (!File.Exists(path))
                throw new FileNotFoundException("Missing data file: " + path);

            List<ControlDocumentation> items =
                JsonSerializer.Deserialize<List<ControlDocumentation>>(File.ReadAllText(path), JsonOptions);
            if (items == null) continue;

            foreach (ControlDocumentation doc in items)
            {
                if (string.IsNullOrWhiteSpace(doc.Name))
                    throw new InvalidDataException("A control without a name was found in " + file);
                if (_byName.ContainsKey(doc.Name))
                    throw new InvalidDataException("Duplicate control '" + doc.Name + "' in " + file);

                _all.Add(doc);
                _byName[doc.Name] = doc;
                if (!_categories.Contains(doc.Category))
                    _categories.Add(doc.Category);
            }
        }

        LoadContent(dataFolder);
        LoadProperties(dataFolder);
        LoadEvents(dataFolder);
        LoadMethods(dataFolder);
        LoadExamples(dataFolder);
    }

    /// <summary>
    /// Loads every *.json file in the Content sub-folder and attaches each entry to the control
    /// with the same name. Throws if a content entry names an unknown control.
    /// </summary>
    private void LoadContent(string dataFolder)
    {
        string folder = Path.Combine(dataFolder, "Content");
        if (!Directory.Exists(folder)) return;

        foreach (string path in Directory.GetFiles(folder, "*.json").OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            string fileName = Path.GetFileName(path);
            List<ControlContent> items;
            try
            {
                items = JsonSerializer.Deserialize<List<ControlContent>>(File.ReadAllText(path), JsonOptions);
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException("Invalid JSON in " + fileName + ": " + ex.Message, ex);
            }
            if (items == null) continue;

            foreach (ControlContent content in items)
            {
                ControlDocumentation doc = Find(content.Name);
                if (doc == null)
                    throw new InvalidDataException("Content file " + fileName + " describes unknown control '" + content.Name + "'.");
                doc.Content = content;
            }
        }
    }

    /// <summary>
    /// Phase 3. Loads Data/Properties/*.properties.json (one entry per control, matched by name) and
    /// Data/Properties/Inherited.json (property sets shared by every Control or ToolStripItem).
    /// Throws if an entry names an unknown control.
    /// </summary>
    private void LoadProperties(string dataFolder)
    {
        string folder = Path.Combine(dataFolder, "Properties");
        if (!Directory.Exists(folder)) return;

        foreach (string path in Directory.GetFiles(folder, "*.properties.json").OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            string fileName = Path.GetFileName(path);
            List<ControlProperties> items = ReadJson<List<ControlProperties>>(path);
            if (items == null) continue;

            foreach (ControlProperties entry in items)
            {
                ControlDocumentation doc = Find(entry.Name);
                if (doc == null)
                    throw new InvalidDataException("Properties file " + fileName + " describes unknown control '" + entry.Name + "'.");
                doc.Properties = entry.Properties ?? new List<PropertyDocumentation>();
            }
        }

        var shared = new Dictionary<string, List<PropertyDocumentation>>(StringComparer.OrdinalIgnoreCase);
        string inheritedPath = Path.Combine(folder, "Inherited.json");
        if (File.Exists(inheritedPath))
        {
            List<ControlProperties> sets = ReadJson<List<ControlProperties>>(inheritedPath);
            if (sets != null)
                foreach (ControlProperties entrySet in sets)
                    shared[entrySet.Name] = entrySet.Properties ?? new List<PropertyDocumentation>();
        }

        foreach (ControlDocumentation doc in _all)
        {
            string baseName = doc.Inheritance.Contains("ToolStripItem") ? "ToolStripItem"
                            : doc.Inheritance.Contains("Control") ? "Control"
                            : "";
            if (baseName.Length == 0) continue;
            if (!shared.TryGetValue(baseName, out List<PropertyDocumentation> baseProperties)) continue;

            // A property documented on the control itself replaces the shared entry of the same name.
            doc.InheritedFrom = baseName;
            doc.InheritedProperties = baseProperties
                .Where(b => !doc.Properties.Any(o => string.Equals(o.Name, b.Name, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }
    }

    private static IEnumerable<string> JsonFiles(string dataFolder, string subFolder, string pattern)
    {
        string folder = Path.Combine(dataFolder, subFolder);
        if (!Directory.Exists(folder)) return new string[0];
        return Directory.GetFiles(folder, pattern).OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private ControlDocumentation RequireControl(string name, string path)
    {
        ControlDocumentation doc = Find(name);
        if (doc == null)
            throw new InvalidDataException(Path.GetFileName(path) + " describes unknown control '" + name + "'.");
        return doc;
    }

    private static string BaseSetName(ControlDocumentation doc)
    {
        if (doc.Inheritance.Contains("ToolStripItem")) return "ToolStripItem";
        if (doc.Inheritance.Contains("Control")) return "Control";
        return "";
    }

    /// <summary>Phase 4. Data/Events/*.events.json: the events of each control.</summary>
    private void LoadEvents(string dataFolder)
    {
        foreach (string path in JsonFiles(dataFolder, "Events", "*.events.json"))
        {
            List<ControlEvents> items = ReadJson<List<ControlEvents>>(path);
            if (items == null) continue;
            foreach (ControlEvents entry in items)
                RequireControl(entry.Name, path).Events = entry.Events ?? new List<EventDocumentation>();
        }
    }

    /// <summary>Phase 4. Data/Methods/*.methods.json plus Data/Methods/Inherited.json (shared Control / ToolStripItem methods).</summary>
    private void LoadMethods(string dataFolder)
    {
        foreach (string path in JsonFiles(dataFolder, "Methods", "*.methods.json"))
        {
            List<ControlMethods> items = ReadJson<List<ControlMethods>>(path);
            if (items == null) continue;
            foreach (ControlMethods entry in items)
                RequireControl(entry.Name, path).Methods = entry.Methods ?? new List<MethodDocumentation>();
        }

        string inheritedPath = Path.Combine(dataFolder, "Methods", "Inherited.json");
        if (!File.Exists(inheritedPath)) return;
        List<ControlMethods> sets = ReadJson<List<ControlMethods>>(inheritedPath);
        if (sets == null) return;

        var shared = new Dictionary<string, List<MethodDocumentation>>(StringComparer.OrdinalIgnoreCase);
        foreach (ControlMethods entrySet in sets)
            shared[entrySet.Name] = entrySet.Methods ?? new List<MethodDocumentation>();

        foreach (ControlDocumentation doc in _all)
        {
            string baseName = BaseSetName(doc);
            if (baseName.Length == 0) continue;
            if (!shared.TryGetValue(baseName, out List<MethodDocumentation> baseMethods)) continue;
            doc.InheritedMethods = baseMethods
                .Where(b => !doc.Methods.Any(o => string.Equals(o.Name, b.Name, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }
    }

    /// <summary>Phase 5. Data/Examples/*.examples.json: one complete example program per control.</summary>
    private void LoadExamples(string dataFolder)
    {
        foreach (string path in JsonFiles(dataFolder, "Examples", "*.examples.json"))
        {
            List<ControlExample> items = ReadJson<List<ControlExample>>(path);
            if (items == null) continue;
            foreach (ControlExample example in items)
            {
                ControlDocumentation doc = RequireControl(example.Name, path);
                if (example.Segments == null || example.Segments.Count == 0)
                    throw new InvalidDataException("The example for '" + example.Name + "' in " + Path.GetFileName(path) + " has no code.");
                doc.Example = example;
            }
        }
    }

    private static T ReadJson<T>(string path)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Invalid JSON in " + Path.GetFileName(path) + ": " + ex.Message, ex);
        }
    }

    public ControlDocumentation Find(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        _byName.TryGetValue(name, out ControlDocumentation doc);
        return doc;
    }

    public int IndexOf(ControlDocumentation doc) => _all.IndexOf(doc);

    public IEnumerable<ControlDocumentation> InCategory(string category) =>
        _all.Where(d => d.Category == category);
}
