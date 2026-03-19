---
description: How to run the C# Blazor project, stopping any existing instance first
---

// turbo-all

1. Stop any existing instance and run the application in one command:

```powershell
Stop-Process -Name 'myapp' -Force -ErrorAction SilentlyContinue; Start-Sleep -Milliseconds 500; dotnet run --project C:\Users\danie\OneDrive\Documentos\git\ogamecloneapp\myapp.csproj --urls http://localhost:5264
```

2. If you only need to stop the running instance without starting it again:

```powershell
Stop-Process -Name 'myapp' -Force -ErrorAction SilentlyContinue; Start-Sleep -Milliseconds 500
```
