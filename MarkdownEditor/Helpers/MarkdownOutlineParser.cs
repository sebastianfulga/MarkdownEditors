using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using MarkdownEditor.Models;

namespace MarkdownEditor.Helpers;

/// <summary>
/// Parses markdown text to extract heading structure for the document outline.
/// </summary>
public static partial class MarkdownOutlineParser
{
    [GeneratedRegex(@"^(#{1,6})\s+(.+)$", RegexOptions.Multiline)]
    private static partial Regex HeadingRegex();

    /// <summary>
    /// Extracts a flat list of outline items from markdown text.
    /// </summary>
    public static ObservableCollection<OutlineItem> Parse(string markdownText)
    {
        var items = new ObservableCollection<OutlineItem>();

        if (string.IsNullOrWhiteSpace(markdownText))
            return items;

        var lines = markdownText.Split('\n');
        bool inCodeBlock = false;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');

            // Track fenced code blocks to avoid parsing headers inside them
            if (line.TrimStart().StartsWith("```") || line.TrimStart().StartsWith("~~~"))
            {
                inCodeBlock = !inCodeBlock;
                continue;
            }

            if (inCodeBlock)
                continue;

            var match = HeadingRegex().Match(line);
            if (match.Success)
            {
                var level = match.Groups[1].Value.Length;
                var title = match.Groups[2].Value.Trim();

                // Remove any trailing # characters (alternate heading syntax)
                title = title.TrimEnd('#').Trim();

                items.Add(new OutlineItem
                {
                    Title = title,
                    Level = level,
                    LineNumber = i + 1 // 1-based line number
                });
            }
        }

        return items;
    }

    /// <summary>
    /// Builds a hierarchical tree of outline items from a flat list.
    /// </summary>
    public static ObservableCollection<OutlineItem> BuildTree(ObservableCollection<OutlineItem> flatItems)
    {
        var root = new ObservableCollection<OutlineItem>();
        var stack = new Stack<OutlineItem>();

        foreach (var item in flatItems)
        {
            // Pop items from stack that are at same level or deeper
            while (stack.Count > 0 && stack.Peek().Level >= item.Level)
                stack.Pop();

            if (stack.Count == 0)
            {
                root.Add(item);
            }
            else
            {
                stack.Peek().Children.Add(item);
            }

            stack.Push(item);
        }

        return root;
    }
}
