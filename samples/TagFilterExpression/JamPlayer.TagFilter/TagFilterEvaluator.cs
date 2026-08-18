namespace JamPlayer.TagFilter;

/// <summary>
/// Reduces a <see cref="TagFilterExpression"/> to the set of asset ids that survive it.
/// </summary>
/// <remarks>
/// <para>
/// The expression is not a parse tree and deliberately has no precedence rules. Users
/// build filters by clicking tags, and a click has to produce an obvious result. So
/// evaluation is a fixed three-phase pass over the members, independent of the order
/// they were added:
/// </para>
/// <list type="number">
///   <item><description>Union members establish the candidate set.</description></item>
///   <item><description>Intersect members narrow it.</description></item>
///   <item><description>Exclude members carve out of it.</description></item>
/// </list>
/// <para>
/// The phases are ordered so that every filter is meaningful. Running excludes before
/// unions would let a later union re-admit assets the user had just excluded, and
/// running intersects first would make the result depend on click order.
/// </para>
/// </remarks>
public static class TagFilterEvaluator
{
    /// <summary>
    /// Evaluates <paramref name="expression"/> against the library.
    /// </summary>
    /// <param name="expression">The filter to evaluate.</param>
    /// <param name="allAssetIds">
    /// Every asset id in the library. Needed because an expression with no union
    /// member starts from the whole library rather than from nothing (see remarks).
    /// </param>
    /// <returns>The ids of assets passing the filter.</returns>
    public static HashSet<int> Evaluate(TagFilterExpression expression, IEnumerable<int> allAssetIds)
    {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(allAssetIds);

        // With no union member there is no positive selection to start from. Starting
        // from the empty set would make a filter of "Exclude: Blue" show nothing,
        // when the user plainly means "everything except the blue ones". So an
        // expression without unions starts from the whole library.
        List<TagFilterMember> unionMembers = expression
            .Where(m => m.Operation == SetOperation.Union)
            .ToList();

        HashSet<int> passing;
        if (unionMembers.Count == 0)
        {
            passing = new HashSet<int>(allAssetIds);
        }
        else
        {
            passing = new HashSet<int>();
            foreach (TagFilterMember member in unionMembers)
                passing.UnionWith(member.AssetIdsOwned);
        }

        foreach (TagFilterMember member in expression.Where(m => m.Operation == SetOperation.Intersect))
            passing.IntersectWith(member.AssetIdsOwned);

        foreach (TagFilterMember member in expression.Where(m => m.Operation == SetOperation.Exclude))
            passing.ExceptWith(member.AssetIdsOwned);

        return passing;
    }

    /// <summary>
    /// Evaluates the expression and installs the result on it, so subsequent
    /// <see cref="TagFilterExpression.AssetPassesFilter"/> calls are single hash lookups.
    /// </summary>
    public static void Apply(TagFilterExpression expression, IEnumerable<int> allAssetIds)
        => expression.SetAllowedAssetIdList(Evaluate(expression, allAssetIds));
}
