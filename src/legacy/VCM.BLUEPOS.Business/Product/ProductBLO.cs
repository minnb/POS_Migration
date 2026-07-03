using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Data.Product;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.Product;
using VCM.BLUEPOS.Model.Barcode;

namespace VCM.BLUEPOS.Business.Product
{
    public interface IProductBLO
    {
        List<ProductListResponseModel> GetProductList(string itemNo, string itemName, string barcodeNo, string taxCode, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        List<ExportProductListModel> ExportProductList(string itemNo, string itemName, string barcodeNo, string taxCode);
        ResultResponseModel SaveArticleList(CreateProductModel req);
        ResultResponseModel SaveBarcodeList(List<CreateBarcodeListModel> req);
        List<UpdateProductListResponseModel> GetUpdateProductList(string itemNo, string itemName, string barcodeNo, string taxCode, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        ResultResponseModel UpdateProductList(UpdateProductListModel req);
        List<StoreProductLockResponseModel> GetStoreProductLock(string setServer, string storeNo, string userName, out int totalRecord, int skip = 0, int take = 100, string SortColumn = null, string SortColumnDirection = null, string searchText = null);
        List<ItemListResponseModel> GetItemListForBlock(string storeNo, string status, out int totalRecord, int skip = 0, int take = 100, string SortColumn = null, string SortColumnDirection = null, string searchText = null);
        ResultResponseModel CreateProductLockOrUnlock(CreateProductLockModel req);
        List<ProductLockListResponseModel> GetProductLockList(string storeNo, string itemNo, string itemName, string status, out int totalRecord, int skip = 0, int take = 100, string SortColumn = null, string SortColumnDirection = null, string searchText = null);
        ResultResponseModel CreateProductUnlockByModal(CreateProductUnlockModel req, string POSUser, string POSPass);

        
        //--------------------------------------------------------------------------------------------------

        List<StoreProductLockResponseModel> POS_GetStoreProductLock(string IPAddress, string POSUser, string POSPass, string storeNo, string userName, out int totalRecord, int skip = 0, int take = 100, string SortColumn = null, string SortColumnDirection = null, string searchText = null);
        List<ProductLockListResponseModel> POS_GetItemLockList(string IPAddress, string POSUser, string POSPass, string storeNo, string userName, string itemNo, string itemName, string status, out int totalRecord, int skip = 0, int take = 100, string SortColumn = null, string SortColumnDirection = null, string searchText = null);
        List<ItemListResponseModel> POS_GetItemListForBlock(string IPAddress, string POSUser, string POSPass, string storeNo, string status, out int totalRecord, int skip = 0, int take = 100, string SortColumn = null, string SortColumnDirection = null, string searchText = null);
        ResultResponseModel POS_CreateProductLockOrUnlock(CreateProductLockModel req, string POSUser, string POSPass);
        ResultResponseModel POS_CreateProductUnlockByModal(CreateProductUnlockModel req, string POSUser, string POSPass);


    }

    public class ProductBLO : IProductBLO
    {
        private ProductData _data { get; set; }
        public ProductBLO()
        {
            _data = new ProductData();
        }

        public List<ProductListResponseModel> GetProductList(string itemNo, string itemName, string barcodeNo, string taxCode, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            return _data.GetProductList(itemNo, itemName, barcodeNo, taxCode, out totalRecord, pageIndex, pageSize);
        }
        public List<ExportProductListModel> ExportProductList(string itemNo, string itemName, string barcodeNo, string taxCode)
        {
            return _data.ExportProductList(itemNo, itemName, barcodeNo, taxCode);
        }
        public ResultResponseModel SaveArticleList(CreateProductModel req)
        {
            return _data.SaveArticleList(req);
        }
        public ResultResponseModel SaveBarcodeList(List<CreateBarcodeListModel> req)
        {
            return _data.SaveBarcodeList(req);
        }

        public List<UpdateProductListResponseModel> GetUpdateProductList(string itemNo, string itemName, string barcodeNo, string taxCode, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            return _data.GetUpdateProductList(itemNo, itemName, barcodeNo, taxCode, out totalRecord, pageIndex, pageSize);
        }

        public ResultResponseModel UpdateProductList(UpdateProductListModel req)
        {
            return _data.UpdateProductList(req);
        }

        public List<StoreProductLockResponseModel> GetStoreProductLock(string setServer, string storeNo, string userName, out int totalRecord, int skip = 0, int take = 100, string SortColumn = null, string SortColumnDirection = null, string searchText = null)
        {
            return _data.GetStoreProductLock(setServer, storeNo, userName, out totalRecord, skip, take, SortColumn, SortColumnDirection, searchText);
        }
        public List<ItemListResponseModel> GetItemListForBlock(string storeNo, string status, out int totalRecord, int skip = 0, int take = 100, string SortColumn = null, string SortColumnDirection = null, string searchText = null)
        {
            return _data.GetItemListForBlock(storeNo, status, out totalRecord, skip, take, SortColumn, SortColumnDirection, searchText);
        }
        public List<ItemListResponseModel> POS_GetItemListForBlock(string IPAddress, string POSUser, string POSPass, string storeNo, string status, out int totalRecord, int skip = 0, int take = 100, string SortColumn = null, string SortColumnDirection = null, string searchText = null)
        {
            return _data.POS_GetItemListForBlock(IPAddress, POSUser, POSPass, storeNo, status, out totalRecord, skip, take, SortColumn, SortColumnDirection, searchText);
        }
        public ResultResponseModel CreateProductLockOrUnlock(CreateProductLockModel req)
        {
            return _data.CreateProductLockOrUnlock(req);
        }
        public List<ProductLockListResponseModel> GetProductLockList(string storeNo, string itemNo, string itemName, string status, out int totalRecord, int skip = 0, int take = 100, string SortColumn = null, string SortColumnDirection = null, string searchText = null)
        {
            return _data.GetProductLockList(storeNo, itemNo, itemName, status, out totalRecord, skip, take, SortColumn, SortColumnDirection, searchText);
        }
        public ResultResponseModel CreateProductUnlockByModal(CreateProductUnlockModel req, string POSUser, string POSPass)
        {
            return _data.CreateProductUnlockByModal(req, POSUser, POSPass);
        }

        //-----------------------------------------------------------

        public List<StoreProductLockResponseModel> POS_GetStoreProductLock(string IPAddress, string POSUser, string POSPass, string storeNo, string userName, out int totalRecord, int skip = 0, int take = 100, string SortColumn = null, string SortColumnDirection = null, string searchText = null)
        {
            return _data.POS_GetStoreProductLock(IPAddress, POSUser, POSPass, storeNo, userName, out totalRecord, skip, take, SortColumn, SortColumnDirection, searchText);
        }

        public List<ProductLockListResponseModel> POS_GetItemLockList(string IPAddress, string POSUser, string POSPass, string storeNo, string userName, string itemNo, string itemName, string status, out int totalRecord, int skip = 0, int take = 100, string SortColumn = null, string SortColumnDirection = null, string searchText = null)
        {
            return _data.POS_GetItemLockList(IPAddress, POSUser, POSPass, storeNo, userName, itemNo, itemName, status, out totalRecord, skip, take, SortColumn, SortColumnDirection, searchText);
        }

        public ResultResponseModel POS_CreateProductLockOrUnlock(CreateProductLockModel req, string POSUser, string POSPass)
        {
            return _data.POS_CreateProductLockOrUnlock(req, POSUser, POSPass);
        }

        public ResultResponseModel POS_CreateProductUnlockByModal(CreateProductUnlockModel req, string POSUser, string POSPass)
        {
            return _data.POS_CreateProductUnlockByModal(req, POSUser, POSPass);
        }
       

    }
}
