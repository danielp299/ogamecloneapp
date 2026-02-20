# AI Development Rules & Context

## 1. Environment & Tools
- **Git:** The user has explicitly authorized the AI agent to execute git commands (commit, add, status) to optimize the workflow.
- **Commits:** Provide clear, descriptive commit messages when committing changes.
- **Server:** .NET 8 / Blazor Web App. Run using `dotnet run`.

## 2. Project Architecture
- **Framework:** Blazor Server (.NET 8).
- **Styling:** Custom CSS. Dark theme.
- **Components:** 
    - Use `Components/Layout` for structural elements.
    - Use `Components/Pages` for views.
- **Services:** Logic resides in `Services/` (Singleton pattern mostly).

## 3. Current State (As of Last Session)
- **Gameplay Loop:** Fully functional (Build -> Fleet -> Attack -> Loot).
- **UI:** Standardized "Card" layout with `repeat(auto-fit, minmax(300px, 1fr))` grid.
- **Resources:** 
    - Real-time updates via `ResourceHeader.razor` (1-second tick).
    - Resources from missions (Loot/Debris) are stored in `FleetMission.Cargo`.
    - Resources are only credited to the player when the fleet returns.

## 4. Coding Conventions
- **Language:** English for code/comments. Spanish for user chat.
- **Paths:** Always use absolute paths.
- **Safety:** Verify file existence before reading/editing.

## 5. Next Steps / Todo
- Implement "Tech Tree" restrictions (dependencies).
- Enhance the Galaxy View visuals.
- Implement "Defense" combat logic (currently only Attacker Power vs Random Defender).
