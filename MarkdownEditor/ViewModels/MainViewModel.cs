using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using MarkdownEditor.Helpers;
using MarkdownEditor.Models;

namespace MarkdownEditor.ViewModels;

public enum ViewMode { Edit, Preview, Split }

/// <summary>
/// Main ViewModel orchestrating the file tree, editor, and outline panels.
/// </summary>
public class MainViewModel : ViewModelBase
{
    private string _rootFolderPath = string.Empty;
    private string _currentFilePath = string.Empty;
    private string _currentFileName = "No file open";
    private string _editorText = string.Empty;
    private bool _hasUnsavedChanges;
    private bool _isFileOpen;
    private string _statusText = "Ready";
    private ViewMode _viewMode = ViewMode.Edit;
    private readonly DispatcherTimer _outlineUpdateTimer;
    private readonly DispatcherTimer _previewUpdateTimer;

    public MainViewModel()
    {
        FileTree = [];
        OutlineItems = [];
        FlatOutlineItems = [];

        OpenFolderCommand = new RelayCommand(OpenFolder);
        OpenFileCommand = new RelayCommand(OpenFileFromDialog);
        SaveCommand = new RelayCommand(SaveFile, () => _isFileOpen && _hasUnsavedChanges);
        NewFileCommand = new RelayCommand(NewFile);
        RefreshTreeCommand = new RelayCommand(RefreshTree, () => !string.IsNullOrEmpty(_rootFolderPath));
        TogglePreviewCommand = new RelayCommand(TogglePreview);
        SetViewModeCommand = new RelayCommand(SetViewMode);

        // Debounce outline updates to avoid excessive parsing while typing
        _outlineUpdateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(400)
        };
        _outlineUpdateTimer.Tick += (_, _) =>
        {
            _outlineUpdateTimer.Stop();
            UpdateOutline();
        };

        _previewUpdateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _previewUpdateTimer.Tick += (_, _) =>
        {
            _previewUpdateTimer.Stop();
            RequestPreviewUpdate();
        };
    }

    #region Properties

    public ObservableCollection<FileNode> FileTree { get; }
    public ObservableCollection<OutlineItem> OutlineItems { get; set; }
    public ObservableCollection<OutlineItem> FlatOutlineItems { get; set; }

    public string RootFolderPath
    {
        get => _rootFolderPath;
        set
        {
            if (SetProperty(ref _rootFolderPath, value))
                OnPropertyChanged(nameof(RootFolderName));
        }
    }

    public string RootFolderName => string.IsNullOrEmpty(_rootFolderPath)
        ? "No folder open"
        : Path.GetFileName(_rootFolderPath);

    public string CurrentFilePath
    {
        get => _currentFilePath;
        set => SetProperty(ref _currentFilePath, value);
    }

    public string CurrentFileName
    {
        get => _currentFileName;
        set => SetProperty(ref _currentFileName, value);
    }

    public string EditorText
    {
        get => _editorText;
        set
        {
            if (SetProperty(ref _editorText, value))
            {
                HasUnsavedChanges = true;
                _outlineUpdateTimer.Stop();
                _outlineUpdateTimer.Start();

                if (_viewMode is ViewMode.Preview or ViewMode.Split)
                {
                    _previewUpdateTimer.Stop();
                    _previewUpdateTimer.Start();
                }
            }
        }
    }

    public ViewMode CurrentViewMode
    {
        get => _viewMode;
        set
        {
            if (SetProperty(ref _viewMode, value))
            {
                OnPropertyChanged(nameof(IsEditorVisible));
                OnPropertyChanged(nameof(IsPreviewVisible));
                OnPropertyChanged(nameof(ViewModeLabel));
                if (_viewMode is ViewMode.Preview or ViewMode.Split)
                    RequestPreviewUpdate();
            }
        }
    }

    public bool IsEditorVisible => _viewMode is ViewMode.Edit or ViewMode.Split;
    public bool IsPreviewVisible => _viewMode is ViewMode.Preview or ViewMode.Split;

    public string ViewModeLabel => _viewMode switch
    {
        ViewMode.Edit => "📝 Edit",
        ViewMode.Preview => "👁 Preview",
        ViewMode.Split => "◫ Split",
        _ => "📝 Edit"
    };

    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        set
        {
            if (SetProperty(ref _hasUnsavedChanges, value))
                OnPropertyChanged(nameof(WindowTitle));
        }
    }

    public bool IsFileOpen
    {
        get => _isFileOpen;
        set => SetProperty(ref _isFileOpen, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public string WindowTitle
    {
        get
        {
            var modified = _hasUnsavedChanges ? " •" : "";
            return _isFileOpen
                ? $"{_currentFileName}{modified} — Markdown Editor"
                : "Markdown Editor";
        }
    }

    #endregion

    #region Commands

    public RelayCommand OpenFolderCommand { get; }
    public RelayCommand OpenFileCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand NewFileCommand { get; }
    public RelayCommand RefreshTreeCommand { get; }
    public RelayCommand TogglePreviewCommand { get; }
    public RelayCommand SetViewModeCommand { get; }

    /// <summary>
    /// Event raised when a file should be loaded into the editor.
    /// The MainWindow subscribes to this to set AvalonEdit text.
    /// </summary>
    public event Action<string>? FileContentLoaded;

    /// <summary>
    /// Event raised when the outline requests scrolling to a specific line.
    /// </summary>
    public event Action<int>? ScrollToLineRequested;

    /// <summary>
    /// Event raised when the preview HTML should be updated.
    /// </summary>
    public event Action<string>? PreviewHtmlUpdated;

    #endregion

    #region File Tree

    public void OpenFolder()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select a folder to open"
        };

        if (dialog.ShowDialog() == true)
        {
            RootFolderPath = dialog.FolderName;
            LoadFileTree(dialog.FolderName);
            StatusText = $"Opened folder: {RootFolderName}";
        }
    }

    public void LoadFileTree(string folderPath)
    {
        FileTree.Clear();
        try
        {
            var rootNode = BuildFileTree(folderPath);
            foreach (var child in rootNode.Children)
                FileTree.Add(child);
        }
        catch (Exception ex)
        {
            StatusText = $"Error loading folder: {ex.Message}";
        }
    }

    private FileNode BuildFileTree(string path)
    {
        var dirInfo = new DirectoryInfo(path);
        var node = new FileNode
        {
            Name = dirInfo.Name,
            FullPath = dirInfo.FullName,
            IsDirectory = true,
            IsExpanded = true
        };

        try
        {
            // Add subdirectories
            foreach (var dir in dirInfo.GetDirectories().OrderBy(d => d.Name))
            {
                // Skip hidden and system directories
                if (dir.Attributes.HasFlag(FileAttributes.Hidden) ||
                    dir.Name.StartsWith('.'))
                    continue;

                node.Children.Add(BuildFileTree(dir.FullName));
            }

            // Add files (show all but highlight markdown)
            foreach (var file in dirInfo.GetFiles().OrderBy(f => f.Name))
            {
                if (file.Attributes.HasFlag(FileAttributes.Hidden))
                    continue;

                node.Children.Add(new FileNode
                {
                    Name = file.Name,
                    FullPath = file.FullName,
                    IsDirectory = false
                });
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Skip directories we can't access
        }

        return node;
    }

    private void RefreshTree()
    {
        if (!string.IsNullOrEmpty(_rootFolderPath))
            LoadFileTree(_rootFolderPath);
    }

    #endregion

    #region File Operations

    public void OpenFileFromTree(FileNode fileNode)
    {
        if (fileNode.IsDirectory)
            return;

        LoadFile(fileNode.FullPath);
    }

    private void OpenFileFromDialog()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Markdown files (*.md;*.markdown)|*.md;*.markdown|All files (*.*)|*.*",
            Title = "Open Markdown File"
        };

        if (dialog.ShowDialog() == true)
        {
            LoadFile(dialog.FileName);
        }
    }

    public void LoadFile(string filePath)
    {
        try
        {
            var content = File.ReadAllText(filePath);
            CurrentFilePath = filePath;
            CurrentFileName = Path.GetFileName(filePath);
            IsFileOpen = true;
            HasUnsavedChanges = false;

            _editorText = content;
            OnPropertyChanged(nameof(EditorText));
            OnPropertyChanged(nameof(WindowTitle));

            FileContentLoaded?.Invoke(content);
            UpdateOutline();
            StatusText = $"Opened: {CurrentFileName}";
        }
        catch (Exception ex)
        {
            StatusText = $"Error opening file: {ex.Message}";
            MessageBox.Show($"Could not open file:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void SaveFile()
    {
        if (!_isFileOpen || string.IsNullOrEmpty(_currentFilePath))
            return;

        try
        {
            File.WriteAllText(_currentFilePath, _editorText);
            HasUnsavedChanges = false;
            StatusText = $"Saved: {CurrentFileName}";
        }
        catch (Exception ex)
        {
            StatusText = $"Error saving: {ex.Message}";
            MessageBox.Show($"Could not save file:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void NewFile()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Markdown files (*.md)|*.md|All files (*.*)|*.*",
            Title = "Create New Markdown File",
            DefaultExt = ".md"
        };

        if (dialog.ShowDialog() == true)
        {
            File.WriteAllText(dialog.FileName, $"# {Path.GetFileNameWithoutExtension(dialog.FileName)}\n\n");
            LoadFile(dialog.FileName);

            // Refresh tree if the file is within the open folder
            if (!string.IsNullOrEmpty(_rootFolderPath) &&
                dialog.FileName.StartsWith(_rootFolderPath))
            {
                RefreshTree();
            }
        }
    }

    #endregion

    #region Outline

    public void UpdateOutline()
    {
        var flat = MarkdownOutlineParser.Parse(_editorText);
        var tree = MarkdownOutlineParser.BuildTree(flat);

        FlatOutlineItems = flat;
        OutlineItems = tree;

        OnPropertyChanged(nameof(OutlineItems));
        OnPropertyChanged(nameof(FlatOutlineItems));
    }

    public void NavigateToOutlineItem(OutlineItem item)
    {
        ScrollToLineRequested?.Invoke(item.LineNumber);
    }

    #endregion

    #region Preview

    private void TogglePreview()
    {
        CurrentViewMode = _viewMode switch
        {
            ViewMode.Edit => ViewMode.Split,
            ViewMode.Split => ViewMode.Preview,
            ViewMode.Preview => ViewMode.Edit,
            _ => ViewMode.Edit
        };
    }

    private void SetViewMode(object? parameter)
    {
        if (parameter is string mode)
        {
            CurrentViewMode = mode switch
            {
                "Edit" => ViewMode.Edit,
                "Preview" => ViewMode.Preview,
                "Split" => ViewMode.Split,
                _ => ViewMode.Edit
            };
        }
    }

    public void RequestPreviewUpdate()
    {
        var html = MarkdownHtmlConverter.ToHtml(_editorText);
        PreviewHtmlUpdated?.Invoke(html);
    }

    #endregion

    #region Drop Support

    public void HandleFileDrop(string[] files)
    {
        if (files.Length == 0) return;

        var path = files[0];
        if (Directory.Exists(path))
        {
            RootFolderPath = path;
            LoadFileTree(path);
        }
        else if (File.Exists(path))
        {
            LoadFile(path);
        }
    }

    #endregion
}
