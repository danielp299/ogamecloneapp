# Git Commit Instructions for AI Agent

## Issue
The system default `git` command (`/usr/bin/git`) on this environment triggers an `xcode-select: note: No developer tools were found` error, preventing standard git operations from the CLI.

## Solution
Use the `git` executable bundled specifically within the **Fork** application, which does not rely on the system-wide Xcode tools.

## Executable Path
**`/Applications/Fork.app/Contents/Resources/git-instance/bin/git`**

## Usage Example
When you need to run git commands (add, commit, status, etc.), always call the executable by its full path:

```bash
# Add files
/Applications/Fork.app/Contents/Resources/git-instance/bin/git add .

# Commit files
/Applications/Fork.app/Contents/Resources/git-instance/bin/git commit -m "Your commit message"

# Check status
/Applications/Fork.app/Contents/Resources/git-instance/bin/git status
```
