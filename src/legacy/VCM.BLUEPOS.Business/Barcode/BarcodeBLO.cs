using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Data.Barcode;
using VCM.BLUEPOS.Model.Barcode;

namespace VCM.BLUEPOS.Business.Barcode
{
    public interface IBarcodeBLO
    {
        List<BarcodeResponseModel> GetBarcodeList(string barcodeNo, string itemNo, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        List<ExportBarcodeResponseModel> ExportBarcodeList(string barcodeNo, string itemNo);


    }

    public  class BarcodeBLO : IBarcodeBLO
    {
        private BarcodeData _data { get; set; }
        public BarcodeBLO()
        {
            _data = new BarcodeData();
        }

        public List<BarcodeResponseModel> GetBarcodeList(string barcodeNo, string itemNo, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            return _data.GetBarcodeList(barcodeNo, itemNo, out totalRecord, pageIndex, pageSize);
        }

        public List<ExportBarcodeResponseModel> ExportBarcodeList(string barcodeNo, string itemNo)
        {
            return _data.ExportBarcodeList(barcodeNo, itemNo);
        }

    }



}
