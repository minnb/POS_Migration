using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Shared
{
    public static class SqlErrorHandler
    {
        public static class SqlErrorCodes
        {
            public const int CheckConstraint = 547;          // Violation of CHECK constraint
            public const int UniqueConstraint = 2627;        // Violation of UNIQUE KEY constraint
            public const int DuplicateKey = 2601;           // Cannot insert duplicate key row
            public const int CannotInsertNull = 515;        // Cannot insert NULL value
            public const int StringTruncation = 8152;       // String data would be truncated
            public const int ArithmeticOverflow = 8115;     // Arithmetic overflow error
        }
        public static string HandleSqlException(SqlException ex, string methodName)
        {
            switch (ex.Number)
            {
                case SqlErrorCodes.CheckConstraint:
                    return "Data validation failed. Please check your input values.";

                case SqlErrorCodes.UniqueConstraint:
                    return "Record already exists with this key.";

                case SqlErrorCodes.DuplicateKey:
                    return "Duplicate record found.";

                case SqlErrorCodes.CannotInsertNull:
                    return "Required field cannot be empty.";

                case SqlErrorCodes.StringTruncation:
                    return "Data too long for one or more fields.";

                case SqlErrorCodes.ArithmeticOverflow:
                    return "Number too large for the field.";

                default:
                    return "An unexpected database error occurred.";
            }
        }
    }
}
