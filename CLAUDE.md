# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is an OGame clone built with **Blazor Server (.NET 7.0)**. It's a browser-based space strategy game featuring resource management, building construction, technology research, fleet management, and combat mechanics.

## Development Commands

### Running the Application
```bash
dotnet run
```
The application will be available at http://localhost:5264 (configured in `Properties/launchSettings.json`).

### Testing
```bash
# Run all tests
dotnet test

# Run tests with verbose output
dotnet test --verbosity normal

# Run specific test file
dotnet test --filter "FullyQualifiedName~FleetServiceTests"
```

### Building
```bash
# Build the project
dotnet build

# Build in Release mode
dotnet build -c Release
```

## Architecture

### Service Layer Pattern
All game logic resides in singleton services (registered in `Program.cs`):

- **ResourceService**: Manages metal, crystal, deuterium, dark matter, and energy. Implements real-time resource accumulation with event-driven updates via `OnChange` event.
- **BuildingService**: Handles building construction, levels, costs (exponential scaling), and production calculations. Manages a construction queue.
- **TechnologyService**: Research system with dependencies and queue management.
- **FleetService**: Ship construction, fleet missions (Attack, Transport, Colonize, Recycle), combat resolution, and cargo management.
- **DefenseService**: Defense structure construction and combat integration.
- **GalaxyService**: Galaxy/solar system generation and planet management.
- **MessageService**: In-game messaging system for combat reports and notifications.
- **DevModeService**: Development mode toggles. When enabled (default), all construction/research durations are reduced to 5 seconds via `DevModeService.GetDuration()`.
- **RequirementService**: Dependency validation for buildings and technologies via `IsUnlocked(Dictionary<string, int>)`.
- **PlayerStateService**: Tracks the currently active planet (`ActiveGalaxy/System/Position`). Its `OnChange` event triggers cascading reloads in all dependent services and UI components.
- **GalaxyService**: Generates a universe of 10 galaxies × 10 systems × 15 planets (1,500 total). Lazy-loads systems into `_universe["galaxy:system"]` cache. Planet images vary by position (Hot: 1-3, Desert/Gas/Water/Forest: 4-12, Ice: 13-15). Enforces max 4 player colonies.
- **EnemyService**: Generates up to 100 NPC bots with AI behavior (building upgrades 70%, research 80%, defenses 60%, ships 50%, colonization 30%). Tracks strategic memory (known enemy coordinates, spied power levels). Triggers attacks when player power × 1.20 advantage threshold is met.
- **GameInitializationService**: Orchestrates startup — loads existing game from DB or creates new one, initializes all services, sets home planet coordinates.
- **GamePersistenceService**: Handles `PlayerPlanetEntity` serialization for custom planet names and images.

### Database

SQLite via **EF Core 7.0**. Database file: `game.db` in the application root.

Key entities in `Data/Entities/`:
- `GameState` — single-row global state (dev mode flag, home coordinates)
- `PlanetState` — per-planet resource totals and last-update timestamp (keyed by Galaxy:System:Position)
- `BuildingEntity`, `TechnologyEntity`, `ShipEntity`, `DefenseEntity` — level/quantity per planet
- `*QueueEntity` tables — `BuildingQueueEntity`, `ResearchQueueEntity`, `ShipyardQueueEntity`, `DefenseQueueEntity`
- `FleetMissionEntity` / `FleetMissionShipEntity` — active missions and ship manifests
- `EnemyEntity` — NPC data with JSON-serialized buildings, technologies, defenses, ships, and strategic memory
- `PlayerPlanetEntity` — custom planet metadata (name, image)

Migrations run automatically at startup in `Program.cs` before `GameInitializationService.InitializeGameAsync()`.

### Component Structure
- **Components/Pages/**: Game screens (Home, BuildingsPage, TechnologyPage, FleetPage, DefensePage, FactoryPage, ConstellationPage, etc.)
- **Components/Layout/**: Structural UI components (MainLayout, NavMenu, ResourceHeader)
- **Components/Shared/**: Shared DTOs and components (e.g., QueueItemDto)

### Key Architectural Patterns

**Event-Driven Updates**: Services use `OnChange` events to notify UI components of state changes. Components should subscribe/unsubscribe in lifecycle methods.

**Resource Flow**:
1. Buildings produce resources at calculated rates (stored in BuildingService)
2. ResourceService updates resources based on elapsed time since last update
3. ResourceHeader.razor polls every second to trigger UI refresh
4. Fleet missions collect resources in `FleetMission.Cargo`
5. Resources are credited to player only when fleet returns home

**Multi-Planet Context**: All service operations are scoped to `PlayerStateService.ActiveGalaxy/System/Position`. Switching planets via the ResourceHeader dropdown fires `PlayerStateService.OnChange`, which cascades reloads across BuildingService, ResourceService, DefenseService, and FleetService. Always read/write resources and buildings relative to the active coordinates.

**Queue Management**: Buildings, technologies, fleet, and defense all use time-based queue systems with `IsBuilding`/`IsResearching`/`IsBuilding` flags and `TimeRemaining` properties. A background `ProcessQueueLoop()` polls ~every second and completes items when `TimeRemaining <= 0`.

**Cost Scaling**: Most items use exponential cost scaling: `BaseCost * Math.Pow(Scaling, Level)`. Default scaling factor is 2.0.

## Static Assets

Located in `wwwroot/assets/`:
- `buildings/` - Building images
- `technologies/` - Technology images
- `ships/` - Fleet ship images
- `defense/` - Defense structure images
- `banners/` - Page banner backgrounds
- `planets/` - Planet visuals
- `common/` - Shared UI elements

## UI Conventions

**Card Layout Pattern**: Standard grid layout for game items:
```css
display: grid;
grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
gap: 20px;
```

**Color Scheme**: Dark theme with:
- Background: `#000000` / `#111`
- Cards: `#222`
- Text: `#eee`
- Accent colors for resources (metal, crystal, deuterium)

**Component Styling**: Components use both:
- Scoped CSS files (`.razor.css`)
- Inline `<style>` blocks in `.razor` files

## Testing

Tests use **xUnit** and **bUnit** (Blazor component testing):
- Service tests: Test game logic directly (e.g., `FleetServiceTests.cs`)
- Component tests: Test Razor components with bUnit (e.g., `ConstellationPageTests.cs`)
- Tests instantiate services manually with required dependencies

## Important Notes

**Resource Updates**: Always call `BuildingService.UpdateProduction()` before checking or consuming resources to ensure values are current.

**Speed Multiplier**: The game uses a 1000x speed multiplier for testing/development (configured in `BuildingService.UpdateProduction()`).

**Mission Completion**: When a fleet mission completes, `FleetService` handles combat resolution, loot collection, and resource crediting automatically.

**Dependency Management**: Some buildings and technologies have dependencies (e.g., "Shipyard" requires "Robotics Factory 2"). Use `RequirementService` to validate.

**Entry Points**:
- `Program.cs` - Application configuration and service registration
- `Pages/_Host.cshtml` - Blazor Server host page
- `Components/App.razor` - Root Blazor component

## Business Rules Reference

Detailed game mechanics are documented in `wiki/business-rules/`:
- `Buildings.md`, `Technology.md`, `Fleet.md`, `Combat.md`, `Defense.md`, `Factory.md`, `Constellation.md`
- `RESOURCE_LOGIC_SUMMARY.md` — canonical reference for resource accumulation and storage capacity formulas

Storage capacity formula: `1,000,000 × 1.68^level` (3 significant figures).
Energy formula (Solar Plant): `20 × Level × 1.1^Level`. Mines consume `10 × Level × 1.1^Level`.

## Development Workflow Notes

- The user manages version control externally using Fork (a Git GUI client)
- Do not execute git commands - provide "Ready for commit" summaries instead
- Use absolute paths when referencing files
- Verify file existence before reading/editing
