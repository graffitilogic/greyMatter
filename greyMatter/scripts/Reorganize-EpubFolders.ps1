<#
.SYNOPSIS
    Reorganizes EPUB folder structure from 62K+ subfolders into batched groups of 256.
    
.DESCRIPTION
    Walks \\DODDNAS\jarvis\trainData\txtDump\cache\epub and moves subfolders into
    batched parent folders to avoid macOS Finder/SMB issues with large directories.
    
.EXAMPLE
    .\Reorganize-EpubFolders.ps1 -WhatIf
    
.NOTES
    Author: GitHub Copilot
    Date: 2025-11-23
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$false)]
    [string]$SourcePath = "\\DODDNAS\jarvis\trainData\txtDump\cache\epub",
    
    [Parameter(Mandatory=$false)]
    [string]$TargetPath = "\\DODDNAS\jarvis\trainData\txtDump",
    
    [Parameter(Mandatory=$false)]
    [int]$BatchSize = 256,
    
    [Parameter(Mandatory=$false)]
    [switch]$WhatIf
)

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "EPUB Folder Reorganization Script" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Source: $SourcePath" -ForegroundColor Yellow
Write-Host "Target: $TargetPath" -ForegroundColor Yellow
Write-Host "Batch Size: $BatchSize folders per batch" -ForegroundColor Yellow
Write-Host ""

if (-not (Test-Path $SourcePath)) {
    Write-Error "Source path does not exist: $SourcePath"
    exit 1
}

if (-not (Test-Path $TargetPath)) {
    Write-Error "Target path does not exist: $TargetPath"
    exit 1
}

Write-Host "Scanning source directory..." -ForegroundColor Cyan
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

$sourceFolders = Get-ChildItem -Path $SourcePath -Directory | Sort-Object Name
$totalFolders = $sourceFolders.Count

$stopwatch.Stop()
Write-Host "Found $totalFolders subfolders in $($stopwatch.Elapsed.TotalSeconds.ToString('F2')) seconds" -ForegroundColor Green
Write-Host ""

if ($totalFolders -eq 0) {
    Write-Warning "No subfolders found."
    exit 0
}

$batchCount = [Math]::Ceiling($totalFolders / $BatchSize)
Write-Host "Will create $batchCount batch folders" -ForegroundColor Cyan
Write-Host ""

if (-not $WhatIf) {
    Write-Host "Press Enter to continue or Ctrl+C to cancel..." -ForegroundColor Yellow
    Read-Host
}

$currentBatch = 0
$currentBatchFolder = $null
$foldersInCurrentBatch = 0
$totalProcessed = 0
$overallStopwatch = [System.Diagnostics.Stopwatch]::StartNew()

Write-Host "Starting reorganization..." -ForegroundColor Cyan
Write-Host ""

foreach ($folder in $sourceFolders) {
    if ($foldersInCurrentBatch -eq 0) {
        $currentBatch++
        $batchGuid = [System.Guid]::NewGuid().ToString()
        $batchName = "batch_$($currentBatch.ToString('D4'))_$batchGuid"
        $currentBatchFolder = Join-Path $TargetPath $batchName
        
        Write-Host "[$currentBatch/$batchCount] Creating: $batchName" -ForegroundColor Green
        
        if (-not $WhatIf) {
            New-Item -ItemType Directory -Path $currentBatchFolder -Force | Out-Null
        }
    }
    
    $targetLocation = Join-Path $currentBatchFolder $folder.Name
    
    if ($WhatIf) {
        Write-Host "  [WHATIF] Would move: $($folder.Name)" -ForegroundColor DarkGray
    }
    else {
        Move-Item -Path $folder.FullName -Destination $targetLocation -Force
        
        if ($totalProcessed % 10 -eq 0 -and $totalProcessed -gt 0) {
            $percentComplete = ($totalProcessed / $totalFolders) * 100
            Write-Host "  Progress: $totalProcessed/$totalFolders ($($percentComplete.ToString('F1'))%)" -ForegroundColor DarkCyan
        }
    }
    
    $foldersInCurrentBatch++
    $totalProcessed++
    
    if ($foldersInCurrentBatch -ge $BatchSize) {
        Write-Host "  Batch complete: $foldersInCurrentBatch folders" -ForegroundColor Green
        Write-Host ""
        $foldersInCurrentBatch = 0
    }
}

$overallStopwatch.Stop()

Write-Host ""
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "COMPLETE" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "Total processed: $totalProcessed" -ForegroundColor Green
Write-Host "Total batches: $currentBatch" -ForegroundColor Green
Write-Host "Total time: $($overallStopwatch.Elapsed.ToString('hh\:mm\:ss'))" -ForegroundColor Green
Write-Host ""

if (-not $WhatIf) {
    Write-Host "Next: Update TrainingDataProvider.cs to use txtDump (not txtDump/cache/epub)" -ForegroundColor Yellow
}
