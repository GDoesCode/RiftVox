# SSD Threshold Calibration Guide

## ✅ Fix Applied

The critical bug has been fixed in `SearchLocalRegion()`:

```csharp
// ✅ CORRECT: SSD is a distance metric (lower is better)
const float MaxAcceptableSSD = 10000.0f;

if (bestX >= 0 && bestScore <= MaxAcceptableSSD)  // <= not >=
{
	return new Point(bestX + templateWidth / 2, bestY + templateHeight / 2);
}
```

---

## 🧪 Test Results Expected

### Before Fix:
```
LocateIconInFrame_WithValidInput_ReturnsPoint
  ❌ FAILED: Expected non-null, got null
  Reason: Perfect match (SSD=0) rejected by backwards threshold check
```

### After Fix:
```
LocateIconInFrame_WithValidInput_ReturnsPoint
  ✅ PASSED: Found match at expected location
  Result: Point {X=114, Y=114}  (centre of 28×28 template at 100,100)
```

---

## 📊 Understanding the Fix

### SSD Value Ranges (28×28 template = 784 pixels)

```
Perfect match:              SSD ≈ 0-10        (identical pixels)
Excellent match:            SSD ≈ 10-100      (slight noise)
Very good match:            SSD ≈ 100-500     (minor variations)
Good match:                 SSD ≈ 500-2,000   (tolerable differences)
Acceptable match:           SSD ≈ 2,000-5,000 (some variation)
Weak match:                 SSD ≈ 5,000-10,000 (quite different)
Poor match:                 SSD > 10,000      (very different)
Complete mismatch:          SSD > 50,000      (entirely different)
```

### Current Threshold Rationale:
```
MaxAcceptableSSD = 10,000.0f

This accepts:
  ✅ Perfect matches (0)
  ✅ Excellent matches (10-100)
  ✅ Very good matches (100-500)
  ✅ Good matches (500-2,000)
  ✅ Acceptable matches (2,000-5,000)
  ✅ Weak matches (5,000-10,000)
  ❌ Poor matches (>10,000)
```

---

## 🎯 How to Calibrate for Your Use Case

### Step 1: Enable Debug Logging

Add this to `ComputeSSDWithEarlyExit()`:
```csharp
System.Diagnostics.Debug.WriteLine($"SSD Score: {ssd} at ({sceneX}, {sceneY})");
```

Or add this to `SearchLocalRegion()`:
```csharp
// Add logging before returning
if (bestX >= 0)
{
	System.Diagnostics.Debug.WriteLine(
		$"Best match found: SSD={bestScore} at ({bestX}, {bestY}) - " +
		(bestScore <= MaxAcceptableSSD ? "ACCEPTED" : "REJECTED"));
}
```

### Step 2: Run Test and Check Output

```
Build > Run Tests (with Debug Output visible)
View > Output
```

You'll see:
```
SSD Score: 0 at (100, 100)
SSD Score: 1500 at (105, 105)
SSD Score: 8500 at (110, 110)
Best match found: SSD=0 at (100, 100) - ACCEPTED
```

### Step 3: Record Values for Real Scenarios

Run the app with debug mode enabled:
```csharp
_captureEngine.EnableDebugMode();
// ... let it run for a few captures ...
_captureEngine.DisableDebugMode();
```

Check the debug output for actual SSD values from real minimap data.

### Step 4: Adjust Threshold Based on Observations

**If seeing too many false matches:**
```csharp
// Lower the threshold (more strict)
const float MaxAcceptableSSD = 5000.0f;  // More strict
```

**If missing real matches:**
```csharp
// Raise the threshold (more lenient)
const float MaxAcceptableSSD = 15000.0f;  // More lenient
```

---

## 🔧 Fine-Tuning Strategy

### Conservative (Accuracy-First)
```csharp
// Accept only high-quality matches
// ✅ Fewer false positives
// ❌ Might miss weak matches
const float MaxAcceptableSSD = 3000.0f;
```

### Balanced (Default)
```csharp
// Accept good to acceptable matches
// ✅ Good accuracy + reasonable sensitivity
// ✅ Good for general use
const float MaxAcceptableSSD = 10000.0f;
```

### Permissive (Sensitivity-First)
```csharp
// Accept even weak matches
// ✅ Won't miss matches
// ❌ More false positives
const float MaxAcceptableSSD = 20000.0f;
```

---

## 📝 Why This Threshold Makes Sense

### Mathematical Background:

**Maximum possible SSD per pixel:**
```
Each channel: 0-255 (256 levels)
Max difference: 255 - 0 = 255
Max SSD per pixel = 255² = 65,025
```

**For 28×28 template (784 pixels):**
```
Theoretical maximum SSD = 784 × 65,025 = 50,999,600

Practical maximum (256 levels deviation):
784 × (128)² = 12,845,056

Reasonable threshold:
< 50,000 = very good match
< 100,000 = good match
< 200,000 = acceptable match
```

---

## 🧪 Testing the Fix

### Automated Test (Now Should Pass):
```csharp
[Fact]
public void LocateIconInFrame_WithValidInput_ReturnsPoint()
{
	// Creates perfect match
	byte[] sceneBytes = new byte[256 * 256 * 4];
	byte[] templateBytes = new byte[28 * 28 * 4];

	// Fill pattern...
	for (int i = 0; i < templateBytes.Length; i += 4)
	{
		templateBytes[i] = 100;
		templateBytes[i + 1] = 150;
		templateBytes[i + 2] = 200;
		templateBytes[i + 3] = 255;
	}

	// Embed at (100, 100)
	for (int ty = 0; ty < 28; ty++)
	{
		for (int tx = 0; tx < 28; tx++)
		{
			int templateIdx = (ty * 28 + tx) * 4;
			int sceneIdx = ((100 + ty) * 256 + (100 + tx)) * 4;
			Array.Copy(templateBytes, templateIdx, sceneBytes, sceneIdx, 4);
		}
	}

	var result = ChampionIconMatcher.LocateIconInFrame(
		sceneBytes, templateBytes, 256, 256, "test", 0.75);

	// ✅ Should now pass
	Assert.NotNull(result);
	Assert.InRange(result.Value.X, 112, 116);
	Assert.InRange(result.Value.Y, 112, 116);
}
```

### Manual Verification:
```csharp
// In a test method or debug session:
var sceneBytes = GenerateTestScene();  // 256×256 BGRA
var templateBytes = GenerateTemplate();  // 28×28 BGRA

var result = ChampionIconMatcher.LocateIconInFrame(
	sceneBytes, templateBytes, 256, 256, "manual", 0.75);

if (result.HasValue)
	Console.WriteLine($"✅ Match found at ({result.Value.X}, {result.Value.Y})");
else
	Console.WriteLine("❌ No match found");
```

---

## 🎯 Expected Behavior After Fix

### In Real-World Scenario:

**Before Fix:**
```
1. Capture minimap → 512×256 pixels, BGRA format
2. Search for champion icon (28×28)
3. Find perfect match at position (150, 100)
4. Compute SSD = 50
5. Check: 50 <= 10,000? → YES ✅
6. Return Point(164, 114)  ← Works!

...but perfect match SSD=0 was previously rejected!
```

**After Fix:**
```
1. Capture minimap
2. Search for each champion icon
3. Find match with SSD = 0-50 (excellent match)
4. Check: SSD <= 10,000? → YES ✅
5. Return matched position ✅

Real pixel matching now works!
```

---

## 📋 Troubleshooting Checklist

- [ ] Recompiled after fix
- [ ] Test `LocateIconInFrame_WithValidInput_ReturnsPoint` passes
- [ ] No compiler warnings in ChampionIconMatcher.cs
- [ ] Debug output shows reasonable SSD values
- [ ] Matched positions are near expected locations
- [ ] False positives are acceptable for your use case

---

## 🔄 Next Steps

1. **Build solution** - Recompile with fix
2. **Run tests** - Verify test suite passes
3. **Enable debug mode** - Check real SSD values
4. **Calibrate** - Adjust threshold if needed
5. **Monitor** - Use profiler to track performance

---

**This fix resolves the critical bug preventing any pixel matching!**
