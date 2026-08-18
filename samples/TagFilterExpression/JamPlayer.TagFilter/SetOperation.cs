namespace JamPlayer.TagFilter;

/// <summary>
/// How a single tag contributes to the filter expression as a whole.
/// </summary>
public enum SetOperation
{
    /// <summary>Assets carrying this tag are added to the result.</summary>
    Union,

    /// <summary>The result is narrowed to assets that also carry this tag.</summary>
    Intersect,

    /// <summary>Assets carrying this tag are removed from the result.</summary>
    Exclude
}
