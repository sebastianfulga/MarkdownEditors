using Markdig;

namespace MarkdownEditor.Helpers;

/// <summary>
/// Converts markdown text to styled HTML for the preview panel.
/// </summary>
public static class MarkdownHtmlConverter
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public static string ToHtml(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
            return WrapInHtmlDocument("");

        var body = Markdig.Markdown.ToHtml(markdown, Pipeline);
        return WrapInHtmlDocument(body);
    }

    private static string WrapInHtmlDocument(string bodyHtml) => $"""
        <!DOCTYPE html>
        <html>
        <head>
        <meta charset="utf-8" />
        <style>
        {CssStyles}
        </style>
        </head>
        <body>
        {bodyHtml}
        </body>
        </html>
        """;

    private const string CssStyles = """
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }
        body {
            font-family: 'Segoe UI', -apple-system, BlinkMacSystemFont, sans-serif;
            font-size: 15px;
            line-height: 1.7;
            color: #CDD6F4;
            background-color: #1E1E2E;
            padding: 24px 32px;
            max-width: 100%;
            overflow-x: hidden;
        }
        h1, h2, h3, h4, h5, h6 {
            margin-top: 1.4em;
            margin-bottom: 0.6em;
            font-weight: 600;
            line-height: 1.3;
        }
        h1 { font-size: 2em; color: #F5C2E7; border-bottom: 1px solid #313244; padding-bottom: 0.3em; }
        h2 { font-size: 1.5em; color: #CBA6F7; border-bottom: 1px solid #313244; padding-bottom: 0.25em; }
        h3 { font-size: 1.25em; color: #89B4FA; }
        h4 { font-size: 1.1em; color: #74C7EC; }
        h5 { font-size: 1em; color: #89DCEB; }
        h6 { font-size: 0.9em; color: #94E2D5; }
        h1:first-child, h2:first-child, h3:first-child { margin-top: 0; }
        p { margin-bottom: 1em; }
        a { color: #89B4FA; text-decoration: none; }
        a:hover { text-decoration: underline; }
        strong { color: #F5E0DC; font-weight: 600; }
        em { color: #F5E0DC; }
        code {
            font-family: 'Cascadia Code', 'Consolas', 'Courier New', monospace;
            background-color: #313244;
            color: #A6E3A1;
            padding: 2px 6px;
            border-radius: 4px;
            font-size: 0.9em;
        }
        pre {
            background-color: #181825;
            border: 1px solid #313244;
            border-radius: 8px;
            padding: 16px;
            margin: 1em 0;
            overflow-x: auto;
        }
        pre code {
            background: none;
            padding: 0;
            border-radius: 0;
            font-size: 0.88em;
            line-height: 1.6;
        }
        blockquote {
            border-left: 3px solid #CBA6F7;
            padding: 8px 16px;
            margin: 1em 0;
            background-color: #181825;
            border-radius: 0 6px 6px 0;
            color: #A6ADC8;
            font-style: italic;
        }
        blockquote p { margin-bottom: 0.4em; }
        blockquote p:last-child { margin-bottom: 0; }
        ul, ol { padding-left: 1.8em; margin-bottom: 1em; }
        li { margin-bottom: 0.3em; }
        li::marker { color: #F38BA8; }
        hr {
            border: none;
            height: 1px;
            background: linear-gradient(90deg, transparent, #585B70, transparent);
            margin: 2em 0;
        }
        table {
            width: 100%;
            border-collapse: collapse;
            margin: 1em 0;
        }
        th, td {
            border: 1px solid #313244;
            padding: 8px 12px;
            text-align: left;
        }
        th {
            background-color: #181825;
            color: #CBA6F7;
            font-weight: 600;
        }
        tr:nth-child(even) { background-color: rgba(49, 50, 68, 0.3); }
        img {
            max-width: 100%;
            border-radius: 8px;
            margin: 1em 0;
        }
        ::-webkit-scrollbar { width: 8px; height: 8px; }
        ::-webkit-scrollbar-track { background: #1E1E2E; }
        ::-webkit-scrollbar-thumb { background: #45475A; border-radius: 4px; }
        ::-webkit-scrollbar-thumb:hover { background: #585B70; }
        """;
}
