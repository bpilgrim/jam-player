namespace JamPlayer.TagFilter;

public sealed class SetOperationChangedEventArgs : EventArgs
{
    public SetOperationChangedEventArgs(SetOperation operation)
    {
        Operation = operation;
    }

    public SetOperation Operation { get; }
}

/// <summary>
/// One tag participating in a filter expression, together with the set operation
/// that decides how it contributes to the result.
/// </summary>
/// <remarks>
/// The asset ids owned by the tag are snapshotted on construction. Filtering runs on
/// every keystroke and every scroll tick, so it must never touch the database; the
/// cost of loading the set is paid once, when the tag enters the expression.
/// </remarks>
public sealed class TagFilterMember
{
    private readonly Tag _tag;
    private SetOperation _operation;

    /// <summary>
    /// Raised when the operation changes, so the owning expression can invalidate
    /// its cached result. Cycling a tag from Union to Intersect changes what the
    /// expression matches without adding or removing a member, which a plain
    /// collection-changed notification would miss.
    /// </summary>
    public event EventHandler<SetOperationChangedEventArgs>? SetOperationChanged;

    public TagFilterMember(Tag tag)
        : this(tag, SetOperation.Union)
    {
    }

    public TagFilterMember(Tag tag, SetOperation operation)
    {
        ArgumentNullException.ThrowIfNull(tag);

        _tag = tag;
        _operation = operation;
        AssetIdsOwned = new HashSet<int>(tag.AssetIds);
    }

    public int TagId => _tag.Id;

    public string Name => _tag.Name;

    public string TagGroupName => _tag.GroupName;

    /// <summary>Ids of the assets carrying this tag, snapshotted at construction.</summary>
    public HashSet<int> AssetIdsOwned { get; }

    public SetOperation Operation
    {
        get => _operation;
        set
        {
            if (_operation == value)
                return;

            _operation = value;
            OnSetOperationChanged(new SetOperationChangedEventArgs(value));
        }
    }

    /// <summary>The symbol shown on the filter bar chip for this member.</summary>
    public string OperationSymbol => Operation switch
    {
        SetOperation.Union => "+",
        SetOperation.Intersect => "x",
        SetOperation.Exclude => "-",
        _ => string.Empty
    };

    /// <summary>
    /// Advances to the next operation. The filter bar binds this to a click on the
    /// chip, so a user cycles Union -> Intersect -> Exclude -> Union in place rather
    /// than removing the tag and re-adding it with a different operation.
    /// </summary>
    public void CycleNextSetOperation() => Operation = Operation switch
    {
        SetOperation.Union => SetOperation.Intersect,
        SetOperation.Intersect => SetOperation.Exclude,
        SetOperation.Exclude => SetOperation.Union,
        _ => SetOperation.Union
    };

    private void OnSetOperationChanged(SetOperationChangedEventArgs e)
        => SetOperationChanged?.Invoke(this, e);

    public override string ToString()
        => $"<TagFilterMember> - ({TagId}) {Operation} [{TagGroupName}:{Name}]";
}
