using VCM.BLUEPOS.Model.Invoice;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using VCM.BLUEPOS.Data.EF.PLG;
using VCM.BLUEPOS.Data.EF.Central;
using VCM.BLUEPOS.Data.EF.EInvoices;
using System.Data.Entity;
using Newtonsoft.Json;
using System.Data;

namespace VCM.BLUEPOS.Data.Invoice
{
    public interface IInvoiceData
    {
        List<BranchModel> ListBranch();
        List<StoreNoseriVATModel> ListNoseriByType(string invoiceType);
        List<StoreNoseriVATModel> LoadNoseriVAT(string branchNo, string storeNo, string noseriCode, string invoiceType, string status, out int total, int skip, int take);
        bool UpdateNoseri(StoreNoseriVATModel model);
        List<VatNumberModel> LoadVATNumber(string invoiceType, string noseriCode, string invoicePattern, string serialNo, string taxCode, string status, out int total, int skip, int take);
        Tuple<bool, string> UpdateVatNumber(VatNumberModel model);

        //List<InvoiceHearderModel> LoadInvoice(InvoiceHearderRequest model);

        List<ExportInvoiceReportModel> ReportInvoiceLoad(InvoiceHearderRequest model);
        InfoInvoiceModel InfoInvoice(string siteNo);
        List<CustomerVATModel> LoadCustomer(string TenKH, string SoDT, string MST, string CongTy, out int totalRecord, int skip, int take);
        List<TransHeaderModel> ListTranHeader(string siteCode, string orderNo, string listOrderSelected, int pageSize, int pageNumber);
        List<ListOrderDetailModel> DetailListOrder(string storeNo, string OrderList);
        InvoiceByViewHeaderModel ViewInvoiceHeader(string storeNo, string OrderList);
        PrintAgainModel GetInfoByFKey(string storeNo, string fKey);
        PrintAgainModel GetInfoByKey(string storeNo, string key);
        List<ListInvoiceReplaceModel> ListOrderReplace(string storeNo, int isEOD, string key, string fKey, string typeChange);
        List<PosTerminalNoModel> GetInfoPosTerminal(string storeNo, string Key);
        Tuple<bool, string> EditEmail(string storeNo, string invoiceType, string key, string email);
        Tuple<bool, string> SendEmail(string key, string email, string storeNo);
        List<InvoiceLineModel> ListInvoiceLine(string storeNo, string DocumentNo, string TypeInvoice, string orderNo, out int recordsTotal, int skip, int take);
        List<TransHeaderModel> CheckListOrder(string listOrder, string siteCode);
        List<TransHeaderModel> CheckListOrderCreatedInvoice(string listOrder, string siteCode);
        CheckPublishInvoiceModel CheckPublishOrderInvoice(string listOrder, string storeNo);
        bool ExcuteStagingInvcoie(string orderNo, string storeNo);
        bool CreatedInvoice(string storeNo, List<InvoiceCreatedModel> listInvoice);
        List<InfoInvoiceModel> ListInvoicePattern(string siteNo, string invoiceType);
        bool CheckInvoiceNo(string storeNo, string invoiceNo, string invoicePattern, string serialNo, string noseriCode, string type);
        
        //List<BranchModel> LoadBranch(string branchNo, string branchName, string compTaxCode, out int total, int skip, int take);
        List<BranchModel> LoadBranch(string branchNo, string branchName, string compTaxCode, string type, out int total, int skip, int take);
        Tuple<bool, string> UpdateBranch(BranchModel model);
        List<InvoiceTypeModel> ListInvoiceType();
        ViewMngInvoiceModel ViewMngInvoi();
        List<InfoInvoiceModel> ListSerialByInvoiceType(string siteNo, string invoiceType);
        List<InvoiceCreatedModel> LoadCustIssueVAT(InvoiceCusIssueVATRequest model);
        InvoiceResultModel GetInfoInvoice(string storeNo, int isEOD, string key);
        InvoiceCreatedModel GetInfoVAT(string ipServer, string storeNo, string orderNo);
        Tuple<bool, string> UpdateInfoVAT(InvoiceCreatedModel model);
        DetailInvoiceWinlifeModel ViewDetailListOrder(string storeNo, string OrderList);
        BranchModel GetBranchNo(string branchNo);

        // 25/11/2024, tungnt8
        Tuple<bool, string, List<string>> CheckListInvoiceNo(string storeNo, List<string> listInvoiceNo, string invoicePattern, string serialNo, string noseriCode, string type);
        CheckInvoiceMTTModel CheckInvoiceByMTT(string storeNo);
        List<CreatedDummyMTTModel> CreatedInvoiceDummyMTT(string ipSetServer, string listInvoice);
        List<InvoiceLineModel> GetDetailInvoiceList(string storeNo, string DocumentNo, string typePublish, string TypeInvoice, string orderNo, out int recordsTotal, int skip, int take);
        List<CustomerInfoInvoiceMTTModel> GetCustomerInfoInvoiceMTT(CustomerInfoInvoiceMTTRequest model);
        List<ExportCustomerInfoInvoiceMTTModel> ExportCustomerInfoInvoiceMTT(CustomerInfoInvoiceMTTRequest model);
        List<InvoiceHearderModel> GetInvoiceList(InvoiceHearderRequest model);
        PrintAgainModel GetInfoByKeyV2(string storeNo, string key, string type);
        List<PosTerminalNoModel> GetInfoPosTerminalV2(string storeNo, string Key, string Type);
        InvoiceResultModel GetInfoInvoiceV2(string storeNo, int isEOD, string key, string Type);
        List<InvoiceCreatedModel> LoadCustIssueVATV2(InvoiceCusIssueVATRequest model);



    }


    public class InvoiceData : IInvoiceData
    {
        public List<BranchModel> ListBranch()
        {
            var result = new List<BranchModel>();
            using (var db = new EInvoicePLHContainer())
            {
                try
                {
                    result = db.Branches
                              .Select(a => new BranchModel
                              {
                                  BranchNo = a.BranchNo,
                                  BranchName = a.BranchName,
                                  Address = a.Address,
                                  CompTaxCode = a.CompTaxCode,
                                  ETaxAccName = a.ETaxAccName,
                                  ETaxAccPass = a.ETaxAccPass,
                                  ETaxWSName = a.ETaxWSName,
                                  ETaxWSPass = a.ETaxWSPass
                              }).ToList();
                }
                catch (Exception ex)
                {
                }
                return result;
            }
        }
        public List<StoreNoseriVATModel> ListNoseriByType(string invoiceType)
        {
            var result = new List<StoreNoseriVATModel>();
            using (var db = new EInvoicePLHContainer())
            {
                try
                {
                    var data = (from a in db.StoreNoseriVATs
                               join b in db.Branches on a.BranchNo equals b.BranchNo
                               where a.Enabled == true
                               select new StoreNoseriVATModel
                               {
                                   InvoiceType = a.InvoiceType,
                                   CompTaxCode = b.CompTaxCode,
                                   NoseriCode = a.NoseriCode
                               }).ToList();
                    if (!string.IsNullOrEmpty(invoiceType))
                    {
                        data = data.Where(m => m.InvoiceType == invoiceType).ToList();
                    }
                    result = data.Select(a => new StoreNoseriVATModel
                    {
                        NoseriCode = a.NoseriCode,
                        CompTaxCode = a.CompTaxCode,
                        InvoiceType = a.InvoiceType
                    }).Distinct().ToList();
                }
                catch (Exception ex)
                {
                }
                return result;
            }
        }

        public List<StoreNoseriVATModel> LoadNoseriVAT(string branchNo, string storeNo, string noseriCode, string invoiceType, string status, out int total, int skip, int take)
        {

            using (var db = new EInvoicePLHContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 120;
                    var data = db.StoreNoseriVATs.Select(a => new StoreNoseriVATModel
                    {
                        ID = a.ID,
                        StoreNo = a.StoreNo,
                        StoreName = a.StoreName,
                        BranchNo = a.BranchNo,
                        NoseriCode = a.NoseriCode,
                        // CompTaxCode = a.CompTaxCode,
                        InvoiceType = a.InvoiceType,
                        Enabled = a.Enabled,
                        CreateDate = a.CreateDate,
                        LastUpdate = a.LastUpdate

                    });
                    if (!string.IsNullOrEmpty(branchNo))
                    {
                        data = data.Where(m => m.BranchNo == branchNo);
                    }
                    if (!string.IsNullOrEmpty(storeNo))
                    {
                        data = data.Where(m => m.StoreNo.ToLower().Contains(storeNo.ToLower()));
                    }
                    if (!string.IsNullOrEmpty(noseriCode))
                    {
                        data = data.Where(m => m.NoseriCode.ToLower().Contains(noseriCode.ToLower()));
                    }
                    if (!string.IsNullOrEmpty(invoiceType))
                    {
                        data = data.Where(m => m.InvoiceType.ToLower().Contains(invoiceType.ToLower()));
                    }

                    if (!string.IsNullOrEmpty(status))
                    {
                        var enable = status == "1" ? true : false;
                        data = data.Where(m => m.Enabled == enable);
                    }

                    total = data.Count();
                    var result = data.OrderByDescending(d => d.CreateDate).Skip(skip).Take(take).ToList();
                    return result;
                }
                catch (Exception ex)
                {
                    total = 0;
                }
                return new List<StoreNoseriVATModel>();
            }
        }

        public bool UpdateNoseri(StoreNoseriVATModel model)
        {
            var result = true;
            using (var db = new EInvoicePLHContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 120;
                    var checkNoseri = db.StoreNoseriVATs.FirstOrDefault(d => d.StoreNo == model.StoreNo && d.InvoiceType == model.InvoiceType);
                    if (checkNoseri != null)
                    {
                        checkNoseri.BranchNo = model.BranchNo;
                        checkNoseri.ObjectID = model.StoreNo;
                        checkNoseri.StoreName = model.StoreName;
                        checkNoseri.NoseriCode = model.NoseriCode;
                        checkNoseri.Enabled = model.Enabled;
                        checkNoseri.LastUpdate = model.LastUpdate;
                    }
                    else
                    {
                        var insert = new EF.EInvoices.StoreNoseriVAT
                        {
                            StoreNo = model.StoreNo,
                            ObjectID = model.StoreNo,
                            BranchNo = model.BranchNo,
                            NoseriCode = model.NoseriCode,
                            InvoiceType = model.InvoiceType,
                            CustNameDefault = $"{"KHÁCH LẺ"}{"-"}{model.StoreName}",   // default
                            Enabled = model.Enabled,
                            CreateDate = model.CreateDate,
                            LastUpdate = model.LastUpdate,
                            Remark = "SAP"   // default
                        };
                        db.StoreNoseriVATs.Add(insert);
                    }
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    result = false;
                }
            }
            return result;
        }


        public Tuple<bool, string> UpdateVatNumber(VatNumberModel model)
        {
            var result = new Tuple<bool, string>(false, "");
            using (var db = new CentralGeneralContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 120;
                    var checkNoseri = db.VatNumbers.FirstOrDefault(d => d.NoseriCode == model.NoseriCode && d.CompTaxCode == model.CompTaxCode);

                    if (checkNoseri != null)
                    {
                        if (checkNoseri.Enabled == true && model.Enabled == true)
                        {
                            result = new Tuple<bool, string>(false, $"Đã tồn tại {model.CompTaxCode}, {model.NoseriCode}, có hiệu lực trong hệ thống");
                            return result;
                        }
                        checkNoseri.InvoicePattern = model.InvoicePattern;
                        checkNoseri.SerialNo = model.SerialNo;
                        checkNoseri.Enabled = model.Enabled;
                        checkNoseri.LastUpdate = model.LastUpdate;
                        db.SaveChanges();
                        result = new Tuple<bool, string>(true, "Cập nhật trạng thái dãi hóa đơn thành công");
                    }
                    else
                    {
                        var insert = new EF.Central.VatNumber
                        {
                            NoseriCode = model.NoseriCode,
                            InvoicePattern = model.InvoicePattern,
                            SerialNo = model.SerialNo,
                            CompTaxCode = model.CompTaxCode,
                            StartNumber = model.StartNumber,
                            EndNumber = model.EndNumber,
                            Enabled = model.Enabled,
                            CreateDate = model.CreateDate,
                            LastUpdate = model.LastUpdate
                        };
                        db.VatNumbers.Add(insert);
                        db.SaveChanges();
                        result = new Tuple<bool, string>(true, "Thêm mới dãi hóa đơn thành công");
                    }
                }
                catch (Exception ex)
                {
                    result = new Tuple<bool, string>(false, $"{ex.Message}");
                }
            }

            using (var db = new EInvoicePLHContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 120;
                    var checkNoseri = db.VatNumberFPTs.FirstOrDefault(d => d.NoseriCode == model.NoseriCode && d.CompTaxCode == model.CompTaxCode);
                    if (checkNoseri != null)
                    {
                        if (checkNoseri.Enabled == true && model.Enabled == true)
                        {
                            result = new Tuple<bool, string>(false, $"Đã tồn tại {model.CompTaxCode}, {model.NoseriCode}, có hiệu lực trong hệ thống");
                            return result;
                        }

                        checkNoseri.InvoicePattern = model.InvoicePattern;
                        checkNoseri.SerialNo = model.SerialNo;
                        checkNoseri.Enabled = model.Enabled;
                        checkNoseri.LastUpdate = model.LastUpdate;
                        db.SaveChanges();

                        //result = new Tuple<bool, string>(true, "Cập nhật trạng thái dãi hóa đơn thành công");
                    }
                    else
                    {
                        var insert = new EF.EInvoices.VatNumberFPT
                        {
                            NoseriCode = model.NoseriCode,
                            InvoicePattern = model.InvoicePattern,
                            SerialNo = model.SerialNo,
                            CompTaxCode = model.CompTaxCode,
                            StartNumber = model.StartNumber,
                            EndNumber = model.EndNumber,
                            Enabled = model.Enabled,
                            CreateDate = model.CreateDate,
                            LastUpdate = model.LastUpdate
                        };

                        db.VatNumberFPTs.Add(insert);
                        db.SaveChanges();

                        //result = new Tuple<bool, string>(true, "Thêm mới dãi hóa đơn thành công");
                    }
                }
                catch (Exception ex)
                {
                    result = new Tuple<bool, string>(false, $"{ex.Message}");
                }
            }

            return result;
        }

        public List<VatNumberModel> LoadVATNumber(string invoiceType, string noseriCode, string invoicePattern, string serialNo, string taxCode, string status, out int total, int skip, int take)
        {
            using (var db = new EInvoicePLHContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 120;
                    var data = from a in db.VatNumberFPTs
                               // join b in db.StoreNoseriVATs on a.NoseriCode equals b.NoseriCode
                               join c in db.Branches on a.CompTaxCode equals c.CompTaxCode
                               select new VatNumberModel
                               {
                                   ID = a.ID,
                                   //InvoiceType = b.InvoiceType,
                                   BranchName = c.BranchName,
                                   NoseriCode = a.NoseriCode,
                                   InvoicePattern = a.InvoicePattern,
                                   SerialNo = a.SerialNo,
                                   CompTaxCode = a.CompTaxCode,
                                   StartNumber = a.StartNumber,
                                   EndNumber = a.EndNumber,
                                   Enabled = a.Enabled,
                                   CreateDate = a.CreateDate,
                                   LastUpdate = a.LastUpdate

                               };
                    if (!string.IsNullOrEmpty(invoiceType))
                    {
                        data = data.Where(m => m.NoseriCode.Contains(invoiceType));
                    }
                    if (!string.IsNullOrEmpty(taxCode))
                    {
                        data = data.Where(m => m.CompTaxCode.Contains(taxCode));
                    }
                    if (!string.IsNullOrEmpty(noseriCode))
                    {
                        data = data.Where(m => m.NoseriCode.ToLower().Contains(noseriCode.ToLower()));
                    }
                    if (!string.IsNullOrEmpty(invoicePattern))
                    {
                        data = data.Where(m => m.InvoicePattern.ToLower().Contains(invoicePattern.ToLower()));
                    }
                    if (!string.IsNullOrEmpty(serialNo))
                    {
                        data = data.Where(m => m.SerialNo.ToLower().Contains(serialNo.ToLower()));
                    }

                    if (!string.IsNullOrEmpty(status))
                    {
                        var enable = status == "1" ? true : false;
                        data = data.Where(m => m.Enabled == enable);
                    }

                    total = data.Count();
                    var result = data.OrderByDescending(d => d.CreateDate).Skip(skip).Take(take).ToList();
                    return result;
                }
                catch (Exception ex)
                {
                    total = 0;
                }
                return new List<VatNumberModel>();
            }
        }

        // 06/12/2024,tungnt8: Quan ly hoa don
        public List<InvoiceHearderModel> GetInvoiceList(InvoiceHearderRequest model)
        {
            try
            {
                if (model.Type == "GTGT")
                {
                    using (var db = new EInvoicePLHContainer())
                    {
                        db.Database.CommandTimeout = 2 * 60;
                        var result = db.Database.SqlQuery<InvoiceHearderModel>("[dbo].[Get_Invoice_List] @ListStoreJson, @TuNgay,@DenNgay,@MauSo,@SoHD,@TenKH,@DienThoai,@TenCTy,@KyHieu,@LoaiHoaDon,@MST,@HinhThuc,@IsSigned,@StatusCQT,@Export,@PageSize,@PageNumber",
                        new SqlParameter("@ListStoreJson", model.SiteNo),
                        new SqlParameter("@TuNgay", model.TuNgay),
                        new SqlParameter("@DenNgay", model.DenNgay),
                        new SqlParameter("@MauSo", model.MauSo),
                        new SqlParameter("@SoHD", model.SoHD),
                        new SqlParameter("@TenKH", model.TenKH),
                        new SqlParameter("@DienThoai", model.DienThoai),
                        new SqlParameter("@TenCTy", model.TenCTy),
                        new SqlParameter("@KyHieu", model.KyHieu),
                        new SqlParameter("@LoaiHoaDon", model.LoaiHoaDon),
                        new SqlParameter("@MST", model.MST),
                        new SqlParameter("@HinhThuc", model.HinhThuc),
                        new SqlParameter("@IsSigned", model.IsSigned),
                        new SqlParameter("@StatusCQT", model.StatusCQT),
                        new SqlParameter("@Export", model.Export),
                        new SqlParameter("@PageSize", model.PageSize),
                        new SqlParameter("@PageNumber", model.PageNumber)).ToList();

                        return result;
                    }
                }
                else // Type == "MTT" : hoa don may tinh tien
                {
                    using (var db1 = new EInvoiceContainer())
                    {
                        db1.Database.CommandTimeout = 2 * 60;
                        var resultMTT = db1.Database.SqlQuery<InvoiceHearderModel>("[dbo].[Cash_GetInvoice_List] @ListStoreJson,@TuNgay,@DenNgay,@MauSo,@SoHD,@TenKH,@DienThoai,@TenCTy,@KyHieu,@LoaiHoaDon,@MST,@HinhThuc,@IsSigned,@Export,@PageSize,@PageNumber",
                            new SqlParameter("@ListStoreJson", model.SiteNo),
                            new SqlParameter("@TuNgay", model.TuNgay),
                            new SqlParameter("@DenNgay", model.DenNgay),
                            new SqlParameter("@MauSo", model.MauSo),
                            new SqlParameter("@SoHD", model.SoHD),
                            new SqlParameter("@TenKH", model.TenKH),
                            new SqlParameter("@DienThoai", model.DienThoai),
                            new SqlParameter("@TenCTy", model.TenCTy),
                            new SqlParameter("@KyHieu", model.KyHieu),
                            new SqlParameter("@LoaiHoaDon", model.LoaiHoaDon),
                            new SqlParameter("@MST", model.MST),
                            new SqlParameter("@HinhThuc", model.HinhThuc),
                            new SqlParameter("@IsSigned", model.IsSigned),
                            new SqlParameter("@Export", model.Export),
                            new SqlParameter("@PageSize", model.PageSize),
                            new SqlParameter("@PageNumber", model.PageNumber)).ToList();

                        return resultMTT;
                    }
                }                
            }
            catch (Exception ex)
            {
                return new List<InvoiceHearderModel>();
            }
        }

        // backup : 06/12/2024

        //public List<InvoiceHearderModel> LoadInvoice(InvoiceHearderRequest model)
        //{
        //    try
        //    {
        //        if (model.Type == "GTGT")
        //        {
        //            using (var db = new EInvoicePLHContainer())
        //            {
        //                db.Database.CommandTimeout = 2 * 60;
        //                var result = db.Database.SqlQuery<InvoiceHearderModel>("[dbo].[SP_QUANLYHOADON_LOAD] @ListStoreJson, @TuNgay,@DenNgay,@MauSo,@SoHD,@TenKH,@DienThoai,@TenCTy,@KyHieu,@LoaiHoaDon,@MST,@HinhThuc,@IsSigned,@StatusCQT,@PageSize,@PageNumber",
        //                new SqlParameter("@ListStoreJson", model.SiteNo),
        //                new SqlParameter("@TuNgay", model.TuNgay),
        //                new SqlParameter("@DenNgay", model.DenNgay),
        //                new SqlParameter("@MauSo", model.MauSo),
        //                new SqlParameter("@SoHD", model.SoHD),
        //                new SqlParameter("@TenKH", model.TenKH),
        //                new SqlParameter("@DienThoai", model.DienThoai),
        //                new SqlParameter("@TenCTy", model.TenCTy),
        //                new SqlParameter("@KyHieu", model.KyHieu),
        //                new SqlParameter("@LoaiHoaDon", model.LoaiHoaDon),
        //                new SqlParameter("@MST", model.MST),
        //                new SqlParameter("@HinhThuc", model.HinhThuc),
        //                new SqlParameter("@IsSigned", model.IsSigned),
        //                new SqlParameter("@StatusCQT", model.StatusCQT),
        //                new SqlParameter("@PageSize", model.PageSize),
        //                new SqlParameter("@PageNumber", model.PageNumber)).ToList();
        //                return result;

        //            }
        //        }
        //        else // Type == "MTT" : hoa don may tinh tien
        //        {
        //            using (var db1 = new EInvoiceContainer())
        //            {
        //                db1.Database.CommandTimeout = 2 * 60;
        //                var resultMTT = db1.Database.SqlQuery<InvoiceHearderModel>("[dbo].[Cash_GetInvoiceList] @ListStoreJson,@TuNgay,@DenNgay,@MauSo,@SoHD,@TenKH,@DienThoai,@TenCTy,@KyHieu,@LoaiHoaDon,@MST,@HinhThuc,@IsSigned,@PageSize,@PageNumber",
        //                    new SqlParameter("@ListStoreJson", model.SiteNo),
        //                    new SqlParameter("@TuNgay", model.TuNgay),
        //                    new SqlParameter("@DenNgay", model.DenNgay),
        //                    new SqlParameter("@MauSo", model.MauSo),
        //                    new SqlParameter("@SoHD", model.SoHD),
        //                    new SqlParameter("@TenKH", model.TenKH),
        //                    new SqlParameter("@DienThoai", model.DienThoai),
        //                    new SqlParameter("@TenCTy", model.TenCTy),
        //                    new SqlParameter("@KyHieu", model.KyHieu),
        //                    new SqlParameter("@LoaiHoaDon", model.LoaiHoaDon),
        //                    new SqlParameter("@MST", model.MST),
        //                    new SqlParameter("@HinhThuc", model.HinhThuc),
        //                    new SqlParameter("@IsSigned", model.IsSigned),
        //                    new SqlParameter("@PageSize", model.PageSize),
        //                    new SqlParameter("@PageNumber", model.PageNumber)).ToList();

        //                return resultMTT;
        //            }
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        return new List<InvoiceHearderModel>();
        //    }
        //}

        public List<ExportInvoiceReportModel> ReportInvoiceLoad(InvoiceHearderRequest model)
        {
            try
            {
                using (var db = new PLGContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    //totalRecord = 0;
                    var data = db.Database.SqlQuery<ExportInvoiceReportModel>("[dbo].[SP_BAOCAO_HOADON_DIENTU] @SiteNo, @TuNgay, @DenNgay, @MauSo,@SoHD, @TenKH, @DienThoai, @TenCTy, @KyHieu, @LoaiHoaDon, @MST, @HinhThuc,@PageSize,@PageNumber",
                        new SqlParameter("@SiteNo", model.SiteNo ?? string.Empty),
                        new SqlParameter("@TuNgay", model.TuNgay),
                        new SqlParameter("@DenNgay", model.DenNgay),
                        new SqlParameter("@MauSo", model.MauSo ?? string.Empty),
                        new SqlParameter("@SoHD", model.SoHD ?? string.Empty),
                        new SqlParameter("@TenKH", model.TenKH ?? string.Empty),
                        new SqlParameter("@DienThoai", model.DienThoai ?? string.Empty),
                        new SqlParameter("@TenCTy", model.TenCTy ?? string.Empty),
                        new SqlParameter("@KyHieu", model.KyHieu ?? string.Empty),
                        new SqlParameter("@LoaiHoaDon", model.LoaiHoaDon ?? string.Empty),
                        new SqlParameter("@MST", model.MST ?? string.Empty),
                        new SqlParameter("@HinhThuc", model.HinhThuc ?? string.Empty),
                        new SqlParameter("@PageSize", model.PageSize),
                        new SqlParameter("@PageNumber", model.PageNumber)
                        ).ToList();
                    return data;
                }

            }
            catch (Exception ex)
            {
                return new List<ExportInvoiceReportModel>();
            }
        }

        public InfoInvoiceModel InfoInvoice(string siteNo)
        {
            try
            {
                using (var db = new EInvoicePLHContainer())
                {
                    var data = new InfoInvoiceModel();
                    db.Database.CommandTimeout = 2 * 60;
                    data = (from a in db.StoreNoseriVATs
                            join b in db.VatNumberFPTs on a.NoseriCode equals b.NoseriCode
                            where a.StoreNo == siteNo && a.InvoiceType == "NOR"
                            select new InfoInvoiceModel
                            {
                                InvoicePattern = b.InvoicePattern,
                                SerialNo = b.SerialNo,
                                LastUsed = b.LastUsed
                            }).FirstOrDefault();
                    return data;
                }
            }
            catch
            {
                return (default(InfoInvoiceModel));
            }
        }
        public List<CustomerVATModel> LoadCustomer(string TenKH, string SoDT, string MST, string CongTy, out int totalRecord, int skip, int take)
        {
            try
            {
                var result = new List<CustomerVATModel>();
                using (var db = new CentralGeneralContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.CustomerVATs.AsNoTracking().AsEnumerable();

                    if (!string.IsNullOrEmpty(TenKH))
                    {
                        data = data.Where(d => d.CustomerName.ToLower().Contains(TenKH.ToLower()));
                    }
                    if (!string.IsNullOrEmpty(SoDT))
                    {
                        data = data.Where(d => d.PhoneNumber == SoDT);
                    }
                    if (!string.IsNullOrEmpty(MST))
                    {
                        data = data.Where(d => d.TaxCode == MST);
                    }
                    if (!string.IsNullOrEmpty(CongTy))
                    {
                        data = data.Where(d => d.CompanyName.ToLower().Contains(CongTy.ToLower()));
                    }

                    var temp = data.GroupBy(a =>
                        new
                        {
                            CompanyName = a.CompanyName,
                            CustomerName = a.CustomerName,
                            PhoneNumber = a.PhoneNumber,
                            TaxCode = a.TaxCode,
                            Address = a.Address,
                            Email = a.Email
                        })
                        .Select(a => new CustomerVATModel
                        {
                            CompanyName = a.FirstOrDefault().CompanyName,
                            CustomerName = a.FirstOrDefault().CustomerName,
                            PhoneNumber = a.FirstOrDefault().PhoneNumber,
                            TaxCode = a.FirstOrDefault().TaxCode,
                            Address = a.FirstOrDefault().Address,
                            Email = a.FirstOrDefault().Email
                        }).Distinct().ToList();

                    totalRecord = temp.Count();
                    result = temp.Skip(skip).Take(take).ToList();
                    return result;
                }
            }
            catch (Exception)
            {
                totalRecord = 0;
                return (default(List<CustomerVATModel>));
            }
        }

        public List<TransHeaderModel> ListTranHeader(string siteCode, string orderNo, string listOrderSelected, int pageSize, int pageNumber)
        {
            try
            {
                var ipServerRead = ServerIPConnection.GetIPServerByStore(siteCode);
                using (var db = new EInvoiceContainer(ipServerRead))
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var result = db.Database.SqlQuery<TransHeaderModel>("[dbo].[SP_LIST_ORDER_TRANSHEADER] @SITECODE,@OrderNo,@ListOrderSelected,@PageSize,@PageNumber ",
                        new SqlParameter("@SITECODE", siteCode),
                        new SqlParameter("@OrderNo", orderNo),
                        new SqlParameter("@ListOrderSelected", listOrderSelected),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageNumber)).ToList();

                    if (result != null)
                        return result;
                    return (default(List<TransHeaderModel>));
                }
            }
            catch (Exception Ex)
            {
                return (default(List<TransHeaderModel>));
            }
        }

        public List<ListOrderDetailModel> DetailListOrder(string storeNo, string OrderList)
        {
            try
            {
                var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
                using (var db = new EInvoiceContainer(ipServer))
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.Database.SqlQuery<ListOrderDetailModel>("[dbo].[PublishInvoiceByViewDetail] @OrderList ",
                        new SqlParameter("@OrderList", OrderList)
                        ).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return default(List<ListOrderDetailModel>);
            }
        }

        public InvoiceByViewHeaderModel ViewInvoiceHeader(string storeNo, string OrderList)
        {
            try
            {
                var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
                using (var db = new EInvoiceContainer(ipServer))
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.Database.SqlQuery<InvoiceByViewHeaderModel>("[dbo].[PublishInvoiceByViewHeader] @OrderList",
                        new SqlParameter("@OrderList", OrderList)).FirstOrDefault();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return default(InvoiceByViewHeaderModel);
            }
        }

        public PrintAgainModel GetInfoByFKey(string storeNo, string fKey)
        {
            try
            {
                var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);

                //using (var db = new EInvoiceContainer(ipServer))
                //{
                //    db.Database.CommandTimeout = 2 * 60;
                //    var invoiceNormal = db.InvoiceHeaderNormals.AsEnumerable()
                //        .Where(d => d.Fkey == fKey && d.InvoiceType == "DTT")
                //        .Select(a => new PrintAgainModel
                //        {
                //            InvoiceNo = a.InvoiceNo,
                //            InvoicePattern = a.InvoicePattern,
                //            SerialNo = a.SerialNo,
                //            ArisingDate = a.ArisingDate,
                //            CusCode = a.CusCode,
                //            CusName = a.CusName,
                //            CusCom = a.CusCom,
                //            CusAddress = a.CusAddress,
                //            CusTaxCode = a.CusTaxCode,
                //            CusPhone = a.CusPhone,
                //            EmailDeliver = a.EmailDeliver,
                //            IsAllow = a.IsAllow,
                //            IsCancel = a.IsCancel,
                //            IsSigned = a.IsSigned,
                //            IsDummy = a.IsDummy,
                //            IsEOD = a.IsEOD,
                //            InvoiceType = a.InvoiceType,
                //            FKey = a.Fkey,
                //            Key = a.Key,
                //            HinhThuc = "Normal"
                //        }).FirstOrDefault();

                //    var invoiceEod = db.InvoiceHeaderEODs.AsEnumerable()
                //        .Where(d => d.Fkey == fKey && d.InvoiceType == "DTT")
                //        .Select(a => new PrintAgainModel
                //        {
                //            InvoiceNo = a.InvoiceNo,
                //            InvoicePattern = a.InvoicePattern,
                //            SerialNo = a.SerialNo,
                //            ArisingDate = a.ArisingDate,
                //            CusCode = a.CusCode,
                //            CusName = a.CusName,
                //            CusCom = a.CusCom,
                //            CusAddress = a.CusAddress,
                //            CusTaxCode = a.CusTaxCode,
                //            CusPhone = a.CusPhone,
                //            EmailDeliver = a.EmailDeliver,
                //            IsAllow = a.IsAllow,
                //            IsCancel = a.IsCancel,
                //            IsSigned = a.IsSigned,
                //            IsDummy = a.IsDummy,
                //            IsEOD = a.IsEOD,
                //            InvoiceType = a.InvoiceType,
                //            FKey = a.Fkey,
                //            Key = a.Key,
                //            HinhThuc = "EOD"
                //        }).FirstOrDefault();

                //    if (invoiceNormal != null)
                //        return invoiceNormal;
                //    if (invoiceEod != null)
                //        return invoiceEod;

                //   return new PrintAgainModel();
                //}

                using (var db = new EInvoicePLHContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var result = db.Database.SqlQuery<PrintAgainModel>("[dbo].[SP_GETINFO_BYFKEY] @FKEY ",
                    new SqlParameter("@FKEY", fKey)).FirstOrDefault();
                    if (result != null)
                        return result;
                    return new PrintAgainModel();
                }
            }
            catch
            {
                return new PrintAgainModel();
            }
        }
        public PrintAgainModel GetInfoByKey(string storeNo, string key)
        {
            try
            {
                using (var db = new EInvoicePLHContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var result = db.Database.SqlQuery<PrintAgainModel>("[dbo].[SP_GETINFO_BYKEY] @KEY ",
                    new SqlParameter("@KEY", key)).FirstOrDefault();
                    if (result != null)
                        return result;
                    return new PrintAgainModel();
                }
            }
            catch
            {
                return new PrintAgainModel();
            }
        }

        // 16/12/2024,tungnt8
        public PrintAgainModel GetInfoByKeyV2(string storeNo, string key, string type)
        {
            try
            {
                if (type == "GTGT")
                {
                    using (var db = new EInvoicePLHContainer())  // prd = 10.235.25.101/EInvoice
                    {
                        db.Database.CommandTimeout = 2 * 60;
                        var result = db.Database.SqlQuery<PrintAgainModel>("[dbo].[SP_GETINFO_BYKEY] @KEY",
                        new SqlParameter("@KEY", key)).FirstOrDefault();
                        if (result != null)
                            return result;
                        return new PrintAgainModel();
                    }
                }
                else // type = MTT
                {
                    using (var db1 = new EInvoiceContainer())  // prd = 10.235.25.103/EInvoice
                    {
                        db1.Database.CommandTimeout = 2 * 60;
                        var result = db1.Database.SqlQuery<PrintAgainModel>("[dbo].[Cash_Get_Info_By_Key] @Key",
                        new SqlParameter("@Key", key)).FirstOrDefault();
                        if (result != null)
                            return result;
                        return new PrintAgainModel();
                    }
                }             
            }
            catch
            {
                return new PrintAgainModel();
            }
        }

        ////--- 10/12/2024, tungnt8
        //public PrintAgainModel GetInfoByKeyV2(string storeNo, string key, string type)
        //{
        //    try
        //    {
        //        if (type == "GTGT")
        //        {
        //            using (var db = new EInvoicePLHContainer()) // // prd : 10.235.25.101
        //            {
        //                db.Database.CommandTimeout = 2 * 60;
        //                var result = db.Database.SqlQuery<PrintAgainModel>("[dbo].[SP_GETINFO_BYKEY] @KEY",
        //                new SqlParameter("@KEY", key)).FirstOrDefault();
        //                if (result != null)
        //                    return result;
        //                return new PrintAgainModel();
        //            }
        //        }
        //        else // type = "MTT"
        //        {
        //            using (var db = new EInvoiceContainer()) // prd : 10.235.25.103
        //            {
        //                db.Database.CommandTimeout = 2 * 60;
        //                var result = db.Database.SqlQuery<PrintAgainModel>("[dbo].[Cash_GetInfo_By_Key] @Key",
        //                new SqlParameter("@Key", key)).FirstOrDefault();
        //                if (result != null)
        //                    return result;
        //                return new PrintAgainModel();
        //            }
        //        }                
        //    }
        //    catch
        //    {
        //        return new PrintAgainModel();
        //    }
        //}
        public List<ListInvoiceReplaceModel> ListOrderReplace(string storeNo, int isEOD, string key, string fKey, string typeChange)
        {
            try
            {
                //var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
                //using (var db = new EInvoiceContainer(ipServer))
                //{
                //    db.Database.CommandTimeout = 2 * 60;
                //    var result = db.Database.SqlQuery<ListInvoiceReplaceModel>("[dbo].[SP_LIST_ORDER_REPLACE] @ISEOD, @KEY, @FKEY, @TYPECHANGE ",
                //        new SqlParameter("@ISEOD", isEOD),
                //        new SqlParameter("@KEY", key),
                //        new SqlParameter("@FKEY", fKey),
                //        new SqlParameter("@TYPECHANGE", typeChange)
                //).ToList();
                //    return result;
                //}

                using (var db = new EInvoicePLHContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var result = db.Database.SqlQuery<ListInvoiceReplaceModel>("[dbo].[SP_LIST_ORDER_REPLACE] @ISEOD, @KEY, @FKEY, @TYPECHANGE ",
                        new SqlParameter("@ISEOD", isEOD),
                        new SqlParameter("@KEY", key),
                        new SqlParameter("@FKEY", fKey),
                        new SqlParameter("@TYPECHANGE", typeChange)
                        ).ToList();

                    return result;
                }
            }
            catch (Exception ex)
            {
                return (default(List<ListInvoiceReplaceModel>));
            }
        }
        public List<PosTerminalNoModel> GetInfoPosTerminal(string storeNo, string Key)
        {
            try
            {
                var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
                using (var db = new EInvoiceContainer(ipServer))
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var result = db.Database.SqlQuery<PosTerminalNoModel>("[dbo].[SP_GET_SITENO_TERMINAL_BY_KEY] @KEY",
                    new SqlParameter("@KEY", Key)).ToList();
                    if (result != null)
                        return result;
                    return new List<PosTerminalNoModel>();
                }
            }
            catch (Exception)
            {
                return (default(List<PosTerminalNoModel>));
            }
        }

        // 16/12/2024, tungnt8
        public List<PosTerminalNoModel> GetInfoPosTerminalV2(string storeNo, string Key, string Type)
        {
            try
            {
                if (Type == "GTGT")
                {
                    
                    using (var db = new CentralGeneralContainer())  // prd = 10.235.25.101/General...
                    {
                        db.Database.CommandTimeout = 2 * 60;
                        var result = db.Database.SqlQuery<PosTerminalNoModel>("[dbo].[SP_GET_STORE_POSTERMINAL_BY_KEY] @KEY",
                        new SqlParameter("@KEY", Key)).ToList();
                        if (result != null)
                            return result;
                        return new List<PosTerminalNoModel>();
                    }
                }
                else // Type = "MTT"
                {
                    using (var db = new EInvoiceContainer()) // prd = 10.235.25.103/Envoice
                    {
                        db.Database.CommandTimeout = 2 * 60;
                        var result = db.Database.SqlQuery<PosTerminalNoModel>("[dbo].[Cash_Get_Store_Pos_By_Key] @Key",
                        new SqlParameter("@Key", Key)).ToList();
                        if (result != null)
                            return result;
                        return new List<PosTerminalNoModel>();
                    }
                }
               
            }
            catch (Exception)
            {
                return (default(List<PosTerminalNoModel>));
            }
        }
        public Tuple<bool, string> EditEmail(string storeNo, string invoiceType, string key, string email)
        {
            var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
            using (var db = new EInvoiceContainer(ipServer))
            {
                db.Database.CommandTimeout = 2 * 60;
                try
                {
                    if (invoiceType == "NOR")
                    {
                        var item = db.InvoiceHeaderNormals.FirstOrDefault(d => d.Key == key.Trim());
                        if (item != null)
                        {
                            item.EmailDeliver = email;
                        }
                    }
                    else if (invoiceType == "EOD")
                    {
                        var item = db.InvoiceHeaderEODs.FirstOrDefault(d => d.Key == key.Trim());
                        if (item != null)
                        {
                            item.EmailDeliver = email;
                        }
                    }
                    else
                    {
                        return new Tuple<bool, string>(false, "Không lấy được loại Invoice");
                    }

                    db.SaveChanges();
                    return new Tuple<bool, string>(true, "Chỉnh sửa thành công");
                }
                catch (Exception ex)
                {
                    return new Tuple<bool, string>(false, $"{ex.Message}");
                }
            }
        }

        public Tuple<bool, string> SendEmail(string key, string email, string storeNo)
        {
            try
            {
                //var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
                //using (var db = new EInvoiceContainer(ipServer))
                //{
                //    db.Database.CommandTimeout = 2 * 60;
                //    var excuteSql = db.Database.ExecuteSqlCommand("[dbo].[SendEmailToCustomer] @Key,@recipients ",
                //        new SqlParameter("@Key", key),
                //        new SqlParameter("@recipients", email));

                //    return new Tuple<bool, string>(true, "Gửi Email thành công");
                //}

                using (var db = new EInvoicePLHContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var excuteSql = db.Database.ExecuteSqlCommand("[dbo].[SendEmailToCustomer] @Key,@recipients ",
                        new SqlParameter("@Key", key),
                        new SqlParameter("@recipients", email));
                    return new Tuple<bool, string>(true, "Gửi Email thành công");
                }
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string>(false, $"{ex.Message}");
            }
        }

        public List<InvoiceLineModel> ListInvoiceLine(string storeNo, string DocumentNo, string TypeInvoice, string orderNo, out int recordsTotal, int skip, int take)
        {
            var result = new List<InvoiceLineModel>();
            try
            {
                //var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
                //using (var db = new EInvoiceContainer(ipServer))
                //{
                //    db.Database.CommandTimeout = 2 * 60;
                //    if (TypeInvoice == "Normal")
                //    {
                //        var data = db.InvoiceLineNormals.AsNoTracking()
                //                    .Where(d => d.DocumentNo == DocumentNo)
                //                    .Select(a => new InvoiceLineModel
                //                    {
                //                        DocumentNo = a.DocumentNo,
                //                        Key = a.Key,
                //                        Code = a.Code,
                //                        ProdName = a.ProdName,
                //                        ProdPrice = a.ProdPrice,
                //                        ProdQuantity = a.ProdQuantity,
                //                        ProdUnit = a.ProdUnit,
                //                        Total = a.Total,
                //                        VATRate = a.VATRate,
                //                        VATAmount = a.VATAmount,
                //                        Amount = a.Amount,
                //                        OrderNo = a.OrderNo,
                //                        VatVinIDAmount = a.VatVinIDAmount,
                //                        NetVinIDAmount = a.NetVinIDAmount,
                //                        TotalVinIDAmount = a.TotalVinIDAmount,
                //                        CreateDate = a.CreateDate,
                //                        LastUpdate = a.LastUpdate
                //                    });
                //        if (!string.IsNullOrEmpty(orderNo))
                //        {
                //            data = data.Where(d => d.OrderNo.Contains(orderNo));
                //        }

                //        recordsTotal = data.Count();
                //        result = data.OrderBy(d => d.OrderNo).Skip(skip).Take(take).ToList();

                //        return result;
                //    }
                //    else
                //    {
                //        var data = db.InvoiceLineEODs.AsNoTracking()
                //                    .Where(d => d.DocumentNo == DocumentNo)
                //                    .Select(a => new InvoiceLineModel
                //                    {
                //                        DocumentNo = a.DocumentNo,
                //                        Key = a.Key,
                //                        Code = a.Code,
                //                        ProdName = a.ProdName,
                //                        ProdPrice = a.ProdPrice,
                //                        ProdQuantity = a.ProdQuantity,
                //                        ProdUnit = a.ProdUnit,
                //                        Total = a.Total,
                //                        VATRate = a.VATRate,
                //                        VATAmount = a.VATAmount,
                //                        Amount = a.Amount,
                //                        OrderNo = a.OrderNo,
                //                        VatVinIDAmount = a.VatVinIDAmount,
                //                        NetVinIDAmount = a.NetVinIDAmount,
                //                        TotalVinIDAmount = a.TotalVinIDAmount,
                //                        CreateDate = a.CreateDate,
                //                        LastUpdate = a.LastUpdate
                //                    });
                //        if (!string.IsNullOrEmpty(orderNo))
                //        {
                //            data = data.Where(d => d.OrderNo.Contains(orderNo));
                //        }
                //        recordsTotal = data.Count();
                //        result = data.OrderBy(d => d.OrderNo).Skip(skip).Take(take).ToList();
                //        return result;
                //    }
                //}

                using (var db = new EInvoicePLHContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    result = db.Database.SqlQuery<InvoiceLineModel>("[dbo].[SP_LOAD_ORDER_INVOICE] @TypeInvoice, @DocumentNo,@OrderNo, @PageSize, @PageNumber ",
                        new SqlParameter("@TypeInvoice", TypeInvoice),
                        new SqlParameter("@DocumentNo", DocumentNo),
                        new SqlParameter("@OrderNo", orderNo),
                        new SqlParameter("@PageSize", take),
                        new SqlParameter("@PageNumber", skip)).ToList();

                    recordsTotal = (result != null && result.Count > 0) ? result.FirstOrDefault().TotalRow : 0;
                    return result;
                }
            }
            catch (Exception ex)
            {
                recordsTotal = 0;
                return result;
            }
        }

        // 25/11/2024, tungnt8 : Xem chi tiet hoa don
        public List<InvoiceLineModel> GetDetailInvoiceList(string storeNo, string DocumentNo, string typePublish, string TypeInvoice, string orderNo, out int recordsTotal, int skip, int take)
        {
            var result = new List<InvoiceLineModel>();
            try
            {
                recordsTotal = 0;
                if (typePublish == "GTGT")
                {
                    using (var db = new EInvoicePLHContainer()) // // PRD : 10.235.25.101 , DB = [EInvoice]
                    {
                        db.Database.CommandTimeout = 2 * 60;
                        recordsTotal = 0;
                        var data = db.Database.SqlQuery<InvoiceLineModel>("[dbo].[SP_LOAD_ORDER_INVOICE] @TypeInvoice, @DocumentNo,@OrderNo, @PageSize, @PageNumber",
                            new SqlParameter("@TypeInvoice", TypeInvoice ?? string.Empty),
                            new SqlParameter("@DocumentNo", DocumentNo ?? string.Empty),
                            new SqlParameter("@OrderNo", orderNo ?? string.Empty),
                            new SqlParameter("@PageSize", take),
                            new SqlParameter("@PageNumber", skip)).ToList();

                        recordsTotal = (data != null && data.Count > 0) ? data.FirstOrDefault().TotalRow : 0;
                        result = data;
                    }
                }
                else if(typePublish == "MTT") // Hoa don may tinh tien
                {
                    using (var db1 = new EInvoiceContainer()) // PRD : 10.235.25.103, DB = [EInvoice]
                    {
                        db1.Database.CommandTimeout = 2 * 60;
                        recordsTotal = 0;
                         var data = db1.Database.SqlQuery<InvoiceLineModel>("[dbo].[Cash_GetDetailInvoiceByDocumentNo] @DocumentNo, @OrderNo, @PageSize, @PageNumber",
                            new SqlParameter("@DocumentNo", DocumentNo ?? string.Empty),
                            new SqlParameter("@OrderNo", orderNo ?? string.Empty),
                            new SqlParameter("@PageSize", take),
                            new SqlParameter("@PageNumber", skip)).ToList();

                        recordsTotal = (data != null && data.Count > 0) ? data.FirstOrDefault().TotalRow : 0;
                        result = data;
                    }
                }
            }
            catch (Exception ex)
            {
                recordsTotal = 0;
                return result;
            }
            return result;
        }
        public List<TransHeaderModel> CheckListOrder(string listOrder, string siteCode)
        {
            try
            {
                var arrayOrder = listOrder.Split(new char[] { ';' }).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToArray();
                if (arrayOrder.Length > 0)
                {
                    var ipServer = ServerIPConnection.GetIPServerByStore(siteCode);

                    using (var db = new CentralSalesContainer(ipServer))
                    {
                        db.Database.CommandTimeout = 2 * 60;
                        var date = DateTime.Now.Date;

                        var data = db.TransHeaders.AsNoTracking().Where(d => d.StoreNo == siteCode && arrayOrder.Any(x => x == d.OrderNo))
                            .Select(a => new TransHeaderModel
                            {
                                OrderNo = a.OrderNo,
                                OrderDate = a.OrderDate,
                                CustomerName = a.CustomerName,
                                PaymentAtPOSAmount = a.PaymentAtPOSAmount,
                                POSTerminalNo = a.POSTerminalNo,
                                CashierID = a.CashierID,
                                TanencyNo = a.TanencyNo,
                                DeliveringMethod = a.DeliveringMethod // 30: Shoppe
                            }).ToList();

                        return data;
                    }
                }
            }
            catch (Exception ex)
            {
                return (default(List<TransHeaderModel>));
            }
            return (default(List<TransHeaderModel>));
        }
        public List<TransHeaderModel> CheckListOrderCreatedInvoice(string listOrder, string siteCode)
        {
            var data = new List<TransHeaderModel>();
            try
            {
                var arrayOrder = listOrder.Split(new char[] { ';' }).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToArray();
                if (arrayOrder.Length > 0)
                {
                    var ipServer = ServerIPConnection.GetIPServerByStore(siteCode);
                    var ipServerRead = ipServer.Split('\\')[0];
                    var isCheckVAT = false;

                    using (var db = new EInvoiceContainer(ipServerRead))
                    {
                        db.Database.CommandTimeout = 2 * 60;
                        data = db.InvoiceCreateds.AsNoTracking().Where(a => a.SiteNo == siteCode && arrayOrder.Any(x => x.Contains(a.OrderNo)))
                            .Select(a => new TransHeaderModel
                            {
                                OrderNo = a.OrderNo,
                                CustomerName = a.CustomerName
                            }).ToList();

                        if (data != null && data.Count == 0)
                        {
                            isCheckVAT = true;
                            data = db.InvoiceLineNormals.AsNoTracking().Where(a => arrayOrder.Any(x => x.Contains(a.OrderNo)))
                            .Select(a => new TransHeaderModel
                            {
                                OrderNo = a.OrderNo,
                                CustomerName = ""
                            }).ToList();

                            if (data != null && data.Count == 0)
                            {
                                data = (from a in db.InvoiceHeaderEODs
                                        join b in db.InvoiceLineEODs on a.Key equals b.Key
                                        where arrayOrder.Any(x => x.Contains(b.OrderNo)) && a.StoreNo == siteCode
                                        select new TransHeaderModel
                                        {
                                            OrderNo = a.OrderNo,
                                            CustomerName = ""
                                        }).ToList();

                            }

                            if (data.Count > 0)
                            {
                                isCheckVAT = false;
                            }
                        }
                        if (isCheckVAT)
                        {
                            data = db.OrderNoCreatedInvoices.AsNoTracking().Where(a => arrayOrder.Any(x => x.Contains(a.OrderNo)))
                            .Select(a => new TransHeaderModel
                            {
                                OrderNo = a.OrderNo,
                                CustomerName = ""
                            }).ToList();

                            data = db.OrderNoCreatedInvoiceOthers.AsNoTracking().Where(a => arrayOrder.Any(x => x.Contains(a.OrderNo)))
                            .Select(a => new TransHeaderModel
                            {
                                OrderNo = a.OrderNo,
                                CustomerName = ""

                            }).ToList();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return data;
        }
        public CheckPublishInvoiceModel CheckPublishOrderInvoice(string listOrder, string storeNo)
        {
            try
            {
                var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
                var ipServerRead = ipServer.Split('\\')[0];

                using (var db = new EInvoiceContainer(ipServerRead))
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var result = db.Database.SqlQuery<CheckPublishInvoiceModel>("[dbo].[CheckPublish] @OrderList ",
                    new SqlParameter("@OrderList", listOrder)).FirstOrDefault();
                    if (result != null)
                        return result;
                    return new CheckPublishInvoiceModel();
                }
            }
            catch (Exception)
            {
                return (default(CheckPublishInvoiceModel));
            }
        }

        public bool ExcuteStagingInvcoie(string orderNo, string storeNo)
        {
            var isExcute = true;
            try
            {
                var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
                using (var db = new EInvoiceContainer(ipServer))
                {
                    db.Database.CommandTimeout = 2 * 60;
                    db.Database.ExecuteSqlCommand($"DELETE from EInvoice..StagingInvoice WHERE OrderNo = '{orderNo}'");
                    db.Database.ExecuteSqlCommand($"DELETE from EInvoice..StagingInvoiceOther WHERE OrderNo = '{orderNo}'");
                    db.Database.ExecuteSqlCommand("CentralSalesStaging..InsertStagingInvoice @OrderNo", new SqlParameter("@OrderNo", orderNo));
                }
            }
            catch (Exception ex)
            {
                isExcute = false;
            }
            return isExcute;
        }

        public bool CreatedInvoice(string storeNo, List<InvoiceCreatedModel> listInvoice)
        {
            try
            {
                var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
                var ipServerRead = ipServer.Split('\\')[0];
                using (var db = new EInvoiceContainer(ipServerRead))
                {
                    db.Database.CommandTimeout = 2 * 60;

                    if (listInvoice.Count > 0)
                    {
                        db.InvoiceCreateds.AddRange(listInvoice.Select(a => new InvoiceCreated
                        {
                            SiteNo = a.SiteNo,
                            OrderNo = a.OrderNo,
                            CustomerName = a.CustomerName,
                            CompanyName = a.CompanyName,
                            TaxCode = a.TaxCode,
                            PhoneNumber = a.PhoneNumber,
                            Email = a.Email,
                            Address = a.Address,
                            IsNotVAT = a.IsNotVAT,
                            CreatedDate = a.CreatedDate,
                            CreatedUser = a.CreatedUser
                        }));

                        db.SaveChanges();

                        using (var dbCentral = new CentralGeneralContainer()) // Insert khách hàng xuất VAT vào central
                        {
                            var cus = listInvoice.FirstOrDefault();
                            dbCentral.Database.CommandTimeout = 2 * 60;
                            var check = dbCentral.CustomerVATs.FirstOrDefault(d => d.CustomerName == cus.CustomerName && d.CompanyName == cus.CompanyName && d.PhoneNumber == cus.PhoneNumber);
                            if (check == null)
                            {
                                dbCentral.CustomerVATs.Add(new CustomerVAT
                                {
                                    CustomerName = cus.CustomerName,
                                    CompanyName = cus.CompanyName,
                                    TaxCode = cus.TaxCode,
                                    PhoneNumber = cus.PhoneNumber,
                                    Email = cus.Email,
                                    Address = cus.Address,
                                    CreatedDate = cus.CreatedDate,
                                    CreatedUser = cus.CreatedUser
                                });
                                dbCentral.SaveChanges();
                            }
                        }
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        public List<InfoInvoiceModel> ListInvoicePattern(string siteNo, string invoiceType)
        {
            var result = new List<InfoInvoiceModel>();
            try
            {
                using (var db = new EInvoicePLHContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    result = (from a in db.StoreNoseriVATs
                              join b in db.VatNumberFPTs on a.NoseriCode equals b.NoseriCode
                              where a.StoreNo == siteNo
                              select new InfoInvoiceModel
                              {
                                  InvoicePattern = b.InvoicePattern,
                                  SerialNo = b.SerialNo,
                                  NoseriCode = b.NoseriCode

                              }).ToList();

                    if (!string.IsNullOrEmpty(invoiceType))
                    {
                        result = result.Where(d => d.InvoicePattern == invoiceType).ToList();
                    }
                }
            }
            catch
            {
            }
            return result;
        }


        public bool CheckInvoiceNo(string storeNo, string invoiceNo, string invoicePattern, string serialNo, string noseriCode, string type)
        {
            var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
            var ipServerRead = ipServer.Split('\\')[0];

            using (var db = new EInvoiceContainer(ipServerRead))
            {
                try
                {
                    if (type == "NOR")
                    {
                        var result = db.InvoiceHeaderNormals.FirstOrDefault(d => d.InvoiceNo.Trim().ToLower() == invoiceNo.Trim().ToLower() && d.InvoicePattern.Trim().ToLower() == invoicePattern.Trim().ToLower() && d.SerialNo.Trim().ToLower() == serialNo.Trim().ToLower() && d.NoseriCode == noseriCode);
                        if (result != null)
                            return true;
                        return false;
                    }

                    if (type == "EOD")
                    {
                        var result = db.InvoiceHeaderEODs.FirstOrDefault(d => d.InvoiceNo.Trim().ToLower() == invoiceNo.Trim().ToLower() && d.InvoicePattern.Trim().ToLower() == invoicePattern.Trim().ToLower() && d.SerialNo.Trim().ToLower() == serialNo.Trim().ToLower() && d.NoseriCode == noseriCode);
                        if (result != null)
                            return true;
                        return false;
                    }

                    return false;

                }
                catch (Exception)
                {
                    return false;
                }

            }
        }
        public List<BranchModel> LoadBranch(string branchNo, string branchName, string compTaxCode, string type, out int total, int skip, int take)
        {
            var result = new List<BranchModel>();
            if (type == "GTGT")
            {
                using (var db = new EInvoicePLHContainer())
                {
                    try
                    {
                        db.Database.CommandTimeout = 2 * 60;
                        var data = db.Branches
                                   .Select(a => new BranchModel
                                   {
                                       ID = a.ID,
                                       BranchNo = a.BranchNo,
                                       BranchName = a.BranchName,
                                       Address = a.Address,
                                       CompTaxCode = a.CompTaxCode,
                                       ETaxAccName = a.ETaxAccName,
                                       ETaxAccPass = a.ETaxAccPass,
                                       ETaxWSName = a.ETaxWSName,
                                       ETaxWSPass = a.ETaxWSPass,
                                       CreateDate = a.CreateDate,
                                       LastUpdate = a.LastUpdate
                                   });
                        if (!string.IsNullOrEmpty(branchNo))
                        {
                            data = data.Where(m => m.BranchNo.Contains(branchNo));
                        }

                        if (!string.IsNullOrEmpty(compTaxCode))
                        {
                            data = data.Where(m => m.CompTaxCode.ToLower().Contains(compTaxCode.ToLower()));
                        }

                        total = data.Count();
                        result = data.OrderByDescending(d => d.CreateDate).Skip(skip).Take(take).ToList();
                        return result;
                    }
                    catch (Exception ex)
                    {
                        total = 0;
                    }
                    return new List<BranchModel>();
                }
            }
            else
            {
                using (var db1 = new EInvoiceContainer())
                {
                    try
                    {
                        db1.Database.CommandTimeout = 2 * 60;
                        total = 0;
                        var data = db1.Database.SqlQuery<BranchModel>("[dbo].[Get_Cash_GetBranchList] @BranchNo, @BranchName, @CompTaxCode, @PageSize, @PageNumber",
                           new SqlParameter("@BranchNo", branchNo ?? string.Empty),
                           new SqlParameter("@BranchName", branchName ?? string.Empty),
                           new SqlParameter("@CompTaxCode", compTaxCode ?? string.Empty),
                           new SqlParameter("@PageSize", take),
                           new SqlParameter("@PageNumber", skip)).ToList();

                        total = data.Count;
                        return result = data;

                    }
                    catch (Exception ex)
                    {
                        total = 0;
                    }
                    return new List<BranchModel>();
                }
            }

            
        }

        public Tuple<bool, string> UpdateBranch(BranchModel model)
        {
            var result = new Tuple<bool, string>(false, "");
            using (var db = new EInvoicePLHContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 120;
                    var checkBranch = db.Branches.FirstOrDefault(d => d.BranchNo == model.BranchNo);

                    if (checkBranch != null)
                    {
                        checkBranch.BranchName = model.BranchName;
                        checkBranch.Address = model.Address;
                        checkBranch.CompTaxCode = model.CompTaxCode;
                        checkBranch.ETaxAccName = model.ETaxAccName;
                        checkBranch.ETaxAccPass = model.ETaxAccPass;
                        checkBranch.ETaxWSName = model.ETaxWSName;
                        checkBranch.ETaxWSPass = model.ETaxWSPass;
                        checkBranch.LastUpdate = model.LastUpdate;
                        db.SaveChanges();
                        result = new Tuple<bool, string>(true, "Cập nhật chi nhánh thành công");
                    }
                    else
                    {
                        var insert = new EF.EInvoices.Branch
                        {
                            BranchNo = model.BranchNo,
                            BranchName = model.BranchName,
                            Address = model.Address,
                            CompTaxCode = model.CompTaxCode,
                            ETaxAccName = model.ETaxAccName,
                            ETaxAccPass = model.ETaxAccPass,
                            ETaxWSName = model.ETaxWSName,
                            ETaxWSPass = model.ETaxWSPass,
                            CreateDate = model.CreateDate,
                            LastUpdate = model.LastUpdate
                        };

                        db.Branches.Add(insert);
                        db.SaveChanges();

                        result = new Tuple<bool, string>(true, "Thêm mới chi nhánh thành công");
                    }
                }
                catch (Exception ex)
                {
                    result = new Tuple<bool, string>(false, $"{ex.Message}");
                }
            }
            return result;
        }

        public List<InvoiceTypeModel> ListInvoiceType()
        {
            try
            {
                using (var db = new CentralGeneralContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = (from a in db.InvoiceTypes
                                select new InvoiceTypeModel
                                {
                                    InvoiceType = a.InvoiceType1,
                                    InvoiceTypeName = a.InvoiceTypeName
                                }).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return (default(List<InvoiceTypeModel>));
            }
        }

        public ViewMngInvoiceModel ViewMngInvoi()
        {
            var result = new ViewMngInvoiceModel();
            try
            {
                using (var db = new CentralGeneralContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var listInvoiceType = (from a in db.InvoiceTypes
                                           select new InvoiceTypeModel
                                           {
                                               InvoiceType = a.InvoiceType1,
                                               InvoiceTypeName = a.InvoiceTypeName
                                           }).ToList();

                    result = new ViewMngInvoiceModel
                    {
                        ListInvoiceType = listInvoiceType
                    };
                }
            }
            catch
            {

            }
            return result;
        }

        public List<InfoInvoiceModel> ListSerialByInvoiceType(string siteNo, string invoiceType)
        {
            var result = new List<InfoInvoiceModel>();
            try
            {
                using (var db = new EInvoicePLHContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    result = (from a in db.StoreNoseriVATs
                              join b in db.VatNumberFPTs on a.NoseriCode equals b.NoseriCode
                              where a.StoreNo == siteNo && a.InvoiceType == invoiceType
                              select new InfoInvoiceModel
                              {
                                  InvoicePattern = b.InvoicePattern,
                                  SerialNo = b.SerialNo,
                                  NoseriCode = b.NoseriCode
                              }).ToList();
                }
            }
            catch
            {
            }
            return result;
        }
        public List<InvoiceCreatedModel> LoadCustIssueVAT(InvoiceCusIssueVATRequest model)
        {
            try
            {
                var ipServer = model.SetDB;
                using (var db = new EInvoiceContainer(ipServer))
                {
                    db.Database.CommandTimeout = 2 * 60;

                    var result = db.Database.SqlQuery<InvoiceCreatedModel>("[dbo].[SP_CUSTOMER_ISSUE_VAT] @ListStoreJson, @TuNgay, @DenNgay, @CustomerName, @CompanyName, @TaxCode, @PhoneNumber, @Email, @Address, @OrderNo, @PageSize, @PageNumber",
                    new SqlParameter("@ListStoreJson", model.ListStoreJson),
                    new SqlParameter("@TuNgay", model.TuNgay),
                    new SqlParameter("@DenNgay", model.DenNgay),
                    new SqlParameter("@CustomerName", model.CustomerName),
                    new SqlParameter("@CompanyName", model.CompanyName),
                    new SqlParameter("@TaxCode", model.TaxCode),
                    new SqlParameter("@PhoneNumber", model.PhoneNumber),
                    new SqlParameter("@Email", model.Email),
                    new SqlParameter("@Address", model.Address),
                    new SqlParameter("@OrderNo", model.OrderNo),
                    new SqlParameter("@PageSize", model.PageSize),
                    new SqlParameter("@PageNumber", model.PageNumber)).ToList();

                    return result;
                }
            }
            catch (Exception ex)
            {
                return new List<InvoiceCreatedModel>();
            }
        }

        // 16/12/2024, tungnt8
        public List<InvoiceCreatedModel> LoadCustIssueVATV2(InvoiceCusIssueVATRequest model)
        {
            try
            {
                if (model.Type == "GTGT")
                {
                    using (var db = new EInvoicePLHContainer())  // prd = 10.235.25.101 / EInvoice
                    {
                        db.Database.CommandTimeout = 2 * 60;
                        var result = db.Database.SqlQuery<InvoiceCreatedModel>("[dbo].[SP_Customer_Issue_Invoice] @ListStoreJson, @TuNgay, @DenNgay, @TaxCode, @TextSearch, @PageSize, @PageNumber",
                            new SqlParameter("@ListStoreJson", model.ListStoreJson),
                            new SqlParameter("@TuNgay", model.TuNgay),
                            new SqlParameter("@DenNgay", model.DenNgay),
                            new SqlParameter("@TaxCode", model.TaxCode),
                            new SqlParameter("@TextSearch", model.TextSearch),
                            new SqlParameter("@PageSize", model.PageSize),
                            new SqlParameter("@PageNumber", model.PageNumber)).ToList();

                        return result;
                    }
                }
                else // Type = "MTT"
                {                   
                    using (var db = new EInvoiceContainer()) // prd = 10.235.25.103 / EInvoice
                    {
                        db.Database.CommandTimeout = 2 * 60;
                        var result = db.Database.SqlQuery<InvoiceCreatedModel>("[dbo].[Cash_Customer_Issue_Invoice] @ListStoreJson, @TuNgay, @DenNgay, @TaxCode, @TextSearch, @PageSize, @PageNumber",
                            new SqlParameter("@ListStoreJson", model.ListStoreJson),
                            new SqlParameter("@TuNgay", model.TuNgay),
                            new SqlParameter("@DenNgay", model.DenNgay),
                            new SqlParameter("@TaxCode", model.TaxCode),
                            new SqlParameter("@TextSearch", model.TextSearch),
                            new SqlParameter("@PageSize", model.PageSize),
                            new SqlParameter("@PageNumber", model.PageNumber)).ToList();

                        return result;
                    }
                }                
            }
            catch (Exception ex)
            {
                return new List<InvoiceCreatedModel>();
            }
        }

        public InvoiceResultModel GetInfoInvoice(string storeNo, int isEOD, string key)
        {
            try
            {
                using (var db = new EInvoicePLHContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var result = db.Database.SqlQuery<InvoiceResultModel>("[dbo].[SP_GETINFO_BRANCH_BYKEY] @isEOD, @KEY",
                        new SqlParameter("@isEOD", isEOD),
                        new SqlParameter("@KEY", key)).FirstOrDefault();
                    return result;
                }
            }
            catch (Exception ex)
            {
                return new InvoiceResultModel();
            }
        }

        // 16/12/2024, tungt8
        public InvoiceResultModel GetInfoInvoiceV2(string storeNo, int isEOD, string key, string Type)
        {
            try
            {
                if (Type == "GTGT")
                {
                    using (var db = new EInvoicePLHContainer())
                    {
                        db.Database.CommandTimeout = 2 * 60;
                        var result = db.Database.SqlQuery<InvoiceResultModel>("[dbo].[SP_GETINFO_BRANCH_BYKEY] @isEOD, @KEY",
                            new SqlParameter("@isEOD", isEOD),
                            new SqlParameter("@KEY", key)).FirstOrDefault();
                        return result;
                    }
                }
                else
                {
                    using (var db1 = new EInvoiceContainer()) // prd = 10.235.25.103/EInvoice
                    {
                        db1.Database.CommandTimeout = 2 * 60;
                        var result = db1.Database.SqlQuery<InvoiceResultModel>("[dbo].[Cash_Get_Info_Branch_By_Key] @isEOD, @Key",
                            new SqlParameter("@isEOD", isEOD),
                            new SqlParameter("@Key", key)).FirstOrDefault();
                        return result;
                    }
                }               
            }
            catch (Exception ex)
            {
                return new InvoiceResultModel();
            }
        }
        public InvoiceCreatedModel GetInfoVAT(string ipServer, string storeNo, string orderNo)
        {
            try
            {
                using (var db = new EInvoiceContainer(ipServer))
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var result = db.Database.SqlQuery<InvoiceCreatedModel>("[dbo].[SP_GET_INFO_CUS_VAT] @StoreNo, @OrderNo",
                        new SqlParameter("@StoreNo", storeNo),
                        new SqlParameter("@OrderNo", orderNo)).FirstOrDefault();
                    return result;
                }
            }
            catch (Exception ex)
            {
                return new InvoiceCreatedModel();
            }
        }
        public Tuple<bool, string> UpdateInfoVAT(InvoiceCreatedModel model)
        {
            var result = new Tuple<bool, string>(false, "");
            using (var db = new EInvoiceContainer(model.IPServer))
            {
                try
                {
                    db.Database.CommandTimeout = 120;
                    var dateNow = DateTime.Now.Date;
                    var dateTimeNow = DateTime.Now;
                    var checkUpd = db.InvoiceCreateds.Where(d => d.OrderNo == model.OrderNo && DbFunctions.TruncateTime(d.CreatedDate) == dateNow).ToList();

                    if (checkUpd != null && checkUpd.Count > 0)
                    {
                        checkUpd.ForEach(x =>
                        {
                            x.TaxCode = model.TaxCode;
                            x.CustomerName = model.CustomerName;
                            x.CompanyName = model.CompanyName;
                            x.PhoneNumber = model.PhoneNumber;
                            x.Email = model.Email;
                            x.Address = model.Address;
                        });

                        try
                        {
                            using (var dbCentral = new CentralGeneralContainer()) //update lại thông tin KH
                            {
                                dbCentral.Database.CommandTimeout = 2 * 60;
                                var checkCus = dbCentral.CustomerVATs.Where(d => d.TaxCode == model.TaxCode).ToList();
                                if (checkCus != null && checkCus.Count > 0)
                                {
                                    checkCus.ForEach(x =>
                                    {
                                        x.CustomerName = model.CustomerName;
                                        x.CompanyName = model.CompanyName;
                                        x.PhoneNumber = model.PhoneNumber;
                                        x.Email = model.Email;
                                        x.Address = model.Address;
                                        x.CreatedDate = dateTimeNow;
                                    });

                                    dbCentral.SaveChanges();
                                }
                            }
                        }
                        catch
                        {
                        }

                        db.SaveChanges();
                        result = new Tuple<bool, string>(true, "Cập nhật thông tin KH thành công");
                    }
                    else
                    {
                        result = new Tuple<bool, string>(false, "Không tìm thấy thông tin KH cần sửa");
                    }
                }
                catch (Exception ex)
                {
                    result = new Tuple<bool, string>(false, $"{ex.Message}");
                }
            }
            return result;
        }

        // 26/11/2024,tungnt8: KH định danh xuất hóa đơn MTT
        public List<CustomerInfoInvoiceMTTModel> GetCustomerInfoInvoiceMTT(CustomerInfoInvoiceMTTRequest model)
        {
            try
            {
                var ipServer = model.SetDB;
                using (var db = new EInvoiceContainer(ipServer))
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var result = db.Database.SqlQuery<CustomerInfoInvoiceMTTModel>("[dbo].[Cash_CustomerInfoIssue_List] @ListStoreJson, @TuNgay, @DenNgay, @TaxCode, @TextSearch, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@ListStoreJson", model.ListStoreJson),
                        new SqlParameter("@TuNgay", model.TuNgay),
                        new SqlParameter("@DenNgay", model.DenNgay),
                        new SqlParameter("@TaxCode", model.TaxCode),
                        new SqlParameter("@TextSearch", model.TextSearch),
                        new SqlParameter("@Export", '0'),
                        new SqlParameter("@PageSize", model.PageSize),
                        new SqlParameter("@PageNumber", model.PageNumber)).ToList();

                    return result;
                }
            }
            catch (Exception ex)
            {
                return new List<CustomerInfoInvoiceMTTModel>();
            }
        }

        public List<ExportCustomerInfoInvoiceMTTModel> ExportCustomerInfoInvoiceMTT(CustomerInfoInvoiceMTTRequest model)
        {
            try
            {
                var ipServer = model.SetDB;
                using (var db = new EInvoiceContainer(ipServer))
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var result = db.Database.SqlQuery<ExportCustomerInfoInvoiceMTTModel>("[dbo].[Cash_CustomerInfoIssue_List] @ListStoreJson, @TuNgay, @DenNgay, @TaxCode, @TextSearch, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@ListStoreJson", model.ListStoreJson),
                        new SqlParameter("@TuNgay", model.TuNgay),
                        new SqlParameter("@DenNgay", model.DenNgay),
                        new SqlParameter("@TaxCode", model.TaxCode),
                        new SqlParameter("@TextSearch", model.TextSearch),
                        new SqlParameter("@Export", '1'),
                        new SqlParameter("@PageSize", model.PageSize),
                        new SqlParameter("@PageNumber", model.PageNumber)).ToList();

                    return result;
                }
            }
            catch (Exception ex)
            {
                return new List<ExportCustomerInfoInvoiceMTTModel>();
            }
        }

        public DetailInvoiceWinlifeModel ViewDetailListOrder(string storeNo, string OrderList)
        {
            var result = new DetailInvoiceWinlifeModel();
            try
            {
                var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
                var ipServerRead = ipServer.Split('\\')[0];
                using (var db = new EInvoiceContainer(ipServerRead))
                {
                    db.Database.CommandTimeout = 2 * 60;
                    DataSet ds = new DataSet();
                    using (var con = new SqlConnection(db.Database.Connection.ConnectionString))
                    using (var cmd = new SqlCommand("PublishInvoiceByView",con))
                    using (var da = new SqlDataAdapter(cmd))
                    {
                        cmd.Parameters.AddWithValue("@OrderList", OrderList);
                        cmd.Parameters.AddWithValue("@StoreNo", storeNo);
                        cmd.CommandType = CommandType.StoredProcedure;
                        da.Fill(ds);

                        var countTable = ds.Tables;
                        if (countTable.Count > 0)
                        {
                            var header = JsonConvert.DeserializeObject<List<InvoiceByViewHeaderModel>>(JsonConvert.SerializeObject(countTable[0])).FirstOrDefault();
                            var detail = JsonConvert.DeserializeObject<List<ListOrderDetailModel>>(JsonConvert.SerializeObject(countTable[1])).ToList();
                            result.InvoiceHeader = header;
                            result.ListDetailInvoiceLine = detail;
                        }
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                return result;
            }
        }

        public BranchModel GetBranchNo(string branchNo)
        {
            using (var db = new EInvoicePLHContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 120;
                    var data = db.Branches.Where(d => d.BranchNo == branchNo)
                               .Select(a => new BranchModel
                               {
                                   ID = a.ID,
                                   BranchNo = a.BranchNo,
                                   BranchName = a.BranchName,
                                   Address = a.Address,
                                   CompTaxCode = a.CompTaxCode,
                                   ETaxAccName = a.ETaxAccName,
                                   ETaxAccPass = a.ETaxAccPass,
                                   ETaxWSName = a.ETaxWSName,
                                   ETaxWSPass = a.ETaxWSPass,
                                   CreateDate = a.CreateDate,
                                   LastUpdate = a.LastUpdate
                               }).FirstOrDefault();
                    return data;
                }
                catch (Exception ex)
                {
                }
                return new BranchModel();
            }
        }

        public Tuple<bool, string, List<string>> CheckListInvoiceNo(string storeNo, List<string> listInvoiceNo, string invoicePattern, string serialNo, string noseriCode, string type)
        {
            var result = new Tuple<bool, string, List<string>>(false, "", listInvoiceNo);
            var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
            using (var db = new EInvoicePLHContainer())
            {
                try
                {
                    if (type == "NOR")
                    {
                        var checkData = db.InvoiceHeaderNormals.Where(d => listInvoiceNo.Any(a => a.Trim().ToLower() == d.InvoiceNo.Trim().ToLower()) && d.InvoicePattern.Trim().ToLower() == invoicePattern.Trim().ToLower() && d.SerialNo.Trim().ToLower() == serialNo.Trim().ToLower() && d.NoseriCode == noseriCode).ToList();
                        if (checkData != null && checkData.Count > 0)
                        {
                            var listExists = checkData.Select(a => a.InvoiceNo).ToList();
                            result = new Tuple<bool, string, List<string>>(true, "Tồn tại số VAT trong hệ thống", listExists);
                        }
                    }
                    if (type == "EOD")
                    {
                        var checkData = db.InvoiceHeaderEODs.Where(d => listInvoiceNo.Any(a => a.Trim().ToLower() == d.InvoiceNo.Trim().ToLower()) && d.InvoicePattern.Trim().ToLower() == invoicePattern.Trim().ToLower() && d.SerialNo.Trim().ToLower() == serialNo.Trim().ToLower() && d.NoseriCode == noseriCode).ToList();
                        if (checkData != null && checkData.Count > 0)
                        {
                            var listExists = checkData.Select(a => a.InvoiceNo).ToList();
                            result = new Tuple<bool, string, List<string>>(true, "Tồn tại số VAT trong hệ thống", listExists);
                        }
                    }
                }
                catch (Exception ex)
                {
                    result = new Tuple<bool, string, List<string>>(false, $"Lỗi check data {JsonConvert.SerializeObject(ex)}", listInvoiceNo);
                    return result;
                }
                return result;
            }
        }

        // 25/11/2024, tungnt8 : Yes : đã xuất hóa đơn MTT rồi thì không cho xuất HĐT trên Web này nữa ; No : cho xuất hóa đơn điện tử
        public CheckInvoiceMTTModel CheckInvoiceByMTT(string storeNo)
        {
            var data = new CheckInvoiceMTTModel();
            using (var db = new CentralGeneralContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 5 * 60;
                    data = db.Database.SqlQuery<CheckInvoiceMTTModel>("[dbo].[SP_Cash_CheckAllow] @StoreNo", new SqlParameter("@StoreNo", storeNo ?? string.Empty)).FirstOrDefault();
                    return data;
                }
                catch (Exception)
                {
                    return (default(CheckInvoiceMTTModel));
                }
            }
        }

        // 25/11/2024,tungnt8
        public List<CreatedDummyMTTModel> CreatedInvoiceDummyMTT(string ipSetServer, string listInvoice)
        {
            try
            {
                using (var db = new EInvoiceContainer(ipSetServer))  // default SET1, A.Hong yeu cau
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var result = db.Database.SqlQuery<CreatedDummyMTTModel>("[dbo].[Cash_Publish_Dummy] @Json", new SqlParameter("@Json", listInvoice)).ToList();
                    return result;
                }
            }
            catch (Exception ex)
            {
                return new List<CreatedDummyMTTModel>();
            }
        }







    }
}
