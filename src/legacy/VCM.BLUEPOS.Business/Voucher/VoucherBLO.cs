using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Data.Voucher;
using VCM.BLUEPOS.Model.Voucher;
using VCM.BLUEPOS.Model.OptionModel;
using VCM.BLUEPOS.Model;

namespace VCM.BLUEPOS.Business.Voucher
{
    public interface IVoucherBLO
    {
        ResultResponseModel CreateVoucherCoupon(CreateVoucherCouponModel req);
        ResultResponseModel UpdateVoucherHeader(UpdateVoucherHeaderModel req);
        List<VoucherCouponHeaderListModel> GetUpdateVoucherHeaderList(string codeVoucher, string nameVoucher, string serialVoucher, string typeVoucher, int statusType, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        ResultResponseModel DeleteVoucherHeaderList(DeleteVoucherHeaderModel req);
        List<ExportVoucherHeaderListModel> ExportGetVoucherHeaderList(string codeVoucher, string nameVoucher, string serialVoucher, string typeVoucher, int statusType);
        List<VoucherLineResponseModel> GetUpdateVoucherLineList(string noVoucher, string nameVoucher, string lineItemNo, string serialVoucher, string barCode, string typeVoucher, int statusVoucher, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        ResultResponseModel DeleteVoucherLineList(DeleteVoucherLineModel req);
        ResultResponseModel UpdateVoucherLine(UpdateVoucherLineModel req);
        List<ExportVoucherLineResponseModel> ExportGetVoucherLineList(string noVoucher, string nameVoucher, string lineItemNo, string serialVoucher, string barCode, string typeVoucher, int statusVoucher);
        List<VoucherPublishedResponseModel> GetVoucherPublishedList(DateTime fromDate, DateTime toDate, string storeNo, string voucherType, string textSearch, string statusVoucher, string sendSAP, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        List<ListVoucherResendModel> GetListVoucherResend(List<string> serialNo);
        ResultResponseModel UpdateStatusVoucher(List<ListVoucherResendModel> listVoucher);
        List<ExportExcelVoucherPublishedResponseModel> ExportExcelVoucherPublishedList(DateTime fromDate, DateTime toDate, string storeNo, string voucherType, string textSearch, string statusVoucher, string sendSAP);
        List<CouponVoucherModel> GetCouponVoucherList(string CouponVoucherType, string Status, string TextSearch, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        List<ExportExcel_VoucherCouponModel> ExportExcel_VoucherCouponList(string VoucherCouponType, string TextSearch, string Status);
        List<DetailCouponVoucherHeaderResponseModel> DetailCouponVoucherHeaderList(string itemNo);
        List<DetailCouponVoucherLineResponseModel> DetailCouponVoucherLineList(string itemNo, out int totalRecord, int skip = 0, int pageSize = 100, string SortColumn = null, string SortColumnDirection = null, string searchText = null);
        List<ExportExcel_DetailCouponVoucherLineModel> ExportToExcel_DetailCouponVoucherLineList(string offerNo);
        List<ListVoucherResendModel> VoucherResendSAP(List<string> serialNo);
        SerialResultResendSAPModel UpdateStatusVoucherV2(List<VoucherResendSAPModel> listVoucher);

    }


    public class VoucherBLO : IVoucherBLO
    {
        private VoucherData _data { get; set; }
        public VoucherBLO()
        {
            _data = new VoucherData();
        }
        public ResultResponseModel CreateVoucherCoupon(CreateVoucherCouponModel req)
        {
            return _data.CreateVoucherCoupon(req);
        }
        public ResultResponseModel UpdateVoucherHeader(UpdateVoucherHeaderModel req)
        {
            return _data.UpdateVoucherHeader(req);
        }
        public List<VoucherCouponHeaderListModel> GetUpdateVoucherHeaderList(string codeVoucher, string nameVoucher, string serialVoucher, string typeVoucher, int statusType, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            return _data.GetUpdateVoucherHeaderList(codeVoucher, nameVoucher, serialVoucher, typeVoucher, statusType, out totalRecord, pageIndex, pageSize);
                
        }
        public ResultResponseModel DeleteVoucherHeaderList(DeleteVoucherHeaderModel req)
        {
            return _data.DeleteVoucherHeaderList(req);
        }
        public List<ExportVoucherHeaderListModel> ExportGetVoucherHeaderList(string codeVoucher, string nameVoucher, string serialVoucher, string typeVoucher, int statusType)
        {
            return _data.ExportGetVoucherHeaderList(codeVoucher, nameVoucher, serialVoucher, typeVoucher, statusType);
        }
        public List<VoucherLineResponseModel> GetUpdateVoucherLineList(string noVoucher, string nameVoucher, string lineItemNo, string serialVoucher, string barCode, string typeVoucher, int statusVoucher, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            return _data.GetUpdateVoucherLineList(noVoucher, nameVoucher, lineItemNo, serialVoucher, barCode, typeVoucher, statusVoucher, out totalRecord, pageIndex, pageSize);
        }
        public ResultResponseModel DeleteVoucherLineList(DeleteVoucherLineModel req)
        {
            return _data.DeleteVoucherLineList(req);
        }
        public ResultResponseModel UpdateVoucherLine(UpdateVoucherLineModel req)
        {
            return _data.UpdateVoucherLine(req);
        }
        public List<ExportVoucherLineResponseModel> ExportGetVoucherLineList(string noVoucher, string nameVoucher, string lineItemNo, string serialVoucher, string barCode, string typeVoucher, int statusVoucher)
        {
            return _data.ExportGetVoucherLineList(noVoucher, nameVoucher, lineItemNo, serialVoucher, barCode, typeVoucher, statusVoucher);
        }

        public List<VoucherPublishedResponseModel> GetVoucherPublishedList(DateTime fromDate, DateTime toDate, string storeNo, string voucherType, string textSearch, string statusVoucher, string sendSAP, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            return _data.GetVoucherPublishedList(fromDate, toDate, storeNo, voucherType, textSearch, statusVoucher, sendSAP, out totalRecord, pageIndex, pageSize);
        }
        public List<ListVoucherResendModel> GetListVoucherResend(List<string> serialNo)
        {
            return _data.GetListVoucherResend(serialNo);
        }
        public ResultResponseModel UpdateStatusVoucher(List<ListVoucherResendModel> listVoucher)
        {
            return _data.UpdateStatusVoucher(listVoucher);
        }
        public List<ExportExcelVoucherPublishedResponseModel> ExportExcelVoucherPublishedList(DateTime fromDate, DateTime toDate, string storeNo, string voucherType, string textSearch, string statusVoucher, string sendSAP)
        {
            return _data.ExportExcelVoucherPublishedList(fromDate, toDate, storeNo, voucherType, textSearch, statusVoucher, sendSAP);
        }
        public List<CouponVoucherModel> GetCouponVoucherList(string CouponVoucherType, string Status, string TextSearch, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            return _data.GetCouponVoucherList(CouponVoucherType, Status, TextSearch, out totalRecord, pageIndex, pageSize);
        }
        public List<ExportExcel_VoucherCouponModel> ExportExcel_VoucherCouponList(string VoucherCouponType, string TextSearch, string Status)
        {
            return _data.ExportExcel_VoucherCouponList(VoucherCouponType, TextSearch, Status);
        }
        public List<DetailCouponVoucherHeaderResponseModel> DetailCouponVoucherHeaderList(string itemNo)
        {
            return _data.DetailCouponVoucherHeaderList(itemNo);
        }
        public List<DetailCouponVoucherLineResponseModel> DetailCouponVoucherLineList(string itemNo, out int totalRecord, int skip = 0, int pageSize = 100, string SortColumn = null, string SortColumnDirection = null, string searchText = null)
        {
            return _data.DetailCouponVoucherLineList(itemNo, out totalRecord, skip, pageSize, SortColumn, SortColumnDirection, searchText);
        }

        public List<ExportExcel_DetailCouponVoucherLineModel> ExportToExcel_DetailCouponVoucherLineList(string offerNo)
        {
            return _data.ExportToExcel_DetailCouponVoucherLineList(offerNo);
        }

        public List<ListVoucherResendModel> VoucherResendSAP(List<string> serialNo)
        {
            return _data.VoucherResendSAP(serialNo);
        }

        public SerialResultResendSAPModel UpdateStatusVoucherV2(List<VoucherResendSAPModel> listVoucher)
        {
            return _data.UpdateStatusVoucherV2(listVoucher);
        }




    }
}
