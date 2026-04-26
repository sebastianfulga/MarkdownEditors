using System.Collections.ObjectModel;
using MarkdownEditor.Helpers;

namespace MarkdownEditor.Models;

/// <summary>
/// Represents a heading entry in the document outline sidebar.
/// </summary>
public class OutlineItem : ViewModelBase
{
    private bool _isExpanded = true;

    /// <summary>
    /// The heading text (without the # prefix).
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The heading level (1-6, corresponding to # through ######).
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// The 1-based line number in the document where this heading appears.
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Child headings (sub-sections).
    /// </summary>
    public ObservableCollection<OutlineItem> Children { get; set; } = [];

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    /// <summary>
    /// Indentation margin based on heading level.
    /// </summary>
    public double IndentMargin => (Level - 1) * 16.0;
}
