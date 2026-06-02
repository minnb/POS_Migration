using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Attribute
{
    public class Base64ValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null || !(value is string))
            {
                return new ValidationResult("Chuỗi Base64 không hợp lệ");
            }

            string base64 = value as string;

            if (IsBase64String(base64))
            {
                return ValidationResult.Success;
            }
            else
            {
                return new ValidationResult("Chuỗi Base64 không hợp lệ");
            }
        }

        private bool IsBase64String(string base64)
        {
            if (string.IsNullOrEmpty(base64))
            {
                return false;
            }

            base64 = base64.Trim();

            if (base64.Length % 4 != 0)
            {
                return false;
            }

            try
            {
                Convert.FromBase64String(base64);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
