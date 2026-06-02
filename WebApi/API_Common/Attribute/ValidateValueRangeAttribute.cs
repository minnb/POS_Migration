using System.ComponentModel.DataAnnotations;
using System.Linq;
using System;

public class ValidateValueRangeAttribute : ValidationAttribute
{
    private readonly int[] _allowedValues;

    public ValidateValueRangeAttribute(params int[] allowedValues)
    {
        _allowedValues = allowedValues;
    }

    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {

        if (value == null)
            return new ValidationResult("Value is required");

        if (int.TryParse(value.ToString(), out int numericValue))
        {
            if (!_allowedValues.Contains(numericValue))
                return new ValidationResult($"Value must be one of: {string.Join(", ", _allowedValues)}");

            return ValidationResult.Success;
        }

        return new ValidationResult("Invalid value format");
    }
}