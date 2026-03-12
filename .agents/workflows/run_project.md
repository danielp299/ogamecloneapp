---
description: How to run the C# Blazor project, stopping any existing instance first
---

// turbo-all

1. Kill any existing myapp process to free the port (silent if not running):

```powershell
Stop-Process -Name 'myapp' -Force -ErrorAction SilentlyContinue; Start-Sleep -Milliseconds 500
```

2. Build and run the application (dotnet run compiles automatically before starting):

```powershell
dotnet run --project C:\Users\danie\OneDrive\Documentos\git\ogamecloneapp\myapp.csproj --urls http://localhost:5264
```
