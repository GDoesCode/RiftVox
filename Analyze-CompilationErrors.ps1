#!/usr/bin/env pwsh
<#
.SYNOPSIS
	RiftVox Compilation Error Analyzer

.DESCRIPTION
	Scans the RiftVox solution for common compilation errors and provides fixes

.EXAMPLE
	.\Analyze-CompilationErrors.ps1
#>

param(
	[string]$SolutionPath = "C:\Users\G\Documents\Projects\RiftVox"
)

function Write-Header {
	param([string]$Text)
	Write-Host "`n╔═══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
	Write-Host "║  $Text" -ForegroundColor Cyan
	Write-Host "╚═══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
}

function Write-Section {
	param([string]$Text)
	Write-Host "`n$Text" -ForegroundColor Yellow
	Write-Host ("=" * $Text.Length) -ForegroundColor Yellow
}

function Test-FileExists {
	param([string]$FilePath, [string]$Description)

	if (Test-Path $FilePath) {
		Write-Host "✅ $Description" -ForegroundColor Green
		return $true
	} else {
		Write-Host "❌ MISSING: $Description" -ForegroundColor Red
		Write-Host "   Expected: $FilePath" -ForegroundColor Red
		return $false
	}
}

function Search-FileContent {
	param([string]$FilePath, [string]$Pattern, [string]$Description)

	if (-not (Test-Path $FilePath)) {
		return $false
	}

	$content = Get-Content $FilePath -Raw
	if ($content -match $Pattern) {
		Write-Host "✅ $Description" -ForegroundColor Green
		return $true
	} else {
		Write-Host "⚠️  CHECK NEEDED: $Description" -ForegroundColor Yellow
		return $false
	}
}

# Main execution
Write-Header "RiftVox Compilation Error Analysis"

Write-Section "FILE INTEGRITY CHECK"

$filesOk = $true
$filesOk = (Test-FileExists "$SolutionPath\src\RiftVox.Core\Services\ChampionIconMatcher.cs" "ChampionIconMatcher.cs") -and $filesOk
$filesOk = (Test-FileExists "$SolutionPath\src\RiftVox.Core\Services\PerformanceProfiler.cs" "PerformanceProfiler.cs") -and $filesOk
$filesOk = (Test-FileExists "$SolutionPath\src\RiftVox.Core\Services\MemoryProfiler.cs" "MemoryProfiler.cs") -and $filesOk
$filesOk = (Test-FileExists "$SolutionPath\src\RiftVox.Core\Services\DebugVisualisation.cs" "DebugVisualisation.cs") -and $filesOk
$filesOk = (Test-FileExists "$SolutionPath\src\RiftVox.Core\Services\VisionCaptureEngine.cs" "VisionCaptureEngine.cs") -and $filesOk
$filesOk = (Test-FileExists "$SolutionPath\src\RiftVox.Core\Debugging\MatchingDebugHelper.cs" "MatchingDebugHelper.cs") -and $filesOk

Write-Section "FUNCTION SIGNATURE VERIFICATION"

$signaturesOk = $true
$signaturesOk = (Search-FileContent "$SolutionPath\src\RiftVox.Core\Services\ChampionIconMatcher.cs" `
	"public static Point\? LocateIconInFrame\(" `
	"LocateIconInFrame method signature exists") -and $signaturesOk

$signaturesOk = (Search-FileContent "$SolutionPath\src\RiftVox.Core\Services\ChampionIconMatcher.cs" `
	"int sceneWidth.*int sceneHeight" `
	"LocateIconInFrame includes width/height parameters") -and $signaturesOk

$signaturesOk = (Search-FileContent "$SolutionPath\src\RiftVox.Core\Services\ChampionIconMatcher.cs" `
	"string cacheKey" `
	"LocateIconInFrame includes cacheKey parameter") -and $signaturesOk

Write-Section "NAMESPACE & IMPORTS CHECK"

$importsOk = $true
$importsOk = (Search-FileContent "$SolutionPath\src\RiftVox.Core\Services\VisionCaptureEngine.cs" `
	"namespace RiftVox\.Core\.Services" `
	"VisionCaptureEngine has correct namespace") -and $importsOk

$importsOk = (Search-FileContent "$SolutionPath\src\RiftVox.Core\Services\VisionCaptureEngine.cs" `
	"using.*System\.Numerics" `
	"Required: System.Numerics using statement (for SIMD)") -and $importsOk

Write-Section "COMMON ERROR PATTERNS"

Write-Host "`nSearching for known problematic patterns...`n"

# Check for old 3-parameter calls
$matcherUsage = Get-ChildItem "$SolutionPath\src" -Recurse -Filter "*.cs" | 
	Where-Object { (Get-Content $_ -Raw) -match "LocateIconInFrame\([^,]+,[^,]+,[^,]+\)" } |
	Select-Object -ExpandProperty FullName

if ($matcherUsage) {
	Write-Host "⚠️  POSSIBLE 3-PARAMETER CALLS FOUND:" -ForegroundColor Yellow
	foreach ($file in $matcherUsage) {
		Write-Host "   - $file" -ForegroundColor Yellow
		Write-Host "   → Check this file for old function signature usage" -ForegroundColor Yellow
	}
} else {
	Write-Host "✅ No obvious 3-parameter LocateIconInFrame calls detected" -ForegroundColor Green
}

# Check for American spelling
$americanSpelling = Get-ChildItem "$SolutionPath\src" -Recurse -Filter "*.cs" | 
	Where-Object { (Get-Content $_ -Raw) -match "DebugVisuali[sz]ation|_debugVisuali[sz]er" } |
	Select-Object -ExpandProperty FullName

if ($americanSpelling) {
	Write-Host "`n⚠️  AMERICAN SPELLING DETECTED (should be British):" -ForegroundColor Yellow
	foreach ($file in $americanSpelling) {
		Write-Host "   - $file" -ForegroundColor Yellow
		Write-Host "   → Replace 'DebugVisualization' with 'DebugVisualisation'" -ForegroundColor Yellow
	}
} else {
	Write-Host "`n✅ British spelling ('Visualisation') is consistent" -ForegroundColor Green
}

Write-Section "TEST FILE COMPLIANCE"

if (Test-Path "$SolutionPath\RiftVox.Core.Tests\ChampionIconMatcherTests.cs") {
	$testContent = Get-Content "$SolutionPath\RiftVox.Core.Tests\ChampionIconMatcherTests.cs" -Raw

	if ($testContent -match "LocateIconInFrame\([^,]+,[^,]+,[^,]+,[^,]+") {
		Write-Host "✅ Test file uses 6-parameter LocateIconInFrame calls" -ForegroundColor Green
	} elseif ($testContent -match "LocateIconInFrame\([^,]+,[^,]+,[^,]+\)") {
		Write-Host "❌ TEST FILE USES OLD 3-PARAMETER SIGNATURE" -ForegroundColor Red
		Write-Host "   File: RiftVox.Core.Tests\ChampionIconMatcherTests.cs" -ForegroundColor Red
		Write-Host "   Action: Replace with ChampionIconMatcherTests_UPDATED.cs" -ForegroundColor Red
	}
} else {
	Write-Host "⚠️  Test file not found at expected location" -ForegroundColor Yellow
}

Write-Section "SUMMARY & NEXT STEPS"

if ($filesOk -and $signaturesOk -and $importsOk) {
	Write-Host "`n✅ All checks passed! Ready to build." -ForegroundColor Green
	Write-Host "`nNext steps:" -ForegroundColor Cyan
	Write-Host "  1. Open Visual Studio Community 2026"
	Write-Host "  2. Build > Clean Solution"
	Write-Host "  3. Build > Rebuild Solution"
	Write-Host "  4. View > Error List to see any remaining issues"
} else {
	Write-Host "`n⚠️  Some checks failed. Please review the items above." -ForegroundColor Yellow
	Write-Host "`nCommon fixes:" -ForegroundColor Cyan
	Write-Host "  1. Verify all files exist in expected locations"
	Write-Host "  2. Check for old 3-parameter LocateIconInFrame() calls"
	Write-Host "  3. Ensure British spelling: 'Visualisation' not 'Visualization'"
	Write-Host "  4. Update test files to use new 6-parameter signature"
}

Write-Host "`n" -ForegroundColor Cyan
