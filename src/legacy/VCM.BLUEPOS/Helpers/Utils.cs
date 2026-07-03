using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Web;
using Zen.Barcode;

namespace VCM.BLUEPOS.Helpers
{
    public class Utils
    {
        public static string RightStr(string value, int length)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= length ? value : value.Substring(value.Length - length);
        }
        public static string LeftStr(string value, int length)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Substring(0, length);
        }
        public static bool CheckPort(string IPAddress, int port, ref string Result)
        {
            try
            {
                var client = new TcpClient();
                if (!client.ConnectAsync(IPAddress, port).Wait(5000))
                {
                    Result = client.Connected.ToString();
                    client.Dispose();
                    return false;
                }
                else
                {
                    Result = client.Connected.ToString();
                    client.Dispose();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Result = ex.Message;
                return false;
            }
        }

        public static DataTable RemoveColumn(DataTable table, string ColumnName)
        {
            try
            {
                foreach (DataColumn col in table.Columns)
                {
                    //check neu trong table duoi DB k ton tai column nay thi k add vao
                    if (col.ColumnName.ToUpper() == ColumnName.ToUpper())
                    {
                        table.Columns.Remove(col.ColumnName);
                        table.AcceptChanges();
                        return table;
                    }
                }
            }
            catch (Exception)
            {
                return table;
            }
            return table;
        }

        public static Tuple<bool, string> CheckTaxCode(string MST)
        {
            var taxCode = MST.Trim();
            var result = new Tuple<bool, string>(true, $"MST:{taxCode} hợp lệ");
            Regex regexMST = new Regex(@"^[0-9-]*$");
            var checkMST = regexMST.Match(taxCode);
            var indexTax = taxCode.IndexOf("-");
            if (indexTax < 10 && indexTax > -1)
            {
                result = new Tuple<bool, string>(false, $"MST {taxCode} không đúng định dạng. Vui lòng kiểm tra lại");
                return result;
            }

            if (!checkMST.Success)
            {
                result = new Tuple<bool, string>(false, $"MST {taxCode} chỉ bao gồm số và dấu (-). Vui lòng kiểm tra lại");
                return result;
            }

            var lengthVAT = taxCode.Length;
            if (lengthVAT < 10 || (lengthVAT > 10 && lengthVAT < 14) || lengthVAT > 14)
            {
                result = new Tuple<bool, string>(false, $"MST {taxCode} phải 10 ký tự và không vượt quá 14 ký tự. Vui lòng kiểm tra lại");
                return result;
            }

            if (lengthVAT == 14)
            {
                //var indexTax = taxCode.IndexOf("-");
                if (indexTax == -1)
                {
                    result = new Tuple<bool, string>(false, $"MST {taxCode}: 14 ký tự phải có dấu(-). Vui lòng kiểm tra lại");
                    return result;
                }
                if (indexTax != 10)
                {
                    result = new Tuple<bool, string>(false, $"MST {taxCode} không đúng định dạng. Vui lòng kiểm tra lại");
                    return result;
                }
                var basoCuoi = taxCode.Substring(11, 3);
                if (basoCuoi == "000")
                {
                    result = new Tuple<bool, string>(false, $"MST {taxCode} không đúng định dạng. Vui lòng kiểm tra lại");
                    return result;
                }

                Regex regexBaSoCuoi = new Regex(@"^[0-9]*$");
                var checkBaSoCuoi = regexBaSoCuoi.Match(basoCuoi);
                if (!checkBaSoCuoi.Success)
                {
                    result = new Tuple<bool, string>(false, $"MST {taxCode} không đúng định dạng. Vui lòng kiểm tra lại");
                    return result;
                }
            }

            //Cấu trúc Mã số thuế số: N1N2N3N4N5N6N7N8N9N10-N11N12N13
            int N1 = 0, N2 = 0, N3 = 0, N4 = 0, N5 = 0, N6 = 0, N7 = 0, N8 = 0, N9 = 0, N10 = 0;
            var tichVATVoiSoNT = 0;
            int chia11 = 0;
            var checkNumber = 0;
            var N1ToN10 = taxCode.Substring(0, 10);
            var charArr = N1ToN10.ToString().Select(t => int.Parse(t.ToString())).ToArray();

            N1 = charArr[0] * 31;
            N2 = charArr[1] * 29;
            N3 = charArr[2] * 23;
            N4 = charArr[3] * 19;
            N5 = charArr[4] * 17;
            N6 = charArr[5] * 13;
            N7 = charArr[6] * 7;
            N8 = charArr[7] * 5;
            N9 = charArr[8] * 3;
            N10 = charArr[9];

            tichVATVoiSoNT = N1 + N2 + N3 + N4 + N5 + N6 + N7 + N8 + N9;
            chia11 = tichVATVoiSoNT % 11;
            checkNumber = 10 - chia11;

            if (N10 != checkNumber)
            {
                result = new Tuple<bool, string>(false, $"MST {taxCode} không hợp lệ. Vui lòng kiểm tra lại");
                return result;
            }
            return result;
        }

        public static string ZenCode128(string input)
        {
            // Image img = new Image();
            var barcodeSimple = BarcodeSymbology.Code128;
            var barcodeDraw = BarcodeDrawFactory.GetSymbology(barcodeSimple);
            var metrics = barcodeDraw.GetDefaultMetrics(80);
            metrics.Scale = 2;
            int bgWidth = 300;
            int singleImage = 70;
            int bgHeight = 70;// listBarcode.Count * singleImage;
            var backgroundImage = new Bitmap(bgWidth, bgHeight, PixelFormat.Format32bppArgb);
            backgroundImage.GetHbitmap(Color.White);
            //int fontTextSize = 14;
            int marginLeft = 0;
            int paddingBarCode = 0;
            //int bottomText = 20;
            using (Graphics graphicsbg = Graphics.FromImage(backgroundImage))
            {
                //using (SolidBrush brush = new SolidBrush(Color.White))
                //{
                //    graphicsbg.FillRectangle(brush, 0, 0, backgroundImage.Width, backgroundImage.Height);
                //}

                //barcode 1

                int index = 0;
                var backgroundBarCode = new Bitmap(bgWidth, singleImage, PixelFormat.Format32bppArgb);
                var barcodeImage = barcodeDraw.Draw(input, metrics);
                using (Graphics graphicsbc = Graphics.FromImage(backgroundBarCode))
                {
                    graphicsbc.DrawImage(barcodeImage, new Rectangle(marginLeft, paddingBarCode, barcodeImage.Width, barcodeImage.Height), 0, 0, barcodeImage.Width, barcodeImage.Height, GraphicsUnit.Pixel);
                    //using (Brush textBrush = new SolidBrush(Color.Black))
                    //{
                    //    graphicsbc.DrawString(input, new Font("Tahoma", fontTextSize), textBrush, marginLeft, bottomText);
                    //}
                }
                graphicsbg.DrawImage(backgroundBarCode, new Rectangle(0, 0, backgroundBarCode.Width, backgroundBarCode.Height), 0, 0, backgroundBarCode.Width, backgroundBarCode.Height, GraphicsUnit.Pixel);
                graphicsbg.Dispose();
            }

            string baseImage64 = string.Empty;
            using (var stream = new MemoryStream())
            {
                backgroundImage.Save(stream, ImageFormat.Png);
                var imageBytes = stream.ToArray();
                baseImage64 = Convert.ToBase64String(imageBytes);
                stream.Dispose();
            }
            return baseImage64;
        }

        public static string GetDescriptionResultTask(string ErrorCode)
        {
            string Desc = "Not Found !";
            switch (ErrorCode)
            {
                case "0":
                    Desc = "The operation completed successfully";
                    break;
                case "1":
                    Desc = "Incorrect function called or unknown function called";
                    break;
                case "2":
                    Desc = "File not found";
                    break;
                case "10":
                    Desc = "The environment is incorrect";
                    break;
                case "0x41300":
                    Desc = "Task is ready to run at its next scheduled time";
                    break;
                case "267008":
                    Desc = "Task is ready to run at its next scheduled time";
                    break;
                case "0x41301":
                    Desc = "Task is currently running";
                    break;
                case "267009":
                    Desc = "Task is currently running";
                    break;
                case "0x41302":
                    Desc = "Task is disabled";
                    break;
                case "267010":
                    Desc = "Task is disabled";
                    break;
                case "0x41303":
                    Desc = "Task has not yet run";
                    break;
                case "267011":
                    Desc = "Task has not yet run";
                    break;
                case "0x41304":
                    Desc = "There are no more runs scheduled for this task";
                    break;
                case "267012":
                    Desc = "There are no more runs scheduled for this task";
                    break;
                case "0x41306":
                    Desc = "Task is terminated";
                    break;
                case "267014":
                    Desc = "Task is terminated";
                    break;
                case "0x8004130F":
                    Desc = "Credentials became corrupted";
                    break;
                case "2147750671":
                    Desc = "Credentials became corrupted";
                    break;
                case "-2147216625":
                    Desc = "Credentials became corrupted";
                    break;
                case "2147750687":
                    Desc = "An instance of this task is already running";
                    break;
                case "-2147216609":
                    Desc = "An instance of this task is already running";
                    break;
                case "2147943645":
                    Desc = "The service is not available (is ‘Run only when an user is logged on’ checked?)";
                    break;
                case "-2147023651":
                    Desc = "The service is not available (is ‘Run only when an user is logged on’ checked?)";
                    break;
                case "3221225786":
                    Desc = "The application terminated as a result of a CTRL+C";
                    break;
                case "-1073741510":
                    Desc = "The application terminated as a result of a CTRL+C";
                    break;
                case "3228369022":
                    Desc = "Unknown software exception";
                    break;
                case "-1066598274":
                    Desc = "Unknown software exception";
                    break;
                default:
                    Desc = ErrorCode;
                    break;
            }
            return Desc;
        }

    }
}
