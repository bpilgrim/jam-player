using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace JamPlayer.TagFilter;

/// <summary>
/// An ordered set of tags, each carrying a set operation, that together decide which
/// assets the browse grid shows.
/// </summary>
/// <remarks>
/// <para>
/// The expression is an <see cref="ObservableCollection{T}"/> because the filter bar
/// binds to it directly. Adding a chip, removing one, or cycling a chip's operation
/// all have to repaint the grid, and the collection is the single thing every
/// interested view is already watching.
/// </para>
/// <para>
/// A member's operation can change without the collection itself changing, so the
/// expression subscribes to each member's <see cref="TagFilterMember.SetOperationChanged"/>
/// and re-raises it as a Reset. Views that only listen to the collection stay correct
/// without knowing that members are individually mutable.
/// </para>
/// </remarks>
public class TagFilterExpression : ObservableCollection<TagFilterMember>
{
    private HashSet<int> _allowedAssetIds = new();
    private HashSet<int>? _aggregateAssetIdsOwned;

    public TagFilterExpression()
    {
        CollectionChanged += OnCollectionChangedInternal;
    }

    /// <summary>
    /// The union of every member's owned assets, computed lazily and cached until the
    /// expression changes.
    /// </summary>
    public IReadOnlySet<int> AggregateAssetIdsOwned
    {
        get
        {
            if (_aggregateAssetIdsOwned is null)
            {
                _aggregateAssetIdsOwned = new HashSet<int>();
                foreach (TagFilterMember member in this)
                    _aggregateAssetIdsOwned.UnionWith(member.AssetIdsOwned);
            }

            return _aggregateAssetIdsOwned;
        }
    }

    /// <summary>
    /// Returns true when the asset survives the current filter.
    /// </summary>
    /// <remarks>
    /// This is called once per asset per repaint on collections in the tens of
    /// thousands, so it is a single hash lookup against a precomputed set. The set
    /// algebra runs once, in <see cref="TagFilterEvaluator"/>, not per asset.
    /// </remarks>
    public bool AssetPassesFilter(int assetId) => _allowedAssetIds.Contains(assetId);

    /// <summary>
    /// Installs the result of an evaluation pass. Called by the view model after
    /// <see cref="TagFilterEvaluator.Evaluate"/>.
    /// </summary>
    public void SetAllowedAssetIdList(HashSet<int> allowedAssetIds)
    {
        ArgumentNullException.ThrowIfNull(allowedAssetIds);
        _allowedAssetIds = allowedAssetIds;
    }

    /// <summary>
    /// Adds a tag to the expression, ignoring tags already present.
    /// </summary>
    /// <remarks>
    /// A tag appearing twice would be meaningless (union with itself) or contradictory
    /// (union and exclude at once), and the filter bar can raise a duplicate add from
    /// several routes: the tag picker, a keyboard shortcut, and a restored session.
    /// De-duplicating in <see cref="InsertItem"/> rather than in a shadowing
    /// <c>Add</c> keeps the guarantee on every path into the collection, including
    /// <see cref="Collection{T}.Insert"/> and calls through the base type.
    /// </remarks>
    protected override void InsertItem(int index, TagFilterMember item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (this.Any(existing => existing.TagId == item.TagId))
            return;

        base.InsertItem(index, item);
    }

    /// <summary>
    /// Detaches every member before clearing.
    /// </summary>
    /// <remarks>
    /// A Reset notification carries neither OldItems nor NewItems, so the handler
    /// below cannot unsubscribe on a clear. Without this override the cleared members
    /// would keep the expression alive through their event handlers and continue
    /// raising Reset at it.
    /// </remarks>
    protected override void ClearItems()
    {
        foreach (TagFilterMember member in this)
            member.SetOperationChanged -= OnMemberSetOperationChanged;

        base.ClearItems();
    }

    /// <summary>
    /// Removes the member holding the given tag, matching on tag id rather than on
    /// object identity so callers can remove by a freshly constructed member.
    /// </summary>
    public bool RemoveByTagId(int tagId)
    {
        TagFilterMember? member = this.FirstOrDefault(m => m.TagId == tagId);
        return member is not null && Remove(member);
    }

    private void OnCollectionChangedInternal(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _aggregateAssetIdsOwned = null;

        if (e.OldItems is not null)
        {
            foreach (TagFilterMember member in e.OldItems.OfType<TagFilterMember>())
                member.SetOperationChanged -= OnMemberSetOperationChanged;
        }

        if (e.NewItems is not null)
        {
            foreach (TagFilterMember member in e.NewItems.OfType<TagFilterMember>())
                member.SetOperationChanged += OnMemberSetOperationChanged;
        }
    }

    private void OnMemberSetOperationChanged(object? sender, SetOperationChangedEventArgs e)
        => OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
}
