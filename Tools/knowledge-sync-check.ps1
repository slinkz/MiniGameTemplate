[CmdletBinding()]
param(
    [switch]$Staged,
    [string]$BaseRef,
    [string]$HeadRef = "HEAD",
    [switch]$AllowNoDocUpdate
)

$ErrorActionPreference = "Stop"

function Invoke-Git {
    & git @args
    if ($LASTEXITCODE -ne 0) {
        throw "git $($args -join ' ') failed with exit code $LASTEXITCODE"
    }
}

function Normalize-PathForMatch([string]$Path) {
    return ($Path -replace "\\", "/").Trim()
}

function Test-AnyPattern([string]$Path, [string[]]$Patterns) {
    foreach ($pattern in $Patterns) {
        if ($Path -match $pattern) {
            return $true
        }
    }
    return $false
}

$repoRoot = (Invoke-Git rev-parse --show-toplevel).Trim()
Set-Location $repoRoot

$changedFiles = @()

if ($Staged) {
    $changedFiles += Invoke-Git diff --cached --name-only --diff-filter=ACDMRTUXB
}
elseif ($BaseRef) {
    $range = "$BaseRef...$HeadRef"
    $changedFiles += Invoke-Git diff --name-only --diff-filter=ACDMRTUXB $range
}
else {
    $changedFiles += Invoke-Git diff --name-only --diff-filter=ACDMRTUXB HEAD
    $changedFiles += Invoke-Git ls-files --others --exclude-standard
}

$changedFiles = $changedFiles |
    Where-Object { $_ -and $_.Trim().Length -gt 0 } |
    ForEach-Object { Normalize-PathForMatch $_ } |
    Sort-Object -Unique

if ($changedFiles.Count -eq 0) {
    Write-Host "[knowledge-sync] No changed files."
    exit 0
}

$knowledgeSensitivePatterns = @(
    "^UnityProj/Assets/_Framework/",
    "^UnityProj/Assets/_Game/Scripts/",
    "^UnityProj/Assets/_Game/Configs/",
    "^UnityProj/Assets/_Game/FairyGUI_Export/",
    "^UnityProj/Assets/_Example/",
    "^UnityProj/DataTables/",
    "^UIProject/",
    "^CloudFunctions/",
    "^skills/",
    "^\.workbuddy/skills/",
    "^\.codebuddy/skills/"
)

$knowledgeDocPatterns = @(
    "^Docs/Agent/",
    "^Docs/Guide/BUILD_MINIGAME\.md$",
    "^Docs/Guide/GETTING_STARTED\.md$",
    "^Docs/Guide/FAQ\.md$",
    "^Docs/Guide/README\.md$",
    "^README\.md$",
    "^CHANGELOG\.md$"
)

$sensitiveChanges = @($changedFiles | Where-Object { Test-AnyPattern $_ $knowledgeSensitivePatterns })
$docChanges = @($changedFiles | Where-Object { Test-AnyPattern $_ $knowledgeDocPatterns })

if ($sensitiveChanges.Count -eq 0) {
    Write-Host "[knowledge-sync] PASS: no knowledge-sensitive code/assets changed."
    exit 0
}

if ($docChanges.Count -gt 0) {
    Write-Host "[knowledge-sync] PASS: knowledge-sensitive changes include documentation updates."
    Write-Host "[knowledge-sync] Sensitive changes: $($sensitiveChanges.Count); doc changes: $($docChanges.Count)."
    exit 0
}

$allowByEnv = $env:KNOWLEDGE_SYNC_ALLOW_NO_DOC -eq "1"
if ($AllowNoDocUpdate -or $allowByEnv) {
    Write-Warning "[knowledge-sync] BYPASSED: knowledge-sensitive changes found without docs. Use only when the doc update checklist confirms no knowledge asset update is needed."
    exit 0
}

Write-Error @"
[knowledge-sync] FAIL: knowledge-sensitive files changed without a documentation/knowledge update.

Changed sensitive files:
$($sensitiveChanges | ForEach-Object { "  - $_" } | Out-String)
Required action:
  - Update the affected Docs/Agent knowledge asset, or
  - Add/update a changes package under Docs/Agent/changes/**, or
  - For a truly no-doc-needed change, run locally with -AllowNoDocUpdate or set KNOWLEDGE_SYNC_ALLOW_NO_DOC=1 after recording the reason in the task/commit context.

Relevant checklist:
  Docs/Agent/templates/DOC_UPDATE_CHECKLIST.md
"@

exit 1
