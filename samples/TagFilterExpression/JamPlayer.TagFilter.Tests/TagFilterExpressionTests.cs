using System.Collections.Specialized;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JamPlayer.TagFilter.Tests;

[TestClass]
public class TagFilterExpressionTests
{
    private TagLibraryFixture _lib = null!;

    [TestInitialize]
    public void Setup() => _lib = new TagLibraryFixture();

    [TestMethod]
    public void AddingSameTagTwice_IsIgnored()
    {
        var expression = new TagFilterExpression();

        expression.Add(new TagFilterMember(_lib.ColorBlue));
        expression.Add(new TagFilterMember(_lib.ColorBlue));

        Assert.AreEqual(1, expression.Count);
    }

    [TestMethod]
    public void InsertingSameTagTwice_IsAlsoIgnored()
    {
        // Guards the de-dup on the Insert path, not just Add.
        var expression = new TagFilterExpression();

        expression.Add(new TagFilterMember(_lib.ColorBlue));
        expression.Insert(0, new TagFilterMember(_lib.ColorBlue));

        Assert.AreEqual(1, expression.Count);
    }

    [TestMethod]
    public void RemoveByTagId_RemovesTheMatchingMember()
    {
        TagFilterExpression expression = TagLibraryFixture.ExpressionOf(_lib.ColorBlue, _lib.MakeFiat);

        Assert.IsTrue(expression.RemoveByTagId(_lib.ColorBlue.Id));

        Assert.AreEqual(1, expression.Count);
        Assert.AreEqual(_lib.MakeFiat.Id, expression.Single().TagId);
    }

    [TestMethod]
    public void RemoveByTagId_ReturnsFalseWhenTagNotPresent()
    {
        TagFilterExpression expression = TagLibraryFixture.ExpressionOf(_lib.ColorBlue);

        Assert.IsFalse(expression.RemoveByTagId(_lib.MakeFiat.Id));
        Assert.AreEqual(1, expression.Count);
    }

    [TestMethod]
    public void ChangingAMembersOperation_RaisesCollectionChanged()
    {
        // The filter bar repaints off CollectionChanged, so an in-place operation
        // change has to surface there even though membership did not change.
        TagFilterExpression expression = TagLibraryFixture.ExpressionOf(_lib.ColorBlue);

        NotifyCollectionChangedAction? observed = null;
        expression.CollectionChanged += (_, e) => observed = e.Action;

        expression.Single().Operation = SetOperation.Exclude;

        Assert.AreEqual(NotifyCollectionChangedAction.Reset, observed);
    }

    [TestMethod]
    public void SettingOperationToItsCurrentValue_RaisesNothing()
    {
        TagFilterExpression expression = TagLibraryFixture.ExpressionOf(_lib.ColorBlue);

        var raised = 0;
        expression.CollectionChanged += (_, _) => raised++;

        expression.Single().Operation = SetOperation.Union; // already Union

        Assert.AreEqual(0, raised);
    }

    [TestMethod]
    public void RemovedMember_NoLongerNotifiesTheExpression()
    {
        TagFilterExpression expression = TagLibraryFixture.ExpressionOf(_lib.ColorBlue);
        TagFilterMember member = expression.Single();

        expression.Remove(member);

        var raised = 0;
        expression.CollectionChanged += (_, _) => raised++;
        member.Operation = SetOperation.Exclude;

        Assert.AreEqual(0, raised);
    }

    [TestMethod]
    public void ClearedMembers_NoLongerNotifyTheExpression()
    {
        TagFilterExpression expression = TagLibraryFixture.ExpressionOf(_lib.ColorBlue, _lib.MakeFiat);
        TagFilterMember member = expression.First();

        expression.Clear();

        var raised = 0;
        expression.CollectionChanged += (_, _) => raised++;
        member.Operation = SetOperation.Exclude;

        Assert.AreEqual(0, raised);
    }

    [TestMethod]
    public void AggregateAssetIdsOwned_IsTheUnionOfAllMembers()
    {
        TagFilterExpression expression = TagLibraryFixture.ExpressionOf(_lib.MakeBmw, _lib.MakeFiat);

        CollectionAssert.AreEqual(
            new[] { TagLibraryFixture.BlueBmw, TagLibraryFixture.GreenFiat, TagLibraryFixture.BlueFiat },
            expression.AggregateAssetIdsOwned.OrderBy(id => id).ToArray());
    }

    [TestMethod]
    public void AggregateAssetIdsOwned_IsRecomputedAfterMembershipChanges()
    {
        TagFilterExpression expression = TagLibraryFixture.ExpressionOf(_lib.MakeBmw);

        _ = expression.AggregateAssetIdsOwned; // prime the cache

        expression.Add(new TagFilterMember(_lib.MakeMercedes));

        CollectionAssert.AreEqual(
            new[] { TagLibraryFixture.BlueBmw, TagLibraryFixture.GreenBenz },
            expression.AggregateAssetIdsOwned.OrderBy(id => id).ToArray());
    }

    [TestMethod]
    public void CycleNextSetOperation_WalksUnionIntersectExcludeAndBack()
    {
        var member = new TagFilterMember(_lib.ColorBlue);
        Assert.AreEqual(SetOperation.Union, member.Operation);

        member.CycleNextSetOperation();
        Assert.AreEqual(SetOperation.Intersect, member.Operation);

        member.CycleNextSetOperation();
        Assert.AreEqual(SetOperation.Exclude, member.Operation);

        member.CycleNextSetOperation();
        Assert.AreEqual(SetOperation.Union, member.Operation);
    }

    [TestMethod]
    public void OperationSymbol_MatchesTheOperation()
    {
        var member = new TagFilterMember(_lib.ColorBlue);

        Assert.AreEqual("+", member.OperationSymbol);

        member.Operation = SetOperation.Intersect;
        Assert.AreEqual("x", member.OperationSymbol);

        member.Operation = SetOperation.Exclude;
        Assert.AreEqual("-", member.OperationSymbol);
    }

    [TestMethod]
    public void MemberSnapshotsAssetIds_SoLaterTagMutationDoesNotLeakIn()
    {
        var ids = new List<int> { TagLibraryFixture.BlueBmw };
        var tag = new Tag(99, "Temp", "Test", ids);
        var member = new TagFilterMember(tag);

        ids.Add(TagLibraryFixture.GreenFiat);

        Assert.AreEqual(1, member.AssetIdsOwned.Count);
    }
}
