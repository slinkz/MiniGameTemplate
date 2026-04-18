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

try {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $unityProjDir = Split-Path -Parent $scriptDir
    $repoRoot = Split-Path -Parent $unityProjDir

    Write-Host "Initializing git submodules..."
    & git -C $repoRoot submodule update --init --recursive UnityProj/ThirdParty/FairyGUI-unity
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to initialize FairyGUI submodule."
    }

    $fairyGuiSrc = Join-Path $unityProjDir "ThirdParty/FairyGUI-unity/Assets"
    $fairyGuiDst = Join-Path $unityProjDir "Assets/FairyGUI"

    Write-Host "Creating FairyGUI directory junction..."
    Write-Host "Source: $fairyGuiSrc"
    Write-Host "Target: $fairyGuiDst"

    if (-not (Test-Path -LiteralPath $fairyGuiSrc -PathType Container)) {
        throw "Source path does not exist: $fairyGuiSrc"
    }

    if (Test-Path -LiteralPath $fairyGuiDst) {
        $existingItem = Get-Item -LiteralPath $fairyGuiDst -Force
        if (($existingItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            Write-Host "Junction/symlink already exists, skipping."
            Finish 0
        }

        Write-Host "WARNING: $fairyGuiDst exists as a regular directory (not a junction)."
        Write-Host "It will be DELETED and replaced with a junction to $fairyGuiSrc."

        if (-not $Force) {
            $confirm = Read-Host "Are you sure you want to continue? (Y/N)"
            if ($confirm -ne "Y") {
                Write-Host "Aborted by user."
                Finish 0
            }
        }

        Write-Host "Removing existing $fairyGuiDst directory..."
        Remove-Item -LiteralPath $fairyGuiDst -Recurse -Force
    }

    New-Item -ItemType Junction -Path $fairyGuiDst -Target $fairyGuiSrc | Out-Null
    Write-Host "Done! FairyGUI SDK is ready."
    Finish 0
}
catch {
    Write-Host "ERROR: $($_.Exception.Message)"
    Finish 1
}
