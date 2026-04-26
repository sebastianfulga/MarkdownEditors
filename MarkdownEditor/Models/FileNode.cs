using System.Collections.ObjectModel;
using MarkdownEditor.Helpers;

namespace MarkdownEditor.Models;

/// <summary>
/// Represents a file or directory node in the file explorer tree.
/// </summary>
public class FileNode : ViewModelBase
{
    private bool _isExpanded;
    private bool _isSelected;

    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public bool IsDirectory { get; set; }
    public string Extension => IsDirectory ? "" : System.IO.Path.GetExtension(Name).ToLowerInvariant();
    public bool IsMarkdown => Extension is ".md" or ".markdown" or ".mdown" or ".mkd";

    public ObservableCollection<FileNode> Children { get; set; } = [];

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    /// <summary>
    /// Gets the icon glyph for this node (using Unicode symbols for simplicity).
    /// </summary>
    public string Icon => IsDirectory
        ? (IsExpanded ? "📂" : "📁")
        : IsMarkdown ? "📝" : "📄";
}
