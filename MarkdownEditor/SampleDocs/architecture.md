# Project Architecture

## Overview
This document describes the architecture of the Markdown Editor application.

## Technology Stack
- **Framework:** WPF (.NET 10)
- **Editor:** AvalonEdit
- **Markdown Parser:** Markdig
- **Pattern:** MVVM

## Components

### Models
Data structures representing the application's domain.

#### FileNode
Represents a file or directory in the explorer tree.

#### OutlineItem
Represents a heading extracted from the markdown document.

### ViewModels
Application logic and state management.

#### MainViewModel
Orchestrates all panels and handles commands.

### Helpers
Utility classes and converters.

## Design Decisions

### Why WPF?
WPF provides the best desktop experience for Windows applications with rich text editing capabilities.

### Why AvalonEdit?
AvalonEdit is a mature, performant text editor control with built-in syntax highlighting support.

### Why Catppuccin Theme?
Catppuccin Mocha provides a modern, eye-friendly dark color palette that reduces visual fatigue.
