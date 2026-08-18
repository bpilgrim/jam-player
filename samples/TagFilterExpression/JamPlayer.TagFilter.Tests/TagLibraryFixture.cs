namespace JamPlayer.TagFilter.Tests;

/// <summary>
/// A small in-memory library used across the tests: four cars tagged by country,
/// make, and colour. Small enough that the expected result of any filter can be
/// checked by eye, which is what makes the set-algebra assertions readable.
/// </summary>
/// <remarks>
/// <code>
///   id  asset        country  make      colour
///   1   blueBmw      German   BMW       Blue
///   2   greenFiat    Italian  Fiat      Green
///   3   blueFiat     Italian  Fiat      Blue
///   4   greenBenz    German   Mercedes  Green
/// </code>
/// </remarks>
internal sealed class TagLibraryFixture
{
    public const int BlueBmw = 1;
    public const int GreenFiat = 2;
    public const int BlueFiat = 3;
    public const int GreenBenz = 4;

    public static readonly int[] AllAssetIds = { BlueBmw, GreenFiat, BlueFiat, GreenBenz };

    public Tag CountryGerman { get; } = new(10, "German", "Country", new[] { BlueBmw, GreenBenz });
    public Tag CountryItalian { get; } = new(11, "Italian", "Country", new[] { GreenFiat, BlueFiat });
    public Tag CountryBritish { get; } = new(12, "British", "Country", Array.Empty<int>());

    public Tag MakeBmw { get; } = new(20, "BMW", "Make", new[] { BlueBmw });
    public Tag MakeFiat { get; } = new(21, "Fiat", "Make", new[] { GreenFiat, BlueFiat });
    public Tag MakeMercedes { get; } = new(22, "Mercedes", "Make", new[] { GreenBenz });

    public Tag ColorBlue { get; } = new(30, "Blue", "Color", new[] { BlueBmw, BlueFiat });
    public Tag ColorGreen { get; } = new(31, "Green", "Color", new[] { GreenFiat, GreenBenz });

    /// <summary>Builds an expression from tags, all as Union members.</summary>
    public static TagFilterExpression ExpressionOf(params Tag[] tags)
    {
        var expression = new TagFilterExpression();
        foreach (Tag tag in tags)
            expression.Add(new TagFilterMember(tag));

        return expression;
    }

    /// <summary>Evaluates against the fixture library and returns the surviving ids, sorted.</summary>
    public static int[] Evaluate(TagFilterExpression expression)
        => TagFilterEvaluator.Evaluate(expression, AllAssetIds).OrderBy(id => id).ToArray();
}
