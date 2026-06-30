using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Data.EF;
using VCM.BLUEPOS.Data.EF.Central;
using VCM.BLUEPOS.Model.Barcode;

namespace VCM.BLUEPOS.Data.Barcode
{
    public interface IBarcodeData
    {
        List<BarcodeResponseModel> GetBarcodeList(string barcodeNo, string itemNo, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        List<ExportBarcodeResponseModel> ExportBarcodeList(string barcodeNo, string itemNo);

    }
    public class BarcodeData : IBarcodeData
    {
        public List<BarcodeResponseModel> GetBarcodeList(string barcodeNo, string itemNo, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;

                    var data = db.Database.SqlQuery<BarcodeResponseModel>("[dbo].[GetBarcodeList] @BarcodeNo, @ItemNo, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@BarcodeNo", barcodeNo ?? string.Empty),
                        new SqlParameter("@ItemNo", itemNo ?? string.Empty),
                        new SqlParameter("@Export", 1),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)
                        ).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<BarcodeResponseModel>();
                    }

                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }

            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<BarcodeResponseModel>();
            }
        }

        public List<ExportBarcodeResponseModel> ExportBarcodeList(string barcodeNo, string itemNo)
        {
            try
            {
                var data = new List<ExportBarcodeResponseModel>();
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    data = db.Database.SqlQuery<ExportBarcodeResponseModel>("[dbo].[GetBarcodeList] @BarcodeNo, @ItemNo, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@BarcodeNo", barcodeNo ?? string.Empty),
                        new SqlParameter("@ItemNo", itemNo ?? string.Empty),
                        new SqlParameter("@Export", 2),
                        new SqlParameter("@PageSize", string.Empty),
                        new SqlParameter("@PageNumber", string.Empty)).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return (default(List<ExportBarcodeResponseModel>));
            }
        }





    }
}
