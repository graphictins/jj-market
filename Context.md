# Context

## Tech Stack
- VB.NET
- WinForms
- WebView2
- .NET 10 Windows

## Architecture
- Entry point module → Main form (creates WebView2, maps `web/` folder as virtual host) → Form designer (layout/properties)

## Key Concept
HTML/CSS/JS in `web/` folder, served via `SetVirtualHostNameToFolderMapping()`. VB↔JS communication via `WebMessageReceived` and `ExecuteScriptAsync()`.

## Build & Run
```bash
dotnet build
dotnet run
```
