using System;
using System.IO;
using System.Text;
using WinFormsLearningGuide.Models;

namespace WinFormsLearningGuide.Services;

/// <summary>Helpers for the source code viewer: the text to copy, the suggested file name, and saving to disk.</summary>
public class CodeExampleService
{
    /// <summary>The program text with Windows line endings, ready for the clipboard or a file.</summary>
    public string GetText(ControlExample example) =>
        example.FullCode.Replace("\r", "").Replace("\n", Environment.NewLine);

    public string GetFileName(ControlDocumentation doc) => doc.Name + "Example.cs";

    /// <summary>Writes the example as a UTF-8 file without a byte order mark.</summary>
    public void Save(string path, ControlExample example) =>
        File.WriteAllText(path, GetText(example), new UTF8Encoding(false));
}
