using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace RiftVox.Core.Services;

/// <summary>
/// Tracks memory allocations during matching operations.
/// Useful for identifying allocation hotspots and GC pressure.
/// </summary>
public class MemoryProfiler
{
    private long _initialMemory;
    private long _peakMemory;
    private long _totalAllocations = 0;
    private long _totalDeallocations = 0;
    private long _sessionStartMemory;
    private readonly Dictionary<string, MemorySnapshot> _snapshots = new();

    public struct MemorySnapshot
    {
        public long Timestamp;
        public long MemoryUsedBytes;
        public long GcGen0Collections;
        public long GcGen1Collections;
        public long GcGen2Collections;
    }

    public void StartSession()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        _sessionStartMemory = GC.GetTotalMemory(false);
        _initialMemory = _sessionStartMemory;
        _peakMemory = _sessionStartMemory;
        _totalAllocations = 0;
        _totalDeallocations = 0;
        _snapshots.Clear();
    }

    public void TakeSnapshot(string label)
    {
        long current = GC.GetTotalMemory(false);
        _peakMemory = Math.Max(_peakMemory, current);

        _snapshots[label] = new MemorySnapshot
        {
            Timestamp = DateTime.Now.Ticks,
            MemoryUsedBytes = current,
            GcGen0Collections = GC.CollectionCount(0),
            GcGen1Collections = GC.CollectionCount(1),
            GcGen2Collections = GC.CollectionCount(2)
        };
    }

    public long GetCurrentMemoryBytes() => GC.GetTotalMemory(false);

    public long GetMemoryDeltaBytes() => GetCurrentMemoryBytes() - _initialMemory;

    public long GetPeakMemoryBytes() => _peakMemory;

    public string GetMemoryReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("═══ MEMORY PROFILE ═══");
        sb.AppendLine($"📦 Session Start: {_sessionStartMemory / 1024}KB");
        sb.AppendLine($"📦 Current Usage: {GetCurrentMemoryBytes() / 1024}KB");
        sb.AppendLine($"📦 Peak Usage: {_peakMemory / 1024}KB");
        sb.AppendLine($"📦 Delta: {GetMemoryDeltaBytes() / 1024:+#;-#;0}KB");
        sb.AppendLine($"🗑️  Gen0 Collections: {GC.CollectionCount(0)}");
        sb.AppendLine($"🗑️  Gen1 Collections: {GC.CollectionCount(1)}");
        sb.AppendLine($"🗑️  Gen2 Collections: {GC.CollectionCount(2)}");

        if (_snapshots.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("📸 Snapshots:");
            foreach (var kvp in _snapshots)
            {
                var snap = kvp.Value;
                sb.AppendLine($"   [{kvp.Key}] {snap.MemoryUsedBytes / 1024}KB " +
                    $"(Gen0:{snap.GcGen0Collections} Gen1:{snap.GcGen1Collections} Gen2:{snap.GcGen2Collections})");
            }
        }

        return sb.ToString();
    }

    public void Reset()
    {
        _snapshots.Clear();
        _initialMemory = 0;
        _peakMemory = 0;
        _totalAllocations = 0;
        _totalDeallocations = 0;
    }
}