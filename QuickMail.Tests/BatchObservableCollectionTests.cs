using System;
using System.Collections.Specialized;
using QuickMail.Helpers;
using Xunit;

namespace QuickMail.Tests;

/// <summary>
/// Regression tests for §1.5 — _batchActive used to leak as true if a mutation threw,
/// silently dropping every subsequent change notification, and nested BeginBatch/EndBatch
/// pairs clobbered _pendingReset.
/// </summary>
public class BatchObservableCollectionTests
{
    [Fact]
    public void BatchScope_RaisesSingleReset()
    {
        var c = new BatchObservableCollection<int>();
        int resetCount = 0;
        int addCount   = 0;
        c.CollectionChanged += (_, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Reset) resetCount++;
            else if (e.Action == NotifyCollectionChangedAction.Add) addCount++;
        };

        using (c.BeginBatchScope())
        {
            c.Add(1); c.Add(2); c.Add(3);
        }

        Assert.Equal(0, addCount);
        Assert.Equal(1, resetCount);
        Assert.Equal(3, c.Count);
    }

    [Fact]
    public void BatchScope_ClosesOnException_NoLeak()
    {
        // Regression: the old implementation left _batchActive = true forever after
        // an exception, so subsequent changes silently went unnoticed.
        var c = new BatchObservableCollection<int> { 0 };
        int events = 0;
        c.CollectionChanged += (_, _) => events++;

        Action act = () =>
        {
            using (c.BeginBatchScope())
            {
                c.Add(1);
                throw new InvalidOperationException("boom");
            }
        };
        Assert.Throws<InvalidOperationException>(act);

        // After the exception the collection must still emit events for new mutations.
        events = 0;
        c.Add(99);
        Assert.True(events > 0, "Collection silently stopped raising events after exception in batch.");
    }

    [Fact]
    public void NestedBatches_OnlyOuterFiresReset()
    {
        var c = new BatchObservableCollection<int>();
        int resetCount = 0;
        c.CollectionChanged += (_, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Reset) resetCount++;
        };

        using (c.BeginBatchScope())
        {
            c.Add(1);
            using (c.BeginBatchScope())
            {
                c.Add(2);
                c.Add(3);
            }
            // Still in outer batch — no event yet.
            Assert.Equal(0, resetCount);
        }

        Assert.Equal(1, resetCount);
        Assert.Equal(3, c.Count);
    }

    [Fact]
    public void EmptyBatch_RaisesNothing()
    {
        var c = new BatchObservableCollection<int>();
        int events = 0;
        c.CollectionChanged += (_, _) => events++;

        using (c.BeginBatchScope())
        {
            // no mutations
        }

        Assert.Equal(0, events);
    }

    [Fact]
    public void EndBatch_WithoutBegin_IsHarmless()
    {
        var c = new BatchObservableCollection<int>();
        // Should not throw or leave the collection in a bad state.
        c.EndBatch();
        c.Add(1);
        Assert.Single(c);
    }
}
