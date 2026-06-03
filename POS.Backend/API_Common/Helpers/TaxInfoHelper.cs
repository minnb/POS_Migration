using System.Text.RegularExpressions;
using TCX.API.Common.Dtos.Tax;

namespace TCX.API.Common.Helpers
{
    public static class TaxInfoHelper
    {
        public static string SanitizeTaxCode(string taxCode)
        {
            if (string.IsNullOrEmpty(taxCode))
                return taxCode;
            // Chỉ giữ lại chữ số và chữ cái (A-Z, a-z, 0-9)
            return Regex.Replace(taxCode, @"[^A-Za-z0-9\-]", "");
        }
        public static string SanitizeEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return email;

            // Loại bỏ khoảng trắng và các ký tự đặc biệt không hợp lệ trong email
            email = email.Trim();
            // Chỉ giữ lại các ký tự hợp lệ cho email: A-Z, a-z, 0-9, @, ., _, -, +
            return Regex.Replace(email, @"[^A-Za-z0-9@._\-+]", "");
        }
        public static string SanitizeName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            // Trim khoảng trắng đầu cuối
            name = name.Trim();

            // Loại bỏ ký tự điều khiển (control characters)
            name = Regex.Replace(name, @"[\p{C}]", "");

            // Loại bỏ emoji và symbols (các ký tự Unicode đặc biệt)
            name = Regex.Replace(name, @"[\p{So}\p{Sk}\p{Sm}\p{Sc}]", "");

            // Loại bỏ các ký tự cấm trong file system: / \ : * ? " < > |
            name = Regex.Replace(name, @"[/\\:*?""<>|]", "");

            // Loại bỏ ký tự & (thường gây lỗi XML/chữ ký số)
            name = name.Replace("&", "");

            // Loại bỏ các khoảng trắng dư thừa (nhiều khoảng trắng liên tiếp thành 1)
            name = Regex.Replace(name, @"\s+", " ");

            return name.Trim();
        }

        public static string SanitizeAddress(string address)
        {
            if (string.IsNullOrEmpty(address))
                return address;

            // Trim khoảng trắng đầu cuối
            address = address.Trim();

            // Loại bỏ ký tự điều khiển (control characters)
            address = Regex.Replace(address, @"[\p{C}]", "");

            // Loại bỏ emoji và symbols (các ký tự Unicode đặc biệt)
            address = Regex.Replace(address, @"[\p{So}\p{Sk}\p{Sm}\p{Sc}]", "");

            // Loại bỏ các ký tự cấm trong file system: / \ : * ? " < > |
            // Trừ dấu "/" vì có thể xuất hiện trong địa chỉ (VD: Số 10/2)
            address = Regex.Replace(address, @"[\\:*?""<>|]", "");

            // Loại bỏ ký tự & (thường gây lỗi XML/chữ ký số)
            address = address.Replace("&", "");

            // Loại bỏ các khoảng trắng dư thừa
            address = Regex.Replace(address, @"\s+", " ");

            return address.Trim();
        }
        public static string SanitizeIdDocument(string idDocument)
        {
            if (string.IsNullOrEmpty(idDocument))
                return idDocument;

            // Chỉ giữ lại chữ số và chữ cái (A-Z, a-z, 0-9)
            return Regex.Replace(idDocument, @"[^A-Za-z0-9]", "").ToUpper();
        }
        public static InvoiceCreated SanitizeInvoiceCreated(InvoiceCreated invoice)
        {
            if (invoice == null)
                return invoice;

            // Sanitize từng field
            invoice.CustomerName = SanitizeName(invoice.CustomerName);
            invoice.CompanyName = SanitizeName(invoice.CompanyName);
            invoice.Address = SanitizeAddress(invoice.Address);
            invoice.Email = SanitizeEmail(invoice.Email);
            invoice.TaxCode = SanitizeTaxCode(invoice.TaxCode);
            invoice.Passport = SanitizeIdDocument(invoice.Passport);
            invoice.CCCD = SanitizeIdDocument(invoice.CCCD);
            //invoice.DVQHNS = SanitizeIdDocument(invoice.DVQHNS);

            return invoice;
        }
    }
}
