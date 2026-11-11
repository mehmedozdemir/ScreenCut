namespace ScreenCut.Annotations;

/// <summary>The ordered list of annotations with snapshot-based undo/redo.</summary>
internal sealed class AnnotationDocument
{
    private readonly Stack<List<Annotation>> _undo = new();
    private readonly Stack<List<Annotation>> _redo = new();
    private List<Annotation> _items = [];

    public IReadOnlyList<Annotation> Items => _items;

    public bool CanUndo => _undo.Count > 0;

    public bool CanRedo => _redo.Count > 0;

    public int NextNumber => _items.OfType<NumberAnnotation>().Select(n => n.Number).DefaultIfEmpty(0).Max() + 1;

    /// <summary>Records the current state so the next mutation can be undone.</summary>
    public void RecordChange()
    {
        _undo.Push(Snapshot());
        _redo.Clear();
    }

    /// <summary>Reverts to the last recorded state without making it redoable (used when an edit is cancelled).</summary>
    public void CancelChange()
    {
        if (_undo.Count > 0)
        {
            _items = _undo.Pop();
        }
    }

    public void Add(Annotation annotation)
    {
        RecordChange();
        _items.Add(annotation);
    }

    public void AddWithoutRecording(Annotation annotation) => _items.Add(annotation);

    public void Remove(Annotation annotation)
    {
        if (!_items.Contains(annotation))
        {
            return;
        }

        RecordChange();
        _items.Remove(annotation);
    }

    public void RemoveWithoutRecording(Annotation annotation) => _items.Remove(annotation);

    public bool Undo()
    {
        if (_undo.Count == 0)
        {
            return false;
        }

        _redo.Push(Snapshot());
        _items = _undo.Pop();
        return true;
    }

    public bool Redo()
    {
        if (_redo.Count == 0)
        {
            return false;
        }

        _undo.Push(Snapshot());
        _items = _redo.Pop();
        return true;
    }

    public void Clear()
    {
        _items = [];
        _undo.Clear();
        _redo.Clear();
    }

    private List<Annotation> Snapshot() => _items.Select(a => a.Clone()).ToList();
}
