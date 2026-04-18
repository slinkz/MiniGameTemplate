param(
    [switch]$NoPause,
    [switch]$Force
)

$ErrorActionPreference = "Stop"

function Finish([int]$Code = 0) {
    if (-not $NoPause) {
        Pause
    }
    exit $Code
}

function Ensure-Junction {
    param(
        [Parameter(Mandatory = $true)][string]$Target,
        [Parameter(Mandatory = $true)][string]$Source
    )

    Write-Host ""
    Write-Host "Preparing $Target -> $Source"

    if (-not (Test-Path -LiteralPath $Source -PathType Container)) {
        throw "Source path does not exist: $Source"
    }

    if (Test-Path -LiteralPath $Target) {
        $existingItem = Get-Item -LiteralPath $Target -Force
        if (($existingItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            Write-Host "Junction/symlink already exists, skipping."
            return
        }

        Write-Host "WARNING: $Target exists as a regular directory (not a junction)."
        Write-Host "It will be DELETED and replaced with a junction to $Source."

        if (-not $Force) {
            $confirm = Read-Host "Continue? (Y/N)"
            if ($confirm -ne "Y") {
                Write-Host "Aborted by user."
                throw "Operation cancelled by user."
            }
        }

        Write-Host "Removing existing directory: $Target"
        Remove-Item -LiteralPath $Target -Recurse -Force
    }

    New-Item -ItemType Junction -Path $Target -Target $Source | Out-Null
}

try {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $unityProjDir = Split-Path -Parent $scriptDir
    $repoRoot = Split-Path -Parent $unityProjDir

    Write-Host "Initializing git submodules..."
    & git -C $repoRoot submodule update --init --recursive UnityProj/ThirdParty/spine-runtimes
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to initialize Spine submodule."
    }

    $spineUnitySrc = Join-Path $unityProjDir "ThirdParty/spine-runtimes/spine-unity/Assets/Spine"
    $spineUnityDst = Join-Path $unityProjDir "Assets/Spine"
    $spineCSharpSrc = Join-Path $unityProjDir "ThirdParty/spine-runtimes/spine-csharp/src"
    $spineCSharpDst = Join-Path $unityProjDir "Assets/SpineCSharp"

    Ensure-Junction -Target $spineUnityDst -Source $spineUnitySrc
    Ensure-Junction -Target $spineCSharpDst -Source $spineCSharpSrc

    Write-Host ""
    Write-Host "Done! Spine source links are ready."
    Write-Host "Next step: enable FAIRYGUI_SPINE define from Unity menu if needed."
    Finish 0
}
catch {
    Write-Host "ERROR: $($_.Exception.Message)"
    Finish 1
}
