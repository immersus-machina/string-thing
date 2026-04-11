using Xunit;

namespace StringThing.Core.Tests;

public class NamedParameterNamerTests
{
    [Fact]
    public void WhenSimpleVariable_UsesVariableName()
    {
        // Act
        var result = NamedParameterNamer.WritePlaceholder(0, "userId");

        // Assert
        Assert.Equal("@userId", result);
    }

    [Fact]
    public void WhenMemberAccess_ReplacesDotsWithUnderscores()
    {
        // Act
        var result = NamedParameterNamer.WritePlaceholder(0, "user.Id");

        // Assert
        Assert.Equal("@user_Id", result);
    }

    [Fact]
    public void WhenNestedMemberAccess_ReplacesAllDots()
    {
        // Act
        var result = NamedParameterNamer.WritePlaceholder(0, "order.Customer.Name");

        // Assert
        Assert.Equal("@order_Customer_Name", result);
    }

    [Fact]
    public void WhenArrayIndex_ReplacesWithDoubleUnderscore()
    {
        // Act
        var result = NamedParameterNamer.WritePlaceholder(0, "values[0]");

        // Assert
        Assert.Equal("@values__0", result);
    }

    [Fact]
    public void WhenDictionaryStringKey_FallsBackToIndexed()
    {
        // Act
        var result = NamedParameterNamer.WritePlaceholder(5, "dict[\"key\"]");

        // Assert
        Assert.Equal("@p5", result);
    }

    [Fact]
    public void WhenDictionaryAccessThenMember_FallsBackToIndexed()
    {
        // Act
        var result = NamedParameterNamer.WritePlaceholder(3, "dict[\"some\"].other");

        // Assert
        Assert.Equal("@p3", result);
    }

    [Fact]
    public void WhenNullExpression_FallsBackToIndexed()
    {
        // Act
        var result = NamedParameterNamer.WritePlaceholder(3, null);

        // Assert
        Assert.Equal("@p3", result);
    }

    [Fact]
    public void WhenEmptyExpression_FallsBackToIndexed()
    {
        // Act
        var result = NamedParameterNamer.WritePlaceholder(0, "");

        // Assert
        Assert.Equal("@p0", result);
    }

    [Fact]
    public void WhenFunctionCall_FallsBackToIndexed()
    {
        // Act
        var result = NamedParameterNamer.WritePlaceholder(0, "GetValue()");

        // Assert
        Assert.Equal("@p0", result);
    }

    [Fact]
    public void WhenInlineLiteral_FallsBackToIndexed()
    {
        // Act
        var result = NamedParameterNamer.WritePlaceholder(0, "42");

        // Assert
        Assert.Equal("@p0", result);
    }

    [Fact]
    public void WhenUnicodeVariable_FallsBackToIndexed()
    {
        // Act
        var result = NamedParameterNamer.WritePlaceholder(0, "café");

        // Assert
        Assert.Equal("@p0", result);
    }

    [Fact]
    public void WhenUnderscoreInName_FallsBackToIndexed()
    {
        // Act
        var result = NamedParameterNamer.WritePlaceholder(0, "user_id");

        // Assert
        Assert.Equal("@p0", result);
    }

    [Fact]
    public void WhenCollidesWithIndexedPattern_FallsBackToIndexed()
    {
        // Act
        var result = NamedParameterNamer.WritePlaceholder(0, "p3");

        // Assert
        Assert.Equal("@p0", result);
    }

    [Fact]
    public void WhenNameStartsWithPButNotIndexPattern_UsesName()
    {
        // Act
        var result = NamedParameterNamer.WritePlaceholder(0, "page");

        // Assert
        Assert.Equal("@page", result);
    }
}
