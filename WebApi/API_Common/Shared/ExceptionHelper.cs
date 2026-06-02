using System;
using System.Data.SqlClient;

namespace TCX.API.Common.Shared
{
    public static class ExceptionHelper
    {
        public static string GetDuplicateKeySqlException(SqlException ex)
        {
            if (ex.Number == 2627) // Error số 2627: Vi phạm PRIMARY KEY
            {
                string duplicateKey = ExtractDuplicateKeyValue(ex.Message);
                if (!string.IsNullOrEmpty(duplicateKey))
                {
                    return duplicateKey;
                }
            }
            return null;
        }

        public static string ExtractDuplicateKeyValue(string message)
        {
            const string keyStart = "The duplicate key value is (";
            const string keyEnd = ").";

            int startIndex = message.IndexOf(keyStart);
            if (startIndex != -1)
            {
                startIndex += keyStart.Length;
                int endIndex = message.IndexOf(keyEnd, startIndex);
                if (endIndex != -1)
                {
                    return message.Substring(startIndex, endIndex - startIndex);
                }
            }

            return "Unknown";
        }
        public static Tuple<bool, string> CheckException(Exception ex, string function)
        {
            if (!string.IsNullOrEmpty(ex.Message))
            {
                if(ex.Message.Contains("Violation of PRIMARY KEY constraint"))
                {
                    return new Tuple<bool, string>(true, function + @" - Violation of PRIMARY KEY constraint");
                }
                else if(ex.Message.Contains("Object reference not set to an instance of an object"))
                {
                    return new Tuple<bool, string>(false, @"Lỗi chuyển đổ dữ liệu");
                }
                return new Tuple<bool, string>(false, ex.Message);
            }

            return new Tuple<bool, string>(false, function + @" - lỗi Exception");
        }
        public static string GetExceptionMessage(Exception ex)
        {
            try
            {
                if (ex == null) return @"Exception";
                if (ex.InnerException != null)
                {
                    if (ex.InnerException.InnerException != null)
                    {
                        return ex.InnerException.InnerException.Message ?? ex.Message.ToString();
                    }

                    return ex.InnerException.Message;
                }
                return ex.Message.ToString();
            }
            catch (Exception e)
            {
                return e.Message.ToString();
            }
        }

        public static object GetObjectException(Exception ex)
        {
            if(ex == null) return null;
            if(ex.InnerException != null) 
            { 
                return ex.InnerException;
            }
            else
            {
                return ex.Message;
            }
        }
    }
}
