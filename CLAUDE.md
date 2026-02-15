# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is an OGame clone built with **Blazor Server (.NET 7.0)**. It's a browser-based space strategy game featuring resource management, building construction, technology research, fleet management, and combat mechanics.

## Development Commands

### Running the Application
```bash
dotnet run
```
The application will be available at http://localhost:5000 (or the port specified in Properties/launchSettings.json).

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
- **DevModeService**: Development mode toggles and testing utilities.
- **RequirementService**: Dependency validation for buildings and technologies.

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

**Queue Management**: Buildings, technologies, fleet, and defense all use time-based queue systems with `IsBuilding`/`IsResearching`/`IsBuilding` flags and `TimeRemaining` properties.

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

## Development Workflow Notes

- The user manages version control externally using Fork (a Git GUI client)
- Do not execute git commands - provide "Ready for commit" summaries instead
- Use absolute paths when referencing files
- Verify file existence before reading/editing
