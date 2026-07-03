using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Data.EF;
using VCM.BLUEPOS.Data.EF.Central;
using VCM.BLUEPOS.Model.Voucher;
using VCM.BLUEPOS.Model.OptionModel;
using VCM.BLUEPOS.Model;
using System.Net;

namespace VCM.BLUEPOS.Data.Voucher
{
    public interface IVoucherData
    {
        ResultResponseModel CreateVoucherCoupon(CreateVoucherCouponModel req);
        string CreateVoucherCode(string voucherNo);
        ResultResponseModel UpdateVoucherHeader(UpdateVoucherHeaderModel req);
        List<VoucherCouponHeaderListModel> GetUpdateVoucherHeaderList(string codeVoucher, string nameVoucher, string serialVoucher, string typeVoucher, int statusType, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        ResultResponseModel DeleteVoucherHeaderList(DeleteVoucherHeaderModel req);
        List<ExportVoucherHeaderListModel> ExportGetVoucherHeaderList(string codeVoucher, string nameVoucher, string serialVoucher, string typeVoucher, int statusType);
        List<VoucherLineResponseModel> GetUpdateVoucherLineList(string noVoucher, string nameVoucher, string lineItemNo, string serialVoucher, string barCode, string typeVoucher, int statusVoucher, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        ResultResponseModel DeleteVoucherLineList(DeleteVoucherLineModel req);
        ResultResponseModel UpdateVoucherLine(UpdateVoucherLineModel req);
        List<ExportVoucherLineResponseModel> ExportGetVoucherLineList(string noVoucher, string nameVoucher, string lineItemNo, string serialVoucher, string barCode, string typeVoucher, int statusVoucher);
        ResultResponseModel CreateVoucherLine(CreateVoucherLineModel req);
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

    public class VoucherData : IVoucherData
    {
        public string CreateVoucherCode(string voucherNo)
        {
            var result = "";
            var _voucherNo = "70000001";
            if (string.IsNullOrEmpty(voucherNo))
            {
                result = _voucherNo;
            }
            else
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var maxItemNo = db.CpnVchBOMHeaders.Max(x => x.ItemNo);
                    int str = int.Parse(_voucherNo.Substring(1));
                    if (str <= 9999999)
                    {
                        int voucherCode = int.Parse(maxItemNo) + 1;
                        result = Convert.ToString(voucherCode);
                    }
                }
            }
            return result;
        }

        public ResultResponseModel CreateVoucherCoupon(CreateVoucherCouponModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var _totalRowVoucherHeader = db.CpnVchBOMHeaders.Select(x => x.ItemNo).ToList();
                    var _totalRowVoucherLine = db.CpnVchBOMLines.Select(x => x.ItemNo).ToList();
                    var _serialVoucher = db.CpnVchBOMHeaders.Where(x => x.CouponCode == req.CouponCode).ToList();

                    var _voucherNo = "";
                    if (_totalRowVoucherHeader == null || _totalRowVoucherHeader.Count == 0)
                    {
                        _voucherNo = "";
                    }
                    else
                    {
                        _voucherNo = db.CpnVchBOMHeaders.Max(x => x.ItemNo);
                    }
                    var _ItemNo = CreateVoucherCode(_voucherNo);

                    if (_totalRowVoucherHeader == null || _totalRowVoucherHeader.Count == 0)   // Chưa có dòng nào trong bảng CpnVchBOMHeader
                    {
                        if (req.IsCheckItem == true)  // Áp dụng total bill
                        {
                            if (_serialVoucher.FirstOrDefault()?.CouponCode != null || _serialVoucher.Count > 0)
                            {
                                return new ResultResponseModel
                                {
                                    Status = Model.Enums.ResultEnum.Fail,
                                    Message = $"Số serial {req.CouponCode} đã tồn tại. Vui lòng kiểm tra lại"
                                };
                            }

                            var insertHeader = new EF.Central.CpnVchBOMHeader
                            {
                                ItemNo = _ItemNo,
                                ItemName = req.ItemName,
                                UnitOfMeasure = req.UnitOfMeasure,
                                DiscountType = req.DiscountType,
                                DiscountValue = req.DiscountValue,
                                MaxAmount = req.MaxAmount,
                                ArticleType = req.ArticleType,
                                ValueOfVoucher = req.ValueOfVoucher,
                                StartingDate = req.StartingDate,
                                EndingDate = req.EndingDate,
                                Blocked = req.Blocked,
                                CouponCode = req.CouponCode,
                                LimitQty = req.LimitQty,
                                LastDateModified = req.LastDateModified,
                                IsCheckItem = req.IsCheckItem,
                                Counter = 1,
                                Pkey = _ItemNo
                            };
                            db.CpnVchBOMHeaders.Add(insertHeader);
                            db.SaveChanges();
                        }
                        else  // Không áp dụng total bill
                        {
                            if (_serialVoucher.FirstOrDefault()?.CouponCode != null || _serialVoucher.Count > 0)
                            {
                                return new ResultResponseModel
                                {
                                    Status = Model.Enums.ResultEnum.Fail,
                                    Message = $"Số serial {req.CouponCode} đã tồn tại. Vui lòng kiểm tra lại"
                                };
                            }

                            var insertHeader = new EF.Central.CpnVchBOMHeader
                            {
                                ItemNo = _ItemNo,
                                ItemName = req.ItemName,
                                UnitOfMeasure = req.UnitOfMeasure,
                                DiscountType = req.DiscountType,
                                DiscountValue = req.DiscountValue,
                                MaxAmount = req.MaxAmount,
                                ArticleType = req.ArticleType,
                                ValueOfVoucher = req.ValueOfVoucher,
                                StartingDate = req.StartingDate,
                                EndingDate = req.EndingDate,
                                Blocked = req.Blocked,
                                CouponCode = req.CouponCode,
                                LimitQty = req.LimitQty,
                                LastDateModified = req.LastDateModified,
                                IsCheckItem = req.IsCheckItem,
                                Counter = 1,
                                Pkey = _ItemNo
                            };
                            db.CpnVchBOMHeaders.Add(insertHeader);
                            db.SaveChanges();

                            foreach (var item in req.ListItemApply)
                            {
                                var _checkLineItemNo = db.Items.Where(x => x.No == item.LineItemNo).ToList();
                                if (_checkLineItemNo.FirstOrDefault()?.No == null || _checkLineItemNo.Count == 0)
                                {
                                    return new ResultResponseModel
                                    {
                                        Status = Model.Enums.ResultEnum.Fail,
                                        Message = $"Mã sản phẩm {item.LineItemNo} không tồn tại. Vui lòng kiểm tra lại"
                                    };
                                }
                                var _checkBarcodeNo = db.Barcodes.Where(x => x.BarcodeNo == item.Barcode).ToList();
                                if (_checkBarcodeNo.FirstOrDefault()?.BarcodeNo == null || _checkBarcodeNo.Count == 0)
                                {
                                    return new ResultResponseModel
                                    {
                                        Status = Model.Enums.ResultEnum.Fail,
                                        Message = $"Mã barcode {item.Barcode} không tồn tại. Vui lòng kiểm tra lại"
                                    };
                                }
                                var _newPkey = $"{_ItemNo}{"-"}{item.LineItemNo}{"-"}{item.Barcode}";
                                var _checkPkey = db.CpnVchBOMLines.Where(x => x.Pkey == _newPkey).ToList();
                                if (_checkPkey.FirstOrDefault()?.Pkey != null || _checkPkey.Count > 0)
                                {
                                    return new ResultResponseModel
                                    {
                                        Status = Model.Enums.ResultEnum.Fail,
                                        Message = $"Pkey {_newPkey} đã tồn tại. Vui lòng kiểm tra lại"
                                    };
                                }
                                var _maxcounter = db.CpnVchBOMLines.Max(x => x.Counter);
                                int _lineNo = 0;
                                var _listDataLineNo = db.CpnVchBOMLines.Where(y => y.ItemNo == _ItemNo).ToList();
                                if (_listDataLineNo == null || _listDataLineNo.Count == 0)
                                {
                                    _lineNo = 1;
                                }
                                else
                                {
                                    var _maxlineno = _listDataLineNo.Max(z => z.LineNo);
                                    _lineNo = Convert.ToInt32(_maxlineno) + 1;
                                }
                                var insertLine = new EF.Central.CpnVchBOMLine
                                {
                                    ItemNo = _ItemNo,
                                    LineNo = _lineNo,
                                    LineItemNo = item.LineItemNo,
                                    Description = string.Empty,
                                    UnitOfMeasure = string.Empty,
                                    Barcode = item.Barcode,
                                    Counter = Convert.ToInt64(_maxcounter) + 1,
                                    Pkey = _newPkey
                                };
                                db.CpnVchBOMLines.Add(insertLine);
                                db.SaveChanges();
                            }
                        }
                    }
                    else
                    {
                        if (req.IsCheckItem == true)    // Áp dụng total bill
                        {
                            if (_serialVoucher.FirstOrDefault()?.CouponCode != null || _serialVoucher.Count > 0)
                            {
                                return new ResultResponseModel
                                {
                                    Status = Model.Enums.ResultEnum.Fail,
                                    Message = $"Số serial {req.CouponCode} đã tồn tại. Vui lòng kiểm tra lại"
                                };
                            }

                            var maxCounter = db.CpnVchBOMHeaders.Max(x => x.Counter);
                            var insertHeader = new EF.Central.CpnVchBOMHeader
                            {
                                ItemNo = _ItemNo,
                                ItemName = req.ItemName,
                                UnitOfMeasure = req.UnitOfMeasure,
                                DiscountType = req.DiscountType,
                                DiscountValue = req.DiscountValue,
                                MaxAmount = req.MaxAmount,
                                ArticleType = req.ArticleType,
                                ValueOfVoucher = req.ValueOfVoucher,
                                StartingDate = req.StartingDate,
                                EndingDate = req.EndingDate,
                                Blocked = req.Blocked,
                                CouponCode = req.CouponCode,
                                LimitQty = req.LimitQty,
                                LastDateModified = req.LastDateModified,
                                IsCheckItem = req.IsCheckItem,
                                Counter = Convert.ToInt64(maxCounter) + 1,
                                Pkey = _ItemNo
                            };
                            db.CpnVchBOMHeaders.Add(insertHeader);
                            db.SaveChanges();
                        }
                        else   // Không áp dụng total bill
                        {
                            if (_serialVoucher.FirstOrDefault()?.CouponCode != null || _serialVoucher.Count > 0)
                            {
                                return new ResultResponseModel
                                {
                                    Status = Model.Enums.ResultEnum.Fail,
                                    Message = $"Số serial {req.CouponCode} đã tồn tại. Vui lòng kiểm tra lại"
                                };
                            }

                            var maxCounter = db.CpnVchBOMHeaders.Max(x => x.Counter);
                            var insertHeader = new EF.Central.CpnVchBOMHeader
                            {
                                ItemNo = _ItemNo,
                                ItemName = req.ItemName,
                                UnitOfMeasure = req.UnitOfMeasure,
                                DiscountType = req.DiscountType,
                                DiscountValue = req.DiscountValue,
                                MaxAmount = req.MaxAmount,
                                ArticleType = req.ArticleType,
                                ValueOfVoucher = req.ValueOfVoucher,
                                StartingDate = req.StartingDate,
                                EndingDate = req.EndingDate,
                                Blocked = req.Blocked,
                                CouponCode = req.CouponCode,
                                LimitQty = req.LimitQty,
                                LastDateModified = req.LastDateModified,
                                IsCheckItem = req.IsCheckItem,
                                Counter = Convert.ToInt64(maxCounter) + 1,
                                Pkey = _ItemNo
                            };
                            db.CpnVchBOMHeaders.Add(insertHeader);
                            db.SaveChanges();

                            foreach (var item in req.ListItemApply)
                            {
                                var _checkLineItemNo = db.Items.Where(x => x.No == item.LineItemNo).ToList();
                                if (_checkLineItemNo.FirstOrDefault()?.No == null || _checkLineItemNo.Count == 0)
                                {
                                    return new ResultResponseModel
                                    {
                                        Status = Model.Enums.ResultEnum.Fail,
                                        Message = $"Mã sản phẩm {item.LineItemNo} không tồn tại. Vui lòng kiểm tra lại"
                                    };
                                }
                                var _checkBarcodeNo = db.Barcodes.Where(x => x.BarcodeNo == item.Barcode).ToList();
                                if (_checkBarcodeNo.FirstOrDefault()?.BarcodeNo == null || _checkBarcodeNo.Count == 0)
                                {
                                    return new ResultResponseModel
                                    {
                                        Status = Model.Enums.ResultEnum.Fail,
                                        Message = $"Mã barcode {item.Barcode} không tồn tại. Vui lòng kiểm tra lại"
                                    };
                                }
                                var _newPkey = $"{_ItemNo}{"-"}{item.LineItemNo}{"-"}{item.Barcode}";
                                var _checkPkey = db.CpnVchBOMLines.Where(x => x.Pkey == _newPkey).ToList();
                                if (_checkPkey.FirstOrDefault()?.Pkey != null || _checkPkey.Count > 0)
                                {
                                    return new ResultResponseModel
                                    {
                                        Status = Model.Enums.ResultEnum.Fail,
                                        Message = $"Pkey {_newPkey} đã tồn tại. Vui lòng kiểm tra lại"
                                    };
                                }
                                var _maxcounter = db.CpnVchBOMLines.Max(x => x.Counter);
                                int _lineNo = 0;
                                var _listDataLineNo = db.CpnVchBOMLines.Where(y => y.ItemNo == _ItemNo).ToList();
                                if (_listDataLineNo == null || _listDataLineNo.Count == 0)
                                {
                                    _lineNo = 1;
                                }
                                else
                                {
                                    var _maxlineno = _listDataLineNo.Max(z => z.LineNo);
                                    _lineNo = Convert.ToInt32(_maxlineno) + 1;
                                }

                                var insertLine = new EF.Central.CpnVchBOMLine
                                {
                                    ItemNo = _ItemNo,
                                    LineNo = _lineNo,
                                    LineItemNo = item.LineItemNo,
                                    Description = string.Empty,
                                    UnitOfMeasure = string.Empty,
                                    Barcode = item.Barcode,
                                    Counter = Convert.ToInt64(_maxcounter) + 1,
                                    Pkey = _newPkey
                                };
                                db.CpnVchBOMLines.Add(insertLine);
                                db.SaveChanges();
                            }
                        }
                    }
                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.Success,
                        Message = "Thêm mới thành công"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }
        public ResultResponseModel CreateVoucherLine(CreateVoucherLineModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var _checkLineItemNo = db.Items.Where(z => z.No == req.LineItemNo).ToList();
                    if (_checkLineItemNo.FirstOrDefault()?.No == null || _checkLineItemNo.Count == 0)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = $"Mã sản phẩm {req.LineItemNo} không tồn tại. Vui lòng kiểm tra lại"
                        };
                    }
                    var _checkBarcodeNo = db.Barcodes.Where(c => c.BarcodeNo == req.Barcode).ToList();
                    if (_checkBarcodeNo.FirstOrDefault()?.BarcodeNo == null || _checkBarcodeNo.Count == 0)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = $"Mã barcode {req.Barcode} không tồn tại. Vui lòng kiểm tra lại"
                        };
                    }
                    var _newPkey = $"{req.ItemNo}{"-"}{req.LineItemNo}{"-"}{req.Barcode}";
                    var _pkey = db.CpnVchBOMLines.Where(x => x.Pkey == _newPkey).ToList();
                    if (_pkey.FirstOrDefault()?.Pkey != null || _pkey.Count > 0)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = $"Pkey này: {_newPkey} đã tồn tại. Vui lòng kiểm tra lại"
                        };
                    }
                    var _listData = db.CpnVchBOMLines.Select(x => x.ItemNo).ToList();
                    var _totalRow = db.CpnVchBOMLines.Where(x => x.ItemNo == req.ItemNo).ToList();
                    var _checkPkey = db.CpnVchBOMLines.Where(y => y.Pkey == req.Pkey).ToList();
                    if (_listData == null || _listData.Count == 0)
                    {
                        var insert = new EF.Central.CpnVchBOMLine
                        {
                            ItemNo = req.ItemNo,
                            LineNo = req.LineNo,
                            LineItemNo = req.LineItemNo,
                            Description = string.Empty,
                            UnitOfMeasure = req.UnitOfMeasure,
                            Barcode = req.Barcode,
                            Counter = 1,
                            Pkey = $"{req.ItemNo}{"-"}{req.LineItemNo}{"-"}{req.Barcode}"
                        };
                        db.CpnVchBOMLines.Add(insert);
                        db.SaveChanges();
                    }
                    else
                    {
                        if (_totalRow == null || _totalRow.Count == 0)
                        {
                            var _maxcounter = _totalRow.Max(x => x.Counter);
                            var insert = new EF.Central.CpnVchBOMLine
                            {
                                ItemNo = req.ItemNo,
                                LineNo = 1,
                                LineItemNo = req.LineItemNo,
                                Description = string.Empty,
                                UnitOfMeasure = req.UnitOfMeasure,
                                Barcode = req.Barcode,
                                Counter = _maxcounter + 1,
                                Pkey = $"{req.ItemNo}{"-"}{req.LineItemNo}{"-"}{req.Barcode}"
                            };
                            db.CpnVchBOMLines.Add(insert);
                            db.SaveChanges();
                        }
                        else
                        {
                            var _maxcounter = _totalRow.Max(x => x.Counter);
                            var _maxLineNo = _totalRow.Max(x => x.LineNo);
                            var insert = new EF.Central.CpnVchBOMLine
                            {
                                ItemNo = req.ItemNo,
                                LineNo = _maxLineNo + 1,
                                LineItemNo = req.LineItemNo,
                                Description = string.Empty,
                                UnitOfMeasure = req.UnitOfMeasure,
                                Barcode = req.Barcode,
                                Counter = _maxcounter + 1,
                                Pkey = $"{req.ItemNo}{"-"}{req.LineItemNo}{"-"}{req.Barcode}"
                            };
                            db.CpnVchBOMLines.Add(insert);
                            db.SaveChanges();
                        }
                    }
                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.Success,
                        Message = "Thêm mới thành công"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }
        public List<VoucherCouponHeaderListModel> GetUpdateVoucherHeaderList(string codeVoucher, string nameVoucher, string serialVoucher, string typeVoucher, int statusType, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<VoucherCouponHeaderListModel>("[dbo].[GetUpdateVoucherHeaderList] @VoucherCode, @VoucherName, @SerialVoucher, @VoucherType, @StatusType, @PageSize, @PageNumber",
                        new SqlParameter("@VoucherCode", codeVoucher ?? string.Empty),
                        new SqlParameter("@VoucherName", nameVoucher ?? string.Empty),
                        new SqlParameter("@SerialVoucher", serialVoucher ?? string.Empty),
                        new SqlParameter("@VoucherType", typeVoucher ?? string.Empty),
                        new SqlParameter("@StatusType", statusType),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<VoucherCouponHeaderListModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<VoucherCouponHeaderListModel>();
            }
        }
        public ResultResponseModel UpdateVoucherHeader(UpdateVoucherHeaderModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var _listData = db.CpnVchBOMHeaders.Where(x => x.ItemNo == req.ItemNo).ToList();
                    if (_listData == null || _listData.Count == 0)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Success,
                            Message = "Không có dữ liệu. Vui lòng kiểm tra lại"
                        };
                    }
                    else // update
                    {
                        var maxCounter = db.CpnVchBOMHeaders.Max(x => x.Counter);
                        _listData.FirstOrDefault().ItemName = req.ItemName;
                        _listData.FirstOrDefault().UnitOfMeasure = req.UnitOfMeasure;
                        _listData.FirstOrDefault().DiscountType = req.DiscountType;
                        _listData.FirstOrDefault().DiscountValue = req.DiscountValue;
                        _listData.FirstOrDefault().MaxAmount = req.MaxAmount;
                        _listData.FirstOrDefault().LimitQty = req.LimitQty;
                        _listData.FirstOrDefault().ArticleType = req.ArticleType;
                        _listData.FirstOrDefault().ValueOfVoucher = req.ValueOfVoucher;
                        _listData.FirstOrDefault().Blocked = Convert.ToByte(req.Blocked);
                        _listData.FirstOrDefault().CouponCode = req.CouponCode;
                        _listData.FirstOrDefault().IsCheckItem = req.IsCheckItem;
                        _listData.FirstOrDefault().StartingDate = req.StartingDate;
                        _listData.FirstOrDefault().EndingDate = req.EndingDate;
                        _listData.FirstOrDefault().LastDateModified = req.LastDateModified;
                        _listData.FirstOrDefault().Counter = maxCounter + 1;
                        db.SaveChanges();
                    }
                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.Success,
                        Message = "Cập nhật thành công"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }
        public ResultResponseModel DeleteVoucherHeaderList(DeleteVoucherHeaderModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.CpnVchBOMHeaders.FirstOrDefault(x => x.ItemNo == req.ItemNo);
                    if (data == null)
                    {
                        return new ResultResponseModel { Status = Model.Enums.ResultEnum.Fail, Message = "Không tìm thấy dữ liệu" };
                    }
                    db.CpnVchBOMHeaders.Remove(data);
                    db.SaveChanges();
                    return new ResultResponseModel { Status = Model.Enums.ResultEnum.Success, Message = "Xóa thành công" };
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel { Status = Model.Enums.ResultEnum.ErrorSystem, Message = ex.Message };
            }
        }

        public List<ExportVoucherHeaderListModel> ExportGetVoucherHeaderList(string codeVoucher, string nameVoucher, string serialVoucher, string typeVoucher, int statusType)
        {
            try
            {
                var data = new List<ExportVoucherHeaderListModel>();
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    data = db.Database.SqlQuery<ExportVoucherHeaderListModel>("[dbo].[GetUpdateVoucherHeaderList_Export] @VoucherCode, @VoucherName, @SerialVoucher, @VoucherType, @StatusType",
                        new SqlParameter("@VoucherCode", codeVoucher ?? string.Empty),
                        new SqlParameter("@VoucherName", nameVoucher ?? string.Empty),
                        new SqlParameter("@SerialVoucher", serialVoucher ?? string.Empty),
                        new SqlParameter("@VoucherType", typeVoucher ?? string.Empty),
                        new SqlParameter("@StatusType", statusType)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ExportVoucherHeaderListModel>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<ExportVoucherHeaderListModel>();
            }
        }

        public List<VoucherLineResponseModel> GetUpdateVoucherLineList(string noVoucher, string nameVoucher, string lineItemNo, string serialVoucher, string barCode, string typeVoucher, int statusVoucher, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<VoucherLineResponseModel>("[dbo].[GetUpdateVoucherLineList] @NoVoucher, @NameVoucher, @LineItemNo, @SerialVoucher, @Barcode, @TypeVoucher, @StatusVoucher, @PageSize, @PageNumber",
                        new SqlParameter("@NoVoucher", noVoucher ?? string.Empty),
                        new SqlParameter("@NameVoucher", nameVoucher ?? string.Empty),
                        new SqlParameter("@LineItemNo", lineItemNo ?? string.Empty),
                        new SqlParameter("@SerialVoucher", serialVoucher ?? string.Empty),
                        new SqlParameter("@Barcode", barCode ?? string.Empty),
                        new SqlParameter("@TypeVoucher", typeVoucher ?? string.Empty),
                        new SqlParameter("@StatusVoucher", statusVoucher),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<VoucherLineResponseModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<VoucherLineResponseModel>();
            }
        }

        public ResultResponseModel DeleteVoucherLineList(DeleteVoucherLineModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.CpnVchBOMLines.FirstOrDefault(x => x.Pkey == req.Pkey);
                    if (data == null)
                    {
                        return new ResultResponseModel { Status = Model.Enums.ResultEnum.Fail, Message = "Không tìm thấy dữ liệu" };
                    }

                    db.CpnVchBOMLines.Remove(data);
                    db.SaveChanges();
                    return new ResultResponseModel { Status = Model.Enums.ResultEnum.Success, Message = "Xóa thành công" };
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel { Status = Model.Enums.ResultEnum.ErrorSystem, Message = ex.Message };
            }
        }

        public ResultResponseModel UpdateVoucherLine(UpdateVoucherLineModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var _newPkey = $"{req.ItemNo}{"-"}{req.LineItemNo}{"-"}{req.Barcode}";
                    var _listData = db.CpnVchBOMLines.Where(x => x.Pkey == req.Pkey).ToList();

                    if (_listData.FirstOrDefault()?.Pkey == null || _listData.Count == 0)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Success,
                            Message = "Không có dữ liệu. Vui lòng kiểm tra lại"
                        };
                    }
                    else   // update
                    {
                        if (_newPkey == req.Pkey)
                        {
                            var maxCounter = db.CpnVchBOMLines.Max(x => x.Counter);
                            _listData.FirstOrDefault().UnitOfMeasure = string.Empty;
                            _listData.FirstOrDefault().Counter = Convert.ToInt64(maxCounter) + 1;
                            db.SaveChanges();
                        }
                        else  // Insert 
                        {
                            // Chek ma san pham, ma barcode
                            var _checkLineItemNo = db.Items.Where(x => x.No == req.LineItemNo).ToList();
                            var _checkBarCode = db.Barcodes.Where(y => y.BarcodeNo == req.Barcode).ToList();
                            if (_checkLineItemNo.FirstOrDefault()?.No == null || _checkLineItemNo.Count == 0)
                            {
                                return new ResultResponseModel
                                {
                                    Status = Model.Enums.ResultEnum.Success,
                                    Message = $"Mã sản phẩm {req.LineItemNo} này không tồn tại. Vui lòng kiểm tra lại"
                                };
                            }

                            if (_checkBarCode.FirstOrDefault()?.BarcodeNo == null || _checkBarCode.Count == 0)
                            {
                                return new ResultResponseModel
                                {
                                    Status = Model.Enums.ResultEnum.Success,
                                    Message = $"Mã Barcode {req.Barcode} này không tồn tại. Vui lòng kiểm tra lại"
                                };
                            }

                            var _maxLineNo = 0;
                            var _maxCounter = db.CpnVchBOMLines.Max(x => x.Counter);
                            var _listItemNo = db.CpnVchBOMLines.Where(x => x.ItemNo == req.ItemNo).ToList();
                            if (_listItemNo.FirstOrDefault()?.ItemNo == null || _listItemNo.Count == 0)
                            {
                                _maxLineNo = 0;
                            }
                            else
                            {
                                _maxLineNo = _listItemNo.Max(z => z.LineNo);
                            }
                            var insert = new EF.Central.CpnVchBOMLine
                            {
                                ItemNo = req.ItemNo,
                                LineNo = Convert.ToInt32(_maxLineNo) + 1,
                                LineItemNo = req.LineItemNo,
                                Description = string.Empty,
                                UnitOfMeasure = string.Empty,
                                Barcode = req.Barcode,
                                Counter = Convert.ToInt64(_maxCounter) + 1,
                                Pkey = _newPkey
                            };
                            db.CpnVchBOMLines.Add(insert);
                            db.SaveChanges();
                        }
                    }
                }
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.Success,
                    Message = "Cập nhật thành công"
                };
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }
        public List<ExportVoucherLineResponseModel> ExportGetVoucherLineList(string noVoucher, string nameVoucher, string lineItemNo, string serialVoucher, string barCode, string typeVoucher, int statusVoucher)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.Database.SqlQuery<ExportVoucherLineResponseModel>("[dbo].[GetUpdateVoucherLineList_Export] @NoVoucher, @NameVoucher, @LineItemNo, @SerialVoucher, @Barcode, @TypeVoucher, @StatusVoucher",
                       new SqlParameter("@NoVoucher", noVoucher ?? string.Empty),
                       new SqlParameter("@NameVoucher", nameVoucher ?? string.Empty),
                       new SqlParameter("@LineItemNo", lineItemNo ?? string.Empty),
                       new SqlParameter("@SerialVoucher", serialVoucher ?? string.Empty),
                       new SqlParameter("@Barcode", barCode ?? string.Empty),
                       new SqlParameter("@TypeVoucher", typeVoucher ?? string.Empty),
                       new SqlParameter("@StatusVoucher", statusVoucher)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ExportVoucherLineResponseModel>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<ExportVoucherLineResponseModel>();
            }
        }

        public List<VoucherPublishedResponseModel> GetVoucherPublishedList(DateTime fromDate, DateTime toDate, string storeNo, string voucherType, string textSearch, string statusVoucher, string sendSAP, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            try
            {
                var ipServerRead = ServerIPConnection.GetIPServerReadByStore(storeNo);
                using (var db = new CentralSalesContainer(ipServerRead))
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<VoucherPublishedResponseModel>("[dbo].[GetTransCpnVchIssueList] @FromDate, @ToDate, @StoreNo, @VoucherType, @TextSearch, @StatusVoucher, @SendSAP, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@VoucherType", voucherType ?? string.Empty),
                        new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                        new SqlParameter("@StatusVoucher", statusVoucher ?? string.Empty),
                        new SqlParameter("@SendSAP", sendSAP ?? string.Empty),
                        new SqlParameter("@Export", 1),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<VoucherPublishedResponseModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<VoucherPublishedResponseModel>();
            }
        }

        public List<ListVoucherResendModel> GetListVoucherResend(List<string> serialNo)
        {
            try
            {
                using (var db = new CentralSalesContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;

                    var result = db.TransCpnVchIssues.Where(a => serialNo.Contains(a.SerialNo) && a.IsSend == false).Select(e => new ListVoucherResendModel
                    {
                        VoucherNumber = e.SerialNo,
                        SerialNo = e.SerialNo,
                        Article_No = e.Article_No,
                        ArticleNo = e.Article_No,
                        SiteCode = e.Site,
                        Value = e.Voucher_Value,
                        ValidityFromDate = e.Validity_From_Date,
                        ExpiryDate = e.Expiry_Date,
                        BonusBuy = e.Bonus_Buy,
                        Status = e.Status,
                        ArticleType = e.ApplyType
                    }).ToList();

                    return result;
                }
            }
            catch
            {
                return new List<ListVoucherResendModel>();
            }
        }

        public ResultResponseModel UpdateStatusVoucher(List<ListVoucherResendModel> listVoucher)
        {
            try
            {
                using (var db = new CentralSalesContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    foreach (var item in listVoucher)
                    {
                        var checkItem = db.TransCpnVchIssues.FirstOrDefault(d => d.SerialNo == item.SerialNo && d.IsSend == false);
                        if (checkItem != null)
                        {
                            checkItem.IsSend = true;
                        }
                    }
                    int result = db.SaveChanges();
                    return new ResultResponseModel { Status = Model.Enums.ResultEnum.Success, Message = $"Cập nhật thành công {result} voucher" };
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel { Status = Model.Enums.ResultEnum.ErrorSystem, Message = ex.Message };
            }
        }

        public List<ExportExcelVoucherPublishedResponseModel> ExportExcelVoucherPublishedList(DateTime fromDate, DateTime toDate, string storeNo, string voucherType, string textSearch, string statusVoucher, string sendSAP)
        {
            try
            {
                var data = new List<ExportExcelVoucherPublishedResponseModel>();
                var ipServerRead = ServerIPConnection.GetIPServerReadByStore(storeNo);
                using (var db = new CentralSalesContainer(ipServerRead))
                {
                    db.Database.CommandTimeout = 2 * 60;
                    data = db.Database.SqlQuery<ExportExcelVoucherPublishedResponseModel>("[dbo].[GetTransCpnVchIssueList] @FromDate, @ToDate, @StoreNo, @VoucherType, @TextSearch, @StatusVoucher, @SendSAP, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@VoucherType", voucherType ?? string.Empty),
                        new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                        new SqlParameter("@StatusVoucher", statusVoucher ?? string.Empty),
                        new SqlParameter("@SendSAP", sendSAP ?? string.Empty),
                        new SqlParameter("@Export", '2'),
                        new SqlParameter("@PageSize", string.Empty),
                        new SqlParameter("@PageNumber", string.Empty)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ExportExcelVoucherPublishedResponseModel>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<ExportExcelVoucherPublishedResponseModel>();
            }
        }
        public List<CouponVoucherModel> GetCouponVoucherList(string CouponVoucherType, string Status, string TextSearch, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<CouponVoucherModel>("[dbo].[GET_COUPON_VOUCHER_HEADER_LIST] @VoucherType, @Status, @TexSearch, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@VoucherType", CouponVoucherType ?? string.Empty),
                        new SqlParameter("@Status", Status ?? string.Empty),
                        new SqlParameter("@TexSearch", TextSearch ?? string.Empty),
                        new SqlParameter("@Export", '1'),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<CouponVoucherModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<CouponVoucherModel>();
            }
        }

        public List<ExportExcel_VoucherCouponModel> ExportExcel_VoucherCouponList(string VoucherCouponType, string TextSearch, string Status)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;

                    var data = db.Database.SqlQuery<ExportExcel_VoucherCouponModel>("[dbo].[GET_COUPON_VOUCHER_HEADER_LIST] @VoucherType, @Status, @TexSearch, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@VoucherType", VoucherCouponType ?? string.Empty),
                        new SqlParameter("@Status", Status ?? string.Empty),
                        new SqlParameter("@TexSearch", TextSearch ?? string.Empty),
                        new SqlParameter("@Export", '2'),
                        new SqlParameter("@PageSize", string.Empty),
                        new SqlParameter("@PageNumber", string.Empty)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ExportExcel_VoucherCouponModel>();
                    }                    
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<ExportExcel_VoucherCouponModel>();
            }
        }

        public List<DetailCouponVoucherHeaderResponseModel> DetailCouponVoucherHeaderList(string itemNo)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.Database.SqlQuery<DetailCouponVoucherHeaderResponseModel>("[dbo].[GET_DETAIL_COUPON_VOUCHER_HEADER_LIST] @itemNo",
                    new SqlParameter("@itemNo", itemNo ?? string.Empty)).ToList();
                    if (data == null)
                    {
                        return new List<DetailCouponVoucherHeaderResponseModel>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<DetailCouponVoucherHeaderResponseModel>();
            }
        }

        public List<DetailCouponVoucherLineResponseModel> DetailCouponVoucherLineList(string itemNo, out int totalRecord, int skip = 0, int pageSize = 100, string SortColumn = null, string SortColumnDirection = null, string searchText = null)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<DetailCouponVoucherLineResponseModel>("[dbo].[GET_DETAIL_COUPON_VOUCHER_LINE_LIST] @ItemNo",
                    new SqlParameter("@ItemNo", itemNo ?? string.Empty)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<DetailCouponVoucherLineResponseModel>();
                    }

                    if (!string.IsNullOrEmpty(searchText))
                    {
                        data = data.Where(x => x.ApplyItemNoSAP.Contains(searchText)).ToList();
                    }

                    totalRecord = data.Count;
                    data = skip == 0 ? data.Take(pageSize).ToList() : data.Skip(skip).Take(pageSize).ToList();
                    return data.ToList();
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<DetailCouponVoucherLineResponseModel>();
            }
        }

        public List<ExportExcel_DetailCouponVoucherLineModel> ExportToExcel_DetailCouponVoucherLineList(string offerNo)
        {
            try
            {
                var listData = new List<ExportExcel_DetailCouponVoucherLineModel>();
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    listData = db.Database.SqlQuery<ExportExcel_DetailCouponVoucherLineModel>("[dbo].[PLG_GET_DETAIL_COUPON_VOUCHER_LINE_LIST_EXPORT] @OfferNo",
                    new SqlParameter("@OfferNo", offerNo ?? string.Empty)).ToList();

                    if (listData == null || listData.Count == 0)
                    {
                        return new List<ExportExcel_DetailCouponVoucherLineModel>();
                    }
                    return listData;
                }
            }
            catch (Exception ex)
            {
                return new List<ExportExcel_DetailCouponVoucherLineModel>();
            }
        }

        // 02/07/2025,tungnt8
        public List<ListVoucherResendModel> VoucherResendSAP(List<string> serialNo)
        {
            try
            {
                using (var db = new CentralSalesContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;

                    var result = db.TransCpnVchIssues.Where(a => serialNo.Contains(a.SerialNo) && a.IsSend == false).Select(e => new ListVoucherResendModel
                    {
                        VoucherNumber = e.SerialNo,
                        SerialNo = e.SerialNo,
                        Article_No = e.Article_No,
                        ArticleNo = e.Article_No,
                        SiteCode = e.Site,
                        Value = e.Voucher_Value,
                        ValidityFromDate = e.Validity_From_Date,
                        ExpiryDate = e.Expiry_Date,
                        BonusBuy = e.Bonus_Buy,
                        Status = e.Status,
                        ArticleType = e.ApplyType
                    }).ToList();

                    return result;
                }
            }
            catch
            {
                return new List<ListVoucherResendModel>();
            }
        }

        public SerialResultResendSAPModel UpdateStatusVoucherV2(List<VoucherResendSAPModel> listVoucher)
        {
            try
            {
                var listSerial = new List<ListSerialResendSAP>();
                var model = new ListSerialResendSAP();
                using (var db = new CentralSalesContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    foreach (var item in listVoucher)
                    {
                        foreach (var b in item.ListSerial)
                        {
                            var checkItem = db.TransCpnVchIssues.FirstOrDefault(d => d.SerialNo == b.SerialNumber && d.IsSend == false);
                            if (checkItem != null)
                            {
                                checkItem.IsSend = true;
                                model = new ListSerialResendSAP
                                {
                                    SerialNumber = b.SerialNumber
                                };
                            }                            
                        }
                        listSerial.Add(model);
                    }
                   
                    int result = db.SaveChanges();
                    return new SerialResultResendSAPModel
                    {
                        Status = HttpStatusCode.OK,
                        Message = $"Cập nhật thành công",
                        //Message = $"Cập nhật thành công {result} voucher",
                        Data = listSerial.ToList()
                    };
                }
            }
            catch (Exception ex)
            {
                return new SerialResultResendSAPModel
                {
                    Status = HttpStatusCode.PreconditionFailed,
                    Message = $"Có lỗi hệ thống xảy ra. Vui lòng kiểm tra lại",
                    Data = null
                };
            }
        }




    }
}
