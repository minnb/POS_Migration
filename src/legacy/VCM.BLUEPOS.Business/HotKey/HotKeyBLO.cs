using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Data.HotKey;
using VCM.BLUEPOS.Model.HotKey;



namespace VCM.BLUEPOS.Business.HotKey
{
    public interface IHotKeyBLO
    {
        List<POSHotKeyItemModel> LoadHotKey(string ProvinceCode, out int total, int Skip, int Take);
        Tuple<int, string> InsertBarCode(string ProvinceCode, string BarCode);
        Tuple<int, string> DeleteHotKeyBarcode(string StoreGroup, string Barcode);



    }
    public class HotKeyBLO : IHotKeyBLO
    {
        private  HotKeyData _data { get; set; }
        public HotKeyBLO()
        {
            _data = new HotKeyData();
        }

        public List<POSHotKeyItemModel> LoadHotKey(string ProvinceCode, out int total, int Skip, int Take)
        {
            return _data.LoadHotKey(ProvinceCode, out total, Skip, Take);
        }
        public Tuple<int, string> InsertBarCode(string ProvinceCode, string BarCode)
        {
            return _data.InsertBarCode(ProvinceCode, BarCode);
        }
        public Tuple<int, string> DeleteHotKeyBarcode(string StoreGroup, string Barcode)
        {
            return _data.DeleteHotKeyBarcode(StoreGroup, Barcode);
        }





    }
}
