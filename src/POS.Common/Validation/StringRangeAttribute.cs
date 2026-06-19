using System.ComponentModel.DataAnnotations;

namespace POS.Common.Validation;

public sealed class StringRangeAttribute(params string[] allowableValues) : ValidationAttribute
{
    public string[] AllowableValues { get; } = allowableValues;

    protected override ValidationResult? IsValid(object? value, ValidationContext ctx)
    {
        if (value is string str && AllowableValues.Contains(str))
            return ValidationResult.Success;

        return new ValidationResult(ErrorMessage ?? $"Giá trị phải là một trong: {string.Join(", ", AllowableValues)}");
    }
}
