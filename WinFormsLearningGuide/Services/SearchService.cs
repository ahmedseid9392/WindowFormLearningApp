using System;
using System.Collections.Generic;
using System.Linq;
using WinFormsLearningGuide.Models;

namespace WinFormsLearningGuide.Services;

/// <summary>Simple ranked search over control names, summaries, properties and events.</summary>
public class SearchService
{
    private readonly DocumentationService _docs;

    public SearchService(DocumentationService docs)
    {
        _docs = docs;
    }

    public IReadOnlyList<ControlDocumentation> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return _docs.All;

        string[] tokens = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var results = new List<(ControlDocumentation Doc, int Score, int Index)>();

        int index = 0;
        foreach (ControlDocumentation doc in _docs.All)
        {
            int total = 0;
            bool matchesAll = true;
            foreach (string token in tokens)
            {
                int s = Score(doc, token);
                if (s == 0) { matchesAll = false; break; }
                total += s;
            }
            if (matchesAll) results.Add((doc, total, index));
            index++;
        }

        return results.OrderByDescending(r => r.Score)
                      .ThenBy(r => r.Index)
                      .Select(r => r.Doc)
                      .ToList();
    }

    private static int Score(ControlDocumentation d, string token)
    {
        if (string.Equals(d.Name, token, StringComparison.OrdinalIgnoreCase)) return 100;
        if (d.Name.StartsWith(token, StringComparison.OrdinalIgnoreCase)) return 80;
        if (Has(d.Name, token)) return 60;
        if (Has(d.ClassName, token)) return 50;
        if (d.ImportantProperties.Any(p => Has(p, token))) return 20;
        if (d.ImportantEvents.Any(p => Has(p, token))) return 20;
        if (d.Events.Any(ev => Has(ev.Name, token))) return 18;
        if (d.Methods.Any(m => Has(m.Name, token))) return 18;
        if (Has(d.Category, token)) return 15;
        if (Has(d.Summary, token)) return 10;
        return 0;
    }

    private static bool Has(string text, string token) =>
        !string.IsNullOrEmpty(text) && text.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
}
