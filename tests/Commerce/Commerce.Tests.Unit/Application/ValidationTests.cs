using Commerce.Framework.Application.Validation;
using Xunit;

namespace Commerce.Tests.Unit.Application;

public sealed class ValidationTests
{
    [Fact]
    public void Success_HasNoErrors()
    {
        var result = ValidationResult.Success();

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Failure_ContainsErrors()
    {
        var result = ValidationResult.Failure(
            new ValidationError("Name", "Name is required."));

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("Name", result.Errors[0].PropertyName);
    }

    [Fact]
    public void Validator_ReturnsExpectedResult()
    {
        IValidator<string> validator = new RequiredStringValidator();
        var result = validator.Validate(" ");

        Assert.False(result.IsValid);
    }

    private sealed class RequiredStringValidator : IValidator<string>
    {
        public ValidationResult Validate(string instance)
        {
            return string.IsNullOrWhiteSpace(instance)
                ? ValidationResult.Failure(new ValidationError("Value", "Value is required."))
                : ValidationResult.Success();
        }
    }
}
