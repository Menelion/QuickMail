using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace QuickMail.Helpers;

/// <summary>
/// An <see cref="ObservableCollection{T}"/> that can batch multiple mutations into a single
/// <see cref="NotifyCollectionChangedAction.Reset"/> notification.
/// <para>
/// Prefer <see cref="BeginBatchScope"/> in <c>using</c> form — it guarantees the batch is
/// closed even if a mutation throws. The manual <see cref="BeginBatch"/>/<see cref="EndBatch"/>
/// pair is retained for callers that already use <c>try</c>/<c>finally</c>.
/// </para>
/// <para>
/// During the batch no <see cref="INotifyCollectionChanged.CollectionChanged"/> or
/// <see cref="INotifyPropertyChanged.PropertyChanged"/> events are raised.  Calling
/// <see cref="EndBatch"/> fires a single <c>Reset</c> event if any mutations occurred, letting
/// the bound <c>ListView</c> emit one UIA <c>StructureChanged</c> notification instead of one
/// per insert — which prevents screen readers from re-announcing the focused item after every
/// individual insert during background sync.
/// </para>
/// </summary>
public sealed class BatchObservableCollection<T> : ObservableCollection<T>
{
    // Depth counter rather than a bool so nested BeginBatch calls don't clobber state and
    // the outer scope is the one that fires the Reset.
    private int _batchDepth;
    private bool _pendingReset;

    public BatchObservableCollection() { }
    public BatchObservableCollection(IEnumerable<T> collection) : base(collection) { }

    /// <summary>Begin suppressing individual <see cref="CollectionChanged"/> events.</summary>
    public void BeginBatch() => _batchDepth++;

    /// <summary>
    /// End the batch.  If any mutations occurred during the batch a single
    /// <see cref="NotifyCollectionChangedAction.Reset"/> event is raised when the
    /// outermost batch closes.
    /// </summary>
    public void EndBatch()
    {
        if (_batchDepth == 0) return;
        _batchDepth--;
        if (_batchDepth > 0) return;
        if (!_pendingReset) return;
        _pendingReset = false;
        OnPropertyChanged(new PropertyChangedEventArgs("Count"));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Open a batch scope that closes automatically when disposed. Use with <c>using</c>:
    /// <code>
    /// using (collection.BeginBatchScope()) { /* mutations */ }
    /// </code>
    /// This guarantees the batch is closed even if a mutation throws — the manual
    /// <see cref="BeginBatch"/>/<see cref="EndBatch"/> pair would leak <c>_batchDepth</c>
    /// on exception and silently drop every subsequent change notification.
    /// </summary>
    public IDisposable BeginBatchScope()
    {
        BeginBatch();
        return new BatchReleaser(this);
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (_batchDepth > 0)
        {
            _pendingReset = true;
            return;
        }
        base.OnCollectionChanged(e);
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (_batchDepth > 0) return;
        base.OnPropertyChanged(e);
    }

    private sealed class BatchReleaser : IDisposable
    {
        private BatchObservableCollection<T>? _owner;
        public BatchReleaser(BatchObservableCollection<T> owner) => _owner = owner;
        public void Dispose()
        {
            var owner = _owner;
            if (owner == null) return;
            _owner = null;
            owner.EndBatch();
        }
    }
}
