using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Drawing.Imaging;
using System.Data;
using System.Reflection;



namespace VCM.BLUEPOS.Helpers
{
    public static class HelpersExtension
    {
        public static bool IsNull<T>(this T list)
        {
            if (list == null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static bool IsNumber(string pValue)
        {
            foreach (Char c in pValue)
            {
                if (!Char.IsDigit(c))
                    return false;
            }
            return true;
        }
        public static bool IsNumberAll(string pValue)
        {
            foreach (Char c in pValue)
            {
                if (!Char.IsDigit(c) && c != '.')
                    return false;
            }
            return true;
        }
        public static decimal ConvertStringBalanceToDecimal(string toroBalance, string cultureName = "en")
        {
            try
            {
                return decimal.Parse(toroBalance, System.Globalization.CultureInfo.GetCultureInfo(cultureName));
            }
            catch
            {
                return 0;
            }
        }
        public static double ConvertToDouble(string s)
        {
            char systemSeparator = Thread.CurrentThread.CurrentCulture.NumberFormat.CurrencyDecimalSeparator[0];
            double result = 0;
            try
            {
                if (s != null)
                    if (!s.Contains(","))
                        result = double.Parse(s, CultureInfo.InvariantCulture);
                    else
                        result = Convert.ToDouble(s.Replace(".", systemSeparator.ToString()).Replace(",", systemSeparator.ToString()));
            }
            catch (Exception e)
            {
                try
                {
                    result = Convert.ToDouble(s);
                }
                catch
                {
                    try
                    {
                        result = Convert.ToDouble(s.Replace(",", ";").Replace(".", ",").Replace(";", "."));
                    }
                    catch
                    {
                        throw new Exception("Lỗi sai định dạng chuỗi.");
                    }
                }
            }
            return result;
        }

        public static float ConvertToFloat(string s)
        {
            char systemSeparator = Thread.CurrentThread.CurrentCulture.NumberFormat.CurrencyDecimalSeparator[0];
            float result = 0;
            try
            {
                if (s != null)
                    if (!s.Contains(","))
                        result = float.Parse(s, CultureInfo.InvariantCulture);
                    else
                        result = Convert.ToInt64(s.Replace(".", systemSeparator.ToString()).Replace(",", systemSeparator.ToString()));
            }
            catch (Exception e)
            {
                try
                {
                    result = Convert.ToInt64(s);
                }
                catch
                {
                    try
                    {
                        result = Convert.ToInt64(s.Replace(",", ";").Replace(".", ",").Replace(";", "."));
                    }
                    catch
                    {
                        throw new Exception("Lỗi sai định dạng chuỗi.");
                    }
                }
            }
            return result;
        }

        public static CultureInfo culture = new CultureInfo("en-US");
        public static string VNCurrency(this decimal input)
        {
            var result = string.Format(culture, "{0:N0}", input);
            return result;
        }
        public static string VNCurrency(this decimal? input)
        {
            var result = string.Format(culture, "{0:N0}", input ?? 0);
            return result;
        }
        public static string VNCurrency(this double input)
        {
            var result = string.Format(culture, "{0:N0}", input);
            return result;
        }
        public static string VNCurrency(this double? input)
        {
            var result = string.Format(culture, "{0:N0}", input ?? 0);
            return result;
        }
        public static string VNCurrency(this int input)
        {
            var result = string.Format(culture, "{0:N0}", input);
            return result;
        }
        public static string VNCurrency(this long input)
        {
            var result = string.Format(culture, "{0:N0}", input);
            return result;
        }

        // 17/05/2023
        public static string GenerateFriendlyName(this string unicodeString)
        {
            try
            {
                //Remove VietNamese character                 
                unicodeString = unicodeString.ToLower();
                unicodeString = Regex.Replace(unicodeString, "[áàảãạâấầẩẫậăắằẳẵặ]", "a");
                unicodeString = Regex.Replace(unicodeString, "[éèẻẽẹêếềểễệ]", "e");
                unicodeString = Regex.Replace(unicodeString, "[iíìỉĩị]", "i");
                unicodeString = Regex.Replace(unicodeString, "[óòỏõọơớờởỡợôốồổỗộ]", "o");
                unicodeString = Regex.Replace(unicodeString, "[úùủũụưứừửữự]", "u");
                unicodeString = Regex.Replace(unicodeString, "[yýỳỷỹỵ]", "y");
                unicodeString = Regex.Replace(unicodeString, "[đ]", "d");

                //Remove space                 
                unicodeString = unicodeString.Replace(" ", "-");

                //Remove more
                unicodeString = unicodeString.Replace(":", "");
                unicodeString = unicodeString.Replace(",", "");

                //Remove special character                 
                //unicodeString = Regex.Replace(unicodeString, "[`~!@#$%^&*()-+=?/>.<,{}[]|]\\]", "");
                unicodeString = Regex.Replace(unicodeString, "[`~!@#$%^&*()-+=?/><{}[]|]\\,.]", "");

                return unicodeString;
            }
            catch (Exception)
            {
                return "";
            }
        }

        // --- 17/05/2023 ---
        public static Image ResizeImage(Image imgToResize, Size size)
        {
            //Get the image current width  
            int sourceWidth = imgToResize.Width;
            //Get the image current height  
            int sourceHeight = imgToResize.Height;
            float nPercent = 0;
            float nPercentW = 0;
            float nPercentH = 0;
            //Calulate  width with new desired size  
            nPercentW = ((float)size.Width / (float)sourceWidth);
            //Calculate height with new desired size  
            nPercentH = ((float)size.Height / (float)sourceHeight);
            if (nPercentH < nPercentW)
                nPercent = nPercentH;
            else
                nPercent = nPercentW;
            //New Width  
            int destWidth = (int)(sourceWidth * nPercent);
            //New Height  
            int destHeight = (int)(sourceHeight * nPercent);
            Bitmap b = new Bitmap(destWidth, destHeight);
            Graphics g = Graphics.FromImage((Image)b);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            // Draw image with new width and height  
            g.DrawImage(imgToResize, 0, 0, destWidth, destHeight);
            g.Dispose();
            return (Image)b;
        }
        public static Image ConvertBase64ToImage(string base64)
        {
            try
            {
                byte[] bytes = Convert.FromBase64String(base64);
                Image image;
                using (MemoryStream ms = new MemoryStream(bytes))
                {
                    image = Image.FromStream(ms);
                }
                return image;
            }
            catch (Exception ex)
            {
                //LogFile.Error(Enums.ObjectType.FileHelper, ex, ex.Message);
                return null;
            }
        }
        public static byte[] ImageToByteArray(Image image)
        {
            using (var ms = new MemoryStream())
            {
                image.Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }
        }
        public static int GetImageSize(string base64)
        {
            //var stringLength = base64.Length - "data:image/png;base64,".Length;
            var sizeInBytes = 4 * Math.Ceiling(((double)base64.Length / 3))*0.5624896334383812;
            return (int)(sizeInBytes / 1000);
        }

        // ---- tungnt8: 18/09/2023 ----
        public class ListtoDataTableConverter
        {
            public static DataTable ConvertToDataTable<T>(List<T> items)
            {
                DataTable dataTable = new DataTable(typeof(T).Name);
                //Get all the properties
                PropertyInfo[] Props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (PropertyInfo prop in Props)
                {
                    //Setting column names as Property names
                    dataTable.Columns.Add(prop.Name);
                }

                foreach (T item in items)
                {
                    var values = new object[Props.Length];
                    for (int i = 0; i < Props.Length; i++)
                    {
                        //inserting property values to datatable rows
                        values[i] = Props[i].GetValue(item, null);
                    }
                    dataTable.Rows.Add(values);
                }
                //put a breakpoint here and check datatable
                return dataTable;
            }
        }

        // ---- tungnt8: 18/09/2023 ----
        public static DataTable ToDataTable<T>(this IEnumerable<T> list)
        {
            Type elementType = typeof(T);
            using (DataTable t = new DataTable())
            {
                PropertyInfo[] _props = elementType.GetProperties();
                foreach (PropertyInfo propInfo in _props)
                {
                    Type _pi = propInfo.PropertyType;
                    Type ColType = Nullable.GetUnderlyingType(_pi) ?? _pi;
                    t.Columns.Add(propInfo.Name, ColType);
                }
                foreach (T item in list)
                {
                    DataRow row = t.NewRow();
                    foreach (PropertyInfo propInfo in _props)
                    {
                        row[propInfo.Name] = propInfo.GetValue(item, null) ?? DBNull.Value;
                    }
                    t.Rows.Add(row);
                }
                return t;
            }
        }

        // ----- tungnt8: 18/09/2023 ----
        public static DataSet ToDataSet<T>(this IEnumerable<T> list)
        {
            using (DataSet ds = new DataSet())
            {
                ds.Tables.Add(list.ToDataTable());
                return ds;
            }
        }


    }
}