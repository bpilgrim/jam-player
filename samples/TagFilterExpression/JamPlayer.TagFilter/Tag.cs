namespace JamPlayer.TagFilter;

/// <summary>
/// A tag and the set of assets carrying it.
/// </summary>
/// <remarks>
/// In the full application this is an EF Core entity with a many-to-many navigation
/// property to assets. The filter engine never needs the persistence concerns, only
/// the identity of the tag and the asset ids it owns, so this sample models it as a
/// plain immutable value. That reduction is the point: the set algebra below has no
/// dependency on the database, the ORM, or the UI.
/// </remarks>
public sealed class Tag
{
    public Tag(int id, string name, string groupName, IEnumerable<int> assetIds)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(assetIds);

        Id = id;
        Name = name;
        GroupName = groupName ?? string.Empty;
        AssetIds = new HashSet<int>(assetIds);
    }

    public int Id { get; }

    public string Name { get; }

    /// <summary>The tag group this tag belongs to ("Country", "Make", "Color", ...).</summary>
    public string GroupName { get; }

    /// <summary>Ids of every asset carrying this tag.</summary>
    public IReadOnlySet<int> AssetIds { get; }

    public override string ToString() => $"[{GroupName}:{Name}] ({Id})";
}
