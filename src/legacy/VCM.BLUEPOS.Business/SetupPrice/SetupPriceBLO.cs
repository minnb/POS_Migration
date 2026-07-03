using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Data.SetupPrice;
using VCM.BLUEPOS.Model.Barcode;
using VCM.BLUEPOS.Model.SetupPrice;
using VCM.BLUEPOS.Model.SetupPromotion;
using VCM.BLUEPOS.Model.Store;

namespace VCM.BLUEPOS.Business.SetupPrice
{
    public interface ISetupPriceBLO
    {
        ViewSetupPriceModel ViewSetupPrice();
        List<StorePriceGroupModel> ListStoreGroup();
        PagingResult<ItemResultCombo> SearchItem(string keyword, int pageSize = 20, int page = 1,string searchBy="item");
        List<TableStoreModel> ListStoreByChannel(string channel);
        BarcodeResponseModel GetInfoBarcode(string barcode);
        List<ExcelExamplePriceModel> ExcelExamplePrice();
        List<ItemModel> ListItemBarcode();
        List<ImportResultSalePriceModel> GetListInfoItem(List<LoadExcelExamplePriceModel> listItemExcel);
        Tuple<bool, string> SaveSalesPrice(List<SalesPriceModel> model);
        List<ListPriceModel> LoadSalesPrice(string itemCode, string itemName, string barCode, string salesCode, string status, out int totalRecord, int pageNumber, int pageSize);
        Tuple<bool, string> DeletePrice(int id);
        Tuple<bool, string> UpdatePrice(int id, float unitPrice);
    }
    public class SetupPriceBLO : ISetupPriceBLO
    {
        private SetupPriceData data { get; set; }
        public SetupPriceBLO()
        {
            data = new SetupPriceData();
        }

        public ViewSetupPriceModel ViewSetupPrice()
        {
            return data.ViewSetupPrice();
        }
        public List<StorePriceGroupModel> ListStoreGroup()
        {
            return data.ListStoreGroup();
        }

        public List<TableStoreModel> ListStoreByChannel(string channel)
        {
            return data.ListStoreByChannel(channel);
        }

        public BarcodeResponseModel GetInfoBarcode(string barcode)
        {
            return data.GetInfoBarcode(barcode);
        }

        public PagingResult<ItemResultCombo> SearchItem(string keyword,  int pageSize = 20, int page = 1, string searchBy = "item")
        {
            return data.SearchItem(keyword,  pageSize, page, searchBy);
        }

        public List<ItemModel> ListItemBarcode()
        {
            return data.ListItemBarcode();
        }

        public List<ExcelExamplePriceModel> ExcelExamplePrice()
        {
            return data.ExcelExamplePrice();
        }

        public List<ImportResultSalePriceModel> GetListInfoItem(List<LoadExcelExamplePriceModel> listItemExcel)
        {
            return data.GetListInfoItem(listItemExcel);
        }

        public Tuple<bool, string> SaveSalesPrice(List<SalesPriceModel> model)
        {
            return data.SaveSalesPrice(model);
        }

        public List<ListPriceModel> LoadSalesPrice(string itemCode, string itemName, string barCode, string salesCode, string status, out int totalRecord, int pageNumber, int pageSize)
        {
            return data.LoadSalesPrice(itemCode, itemName, barCode, salesCode, status, out totalRecord, pageNumber, pageSize);
        }

        public Tuple<bool, string> DeletePrice(int id)
        {
            return data.DeletePrice(id);
        }

        public Tuple<bool, string> UpdatePrice(int id, float unitPrice)
        {
            return data.UpdatePrice(id, unitPrice);
        }
    }
}
