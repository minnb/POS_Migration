using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Common.Helpers;
using VCM.BLUEPOS.Data.EF;
using VCM.BLUEPOS.Data.EF.Central;
using VCM.BLUEPOS.Data.EF.IFSAP;
using VCM.BLUEPOS.Model.HotKey;



namespace VCM.BLUEPOS.Data.HotKey
{
    public interface IHotKeyData
    {
        List<POSHotKeyItemModel> LoadHotKey(string ProvinceCode, out int total, int Skip, int Take);
        Tuple<int, string> InsertBarCode(string ProvinceCode, string BarCode);
        Tuple<int, string> DeleteHotKeyBarcode(string StoreGroup, string Barcode);



    }
    public class HotKeyData : IHotKeyData
    {
        public List<POSHotKeyItemModel> LoadHotKey(string ProvinceCode, out int total, int Skip, int Take)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = (from a in db.POSHotKeyItems
                                join b in db.Provinces on a.StoreGroup equals b.ProvinceCode
                                where a.StoreGroup == ProvinceCode
                                select new POSHotKeyItemModel
                                {
                                    StoreGroup = a.StoreGroup,
                                    ProvinceName = b.ProvinceName,
                                    Barcode = a.Barcode,
                                    ItemNo = a.ItemNo,
                                    Description = a.Description,
                                    Counter = a.Counter,
                                    Seq = a.Seq,
                                    Status = a.Status,
                                    Pkey = a.Pkey
                                }).ToList();
                    total = data.Count();
                    var result = data.OrderByDescending(d => d.Seq).Skip(Skip).Take(Take).ToList();
                    return result;
                }
            }
            catch (Exception ex)
            {
                total = 0;
                return (default(List<POSHotKeyItemModel>));
            }
        }

        public Tuple<int, string> InsertBarCode(string ProvinceCode, string BarCode)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = (from a in db.Items
                                join b in db.Barcodes on a.No equals b.ItemNo
                                where b.BarcodeNo == BarCode
                                select new ItemModel
                                {
                                    BarcodeNo = BarCode,
                                    ItemNo = a.No,
                                    ItemName = a.Description
                                }).FirstOrDefault();

                    if (data == null)
                    {
                        return new Tuple<int, string>(0, "Barcode này không tồn tại trong hệ thống");
                    }                       
                    var checkHotKey = db.POSHotKeyItems.FirstOrDefault(d => d.StoreGroup == ProvinceCode && d.Barcode == BarCode);
                    if (checkHotKey != null)
                    {
                        return new Tuple<int, string>(0, $"Barcode này {BarCode} đã tồn tại trong bảng chạm");
                    }
                    var maxSeq = db.POSHotKeyItems.Where(d => d.StoreGroup == ProvinceCode).Select(d => d.Seq).Max();
                    var maxCounter = db.POSHotKeyItems.Where(d => d.StoreGroup == ProvinceCode).Select(d => d.Counter).Max();
                    var insert = new POSHotKeyItem
                    {
                        StoreGroup = ProvinceCode,
                        Barcode = data.BarcodeNo,
                        ItemNo = data.ItemNo,
                        Description = data.ItemName.ToUpper(),
                        Counter = maxCounter == null ? 1 : maxCounter + 1,
                        Seq = maxSeq == null ? 1 : maxSeq + 1,
                        Pkey = $"{ProvinceCode}-{data.BarcodeNo}",
                        Status = true   // == 1
                    };
                    db.POSHotKeyItems.Add(insert);
                    db.SaveChanges();
                    return new Tuple<int, string>(1, "Success");
                }
            }
            catch (Exception ex)
            {
                return new Tuple<int, string>(0, ex.Message);
            }
        }

        public Tuple<int, string> DeleteHotKeyBarcode(string StoreGroup, string Barcode)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var checkBarcode = db.POSHotKeyItems.FirstOrDefault(d => d.StoreGroup == StoreGroup && d.Barcode == Barcode);
                    if (checkBarcode != null)
                    {
                        db.POSHotKeyItems.Remove(checkBarcode);
                        db.SaveChanges();
                        return new Tuple<int, string>(1, "Success");
                    }
                    else
                    {
                        return new Tuple<int, string>(0, "Xóa thất bại");
                    }                        
                }
            }
            catch (Exception ex)
            {
                return new Tuple<int, string>(0, ex.Message);
            }
        }




    }
}
