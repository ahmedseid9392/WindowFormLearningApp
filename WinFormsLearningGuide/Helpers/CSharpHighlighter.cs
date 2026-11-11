using System;
using System.Collections.Generic;

namespace WinFormsLearningGuide.Helpers;

public enum TokenKind { Plain, Keyword, Type, String, Comment, Number }

/// <summary>A colored piece of one code line. Pieces that are plain text are not reported.</summary>
public readonly struct CodeToken
{
    public int Start { get; }
    public int Length { get; }
    public TokenKind Kind { get; }

    public CodeToken(int start, int length, TokenKind kind)
    {
        Start = start;
        Length = length;
        Kind = kind;
    }
}

/// <summary>
/// A small, line-based C# tokenizer for syntax highlighting. It understands keywords, known type names,
/// strings, characters, numbers and // comments. It does not need to be a full lexer: the examples in this
/// application are simple and never contain block comments or strings that span lines.
/// </summary>
public static class CSharpHighlighter
{
    private static readonly HashSet<string> Keywords = new HashSet<string>(StringComparer.Ordinal)
    {
        "abstract", "as", "async", "await", "base", "bool", "break", "byte", "case", "catch", "char", "class", "const",
        "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "false", "finally",
        "float", "for", "foreach", "if", "in", "int", "interface", "internal", "is", "long", "namespace", "new",
        "null", "object", "out", "override", "params", "private", "protected", "public", "readonly", "ref", "return",
        "sealed", "short", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "using",
        "var", "virtual", "void", "while"
    };

    private static readonly HashSet<string> DeclarationWords = new HashSet<string>(StringComparer.Ordinal)
    {
        "new", "class", "private", "public", "protected", "internal", "static", "readonly", "override", "as", "is"
    };

    private static readonly HashSet<string> KnownTypes = new HashSet<string>(StringComparer.Ordinal);

    static CSharpHighlighter()
    {
        const string names =
            // controls and components
            "Label Button TextBox RichTextBox MaskedTextBox CheckBox RadioButton ComboBox ListBox CheckedListBox PictureBox " +
            "DateTimePicker MonthCalendar NumericUpDown ProgressBar TrackBar LinkLabel ToolTip ErrorProvider Panel GroupBox " +
            "TabControl TabPage SplitContainer FlowLayoutPanel TableLayoutPanel MenuStrip ContextMenuStrip ToolStrip StatusStrip " +
            "ToolStripButton ToolStripLabel ToolStripComboBox ToolStripMenuItem ToolStripStatusLabel ToolStripSeparator ToolStripItem " +
            "DataGridView BindingSource BindingNavigator OpenFileDialog SaveFileDialog FolderBrowserDialog ColorDialog FontDialog " +
            "Timer BackgroundWorker NotifyIcon Form Control Application MessageBox DialogResult " +
            // drawing
            "Color Point Size Font FontStyle Image Icon SystemIcons Cursors Cursor Padding Rectangle Graphics Pens Brushes " +
            "ColorTranslator ContentAlignment " +
            // enums and helper types
            "BorderStyle DockStyle AnchorStyles FlatStyle FormWindowState MessageBoxButtons MouseButtons Keys ScrollBars " +
            "SelectionMode ComboBoxStyle TabAlignment Orientation ProgressBarStyle TickStyle LinkBehavior LinkArea FixedPanel " +
            "FlowDirection SizeType ColumnStyle RowStyle CheckState MaskFormat RichTextBoxStreamType DateTimePickerFormat " +
            "PictureBoxSizeMode Appearance AutoCompleteMode ErrorIconAlignment HorizontalAlignment ToolTipIcon " +
            "ToolStripItemDisplayStyle ToolStripItemAlignment ToolStripGripStyle DataGridViewSelectionMode " +
            "DataGridViewAutoSizeColumnsMode DataGridViewColumn ListSortDirection DataTable " +
            // system
            "DateTime TimeSpan Environment Math File Path Directory Stream Process ProcessStartInfo List Dictionary Task " +
            "Thread Exception Console Convert";
        foreach (string name in names.Split(' '))
            if (name.Length > 0) KnownTypes.Add(name);
    }

    public static List<CodeToken> Tokenize(string line)
    {
        var tokens = new List<CodeToken>();
        if (string.IsNullOrEmpty(line)) return tokens;

        int i = 0;
        string previousWord = "";
        char previousChar = ' ';

        while (i < line.Length)
        {
            char ch = line[i];

            if (char.IsWhiteSpace(ch))
            {
                i++;
                continue;
            }

            // comment
            if (ch == '/' && i + 1 < line.Length && line[i + 1] == '/')
            {
                tokens.Add(new CodeToken(i, line.Length - i, TokenKind.Comment));
                break;
            }

            // string: "..."  @"..."  $"..."
            if (ch == '"' || ((ch == '@' || ch == '$') && i + 1 < line.Length && line[i + 1] == '"'))
            {
                int start = i;
                bool verbatim = ch == '@';
                i += ch == '"' ? 1 : 2;
                while (i < line.Length)
                {
                    if (line[i] == '\\' && !verbatim) { i += 2; continue; }
                    if (line[i] == '"')
                    {
                        if (verbatim && i + 1 < line.Length && line[i + 1] == '"') { i += 2; continue; }
                        i++;
                        break;
                    }
                    i++;
                }
                if (i > line.Length) i = line.Length;
                tokens.Add(new CodeToken(start, i - start, TokenKind.String));
                previousWord = "";
                previousChar = '"';
                continue;
            }

            // character literal
            if (ch == '\'')
            {
                int start = i;
                i++;
                while (i < line.Length)
                {
                    if (line[i] == '\\') { i += 2; continue; }
                    if (line[i] == '\'') { i++; break; }
                    i++;
                }
                if (i > line.Length) i = line.Length;
                tokens.Add(new CodeToken(start, i - start, TokenKind.String));
                previousWord = "";
                previousChar = '\'';
                continue;
            }

            // number
            if (char.IsDigit(ch))
            {
                int start = i;
                while (i < line.Length && (char.IsLetterOrDigit(line[i]) ||
                       (line[i] == '.' && i + 1 < line.Length && char.IsDigit(line[i + 1]))))
                    i++;
                tokens.Add(new CodeToken(start, i - start, TokenKind.Number));
                previousWord = "";
                previousChar = '0';
                continue;
            }

            // identifier or keyword
            if (char.IsLetter(ch) || ch == '_')
            {
                int start = i;
                while (i < line.Length && (char.IsLetterOrDigit(line[i]) || line[i] == '_'))
                    i++;
                string word = line.Substring(start, i - start);
                TokenKind kind = Classify(word, previousWord, previousChar);
                if (kind != TokenKind.Plain)
                    tokens.Add(new CodeToken(start, i - start, kind));
                previousWord = word;
                previousChar = word[word.Length - 1];
                continue;
            }

            // punctuation
            previousChar = ch;
            previousWord = "";
            i++;
        }

        return tokens;
    }

    private static TokenKind Classify(string word, string previousWord, char previousChar)
    {
        if (Keywords.Contains(word)) return TokenKind.Keyword;
        if (previousChar == '.') return TokenKind.Plain;                 // a member such as Color.Red -> Red
        if (word.EndsWith("EventArgs", StringComparison.Ordinal) || KnownTypes.Contains(word)) return TokenKind.Type;
        if (DeclarationWords.Contains(previousWord) && char.IsUpper(word[0])) return TokenKind.Type;
        return TokenKind.Plain;
    }
}
