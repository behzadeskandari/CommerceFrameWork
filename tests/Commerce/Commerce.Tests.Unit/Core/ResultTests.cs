using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Xunit;

namespace Commerce.Tests.Unit.Core;

public sealed class ResultTests
{
    [Fact]
    public void Success_Result_HasNoError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_Result_ContainsError()
    {
        var error = Error.Validation("Invalid input");

        var result = Result.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void GenericSuccess_ReturnsValue()
    {
        var result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void GenericFailure_ContainsErrorMetadata()
    {
        var metadata = new Dictionary<string, object?> { ["field"] = "name" };
        var error = Error.Validation("Invalid", metadata: metadata);

        var result = Result.Failure<string>(error);

        Assert.True(result.IsFailure);
        Assert.Equal("name", error.Metadata["field"]);
    }

    [Fact]
    public void Map_PreservesFailure()
    {
        var error = Error.NotFound("Missing");
        var result = Result.Failure<int>(error);

        var mapped = result.Map(x => x.ToString());

        Assert.True(mapped.IsFailure);
        Assert.Equal(error, mapped.Error);
    }
}
