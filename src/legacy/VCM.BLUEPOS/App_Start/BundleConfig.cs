using System.Web;
using System.Web.Optimization;

namespace VCM.BLUEPOS
{
    public class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new Bundle("~/bundles/base").Include(
                    "~/Content/themekeen/plugins/global/plugins.bundle.js",
                    "~/Content/themekeen/js/scripts.bundle.js",
                    "~/Content/themekeen/js/pages/features/miscellaneous/blockui.js"
            ));

            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                         "~/Content/themekeen/plugins/custom/fullcalendar/fullcalendar.bundle.js",                        
                         "~/Content/themekeen/js/pages/crud/forms/widgets/select2.js",
                         "~/Content/themekeen/js/pages/crud/forms/widgets/jquery-mask.js",
                         "~/Content/themekeen/js/pages/widgets.js",
                         "~/Content/themekeen/plugins/custom/datatables/datatables.bundle.js",
                         "~/Content/themekeen/js/pages/moment.min.js",
                         "~/Content/themekeen/js/pages/crud/forms/widgets/bootstrap-datepicker.js",
                         "~/Content/themekeen/js/pages/crud/forms/widgets/bootstrap-datetimepicker.min.js",
                         "~/Content/setuppromotion.js",
                         "~/Content/dataTables.buttons.min.js",
                         "~/Content/dataTables/api/Sum().js",
                         "~/Content/buttons.html5.min.js",
                         "~/Content/bootstrap-multiple-select/js/bootstrap-multiselect.min.js",
                         "~/Content/multiple-select-custom/multiple-select-custom.js"                         
                    )
               );
            
               bundles.Add(new StyleBundle("~/Content/css").Include(
                        "~/Content/custom.css",
                        "~/Content/confirm/jquery-confirm.min.css",
                        "~/Content/themekeen/plugins/custom/fullcalendar/fullcalendar.bundle.css",
                        "~/Content/themekeen/plugins/custom/datatables/datatables.bundle.css",
                        "~/Content/bootstrap-multiple-select/css/bootstrap-multiselect.min.css",
                        "~/Content/multiple-select-custom/multiple-select-custom.css"
                        )
                      .Include("~/Content/themekeen/plugins/global/plugins.bundle.css", new CssRewriteUrlTransform())
                      .Include("~/Content/themekeen/custom/prismjs/prismjs.bundle.css", new CssRewriteUrlTransform())
                      .Include("~/Content/themekeen/css/style.bundle.css", new CssRewriteUrlTransform())
                      );
        }
    }
}
