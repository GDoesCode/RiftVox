# LocateIconInFrame Test Failure Analysis

## 🔴 ROOT CAUSE: Threshold Logic Inversion

### The Problem

**SSD (Sum of Squared Differences):**
- `SSD = 0` → Perfect match (lower is better)
- `SSD = 1000` → Poor match (higher is worse)

**But the code treats threshold as similarity (0-1 where higher is better):**
```csharp
// ❌ WRONG - This never evaluates correctly
if (highestMatchScore >= similarityThreshold)  // SSD >= 0.75?
{
	return new Point(centerX, centerY);
}
```

**Example Scenario:**
- Perfect match: `SSD = 0.0`
- Check: `0.0 >= 0.75`? → **FALSE** ❌ Rejects perfect match!
- Bad match: `SSD = 50000`
- Check: `50000 >= 0.75`? → **TRUE** ✅ Accepts terrible match!

---

## 🧪 Why the Test Fails

### Test Code (from ChampionIconMatcherTests_UPDATED.cs):
```csharp
[Fact]
public void LocateIconInFrame_WithValidInput_ReturnsPoint()
{
	// Scene: 256x256 BGRA = 262,144 bytes
	// Template: 28x28 BGRA = 3,136 bytes
	// Embed identical template at (100, 100)
	// Expected: Match found at ~(100+14, 100+14) = (114, 114)

	var result = ChampionIconMatcher.LocateIconInFrame(
		sceneBytes, templateBytes, 256, 256, "test", 0.75);

	Assert.NotNull(result);  // ❌ FAILS because result is null
}
```

### Why It Returns Null:

1. **Test creates perfect matching pattern** - fills template pixels with identical values
2. **Matcher runs, finds perfect match** - SSD = 0 (perfect)
3. **Threshold check fails** - `0 >= 0.75`? → FALSE
4. **Returns null** ❌

---

## 🔧 The Fix: Invert Threshold Logic

### Current Code (WRONG):
```csharp
// In LocateIconInFrame method
if (highestMatchScore >= similarityThreshold)
{
	int centreX = bestX + (templateBmp.Width / 2);
	int centreY = bestY + (templateBmp.Height / 2);
	return new Point(centreX, centreY);
}

return null;
```

### Corrected Code (RIGHT):
```csharp
// SSD is a DISTANCE metric - lower is better
// We want: SSD <= threshold (invert the logic!)

// Option 1: Invert the comparison
if (highestMatchScore <= similarityThreshold * 1000)  // Scale threshold to SSD range
{
	int centreX = bestX + (templateBmp.Width / 2);
	int centreY = bestY + (templateBmp.Height / 2);
	return new Point(centreX, centreY);
}

// Option 2: Use a proper SSD threshold (easier)
const float MaxAcceptableSSD = 5000.0f;  // Adjust based on testing
if (highestMatchScore <= MaxAcceptableSSD)
{
	int centreX = bestX + (templateBmp.Width / 2);
	int centreY = bestY + (templateBmp.Height / 2);
	return new Point(centreX, centreY);
}
```

---

## 🎯 Understanding SSD Values

### Pixel Range: 0-255 per channel

For a single pixel mismatch:
```
Pixel A: R=255, G=255, B=255 (White)
Pixel B: R=0, G=0, B=0 (Black)

Difference = (255-0)² + (255-0)² + (255-0)² = 65025 per pixel
```

For 28×28 template (784 pixels):
```
Perfect match:    SSD = 0
Slight variation: SSD = 784 × 100 = 78,400 (if avg 10-unit error)
Major mismatch:   SSD = 784 × 65,025 = 50,999,600
```

### Recommended Thresholds:
```
- Perfect match threshold: SSD <= 1,000
- Good match threshold:    SSD <= 10,000
- Acceptable threshold:    SSD <= 50,000
- Poor match threshold:    SSD <= 100,000+
```

---

## 🔴 CRITICAL: The similarityThreshold Parameter

**Current interpretation (WRONG):**
- `similarityThreshold = 0.75` means "similarity must be 75%" (0-1 scale)
- But SSD values are in thousands/millions, not 0-1

**Two Solutions:**

### Solution A: Scale the threshold
```csharp
// Threshold is 0-1, scale it to SSD range
const float MaxSSDPerPixel = 65025;  // Max possible difference
float maxAcceptableSSD = similarityThreshold * MaxSSDPerPixel * 784;  // 784 pixels in 28×28

if (highestMatchScore <= maxAcceptableSSD)
{
	return new Point(...);
}
```

### Solution B: Use raw SSD values (BETTER)
```csharp
// Ignore the similarityThreshold parameter for now
// Use fixed thresholds based on actual SSD measurements

if (highestMatchScore <= 5000)  // Good match
{
	return new Point(...);
}
```

---

## 📊 How to Calibrate the Right Threshold

### Step 1: Run debug mode and capture SSD values

Add logging to the matcher:
```csharp
private static float ComputeSSDWithEarlyExit(...)
{
	float ssd = 0f;
	// ... computation ...

	System.Diagnostics.Debug.WriteLine($"SSD computed: {ssd}");  // Log it!
	return ssd;
}
```

### Step 2: Run with test data and note SSD values

```
Perfect match (test): SSD = 0-50
Good real-world match: SSD = 100-500
Weak match: SSD = 1000-5000
No match: SSD = 10000+
```

### Step 3: Set threshold based on measurements

```csharp
// After observing real values:
if (highestMatchScore <= 500)  // Accept matches with SSD <= 500
{
	return new Point(...);
}
```

---

## 🧪 Test Fix

### Current Failing Test:
```csharp
[Fact]
public void LocateIconInFrame_WithValidInput_ReturnsPoint()
{
	// Creates perfect match, but test fails because threshold logic is backwards
	byte[] sceneBytes = new byte[256 * 256 * 4];
	byte[] templateBytes = new byte[28 * 28 * 4];

	// Fill with identical pattern
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

	// This fails because:
	// 1. Perfect match found: SSD = 0
	// 2. Check: 0 >= 0.75? → FALSE
	// 3. Returns null ❌
	Assert.NotNull(result);
}
```

### Fixed Test:
```csharp
[Fact]
public void LocateIconInFrame_WithValidInput_ReturnsPoint()
{
	// [Same setup as above...]

	var result = ChampionIconMatcher.LocateIconInFrame(
		sceneBytes, 
		templateBytes, 
		256, 256, 
		"test", 
		similarityThreshold: 0.75);  // This param is now ignored or reinterpreted

	// Should now succeed because threshold logic is fixed
	Assert.NotNull(result);
	Assert.InRange(result.Value.X, 112, 116);  // ~(100 + 14)
	Assert.InRange(result.Value.Y, 112, 116);  // ~(100 + 14)
}
```

---

## 📋 Complete Fix Required

### ChampionIconMatcher.cs - LocateIconInFrame method

**BEFORE (currently fails):**
```csharp
if (highestMatchScore >= similarityThreshold)
{
	int centreX = bestX + (templateBmp.Width / 2);
	int centreY = bestY + (templateBmp.Height / 2);
	return new Point(centreX, centreY);
}

return null;
```

**AFTER (correct):**
```csharp
// Convert similarity threshold (0-1) to SSD threshold
// Higher similarity = lower SSD tolerance
// similarityThreshold 0.75 = accept SSD up to a reasonable value
const float MaxSSDForThreshold = 10000.0f;  // Calibrate based on real data
float ssdThreshold = (1.0f - similarityThreshold) * MaxSSDForThreshold;

if (highestMatchScore <= ssdThreshold)  // Lower SSD is better
{
	int centreX = bestX + (templateWidth / 2);  // Use templateWidth, not bmpData
	int centreY = bestY + (templateHeight / 2);
	return new Point(centreX, centreY);
}

return null;
```

---

## ✅ Why This Fixes Both Issues

1. **Test now passes** - Perfect match (SSD=0) passes threshold check ✅
2. **Real code now matches pixels** - Actual matches are recognized instead of rejected ✅
3. **Threshold works correctly** - Higher threshold = more lenient ✅

---

## 🧪 Verification Steps

1. **Fix the threshold logic** (change `>=` to `<=` with proper scaling)
2. **Rebuild solution**
3. **Run test** - should now pass
4. **Enable debug mode** - enable frame output to verify matches
5. **Monitor SSD values** - add logging to calibrate optimal threshold

---

**This is the CRITICAL BUG preventing all matching!**
