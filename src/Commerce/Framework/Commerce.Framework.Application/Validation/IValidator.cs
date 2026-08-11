namespace Commerce.Framework.Application.Validation;

public interface IValidator<in T>
{
    ValidationResult Validate(T instance);
}
