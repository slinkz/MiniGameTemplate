[CmdletBinding()]
param(
    [switch]$VerboseOutput,
    [switch]$AllowWarnings
)

$ErrorActionPreference = "Continue"
$script:errors = @()
$script:warnings = @()
$script:checked = 0

function Log-Error($msg) {
    $script:errors += $msg
    Write-Host "  FAIL: $msg" -ForegroundColor Red
}

function Log-Warning($msg) {
    $script:warnings += $msg
    if ($VerboseOutput) { Write-Host "  WARN: $msg" -ForegroundColor Yellow }
}

function Log-OK($msg) {
    $script:checked++
    if ($VerboseOutput) { Write-Host "  OK: $msg" -ForegroundColor Green }
}

function Test-LooksPathLike([string]$Value) {
    $value = $Value.Trim()
    if (-not $value -or $value -eq "-") { return $false }
    if ($value -match '^(ADR|PIT)-') { return $false }
    return (
        $value.Contains("/") -or
        $value.Contains("\") -or
        $value.Contains("*") -or
        $value.Contains("?") -or
        $value -match '\.(cs|asset|unity|md|json|xml|bytes|prefab|mat)$'
    )
}

function Test-LooksDocLike([string]$Value) {
    $value = $Value.Trim()
    return (
        $value -match '\.md$' -or
        $value.StartsWith("Docs/") -or
        $value.StartsWith("Docs\") -or
        $value.StartsWith("skills/") -or
        $value.StartsWith("skills\") -or
        (($value.Contains("*") -or $value.Contains("?")) -and $value.Contains("/"))
    )
}

function Get-RefsFromCell([string]$Cell) {
    $refs = [regex]::Matches($Cell, '`([^`]+)`')
    if ($refs.Count -gt 0) {
        return @($refs | ForEach-Object { $_.Groups[1].Value.Trim() } | Where-Object { $_ })
    }
    return @($Cell -split '\s*,\s*' | ForEach-Object { $_.Trim(' ', '`') } | Where-Object { $_ })
}

$repoRoot = (git -C "$PSScriptRoot/.." rev-parse --show-toplevel 2>$null) -replace "\\", "/"
if (-not $repoRoot) {
    $repoRoot = (Resolve-Path "$PSScriptRoot/..").Path -replace "\\", "/"
}

Write-Host "[knowledge-consistency] Repo root: $repoRoot" -ForegroundColor Cyan
Write-Host "[knowledge-consistency] Checking knowledge document references against filesystem..." -ForegroundColor Cyan

# ============================================================
# Path Resolution Helpers
# ============================================================

function Resolve-CodePath([string]$Path) {
    $path = $Path.Trim()
    $resolved = $null

    if (-not (Test-LooksPathLike $path)) { return "[non-path reference skipped]" }

    # Try: absolute path under repo
    $absPath = Join-Path $repoRoot $path
    if (Test-Path $absPath) { return $absPath }

    # Try: prepend UnityProj/Assets/
    $absPath = Join-Path $repoRoot "UnityProj/Assets/$path"
    if (Test-Path $absPath) { return $absPath }

    # Common shorthand in ADR/knowledge docs: EntitySystem/** means _Framework/EntitySystem/**
    $absPath = Join-Path $repoRoot "UnityProj/Assets/_Framework/$path"
    if (Test-Path $absPath) { return $absPath }

    # Try: glob pattern (contains * or ? or not a single file)
    if ($path -match '[\*\?]' -or $path -like '*/**' -or $path -notmatch '\.\w+$') {
        foreach ($base in @($repoRoot, (Join-Path $repoRoot "UnityProj/Assets"), (Join-Path $repoRoot "UnityProj/Assets/_Framework"), (Join-Path $repoRoot "Docs"), (Join-Path $repoRoot "Docs/Agent"))) {
            $globPath = Join-Path $base $path
            try {
                $matches = Get-ChildItem -Path $globPath -ErrorAction SilentlyContinue
                if ($matches -and $matches.Count -gt 0) {
                    return "$globPath -> $($matches.Count) files matched"
                }
            } catch {
            }
        }
    }

    # Try: search by filename (for shorthand paths like Core/BattleController.cs)
    $filename = Split-Path $path -Leaf
    if ($filename -match '\.\w+$') {
        $found = Get-ChildItem -Path (Join-Path $repoRoot "UnityProj/Assets") -Recurse -Filter $filename -File -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($found) {
            $absPath = $found.FullName
            return "[partial-match] $absPath"
        }
    }

    return $null
}

function Resolve-DocPath([string]$Path) {
    $path = $Path.Trim()

    if ($path -match '[\*\?]') {
        foreach ($base in @($repoRoot, (Join-Path $repoRoot "Docs"), (Join-Path $repoRoot "Docs/Agent"))) {
            $globPath = Join-Path $base $path
            try {
                $matches = Get-ChildItem -Path $globPath -ErrorAction SilentlyContinue
                if ($matches -and $matches.Count -gt 0) {
                    return "$globPath -> $($matches.Count) files matched"
                }
            } catch {
            }
        }
    }

    $absPath = Join-Path $repoRoot $path
    if (Test-Path $absPath) { return $absPath }

    $absPath = Join-Path $repoRoot "Docs/Agent/$path"
    if (Test-Path $absPath) { return $absPath }

    $absPath = Join-Path $repoRoot "Docs/$path"
    if (Test-Path $absPath) { return $absPath }

    $absPath = Join-Path $repoRoot "Docs/Guide/$path"
    if (Test-Path $absPath) { return $absPath }

    return $null
}

function Resolve-ModuleCard([string]$Name) {
    $name = $Name.Trim() -replace '^.*/', ''
    $absPath = Join-Path $repoRoot "Docs/Agent/MODULE_CARDS/$name"
    if (Test-Path $absPath) { return $absPath }
    return $null
}

function Resolve-ContextPack([string]$Name) {
    $name = $Name.Trim() -replace '^.*/', ''
    $absPath = Join-Path $repoRoot "Docs/Agent/CONTEXT_PACKS/$name"
    if (Test-Path $absPath) { return $absPath }
    return $null
}

# ============================================================
# KNOWLEDGE/CODE_KNOWLEDGE_MAP.md Verification
# ============================================================

Write-Host "`n=== Checking KNOWLEDGE/CODE_KNOWLEDGE_MAP.md ===" -ForegroundColor Yellow

$mapFile = Join-Path $repoRoot "Docs/Agent/KNOWLEDGE/CODE_KNOWLEDGE_MAP.md"
if (-not (Test-Path $mapFile)) {
    Log-Error "KNOWLEDGE/CODE_KNOWLEDGE_MAP.md not found"
} else {
    $mapContent = Get-Content $mapFile -Raw -Encoding UTF8

    $lines = $mapContent -split "`r?`n"
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if (-not $line.StartsWith('| `')) { continue }

        $cells = $line.Trim().Trim('|') -split '\|'
        if ($cells.Count -lt 4) { continue }

        $codeCell = $cells[0].Trim()
        $moduleCard = $cells[1].Trim()
        $contextPack = $cells[2].Trim()
        $tddWorkflow = $cells[3].Trim()
        $lineNo = $i + 1

        foreach ($codePath in (Get-RefsFromCell $codeCell)) {
            if (-not (Test-LooksPathLike $codePath)) { continue }
            $resolved = Resolve-CodePath $codePath
            if ($resolved) {
                Log-OK "Code path: $codePath"
            } else {
                Log-Warning "[CODE_MAP:$lineNo] Code path NOT FOUND: '$codePath'"
            }
        }

        # Check Module Card references (may be comma-separated)
        if ($moduleCard -and $moduleCard -ne '-') {
            if ($moduleCard -match 'MODULE_CARDS/') {
                foreach ($card in (Get-RefsFromCell $moduleCard)) {
                    $card = $card.Trim(' ', '`')
                    if ($card -and $card -ne '-') {
                        $resolved = Resolve-ModuleCard $card
                        if ($resolved) {
                            Log-OK "Module Card: $card"
                        } else {
                            Log-Warning "[CODE_MAP:$lineNo] Module Card NOT FOUND: '$card'"
                        }
                    }
                }
            }
        }

        # Check Context Pack references
        if ($contextPack -and $contextPack -ne '-') {
            foreach ($pack in (Get-RefsFromCell $contextPack)) {
                $pack = $pack.Trim(' ', '`')
                if ($pack -and $pack -ne '-') {
                    if ($pack -match '^CONTEXT_PACKS/') {
                        $resolved = Resolve-ContextPack $pack
                        if ($resolved) {
                            Log-OK "Context Pack: $pack"
                        } else {
                            Log-Warning "[CODE_MAP:$lineNo] Context Pack NOT FOUND: '$pack'"
                        }
                    } elseif (Test-LooksDocLike $pack) {
                        $resolved = Resolve-DocPath $pack
                        if ($resolved) {
                            Log-OK "Doc ref: $pack"
                        } else {
                            Log-Warning "[CODE_MAP:$lineNo] Doc ref NOT FOUND: '$pack'"
                        }
                    } else {
                        continue
                    }
                }
            }
        }

        # Check TDD/Workflow references
        if ($tddWorkflow -and $tddWorkflow -ne '-') {
            foreach ($tdd in (Get-RefsFromCell $tddWorkflow)) {
                $tdd = $tdd.Trim(' ', '`')
                if (-not (Test-LooksDocLike $tdd)) { continue }
                if ($tdd -and $tdd -ne '-') {
                    $resolved = Resolve-DocPath $tdd
                    if ($resolved) {
                        Log-OK "TDD/Workflow: $tdd"
                    } else {
                        Log-Warning "[CODE_MAP.TDD:$lineNo] TDD/Workflow NOT FOUND: '$tdd'"
                    }
                }
            }
        }
    }
}

# ============================================================
# ADR/ADR_SCHEMA.md AppliesTo Verification
# ============================================================

Write-Host "`n=== Checking ADR/ADR_SCHEMA.md AppliesTo ===" -ForegroundColor Yellow

$adrSchemaFile = Join-Path $repoRoot "Docs/Agent/ADR/ADR_SCHEMA.md"
if (-not (Test-Path $adrSchemaFile)) {
    Log-Error "ADR/ADR_SCHEMA.md not found"
} else {
    $adrContent = Get-Content $adrSchemaFile -Raw -Encoding UTF8

    # Extract AppliesTo blocks: AppliesTo | `paths`
    $adrBlocks = [regex]::Matches($adrContent, '### (ADR-\d+).*?\| AppliesTo \| (.+?) \|', [System.Text.RegularExpressions.RegexOptions]::Singleline)

    foreach ($block in $adrBlocks) {
        $adrId = $block.Groups[1].Value.Trim()
        $appliesTo = $block.Groups[2].Value.Trim()

        if ($appliesTo) {
            $explicitRefs = [regex]::Matches($appliesTo, '`([^`]+)`')
            foreach ($ref in $explicitRefs) {
                $p = $ref.Groups[1].Value.Trim(' ', '`', "'")
                if (-not (Test-LooksPathLike $p)) { continue }
                if ($p -and $p -ne '-') {
                    $resolved = Resolve-CodePath $p
                    if ($resolved) {
                        Log-OK "[$adrId] AppliesTo: $p"
                    } else {
                        Log-Warning "[$adrId] AppliesTo path NOT FOUND: '$p'"
                    }
                }
            }
        }
    }
}

# ============================================================
# INDEX.md Document Reference Verification (spot-check key refs)
# ============================================================

Write-Host "`n=== Checking INDEX.md key document refs ===" -ForegroundColor Yellow

$indexFile = Join-Path $repoRoot "Docs/Agent/INDEX.md"
if (-not (Test-Path $indexFile)) {
    Log-Error "INDEX.md not found"
} else {
    $indexContent = Get-Content $indexFile -Raw -Encoding UTF8

    # Extract document paths from "读什么文件" column and code mapping section
    # Pattern: look for .md references in backticks
    $docRefs = [regex]::Matches($indexContent, '`([A-Z_]+.*?\.md)`')
    $seen = @{}
    foreach ($ref in $docRefs) {
        $docPath = $ref.Groups[1].Value.Trim()
        if ($docPath -match '\.md$' -and -not $seen.ContainsKey($docPath)) {
            $seen[$docPath] = $true
            $resolved = Resolve-DocPath $docPath
            if ($resolved) {
                Log-OK "INDEX ref: $docPath"
            } else {
                Log-Warning "INDEX ref NOT FOUND: '$docPath'"
            }
        }
    }
}

# ============================================================
# Module Card Code Path Verification
# ============================================================

Write-Host "`n=== Checking Module Card code paths ===" -ForegroundColor Yellow

$moduleCardDir = Join-Path $repoRoot "Docs/Agent/MODULE_CARDS"
if (Test-Path $moduleCardDir) {
    $moduleCards = Get-ChildItem $moduleCardDir -Filter "*.md" -Exclude "README.md"
    foreach ($card in $moduleCards) {
        $content = Get-Content $card.FullName -Raw -Encoding UTF8
        # Find code-like paths (backtick-wrapped strings that look like .cs files or paths)
        $codeRefs = [regex]::Matches($content, '`([A-Za-z_][A-Za-z_/\.]+\.cs)`')
        foreach ($ref in $codeRefs) {
            $codePath = $ref.Groups[1].Value.Trim()
            # Skip if it's just a class name (no slashes)
            if ($codePath -match '/' -or $codePath -match '\\') {
                $resolved = Resolve-CodePath $codePath
                if ($resolved) {
                    Log-OK "[$($card.BaseName)] Code: $codePath"
                } else {
                    Log-Warning "[$($card.BaseName)] Code NOT FOUND: '$codePath'"
                }
            }
        }
    }
}

# ============================================================
# Report Summary
# ============================================================

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "CONSISTENCY CHECK SUMMARY" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Total checks passed: $script:checked"
Write-Host "Warnings: $($script:warnings.Count)"
Write-Host "Errors: $($script:errors.Count)"

if ($script:errors.Count -gt 0) {
    Write-Host "`nERRORS:" -ForegroundColor Red
    foreach ($e in $script:errors) {
        Write-Host "  - $e" -ForegroundColor Red
    }
}

if ($script:warnings.Count -gt 0) {
    Write-Host "`nWARNINGS (potential stale references):" -ForegroundColor Yellow
    foreach ($w in $script:warnings) {
        Write-Host "  - $w" -ForegroundColor Yellow
    }
}

if ($script:errors.Count -eq 0 -and $script:warnings.Count -eq 0) {
    Write-Host "`n[PASS] All knowledge references are consistent with filesystem." -ForegroundColor Green
    exit 0
} elseif ($script:warnings.Count -gt 0 -and $script:errors.Count -eq 0) {
    Write-Host "`n[WARN] Some references may be stale - review warnings above." -ForegroundColor Yellow
    if ($AllowWarnings) { exit 0 }
    exit 1
} else {
    Write-Host "`n[FAIL] Knowledge consistency check found errors." -ForegroundColor Red
    exit 1
}
