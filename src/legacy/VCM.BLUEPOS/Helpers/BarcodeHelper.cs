using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Zen.Barcode;
using Gma.QrCodeNet.Encoding;
using Gma.QrCodeNet.Encoding.Windows.Render;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;


namespace VCM.BLUEPOS.Helpers
{
    public static class BarcodeHelper
    {
        public static string GenerateBarcodeToString(string input)
        {
            var result = string.Empty;
            if (string.IsNullOrEmpty(input))
            {
                return result;
            }

            var zen = BarcodeSymbology.Code128;
            var drawObject = BarcodeDrawFactory.GetSymbology(zen);
            var metrics = drawObject.GetDefaultMetrics(20);
            metrics.Scale = 1;
            var barcodeImage = drawObject.Draw(input, metrics);

            using (var ms = new MemoryStream())
            {
                barcodeImage.Save(ms, ImageFormat.Png);
                var imageBytes = ms.ToArray();
                result = Convert.ToBase64String(imageBytes);
            }
            return result;
        }

        public static Image GenerateBarcode(string input)
        {
            var zen = BarcodeSymbology.Code128;
            var drawObject = BarcodeDrawFactory.GetSymbology(zen);
            var metrics = drawObject.GetDefaultMetrics(20);
            metrics.Scale = 1;
            var barcodeImage = drawObject.Draw(input, metrics);
            return barcodeImage;
        }

        public static string GenerateQRCodeToString(string content, int size)
        {
            var result = string.Empty;
            if (string.IsNullOrEmpty(content))
            {
                return result;
            }

            QrEncoder encoder = new QrEncoder(ErrorCorrectionLevel.H);
            QrCode qrCode;
            encoder.TryEncode(content, out qrCode);
          
            GraphicsRenderer gRenderer = new GraphicsRenderer(new FixedModuleSize(4, QuietZoneModules.Two), System.Drawing.Brushes.Black, System.Drawing.Brushes.White);
            MemoryStream ms = new MemoryStream();
            gRenderer.WriteToStream(qrCode.Matrix, ImageFormat.Bmp, ms);

            var imageTemp = new Bitmap(ms);
            var image = (Image)(new Bitmap(imageTemp, new System.Drawing.Size(new System.Drawing.Point(size, size))));

            using (var ms1 = new MemoryStream())
            {
                image.Save(ms1, ImageFormat.Png);
                var imageBytes = ms1.ToArray();
                result = Convert.ToBase64String(imageBytes);
            }

            return result;
        }

        public static Image GenerateQRCode(string content, int size)
        {
            QrEncoder encoder = new QrEncoder(ErrorCorrectionLevel.H);
            QrCode qrCode;
            encoder.TryEncode(content, out qrCode);

            GraphicsRenderer gRenderer = new GraphicsRenderer(new FixedModuleSize(4, QuietZoneModules.Two), System.Drawing.Brushes.Black, System.Drawing.Brushes.White);
            MemoryStream ms = new MemoryStream();
            gRenderer.WriteToStream(qrCode.Matrix, ImageFormat.Bmp, ms);

            var imageTemp = new Bitmap(ms);
            var image = new Bitmap(imageTemp, new System.Drawing.Size(new System.Drawing.Point(size, size)));
            return (System.Drawing.Image)image;
        }


    }







}