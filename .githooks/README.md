# Git Hooks

This directory contains versioned local hooks for MiniGameTemplate.

Enable them once per clone:

```powershell
git config core.hooksPath .githooks
```

The pre-commit hook runs:

```powershell
Tools/knowledge-sync-check.ps1 -Staged
```

If a commit changes knowledge-sensitive code/assets without a matching knowledge-documentation update, the hook fails and points to `Docs/Agent/templates/DOC_UPDATE_CHECKLIST.md`.
