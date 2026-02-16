# AGENTS.md

Instructions for AI coding agents operating in this repository.

## Project Overview

OGame clone built with **Blazor Server (.NET 7.0)**. Browser-based space strategy game with resource management, building construction, technology research, fleet management, and combat. All services are singletons with no database — state lives in memory.

## Build / Run / Test Commands

```bash
# Build
dotnet build

# Run (http://localhost:5000)
dotnet run

# Run all tests
dotnet test

# Run all tests verbose
dotnet test --verbosity normal

# Run a single test class
dotnet test --filter "FullyQualifiedName~FleetServiceTests"

# Run a single test method
dotnet test --filter "FullyQualifiedName~FleetServiceTests.HandleCombat_Should_Generate_Debris_On_Victory"

# Run tests in the test project only
dotnet test myapp.Tests/myapp.Tests.csproj
```

There is no linter or formatter configured. No `.editorconfig` exists.

## Project Structure

```
Program.cs                    # Entry point, service registration (all AddSingleton)
Services/*.cs                 # All game logic (models co-located with their service)
Components/Pages/*.razor      # Page components (@page routes)
Components/Layout/            # MainLayout, NavMenu, ResourceHeader
Components/Shared/            # Shared DTOs and components
myapp.Tests/                  # xUnit + bUnit tests
wwwroot/assets/               # Static images (buildings, ships, technologies, defense)
```

## Workflow Rules

- **Git operations MUST be delegated to the `@git-commit` subagent.** Whenever you detect that a commit is needed, the user mentions committing, or any git operation is required (status, diff, log, add, commit), automatically invoke `@git-commit` via the Task tool to handle it. Do NOT run git commands directly — always delegate to the subagent.
- Always use absolute paths when referencing files.
- Verify file existence before reading or editing.

## Code Style

### Namespaces
- **Services:** File-scoped namespaces: `namespace myapp.Services;`
- **Tests:** Block-scoped namespaces: `namespace myapp.Tests.Services { ... }`

### Naming Conventions
| Element | Convention | Example |
|---|---|---|
| Classes | PascalCase | `BuildingService`, `FleetMission` |
| Methods | PascalCase | `UpdateResources`, `SendFleet` |
| Properties | PascalCase | `Metal`, `IsBuilding`, `TimeRemaining` |
| Private fields | `_camelCase` | `_resourceService`, `_isProcessingQueue` |
| Local variables | camelCase | `metalProduction`, `flightTime` |
| Parameters | camelCase | `metal`, `crystal`, `quantity` |
| Enums | PascalCase name and members | `FleetStatus.Return` |

### Collections and Initialization
- Always use target-typed `new()`: `= new();` not `= new List<T>();`
- Example: `public List<Building> Buildings { get; private set; } = new();`
- Inline initializers: `Requirements = new() { { "Shipyard", 2 } }`

### Properties
- Auto-properties with initializers: `public string Title { get; set; } = "";`
- Private set for service-owned collections: `public List<Ship> ShipDefinitions { get; private set; } = new();`
- Expression-bodied for computed values: `public long MetalCost => (long)(BaseMetalCost * Math.Pow(Scaling, Level));`

### Multi-Class Files
Models and enums are defined in the same file as their service. Do not create separate files for model classes.
- `BuildingService.cs` contains `Building` + `BuildingService`
- `FleetService.cs` contains `FleetStatus`, `FleetMission`, `Ship`, `ShipyardItem`, `FleetService`

### Dependency Injection
- **Concrete types only** — no interfaces (`IResourceService`, etc.) exist.
- Constructor injection with private readonly fields:
```csharp
private readonly ResourceService _resourceService;
public BuildingService(ResourceService resourceService, DevModeService devModeService)
{
    _resourceService = resourceService;
    // ...
}
```
- All services registered as `AddSingleton<T>()` in `Program.cs`.

### Events
Every service uses this exact pattern:
```csharp
public event Action OnChange;
private void NotifyStateChanged() => OnChange?.Invoke();
```
Call `NotifyStateChanged()` after any state mutation.

### Razor Components

**Directive ordering:**
1. `@page "/route"`
2. `@using` directives
3. `@inject` directives (one per line)
4. `@implements IDisposable`

**Lifecycle — always synchronous `OnInitialized`, never async:**
```csharp
protected override void OnInitialized()
{
    ResourceService.OnChange += RefreshState;
    BuildingService.OnChange += RefreshState;
}
```

**Event subscription/cleanup:**
```csharp
private async void RefreshState()
{
    await InvokeAsync(StateHasChanged);
}

public void Dispose()
{
    ResourceService.OnChange -= RefreshState;
    BuildingService.OnChange -= RefreshState;
    _timer?.Dispose();
}
```

**Timer pattern** for periodic UI refresh:
```csharp
_timer = new System.Threading.Timer(_ => InvokeAsync(StateHasChanged), null, 0, 1000);
```

**Styling:** Inline `<style>` blocks in `.razor` files and inline `style=""` attributes on elements. Scoped `.razor.css` files exist only for Layout components. Use existing CSS utility classes (`ogame-container`, `ogame-card`, `ogame-btn`, `ogame-grid`, `ogame-badge`, `ogame-panel`, `text-muted`, `mb-lg`).

### Error Handling
- **No exceptions.** No try-catch blocks exist in the codebase.
- Return `null` for success, error message string for failure:
```csharp
public string SendFleet(...) {
    if (!shipsToSend.Any()) return "No ships selected.";
    // ... success
    return null;
}
```
- Boolean returns for resource checks: `public bool ConsumeResources(long metal, long crystal, long deuterium)`
- Silent early returns when preconditions fail: `if (quantity <= 0) return;`

### Formatting
- **Allman braces** (opening brace on new line) for classes, methods, control structures.
- **4-space indentation.**
- Expression-bodied members for single expressions: `private void NotifyStateChanged() => OnChange?.Invoke();`
- Guard clauses on single line without braces: `if (quantity <= 0) return;`
- String interpolation preferred: `$"{galaxy}:{system}:{position}"`
- Number formatting: `.ToString("N0")`, TimeSpan: `.ToString(@"hh\:mm\:ss")`
- LINQ fluent syntax only (no query syntax).

### Nullable Reference Types
- Enabled in `.csproj`. Use `?` for genuinely nullable references: `public Technology? CurrentResearch { get; private set; }`
- Use null-conditional: `OnChange?.Invoke()`, `_timer?.Dispose()`

### Imports
- `ImplicitUsings` is enabled — do not add `using System;`, `using System.Linq;`, etc. unless specifically needed.
- Only add explicit `using` statements for project namespaces: `using myapp.Services;`

## Testing Conventions

- **Framework:** xUnit (`[Fact]` only, no `[Theory]`), bUnit for component tests.
- **No mocking frameworks.** Instantiate real services with all dependencies manually.
- **Test method naming:** `MethodName_Should_ExpectedBehavior` or `Component_Action_ExpectedResult`
- **AAA pattern:** Use `// Arrange`, `// Act`, `// Assert` comments.
- **Component tests** inherit from `Bunit.TestContext` and register services via `Services.AddSingleton<T>()`.
- Tests live in `myapp.Tests/` with a project reference to the main project.

## Key Domain Rules

- Always call `BuildingService.UpdateProduction()` before checking or consuming resources.
- Resources from fleet missions are only credited when the fleet returns (stored in `FleetMission.Cargo`).
- Cost scaling formula: `BaseCost * Math.Pow(Scaling, Level)` (default scaling = 2.0).
- The game runs at 1000x speed multiplier for development.
- Use `RequirementService` to validate building/technology dependencies before allowing construction.
