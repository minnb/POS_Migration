using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace VCM.BLUEPOS.Models
{
    public class Ultils
    {
        public static string RenderViewToString(ControllerContext context, string viewName, object model)
        {
            if (string.IsNullOrEmpty(viewName))
                viewName = context.RouteData.GetRequiredString("action");
            var viewData = new ViewDataDictionary(model);
            using (var sw = new StringWriter())
            {
                var viewResult = ViewEngines.Engines.FindPartialView(context, viewName);
                var viewContext = new ViewContext(context, viewResult.View, viewData, new TempDataDictionary(), sw);
                viewResult.View.Render(viewContext, sw);
                return sw.GetStringBuilder().ToString();
            }
        }
        public Image ConvertStringtoImage(string imgBase64)
        {
            byte[] photoarray = Convert.FromBase64String(imgBase64);
            MemoryStream ms = new MemoryStream(photoarray, 0, photoarray.Length);
            ms.Write(photoarray, 0, photoarray.Length);
            Image image = Image.FromStream(ms);
            return image;
        }
    }
}