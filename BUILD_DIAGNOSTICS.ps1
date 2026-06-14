#!/usr/bin/env pwsh

# RiftVox Build Diagnostic Script
# This script helps identify remaining compilation errors in the solution

Write-Host "╔═══════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  RiftVox Solution - Build Error Analysis Report                  ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Project paths
$solutionPath = "C:\Users\G\Documents\Projects\RiftVox"
$coreProjectPath = "$solutionPath\src\RiftVox.Core\RiftVox.Core.csproj"
$uiProjectPath = "$solutionPath\src\RiftVox.UI\RiftVox.UI.csproj"
$testsProjectPath = "$solutionPath\RiftVox.Core.Tests\RiftVox.Core.Tests.csproj"

Write-Host "✅ FILES CREATED/UPDATED:" -ForegroundColor Green
Write-Host "   1. src\RiftVox.Core\Services\ChampionIconMatcher.cs"
Write-Host "      - New signature: LocateIconInFrame(sceneFrameBytes, templateBytes, sceneWidth, sceneHeight, cacheKey, threshold)"
Write-Host "      - Implements SIMD-accelerated SSD matching with early-exit"
Write-Host "      - Includes temporal coherence caching"
Write-Host ""

Write-Host "   2. src\RiftVox.Core\Services\PerformanceProfiler.cs"
Write-Host "      - Frame timing metrics"
Write-Host "      - Per-player match detection statistics"
Write-Host "      - FPS estimation"
Write-Host ""

Write-Host "   3. src\RiftVox.Core\Services\MemoryProfiler.cs"
Write-Host "      - Heap allocation tracking"
Write-Host "      - GC collection counting"
Write-Host "      - Memory snapshot support"
Write-Host ""

Write-Host "   4. src\RiftVox.Core\Services\DebugVisualisation.cs"
Write-Host "      - Frame visualisation with match markers"
Write-Host "      - CSV & HTML report generation"
Write-Host "      - Match accuracy statistics"
Write-Host ""

Write-Host "   5. src\RiftVox.Core\Services\VisionCaptureEngine.cs"
Write-Host "      - Updated to use new ChampionIconMatcher signature"
Write-Host "      - Integrated PerformanceProfiler"
Write-Host "      - Integrated DebugVisualisation"
Write-Host "      - Integrated MemoryProfiler"
Write-Host ""

Write-Host "   6. src\RiftVox.Core\Debugging\MatchingDebugHelper.cs"
Write-Host "      - Test utilities for matcher validation"
Write-Host "      - Benchmark functions"
Write-Host ""

Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Yellow
Write-Host "⚠️  POTENTIAL COMPILATION ERRORS - CHECK THE FOLLOWING:" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Yellow
Write-Host ""

Write-Host "1. TEST FILE SIGNATURE MISMATCH"
Write-Host "   File: RiftVox.Core.Tests\ChampionIconMatcherTests.cs"
Write-Host "   Issue: May be calling LocateIconInFrame with old 3-parameter signature"
Write-Host "   Fix: Update test calls to use new 6-parameter signature:"
Write-Host "        ChampionIconMatcher.LocateIconInFrame(scene, template, width, height, cacheKey, threshold)"
Write-Host ""

Write-Host "2. INTERFACE COMPATIBILITY"
Write-Host "   Check: Verify IScreenCapturer.CaptureRegion returns BGRA byte[] (4 bytes/pixel)"
Write-Host "   Current assumption: rawPixels is BGRA format"
Write-Host "   If different: Update ConvertBgraToGrayscale() or CaptureRegion() call"
Write-Host ""

Write-Host "3. NAMESPACE IMPORTS"
Write-Host "   Verify all files have correct 'using' statements:"
Write-Host "   - VisionCaptureEngine.cs imports PerformanceProfiler"
Write-Host "   - VisionCaptureEngine.cs imports MemoryProfiler"
Write-Host "   - VisionCaptureEngine.cs imports DebugVisualisation"
Write-Host ""

Write-Host "4. BITMAP LOCKING"
Write-Host "   Check: PrepareMinimapTemplate() assumes PixelFormat.Format32bppArgb (BGRA)"
Write-Host "   If error: Verify System.Drawing.Common package is installed"
Write-Host ""

Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "📋 BUILD INSTRUCTIONS" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""

Write-Host "1. Open Visual Studio Community 2026"
Write-Host "2. Solution > Clean Solution"
Write-Host "3. Solution > Rebuild All"
Write-Host "4. View > Error List (Ctrl+\\, E)"
Write-Host "5. For each error:"
Write-Host "   - Right-click and 'Go To Code'"
Write-Host "   - Verify the calling signature matches the function definition"
Write-Host ""

Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🔍 COMMON FIXES:" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "ERROR: 'The type or namespace name ... could not be found'"
Write-Host "  → Add missing 'using' statement at top of file"
Write-Host ""

Write-Host "ERROR: 'No overload for method ... takes X parameters'"
Write-Host "  → Update function call signature to match definition"
Write-Host "  → Check parameter order: (scene, template, width, height, cacheKey, threshold)"
Write-Host ""

Write-Host "ERROR: 'Cannot convert null literal to non-nullable reference type'"
Write-Host "  → Use nullable reference: 'string?' or 'byte[]?'"
Write-Host "  → Add null checks with '?.'' operator"
Write-Host ""

Write-Host ""
Write-Host "✅ Build diagnostic complete. Review errors and apply fixes above." -ForegroundColor Green
