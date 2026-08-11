namespace Commerce.Framework.Application.Validation;

public sealed record ValidationError(string PropertyName, string Message);
