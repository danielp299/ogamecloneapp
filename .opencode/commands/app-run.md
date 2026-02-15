---
name: app-run
description: Compile and run the Blazor application in background, automatically fixing compilation errors if possible
---

# /app-run

Compile and run the Blazor Server application in background. Automatically detects and fixes common compilation errors.

## Usage
```
/app-run
```

## Behavior

### 1. Build and Run in Background
First try to build the application with `dotnet build`. If successful, start the application in background using `start dotnet run` (Windows) or `dotnet run &` (Linux/Mac).

### 2. Error Detection
If compilation fails, automatically detects these error types:
- **CS1061**: Member not found (wrong property/method name)
- **CS0103**: Undefined name
- **CS0246**: Type not found
- **CS1501**: Method overload issues
- **CS7036**: Missing arguments

### 3. Auto-Fix Strategy

For **CS1061** errors like `"X" no contiene una definición para "Y"`:
- Searches the class definition for correct member names
- Automatically replaces incorrect references

For **CS0103** undefined name errors:
- Checks for typos in variable names
- Adds missing using/imports if needed

### 4. Retry Loop
After applying fixes, retries up to 3 times until successful or unfixable errors remain.

### 5. Verification
On success, verifies the server started by checking for:
- "Application started" message
- "Now listening on:" with URL
- Default URL: http://localhost:5264

## Examples

### Example 1: Successful run
```
/app-run

✅ Application running at http://localhost:5264 in background
Process ID: 12345
```

### Example 2: Auto-fixed error
```
/app-run

⚠️  Compilation error detected: CS1061
   File: FactoryPage.razor, Line 78
   Issue: ship.Cargo does not exist
   Fix: Changed to ship.Capacity

✅ Application running at http://localhost:5264 in background
Process ID: 12345
```

### Example 3: Unfixable errors
```
/app-run

❌ Compilation errors require manual intervention:
   - CS0246: Type 'UnknownType' not found
   - CS0103: Variable 'xyz' not defined

Please fix these errors manually.
```

## Notes
- Maximum 3 auto-fix attempts
- Only handles common compilation errors
- Complex errors require manual fixes
- Server runs in background (non-blocking)
- Use `taskkill /F /IM dotnet.exe` (Windows) or `pkill dotnet` (Linux/Mac) to stop the server

---

Execute these commands to build and run the application in background:

First, build to check for errors:
!`dotnet build`

If build succeeds, run in background:
!`start dotnet run`

Then verify the application started by checking if it's listening:
!`powershell -Command "Start-Sleep -Seconds 3; Get-NetTCPConnection -LocalPort 5264 -ErrorAction SilentlyContinue | Select-Object -First 1"
