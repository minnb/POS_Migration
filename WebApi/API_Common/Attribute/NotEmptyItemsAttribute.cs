using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Attribute
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class NotEmptyItemsAttribute : ValidationAttribute
    {
        public NotEmptyItemsAttribute()
        {
            ErrorMessage = "Danh sách không được chứa phần tử rỗng hoặc chỉ có khoảng trắng.";
        }
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is IEnumerable<string> list)
            {
                if (list.Any(s => string.IsNullOrWhiteSpace(s)))
                {
                    return new ValidationResult(ErrorMessage);
                }
            }

            return ValidationResult.Success;
        }
    }
}
