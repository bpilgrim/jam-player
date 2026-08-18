using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JamPlayer.TagFilter.Tests;

[TestClass]
public class TagFilterEvaluatorTests
{
    private TagLibraryFixture _lib = null!;

    [TestInitialize]
    public void Setup() => _lib = new TagLibraryFixture();

    [TestMethod]
    public void EmptyExpression_ReturnsWholeLibrary()
    {
        var expression = new TagFilterExpression();

        CollectionAssert.AreEqual(
            TagLibraryFixture.AllAssetIds,
            TagLibraryFixture.Evaluate(expression));
    }

    [TestMethod]
    public void SingleUnion_ReturnsOnlyAssetsCarryingThatTag()
    {
        TagFilterExpression expression = TagLibraryFixture.ExpressionOf(_lib.CountryGerman);

        CollectionAssert.AreEqual(
            new[] { TagLibraryFixture.BlueBmw, TagLibraryFixture.GreenBenz },
            TagLibraryFixture.Evaluate(expression));
    }

    [TestMethod]
    public void TwoUnions_ReturnAssetsCarryingEitherTag()
    {
        TagFilterExpression expression = TagLibraryFixture.ExpressionOf(_lib.MakeBmw, _lib.MakeMercedes);

        CollectionAssert.AreEqual(
            new[] { TagLibraryFixture.BlueBmw, TagLibraryFixture.GreenBenz },
            TagLibraryFixture.Evaluate(expression));
    }

    [TestMethod]
    public void UnionWithTagOwningNothing_ContributesNothing()
    {
        TagFilterExpression expression = TagLibraryFixture.ExpressionOf(_lib.MakeBmw, _lib.CountryBritish);

        CollectionAssert.AreEqual(
            new[] { TagLibraryFixture.BlueBmw },
            TagLibraryFixture.Evaluate(expression));
    }

    [TestMethod]
    public void Intersect_NarrowsTheUnionToAssetsCarryingBoth()
    {
        TagFilterExpression expression = TagLibraryFixture.ExpressionOf(_lib.ColorBlue, _lib.CountryGerman);

        // Both Union: blue (1,3) + German (1,4).
        CollectionAssert.AreEqual(
            new[] { TagLibraryFixture.BlueBmw, TagLibraryFixture.BlueFiat, TagLibraryFixture.GreenBenz },
            TagLibraryFixture.Evaluate(expression));

        // German flipped to Intersect: only the blue car that is also German.
        expression.Single(m => m.TagId == _lib.CountryGerman.Id).Operation = SetOperation.Intersect;

        CollectionAssert.AreEqual(
            new[] { TagLibraryFixture.BlueBmw },
            TagLibraryFixture.Evaluate(expression));
    }

    [TestMethod]
    public void Exclude_RemovesAssetsCarryingThatTag()
    {
        TagFilterExpression expression = TagLibraryFixture.ExpressionOf(_lib.CountryItalian, _lib.ColorGreen);

        expression.Single(m => m.TagId == _lib.ColorGreen.Id).Operation = SetOperation.Exclude;

        // Italian (2,3) minus green (2,4).
        CollectionAssert.AreEqual(
            new[] { TagLibraryFixture.BlueFiat },
            TagLibraryFixture.Evaluate(expression));
    }

    [TestMethod]
    public void ExcludeWithNoUnion_StartsFromWholeLibrary()
    {
        // "everything except the blue ones" — the user never selected a positive tag.
        var expression = new TagFilterExpression();
        expression.Add(new TagFilterMember(_lib.ColorBlue, SetOperation.Exclude));

        CollectionAssert.AreEqual(
            new[] { TagLibraryFixture.GreenFiat, TagLibraryFixture.GreenBenz },
            TagLibraryFixture.Evaluate(expression));
    }

    [TestMethod]
    public void IntersectWithNoUnion_StartsFromWholeLibrary()
    {
        var expression = new TagFilterExpression();
        expression.Add(new TagFilterMember(_lib.MakeFiat, SetOperation.Intersect));

        CollectionAssert.AreEqual(
            new[] { TagLibraryFixture.GreenFiat, TagLibraryFixture.BlueFiat },
            TagLibraryFixture.Evaluate(expression));
    }

    [TestMethod]
    public void ExcludeIsAppliedAfterUnion_RegardlessOfInsertionOrder()
    {
        // Exclude added first, union second. Phase ordering must not depend on this.
        var excludeFirst = new TagFilterExpression();
        excludeFirst.Add(new TagFilterMember(_lib.ColorBlue, SetOperation.Exclude));
        excludeFirst.Add(new TagFilterMember(_lib.CountryItalian));

        var unionFirst = new TagFilterExpression();
        unionFirst.Add(new TagFilterMember(_lib.CountryItalian));
        unionFirst.Add(new TagFilterMember(_lib.ColorBlue, SetOperation.Exclude));

        CollectionAssert.AreEqual(
            TagLibraryFixture.Evaluate(unionFirst),
            TagLibraryFixture.Evaluate(excludeFirst));

        CollectionAssert.AreEqual(
            new[] { TagLibraryFixture.GreenFiat },
            TagLibraryFixture.Evaluate(excludeFirst));
    }

    [TestMethod]
    public void ExcludeBeatsUnion_WhenATagIsBothUnionedAndExcluded()
    {
        // Union: Italian (2,3). Exclude: Fiat (2,3). Exclude runs last, so nothing survives.
        var expression = new TagFilterExpression();
        expression.Add(new TagFilterMember(_lib.CountryItalian));
        expression.Add(new TagFilterMember(_lib.MakeFiat, SetOperation.Exclude));

        Assert.AreEqual(0, TagLibraryFixture.Evaluate(expression).Length);
    }

    [TestMethod]
    public void Apply_InstallsResultForAssetPassesFilter()
    {
        TagFilterExpression expression = TagLibraryFixture.ExpressionOf(_lib.ColorBlue);

        TagFilterEvaluator.Apply(expression, TagLibraryFixture.AllAssetIds);

        Assert.IsTrue(expression.AssetPassesFilter(TagLibraryFixture.BlueBmw));
        Assert.IsTrue(expression.AssetPassesFilter(TagLibraryFixture.BlueFiat));
        Assert.IsFalse(expression.AssetPassesFilter(TagLibraryFixture.GreenFiat));
    }

    [TestMethod]
    public void AssetPassesFilter_IsFalseBeforeAnyEvaluation()
    {
        TagFilterExpression expression = TagLibraryFixture.ExpressionOf(_lib.ColorBlue);

        Assert.IsFalse(expression.AssetPassesFilter(TagLibraryFixture.BlueBmw));
    }
}
