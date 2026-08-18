# Tag filter expressions

This is a readable slice of jam-player's real filtering engine — the part that turns a
row of tag chips into the set of assets the browse grid shows.

It is extracted from the shipping application and decoupled from it. The production
code is a WPF/Prism module backed by EF Core; everything here is plain .NET with no
UI, no ORM, and no container, so the logic can be read and tested on its own.

```
JamPlayer.TagFilter/
  SetOperation.cs        Union | Intersect | Exclude
  Tag.cs                 a tag and the asset ids carrying it
  TagFilterMember.cs     one tag in an expression, plus its operation
  TagFilterExpression.cs the observable collection the filter bar binds to
  TagFilterEvaluator.cs  the set algebra

JamPlayer.TagFilter.Tests/   25 tests
```

## The idea

A filter is a row of chips. Each chip is a tag plus one of three operations, and
clicking a chip cycles it:

| Symbol | Operation | Effect |
| --- | --- | --- |
| `+` | Union | assets with this tag are added to the result |
| `x` | Intersect | the result is narrowed to assets that also have this tag |
| `-` | Exclude | assets with this tag are removed from the result |

So `+Italian +German xBlue -Fiat` reads as "Italian or German cars, but only the blue
ones, and no Fiats."

## Two decisions worth explaining

**Evaluation is phased, not parsed.** There is no expression tree and no operator
precedence. Unions run first and establish the candidate set, then intersects narrow
it, then excludes carve out of it — regardless of the order the user added the chips.
A parse tree would be more expressive, but it would make the result depend on click
order, and a filter bar has nowhere sensible to show parentheses. Fixed phases mean
the same chips always produce the same set.

**An expression with no union starts from the whole library.** Otherwise `-Blue` would
begin from the empty set and match nothing, when the user plainly means "everything
except the blue ones." Only a positive selection can narrow the library, so in its
absence the library itself is the starting set.

## Performance note

`AssetPassesFilter` is called once per asset per repaint, on libraries in the tens of
thousands. It is a single `HashSet` lookup against a precomputed result — the set
algebra runs once per filter change, not once per asset. Likewise, a member snapshots
its tag's asset ids when it enters the expression, so filtering never touches the
database.

## Running it

```bash
dotnet test
```

25 tests, no external dependencies beyond MSTest.
