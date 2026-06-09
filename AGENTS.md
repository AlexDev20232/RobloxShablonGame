## GitHub backup

Remote repository: `git@github.com:AlexDev20232/RobloxShablonGame.git`

After every completed project update:
- Run `git status`.
- Stage relevant Unity project files only: `Assets`, `Packages`, `ProjectSettings`, `.gitignore`, `.gitattributes`.
- Always include changed Unity `.meta` files.
- Never commit `Library`, `Temp`, `Obj`, `Logs`, `Build`, `Builds`, secrets, API keys, or local credentials.
- Commit with a short message like `backup: update project`.
- Push to `origin main`.
- If push fails or remote history conflicts, stop and report the issue instead of force pushing.
