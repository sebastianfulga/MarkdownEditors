using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using MarkdownEditor.Models;
using MarkdownEditor.ViewModels;

namespace MarkdownEditor;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private bool _webViewReady;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        // Subscribe to ViewModel events
        _viewModel.FileContentLoaded += OnFileContentLoaded;
        _viewModel.ScrollToLineRequested += OnScrollToLineRequested;
        _viewModel.PreviewHtmlUpdated += OnPreviewHtmlUpdated;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        // Wire up AvalonEdit text changes to ViewModel
        MarkdownTextEditor.TextChanged += (_, _) =>
        {
            if (_viewModel.IsFileOpen)
            {
                _viewModel.EditorText = MarkdownTextEditor.Text;
                UpdateCursorPosition();
            }
        };

        MarkdownTextEditor.TextArea.Caret.PositionChanged += (_, _) => UpdateCursorPosition();

        // Load markdown syntax highlighting
        LoadMarkdownHighlighting();

        // Set editor colors
        ConfigureEditor();

        // Initialize WebView2
        InitializeWebView();
    }

    #region WebView2

    private async void InitializeWebView()
    {
        try
        {
            await PreviewWebView.EnsureCoreWebView2Async();
            _webViewReady = true;

            // Configure WebView2 settings
            var settings = PreviewWebView.CoreWebView2.Settings;
            settings.IsScriptEnabled = false;
            settings.AreDefaultContextMenusEnabled = false;
            settings.IsStatusBarEnabled = false;
            settings.AreDevToolsEnabled = false;

            // Show empty dark page initially
            PreviewWebView.CoreWebView2.NavigateToString(
                "<html><body style='background:#1E1E2E'></body></html>");
        }
        catch (Exception ex)
        {
            _viewModel.StatusText = $"WebView2 init failed: {ex.Message}";
        }
    }

    private void OnPreviewHtmlUpdated(string html)
    {
        if (_webViewReady && PreviewWebView.CoreWebView2 != null)
        {
            PreviewWebView.CoreWebView2.NavigateToString(html);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.CurrentViewMode))
        {
            UpdateViewModeLayout();
        }
    }

    private void UpdateViewModeLayout()
    {
        switch (_viewModel.CurrentViewMode)
        {
            case ViewMode.Edit:
                EditorColumn.Width = new GridLength(1, GridUnitType.Star);
                PreviewSplitterColumn.Width = new GridLength(0);
                PreviewColumn.Width = new GridLength(0);
                break;

            case ViewMode.Preview:
                EditorColumn.Width = new GridLength(0);
                PreviewSplitterColumn.Width = new GridLength(0);
                PreviewColumn.Width = new GridLength(1, GridUnitType.Star);
                break;

            case ViewMode.Split:
                EditorColumn.Width = new GridLength(1, GridUnitType.Star);
                PreviewSplitterColumn.Width = new GridLength(1);
                PreviewColumn.Width = new GridLength(1, GridUnitType.Star);
                break;
        }
    }

    #endregion

    #region Editor

    private void LoadMarkdownHighlighting()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "MarkdownEditor.Helpers.MarkdownHighlighting.xshd";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using var reader = new XmlTextReader(stream);
                var highlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                MarkdownTextEditor.SyntaxHighlighting = highlighting;
            }
        }
        catch (Exception ex)
        {
            _viewModel.StatusText = $"Could not load syntax highlighting: {ex.Message}";
        }
    }

    private void ConfigureEditor()
    {
        var editor = MarkdownTextEditor;
        editor.Options.EnableHyperlinks = true;
        editor.Options.EnableEmailHyperlinks = true;
        editor.Options.HighlightCurrentLine = true;
        editor.Options.ConvertTabsToSpaces = true;
        editor.Options.IndentationSize = 2;
        editor.Options.ShowEndOfLine = false;
        editor.Options.ShowSpaces = false;
        editor.Options.ShowTabs = false;

        // Set current line highlight color
        editor.TextArea.TextView.CurrentLineBackground =
            new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(20, 205, 214, 244));
        editor.TextArea.TextView.CurrentLineBorder =
            new System.Windows.Media.Pen(
                new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(15, 205, 214, 244)), 1);

        // Selection brush
        editor.TextArea.SelectionBrush =
            new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(80, 137, 180, 250));
        editor.TextArea.SelectionForeground = null; // Keep syntax colors in selection
    }

    private void OnFileContentLoaded(string content)
    {
        MarkdownTextEditor.Text = content;
        MarkdownTextEditor.ScrollToHome();
        _viewModel.HasUnsavedChanges = false;
    }

    private void OnScrollToLineRequested(int lineNumber)
    {
        if (lineNumber > 0 && lineNumber <= MarkdownTextEditor.Document.LineCount)
        {
            var line = MarkdownTextEditor.Document.GetLineByNumber(lineNumber);
            MarkdownTextEditor.ScrollToLine(lineNumber);
            MarkdownTextEditor.Select(line.Offset, line.Length);
            MarkdownTextEditor.TextArea.Caret.Offset = line.Offset;
            MarkdownTextEditor.TextArea.Focus();
        }
    }

    private void UpdateCursorPosition()
    {
        var caret = MarkdownTextEditor.TextArea.Caret;
        CursorPositionText.Text = $"Ln {caret.Line}, Col {caret.Column}";
    }

    #endregion

    #region UI Events

    private void FileTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is FileNode fileNode && !fileNode.IsDirectory)
        {
            _viewModel.OpenFileFromTree(fileNode);
        }
    }

    private void OutlineItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is OutlineItem item)
        {
            _viewModel.NavigateToOutlineItem(item);
        }
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
            _viewModel.HandleFileDrop(files);
        }
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    #endregion
}