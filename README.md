# MarkdownEditors

## Overview

Project has been created with Google Antigravity.
A modern, feature-rich WPF desktop application for editing and previewing Markdown files with a clean, intuitive interface.

## Features

- **Live Preview**: Real-time HTML preview of your Markdown content with split-view editing
- **Multiple View Modes**: 
  - Edit mode for focused writing
  - Preview mode for rendering
  - Split view for simultaneous editing and preview
- **File Tree Navigation**: Browse and open Markdown files from your project directory
- **Syntax Highlighting**: Custom Markdown syntax highlighting using AvalonEdit
- **Document Outline**: Auto-generated outline based on heading structure for quick navigation
- **File Management**: Create, open, and save Markdown files
- **Folder Management**: Open entire folders to browse Markdown projects
- **Change Tracking**: Visual indicators for unsaved changes

## Technology Stack

- **Framework**: .NET 10.0 Windows (WPF)
- **Text Editor**: AvalonEdit for syntax-highlighted editing
- **Markdown Parsing**: Markdig for accurate Markdown rendering
- **Web Preview**: WebView2 for HTML preview rendering
- **Architecture**: MVVM pattern with ViewModel-based state management

## Technical Highlights
- Built on WPF for native Windows desktop experience
- MVVM architecture for clean separation of concerns
- Markdig integration for accurate Markdown parsing
- AvalonEdit for professional text editing
- WebView2 for modern HTML rendering

## Getting Started

### Prerequisites
- .NET 10.0 SDK or later
- Windows OS (WPF requirement)

### Installation

1. Clone the repository
2. Open `MarkdownEditor.slnx` in Visual Studio
3. Build the solution
4. Run the application

## Usage

1. **Open a Folder**: Click "Open Folder" to browse a directory containing Markdown files
2. **Open a File**: Select files from the file tree or use "Open File" to browse
3. **Edit**: Write or modify Markdown content in the editor
4. **Preview**: Toggle preview mode to see rendered output
5. **Save**: Use Ctrl+S or the Save button to save changes
6. **Navigate**: Use the outline panel to jump to different sections

## Project Structure

```
MarkdownEditor/
├── ViewModels/          # MVVM ViewModels
├── Models/              # Data models and helpers
├── Helpers/             # Utility functions and highlighting definitions
├── Themes/              # UI themes and styling
├── Resources/           # Application icons and assets
├── SampleDocs/          # Sample Markdown files
├── MainWindow.xaml      # Main UI layout
└── App.xaml             # Application configuration
```
