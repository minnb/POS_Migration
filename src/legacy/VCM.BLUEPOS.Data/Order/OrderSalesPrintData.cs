using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Data;
using VCM.BLUEPOS.Data.Authen;
using VCM.BLUEPOS.Data.Common;
using VCM.BLUEPOS.Data.EF;
using VCM.BLUEPOS.Data.EF.Central;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.Order;
using VCM.BLUEPOS.Model.Order.PrintInvoiceOrderSalesModel;
using VCM.BLUEPOS.Common.Helpers;

namespace VCM.BLUEPOS.Data.Order
{
    public interface IOrderSalesPrintData
    {
        string GetIPAddessByPOSTerminal(string posID);
        List<TransHeaderPrintInvoiceOrderSalesModel> GetTransHeaderByPrintInvoiceOrderSales(string storeNo, string posID, string orderNo);
        List<TransLinePrintInvoiceOrderSalesModel> GetTransLineByPrintInvoiceOrderSales(string storeNo, string posID, string orderNo);
        List<TranBluePointModel> GetTransBluePointByPrintInvoiceOrderSales(string posID, string orderNo);
        List<CouponInforModel> GetListCouponByPrintInvoiceOrderSales(string posID, string orderNo);
        List<PromotionInforModel> GetListPromotionByPrintInvoiceOrderSales(string posID, string orderNo);
        List<PaymentEntryListModel> GetListPaymentEntryByPrintInvoiceOrderSales(string posID, string orderNo);
        List<TenderTypePaymentModel> GetListTenderTypePayment(string posID, string orderNo);
        List<TenderTypeSetUpModel> GetListTenderTypeSetUp(string posID, string orderNo);
        List<TransCpnVchIssueModel> GetTransCpnVchIssue(string posID, string orderNo);
        List<MarketingResponseModel> GetMarketingDataList(string posID, string storeNo);
        List<CompanyInforModel> GetCompanyInforList(string posID, string storeNo);
        List<PaymentEntryReturnVoucherListModel> GetReturnVoucherAmountByPrintInvoiceOrderSales(string storeNo, string posID, string orderNo);
        List<ReasonReturnProductModel> GetReasonCodeByPrintInvoiceOrderSales(string storeNo, string posID, string reasonCode);
        List<VinIDRateByLoyatylRateModel> GetVinIDRateByLoyaltyRate(string storeNo, string posID);
        List<ListPOSDataSetupModel> GetPOSDataSetup(string storeNo, string posID);


    }

    public class OrderSalesPrintData : IOrderSalesPrintData
    {
        public string GetIPAddessByPOSTerminal(string posID)
        {
            try
            {
                var result = string.Empty;
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    result = db.POSTerminals.Where(d => d.No == posID).FirstOrDefault()?.IPAddress;
                    return result;
                }
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }

        public List<TransHeaderPrintInvoiceOrderSalesModel> GetTransHeaderByPrintInvoiceOrderSales(string storeNo, string posID, string orderNo)
        {
            var data = new List<TransHeaderPrintInvoiceOrderSalesModel>();
            var result = new List<TransHeaderPrintInvoiceOrderSalesModel>();
            var ipAddress = GetIPAddessByPOSTerminal(posID);
            try
            {
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    string connectStr = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", ipAddress, "POS", "POS@1234", "Store");
                    var sql = "SELECT H.[OrderNo], H.[StoreNo], S.[Name] AS StoreName, S.[Address], S.[PhoneNo], H.[POSTerminalNo], H.[SalesIsReturn], H.[MemberPointsRedeem], " +
                              " H.[CashierID], H.[MemberPointsEarn], H.[MemberCardNo], H.[ReturnedOrderNo], H.[IsOfflineVinID], H.[IsTenancy], H.[TanencyNo], H.[RefKey1], H.[RefKey2], H.[OrderTime], H.[ReturnVoucherNo] ";
                    sql += " FROM [dbo].[TransHeader] AS H ";
                    sql += " LEFT JOIN [dbo].[Store] AS S ON S.[No] = H.[StoreNo] ";
                    sql += " WHERE H.[StoreNo] = '" + storeNo + "'";
                    sql += " AND H.[OrderNo] = '" + orderNo + "'";

                    try
                    {
                        using (SqlConnection connection = new SqlConnection(connectStr))
                        {
                            connection.Open();
                            SqlDataAdapter da = new SqlDataAdapter(sql, connection);
                            DataSet ds = new DataSet();
                            da.Fill(ds, "TransHeaderList");
                            DataTable dt = ds.Tables["TransHeaderList"];

                            data = (from DataRow dr in dt.Rows
                                    select new TransHeaderPrintInvoiceOrderSalesModel()
                                    {
                                        StoreNo = dr["StoreNo"].ToString(),
                                        StoreName = dr["StoreName"].ToString(),
                                        Address = dr["Address"].ToString(),
                                        PhoneNo = dr["PhoneNo"].ToString(),
                                        POSTerminalNo = dr["POSTerminalNo"].ToString(),
                                        OrderNo = dr["OrderNo"].ToString(),
                                        SalesIsReturn = int.Parse(dr["SalesIsReturn"].ToString()),
                                        OrderTime = dr["OrderTime"] == null ? default(DateTime?) : Convert.ToDateTime(dr["OrderTime"].ToString()),
                                        CashierID = dr["CashierID"].ToString(),
                                        MemberPointsEarn = string.IsNullOrEmpty(dr["MemberPointsEarn"].ToString()) ? 0.0 : double.Parse(dr["MemberPointsEarn"].ToString()),
                                        MemberPointsRedeem = string.IsNullOrEmpty(dr["MemberPointsRedeem"].ToString()) ? 0.0 : double.Parse(dr["MemberPointsRedeem"].ToString()),
                                        MemberCardNo = dr["MemberCardNo"].ToString(),
                                        IsOfflineVinID = bool.Parse(dr["IsOfflineVinID"].ToString()),
                                        IsTenancy = int.Parse(dr["IsTenancy"].ToString()),
                                        TanencyNo = dr["TanencyNo"].ToString(),
                                        ReturnedOrderNo = dr["ReturnedOrderNo"].ToString(),
                                        RefKey2 = dr["RefKey2"].ToString()
                                    }).ToList();

                            connection.Close();

                            if (data.Count == 0 || data == null) // Không có dữ liệu
                            {
                                return new List<TransHeaderPrintInvoiceOrderSalesModel>();
                            }
                            result = data.ToList();
                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        BLUEPOS.Common.Helpers.LogsFile.WriteLogFile("GetTransHeaderByPrintInvoiceOrderSales", "Error=" + "  " + JsonConvert.SerializeObject(ex));
                    }
                }
            }
            catch (Exception ex)
            {
                BLUEPOS.Common.Helpers.LogsFile.WriteLogFile("GetTransHeaderByPrintInvoiceOrderSales", "Error=" + "  " + JsonConvert.SerializeObject(ex));
                return null;
            }
            return new List<TransHeaderPrintInvoiceOrderSalesModel>();
        }

        public List<TransLinePrintInvoiceOrderSalesModel> GetTransLineByPrintInvoiceOrderSales(string storeNo, string posID, string orderNo)
        {
            var data = new List<TransLinePrintInvoiceOrderSalesModel>();
            var result = new List<TransLinePrintInvoiceOrderSalesModel>();
            var ipAddress = GetIPAddessByPOSTerminal(posID);
            try
            {
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    string connectStr = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", ipAddress, "POS", "POS@1234", "Store");
                    var sql = "SELECT L.[LineNo], L.[DocumentNo], L.[ItemNo], L.[Description] AS ItemName, L.[Barcode], L.[SerialNo], L.[UnitOfMeasure], L.[UnitPrice]," +
                        " L.[Quantity], L.[AmountBeforeDiscount], L.[DiscountAmount], L.[VATAmount], L.[LineAmountIncVAT], L.[PrepaymentAmount], L.[MemberPointsEarn], L.[StaffID], L.[ScanTime] ";
                    sql += " FROM [dbo].[TransLine] AS L ";
                    sql += " WHERE L.[DocumentNo] = '" + orderNo + "' ";
                    sql += " AND L.[LineType] = 0 ";

                    try
                    {
                        using (SqlConnection connection = new SqlConnection(connectStr))
                        {
                            connection.Open();
                            SqlDataAdapter da = new SqlDataAdapter(sql, connection);
                            DataSet ds = new DataSet();
                            da.Fill(ds, "TransLineList");
                            DataTable dt = ds.Tables["TransLineList"];

                            data = (from DataRow dr in dt.Rows
                                    select new TransLinePrintInvoiceOrderSalesModel()
                                    {
                                        LineNo = int.Parse(dr["LineNo"].ToString()),
                                        ItemNo = dr["ItemNo"].ToString(),
                                        ItemName = dr["ItemName"].ToString(),
                                        Barcode = dr["Barcode"].ToString(),
                                        SerialNo = dr["SerialNo"].ToString(),
                                        OrderNo = dr["DocumentNo"].ToString(),
                                        UnitOfMeasure = dr["UnitOfMeasure"].ToString(),
                                        CashierID = dr["StaffID"].ToString(),
                                        ScanTime = dr["ScanTime"] == null ? default(DateTime?) : Convert.ToDateTime(dr["ScanTime"].ToString()),
                                        UnitPrice = string.IsNullOrEmpty(dr["UnitPrice"].ToString()) ? 0.0 : double.Parse(dr["UnitPrice"].ToString()),
                                        Quantity = string.IsNullOrEmpty(dr["Quantity"].ToString()) ? 0.0 : double.Parse(dr["Quantity"].ToString()),
                                        AmountBeforeDiscount = string.IsNullOrEmpty(dr["AmountBeforeDiscount"].ToString()) ? 0.0 : double.Parse(dr["AmountBeforeDiscount"].ToString()),
                                        DiscountAmount = string.IsNullOrEmpty(dr["DiscountAmount"].ToString()) ? 0.0 : double.Parse(dr["DiscountAmount"].ToString()),
                                        VATAmount = string.IsNullOrEmpty(dr["VATAmount"].ToString()) ? 0.0 : double.Parse(dr["VATAmount"].ToString()),
                                        LineAmountIncVAT = string.IsNullOrEmpty(dr["LineAmountIncVAT"].ToString()) ? 0.0 : double.Parse(dr["LineAmountIncVAT"].ToString()),
                                        PrepaymentAmount = string.IsNullOrEmpty(dr["PrepaymentAmount"].ToString()) ? 0.0 : double.Parse(dr["PrepaymentAmount"].ToString()),
                                        MemberPointsEarn = string.IsNullOrEmpty(dr["MemberPointsEarn"].ToString()) ? 0.0 : double.Parse(dr["MemberPointsEarn"].ToString())

                                    }).ToList();

                            connection.Close();

                            if (data.Count == 0 || data == null) // Không có dữ liệu
                            {
                                return new List<TransLinePrintInvoiceOrderSalesModel>();
                            }
                            result = data.ToList();
                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        VCM.BLUEPOS.Common.Helpers.LogsFile.WriteLogFile("GetTransLineByPrintInvoiceOrderSales", "Error=" + "  " + JsonConvert.SerializeObject(ex));
                    }
                }
            }
            catch (Exception ex)
            {
                VCM.BLUEPOS.Common.Helpers.LogsFile.WriteLogFile("GetTransLineByPrintInvoiceOrderSales", "Error=" + "  " + JsonConvert.SerializeObject(ex));
                return null;
            }
            return new List<TransLinePrintInvoiceOrderSalesModel>();
        }

        public List<TranBluePointModel> GetTransBluePointByPrintInvoiceOrderSales(string posID, string orderNo)
        {
            var data = new List<TranBluePointModel>();
            var result = new List<TranBluePointModel>();
            var ipAddress = GetIPAddessByPOSTerminal(posID);
            try
            {
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    string connectStr = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", ipAddress, "POS", "POS@1234", "Store");
                    var sql = "SELECT P.[LineNo], P.[TransactionType], P.[OrderNo], P.[OrderLineNo], P.[ItemNo], P.[Barcode], P.[Uom], P.[UnitPrice], P.[Quantity], P.[Amount], P.[DiscountAmount], " +
                        " P.[LineAmount], P.[DiscountType], P.[ExtraQuantityEarn], P.[ExtraAmountEarn], P.[ExtraQuantityNotEarn], P.[ExtraQuantityNotEarnReturned], P.[ReferenceNumber], P.[CreatedDate] ";
                    sql += " FROM [dbo].[TransBluePoint] AS P ";
                    sql += " WHERE P.[OrderNo] = '" + orderNo + "' ";

                    try
                    {
                        using (SqlConnection connection = new SqlConnection(connectStr))
                        {
                            connection.Open();
                            SqlDataAdapter da = new SqlDataAdapter(sql, connection);
                            DataSet ds = new DataSet();
                            da.Fill(ds, "TransBluePointList");
                            DataTable dt = ds.Tables["TransBluePointList"];

                            data = (from DataRow dr in dt.Rows
                                    select new TranBluePointModel()
                                    {
                                        TransactionType = int.Parse(dr["TransactionType"].ToString()),
                                        LineNo = int.Parse(dr["LineNo"].ToString()),
                                        OrderLineNo = int.Parse(dr["OrderLineNo"].ToString()),
                                        OrderNo = dr["OrderNo"].ToString(),
                                        ItemNo = dr["ItemNo"].ToString(),
                                        Uom = dr["Uom"].ToString(),
                                        Barcode = dr["Barcode"].ToString(),
                                        DiscountType = dr["DiscountType"].ToString(),
                                        Quantity = string.IsNullOrEmpty(dr["Quantity"].ToString()) ? 0.0 : double.Parse(dr["Quantity"].ToString()),
                                        UnitPrice = string.IsNullOrEmpty(dr["UnitPrice"].ToString()) ? 0.0 : double.Parse(dr["UnitPrice"].ToString()),
                                        Amount = string.IsNullOrEmpty(dr["Amount"].ToString()) ? 0.0 : double.Parse(dr["Amount"].ToString()),
                                        DiscountAmount = string.IsNullOrEmpty(dr["DiscountAmount"].ToString()) ? 0.0 : double.Parse(dr["DiscountAmount"].ToString()),
                                        LineAmount = string.IsNullOrEmpty(dr["LineAmount"].ToString()) ? 0.0 : double.Parse(dr["LineAmount"].ToString()),
                                        ExtraQuantityEarn = string.IsNullOrEmpty(dr["ExtraQuantityEarn"].ToString()) ? 0.0 : double.Parse(dr["ExtraQuantityEarn"].ToString()),
                                        ExtraAmountEarn = string.IsNullOrEmpty(dr["ExtraAmountEarn"].ToString()) ? 0.0 : double.Parse(dr["ExtraAmountEarn"].ToString()),
                                        ExtraQuantityNotEarn = string.IsNullOrEmpty(dr["ExtraQuantityNotEarn"].ToString()) ? 0.0 : double.Parse(dr["ExtraQuantityNotEarn"].ToString()),
                                        ExtraQuantityNotEarnReturned = string.IsNullOrEmpty(dr["ExtraQuantityNotEarnReturned"].ToString()) ? 0.0 : double.Parse(dr["ExtraQuantityNotEarnReturned"].ToString()),
                                        CreatedDate = dr["CreatedDate"] == null ? default(DateTime?) : Convert.ToDateTime(dr["CreatedDate"].ToString()),
                                        ReferenceNumber = dr["ReferenceNumber"].ToString()

                                    }).ToList();

                            connection.Close();

                            if (data.Count == 0 || data == null) // Không có dữ liệu
                            {
                                return new List<TranBluePointModel>();
                            }
                            result = data.ToList();
                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        VCM.BLUEPOS.Common.Helpers.LogsFile.WriteLogFile("GetTransBluePointByPrintInvoiceOrderSales", "Error=" + "  " + JsonConvert.SerializeObject(ex));
                    }
                }
            }
            catch (Exception ex)
            {
                VCM.BLUEPOS.Common.Helpers.LogsFile.WriteLogFile("GetTransBluePointByPrintInvoiceOrderSales", "Error=" + "  " + JsonConvert.SerializeObject(ex));
                return null;
            }
            return new List<TranBluePointModel>();
        }

        public List<CouponInforModel> GetListCouponByPrintInvoiceOrderSales(string posID, string orderNo)
        {
            var data = new List<CouponInforModel>();
            var result = new List<CouponInforModel>();
            var ipAddress = GetIPAddessByPOSTerminal(posID);

            try
            {
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    string connectStr = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", ipAddress, "POS", "POS@1234", "Store");

                    var sql = "SELECT C.[LineNo], C.[OrderNo],  C.[OrderLineNo], C.[ItemNo], C.[OfferNo], C.[OfferType], C.[Quantity], C.[DiscountType], C.[DiscountAmount], C.[Barcode], H.[ItemName] ";
                    sql += " FROM [dbo].[TransDiscountCouponEntry] AS C ";
                    sql += " LEFT JOIN [dbo].[CpnVchBOMHeader] AS H ON H.[ItemNo] = C.[ItemNo] ";
                    sql += " WHERE C.[OrderNo] = '" + orderNo + "' ";

                    try
                    {
                        using (SqlConnection connection = new SqlConnection(connectStr))
                        {
                            connection.Open();
                            SqlDataAdapter da = new SqlDataAdapter(sql, connection);
                            DataSet ds = new DataSet();
                            da.Fill(ds, "TransDiscountCouponEntryList");
                            DataTable dt = ds.Tables["TransDiscountCouponEntryList"];

                            data = (from DataRow dr in dt.Rows
                                    select new CouponInforModel()
                                    {
                                        LineNo = int.Parse(dr["LineNo"].ToString()),
                                        OrderNo = dr["OrderNo"].ToString(),
                                        OrderLineNo = int.Parse(dr["OrderLineNo"].ToString()),
                                        ItemNo = dr["ItemNo"].ToString(),
                                        OfferNo = dr["OfferNo"].ToString(),
                                        OfferType = dr["OfferType"].ToString(),
                                        Quantity = string.IsNullOrEmpty(dr["Quantity"].ToString()) ? 0.0 : double.Parse(dr["Quantity"].ToString()),
                                        DiscountType = int.Parse(dr["DiscountType"].ToString()),
                                        DiscountAmount = string.IsNullOrEmpty(dr["Quantity"].ToString()) ? 0.0 : double.Parse(dr["DiscountAmount"].ToString()),
                                        CouponCode = dr["Barcode"].ToString(),
                                        CouponName = dr["ItemName"].ToString()

                                    }).ToList();

                            connection.Close();

                            if (data.Count == 0 || data == null) // Không có dữ liệu
                            {
                                return new List<CouponInforModel>();
                            }
                            result = data.ToList();
                            return result;
                        }

                    }
                    catch (Exception ex)
                    {
                        VCM.BLUEPOS.Common.Helpers.LogsFile.WriteLogFile("GetListCouponByPrintInvoiceOrderSales", "Error=" + "  " + JsonConvert.SerializeObject(ex));
                    }
                }

            }
            catch (Exception ex)
            {
                VCM.BLUEPOS.Common.Helpers.LogsFile.WriteLogFile("GetListCouponByPrintInvoiceOrderSales", "Error=" + "  " + JsonConvert.SerializeObject(ex));
                return null;
            }
            return new List<CouponInforModel>();
        }

        // Danh sách khuyến mãi/voucher
        public List<PromotionInforModel> GetListPromotionByPrintInvoiceOrderSales(string posID, string orderNo)
        {
            var data = new List<PromotionInforModel>();
            var result = new List<PromotionInforModel>();
            var ipAddress = GetIPAddessByPOSTerminal(posID);

            try
            {
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    string connectStr = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", ipAddress, "POS", "POS@1234", "Store");
                    var sql = "SELECT D.[LineNo], D.[OrderNo],  D.[OrderLineNo], D.[ItemNo], D.[OfferNo], D.[OfferType], D.[Quantity], D.[DiscountType], D.[DiscountAmount], D.[Barcode], H.[Description] AS ItemName ";
                    sql += " FROM [dbo].[TransDiscountEntry] AS D ";
                    sql += " LEFT JOIN [dbo].[OfferHeader] AS H ON H.[No] = D.[OfferNo] ";
                    sql += " WHERE D.[OrderNo] = '" + orderNo + "' ";

                    try
                    {
                        using (SqlConnection connection = new SqlConnection(connectStr))
                        {
                            connection.Open();
                            SqlDataAdapter da = new SqlDataAdapter(sql, connection);
                            DataSet ds = new DataSet();
                            da.Fill(ds, "TransDiscountEntryList");
                            DataTable dt = ds.Tables["TransDiscountEntryList"];

                            data = (from DataRow dr in dt.Rows
                                    select new PromotionInforModel()
                                    {
                                        LineNo = int.Parse(dr["LineNo"].ToString()),
                                        OrderNo = dr["OrderNo"].ToString(),
                                        ItemNo = dr["ItemNo"].ToString(),
                                        OfferNo = dr["OfferNo"].ToString(),
                                        OfferType = dr["OfferType"].ToString(),
                                        Quantity = string.IsNullOrEmpty(dr["Quantity"].ToString()) ? 0.0 : double.Parse(dr["Quantity"].ToString()),
                                        DiscountType = int.Parse(dr["DiscountType"].ToString()),
                                        Barcode = dr["Barcode"].ToString(),
                                        VoucherCode = dr["OfferNo"].ToString(),
                                        VoucherName = "KM",
                                        OrderLineNo = int.Parse(dr["OrderLineNo"].ToString()),
                                        DiscountAmount = string.IsNullOrEmpty(dr["Quantity"].ToString()) ? 0.0 : double.Parse(dr["DiscountAmount"].ToString())

                                    }).ToList();

                            connection.Close();

                            if (data.Count == 0 || data == null) // Không có dữ liệu
                            {
                                return new List<PromotionInforModel>();
                            }
                            result = data.ToList();
                            return result;
                        }

                    }
                    catch (Exception ex)
                    {
                        VCM.BLUEPOS.Common.Helpers.LogsFile.WriteLogFile("GetListPromotionByPrintInvoiceOrderSales", "Error=" + "  " + JsonConvert.SerializeObject(ex));
                    }
                }

            }
            catch (Exception ex)
            {
                VCM.BLUEPOS.Common.Helpers.LogsFile.WriteLogFile("GetListPromotionByPrintInvoiceOrderSales", "Error=" + "  " + JsonConvert.SerializeObject(ex));
                return null;
            }
            return new List<PromotionInforModel>();
        }

        //
        public List<PaymentEntryListModel> GetListPaymentEntryByPrintInvoiceOrderSales(string posID, string orderNo)
        {
            var data = new List<PaymentEntryListModel>();
            var result = new List<PaymentEntryListModel>();
            var ipAddress = GetIPAddessByPOSTerminal(posID);
            try
            {
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    string connectStr = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", ipAddress, "POS", "POS@1234", "Store");
                    var sql = "SELECT P.[OrderNo], P.[LineNo],  P.[StoreNo], P.[POSTerminalNo], P.[TenderType], P.[TenderTypeName], P.[AmountTendered], P.[StaffID], P.[BankPOSCode], P.[BankCardType], ";
                    sql += " P.[PaymentDate], ";
                    sql += " P.[PaymentTime] ";
                    sql += " FROM [dbo].[TransPaymentEntry] AS P ";
                    sql += " WHERE P.[OrderNo] = '" + orderNo + "' ";
                    try
                    {
                        using (SqlConnection connection = new SqlConnection(connectStr))
                        {
                            connection.Open();
                            SqlDataAdapter da = new SqlDataAdapter(sql, connection);
                            DataSet ds = new DataSet();
                            da.Fill(ds, "TransPaymentEntryList");
                            DataTable dt = ds.Tables["TransPaymentEntryList"];

                            data = (from DataRow dr in dt.Rows
                                    select new PaymentEntryListModel()
                                    {
                                        LineNo = int.Parse(dr["LineNo"].ToString()),
                                        OrderNo = dr["OrderNo"].ToString(),
                                        StoreNo = dr["StoreNo"].ToString(),
                                        POSTerminalNo = dr["POSTerminalNo"].ToString(),
                                        TenderType = dr["TenderType"].ToString(),
                                        TenderTypeName = dr["TenderTypeName"].ToString(),
                                        AmountTendered = string.IsNullOrEmpty(dr["AmountTendered"].ToString()) ? 0.0 : double.Parse(dr["AmountTendered"].ToString()),
                                        StaffID = dr["StaffID"].ToString(),
                                        BankPOSCode = dr["BankPOSCode"].ToString(),
                                        BankCardType = dr["BankCardType"].ToString(),
                                        PaymentDate = dr["PaymentDate"] == null ? default(DateTime?) : Convert.ToDateTime(dr["PaymentDate"].ToString()),
                                        PaymentTime = dr["PaymentTime"] == null ? default(DateTime?) : Convert.ToDateTime(dr["PaymentTime"].ToString())

                                    }).ToList();

                            connection.Close();

                            if (data.Count == 0 || data == null) // Không có dữ liệu
                            {
                                return new List<PaymentEntryListModel>();
                            }
                            result = data.ToList();
                            return result;
                        }

                    }
                    catch (Exception ex)
                    {
                        VCM.BLUEPOS.Common.Helpers.LogsFile.WriteLogFile("GetListPaymentEntryByPrintInvoiceOrderSales", "Error=" + "  " + JsonConvert.SerializeObject(ex));
                    }
                }

            }
            catch (Exception ex)
            {
                VCM.BLUEPOS.Common.Helpers.LogsFile.WriteLogFile("GetListPaymentEntryByPrintInvoiceOrderSales", "Error=" + "  " + JsonConvert.SerializeObject(ex));
                return null;
            }
            return new List<PaymentEntryListModel>();
        }


        public List<TenderTypePaymentModel> GetListTenderTypePayment(string posID, string orderNo)
        {
            var data = new List<TenderTypePaymentModel>();
            var result = new List<TenderTypePaymentModel>();
            var ipAddress = GetIPAddessByPOSTerminal(posID);
            try
            {
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    string connectStr = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", ipAddress, "POS", "POS@1234", "Store");
                    var sql = "SELECT P.[OrderNo], P.[AmountTendered], P.[AmountInCurrency], P.[ApprovalCode], P.[CardOrAccount], P.[ReferenceNo], P.[TenderType], P.[TenderTypeName], T.[Code], T.[PrimaryCode],  T.[Description], T.[MayBeUsed], T.[Status]";
                    sql += " FROM [dbo].[TenderTypeSetup] AS T ";
                    sql += " INNER JOIN [dbo].[TransPaymentEntry] AS P ON P.[TenderType] = T.[Code] ";
                    sql += " WHERE T.[MayBeUsed] = 1 ";
                    sql += " AND T.[Status] = 1 ";
                    sql += " AND P.[OrderNo] = '" + orderNo + "' ";

                    try
                    {
                        using (SqlConnection connection = new SqlConnection(connectStr))
                        {
                            connection.Open();
                            SqlDataAdapter da = new SqlDataAdapter(sql, connection);
                            DataSet ds = new DataSet();
                            da.Fill(ds, "TenderTypePaymentList");
                            DataTable dt = ds.Tables["TenderTypePaymentList"];

                            data = (from DataRow dr in dt.Rows
                                    select new TenderTypePaymentModel()
                                    {
                                        Code = dr["TenderType"].ToString(),
                                        TenderType = dr["TenderType"].ToString(),
                                        TenderTypeName = dr["TenderTypeName"].ToString(),
                                        Amount = string.IsNullOrEmpty(dr["AmountTendered"].ToString()) ? 0.0 : double.Parse(dr["AmountTendered"].ToString()),
                                        AmountTendered = string.IsNullOrEmpty(dr["AmountTendered"].ToString()) ? 0.0 : double.Parse(dr["AmountTendered"].ToString()),
                                        AmountInCurrency = string.IsNullOrEmpty(dr["AmountInCurrency"].ToString()) ? 0.0 : double.Parse(dr["AmountInCurrency"].ToString()),
                                        Description = dr["Description"].ToString(),
                                        ApprovalCode = dr["ApprovalCode"].ToString(),
                                        CardOrAccount = dr["CardOrAccount"].ToString(),
                                        ReferenceNo = dr["ReferenceNo"].ToString()

                                    }).ToList();
                            connection.Close();
                            if (data.Count == 0 || data == null) // Không có dữ liệu
                            {
                                return new List<TenderTypePaymentModel>();
                            }
                            result = data.ToList();
                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        VCM.BLUEPOS.Common.Helpers.LogsFile.WriteLogFile("GetListTenderTypePayment", "Error=" + "  " + JsonConvert.SerializeObject(ex));
                    }
                }
            }
            catch (Exception ex)
            {
                VCM.BLUEPOS.Common.Helpers.LogsFile.WriteLogFile("GetListTenderTypePayment", "Error=" + "  " + JsonConvert.SerializeObject(ex));
                return null;
            }
            return new List<TenderTypePaymentModel>();
        }

        public List<TenderTypeSetUpModel> GetListTenderTypeSetUp(string posID, string orderNo)
        {
            var data = new List<TenderTypeSetUpModel>();
            var result = new List<TenderTypeSetUpModel>();
            var ipAddress = GetIPAddessByPOSTerminal(posID);
            try
            {
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    string connectStr = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", ipAddress, "POS", "POS@1234", "Store");
                    var sql = "SELECT T.[Code], T.[PrimaryCode],  T.[Description], T.[MayBeUsed], T.[Status] ";
                    sql += " FROM [dbo].[TenderTypeSetup] AS T ";
                    sql += " WHERE T.[MayBeUsed] = 1 ";
                    sql += " AND T.[Status] = 1 ";

                    try
                    {
                        using (SqlConnection connection = new SqlConnection(connectStr))
                        {
                            connection.Open();
                            SqlDataAdapter da = new SqlDataAdapter(sql, connection);
                            DataSet ds = new DataSet();
                            da.Fill(ds, "TenderTypeSetUpList");
                            DataTable dt = ds.Tables["TenderTypeSetUpList"];
                            data = (from DataRow dr in dt.Rows
                                    select new TenderTypeSetUpModel()
                                    {
                                        Code = dr["Code"].ToString(),
                                        PrimaryCode = dr["PrimaryCode"].ToString(),
                                        Description = dr["Description"].ToString(),
                                        MayBeUsed = int.Parse(dr["MayBeUsed"].ToString()),
                                        Status = bool.Parse(dr["Status"].ToString())
                                    }).ToList();

                            connection.Close();
                            if (data.Count == 0 || data == null) // Không có dữ liệu
                            {
                                return new List<TenderTypeSetUpModel>();
                            }
                            result = data.ToList();
                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        VCM.BLUEPOS.Common.Helpers.LogsFile.WriteLogFile("GetListTenderTypeSetUp", "Error=" + "  " + JsonConvert.SerializeObject(ex));
                    }
                }

            }
            catch (Exception ex)
            {
                VCM.BLUEPOS.Common.Helpers.LogsFile.WriteLogFile("GetListTenderTypeSetUp", "Error=" + "  " + JsonConvert.SerializeObject(ex));
                return null;
            }
            return new List<TenderTypeSetUpModel>();
        }

        //
        public List<TransCpnVchIssueModel> GetTransCpnVchIssue(string posID, string orderNo)
        {
            var data = new List<TransCpnVchIssueModel>();
            var result = new List<TransCpnVchIssueModel>();
            var ipAddress = GetIPAddessByPOSTerminal(posID);
            try
            {
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    string connectStr = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", ipAddress, "POS", "POS@1234", "Store");
                    var sql = "SELECT I.[OrderNo], I.[Voucher_Type], I.[SerialNo], I.[Voucher_Value], I.[ItemName], ";
                    sql += " I.[Validity_From_Date], ";
                    sql += " I.[Expiry_Date] ";
                    sql += " FROM [dbo].[TransCpnVchIssue] AS I ";
                    sql += " WHERE I.[OrderNo] = '" + orderNo + "' ";

                    try
                    {
                        using (SqlConnection connection = new SqlConnection(connectStr))
                        {
                            connection.Open();
                            SqlDataAdapter da = new SqlDataAdapter(sql, connection);
                            DataSet ds = new DataSet();
                            da.Fill(ds, "TransCpnVchIssueList");
                            DataTable dt = ds.Tables["TransCpnVchIssueList"];
                            data = (from DataRow dr in dt.Rows
                                    select new TransCpnVchIssueModel()
                                    {
                                        ItemName = dr["ItemName"].ToString(),
                                        OrderNo = dr["OrderNo"].ToString(),
                                        Voucher_Type = dr["Voucher_Type"].ToString(),
                                        SerialNo = dr["SerialNo"].ToString(),
                                        Voucher_Value = string.IsNullOrEmpty(dr["Voucher_Value"].ToString()) ? 0.0 : double.Parse(dr["Voucher_Value"].ToString()),
                                        Validity_From_Date = dr["Validity_From_Date"] == null ? default(DateTime?) : Convert.ToDateTime(dr["Validity_From_Date"].ToString()),
                                        Expiry_Date = dr["Expiry_Date"] == null ? default(DateTime?) : Convert.ToDateTime(dr["Expiry_Date"].ToString())
                                    }).ToList();

                            connection.Close();

                            if (data.Count == 0 || data == null) // Không có dữ liệu
                            {
                                return new List<TransCpnVchIssueModel>();
                            }
                            result = data.ToList();
                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        VCM.BLUEPOS.Common.Helpers.LogsFile.WriteLogFile("GetTransCpnVchIssue", "Error=" + "  " + JsonConvert.SerializeObject(ex));
                    }
                }
            }
            catch (Exception ex)
            {
                VCM.BLUEPOS.Common.Helpers.LogsFile.WriteLogFile("GetTransCpnVchIssue", "Error=" + "  " + JsonConvert.SerializeObject(ex));
                return null;
            }
            return new List<TransCpnVchIssueModel>();
        }

        //
        public List<MarketingResponseModel> GetMarketingDataList(string posID, string storeNo)
        {
            var data = new List<MarketingResponseModel>();
            var result = new List<MarketingResponseModel>();
            var ipAddress = GetIPAddessByPOSTerminal(posID);
            try
            {
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    string connectStr = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", ipAddress, "POS", "POS@1234", "Store");
                    var sql = "SELECT M.[StoreNo], M.[POSTerminal], M.[QRLink], M.[Content], M.[ContentType], M.[IsActive], M.[Status] ";
                    sql += " FROM [dbo].[Marketing] AS M ";
                    sql += " WHERE M.[POSTerminal] = '" + posID + "' ";
                    sql += " AND M.[StoreNo] = '" + storeNo + "' ";
                    sql += " AND M.[IsActive] = 1 ";

                    try
                    {
                        using (SqlConnection connection = new SqlConnection(connectStr))
                        {
                            connection.Open();
                            SqlDataAdapter da = new SqlDataAdapter(sql, connection);
                            DataSet ds = new DataSet();
                            da.Fill(ds, "MarketingList");
                            DataTable dt = ds.Tables["MarketingList"];
                            data = (from DataRow dr in dt.Rows
                                    select new MarketingResponseModel()
                                    {
                                        ContentType = dr["ContentType"].ToString(),
                                        QRLink = dr["QRLink"].ToString(),
                                        Content = dr["Content"].ToString()
                                    }).ToList();

                            connection.Close();

                            if (data.Count == 0 || data == null) // Không có dữ liệu
                            {
                                return new List<MarketingResponseModel>();
                            }
                            result = data.ToList();
                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        VCM.BLUEPOS.Common.Helpers.LogsFile.WriteLogFile("GetMarketingDataList", "Error=" + "  " + JsonConvert.SerializeObject(ex));
                    }
                }
            }
            catch (Exception ex)
            {
                VCM.BLUEPOS.Common.Helpers.LogsFile.WriteLogFile("GetMarketingDataList", "Error=" + "  " + JsonConvert.SerializeObject(ex));
                return null;
            }
            return new List<MarketingResponseModel>();
        }

        public List<CompanyInforModel> GetCompanyInforList(string posID, string storeNo)
        {
            var data = new List<CompanyInforModel>();
            var result = new List<CompanyInforModel>();
            var ipAddress = GetIPAddessByPOSTerminal(posID);

            try
            {
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    string connectStr = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", ipAddress, "POS", "POS@1234", "Store");
                    var sql = "SELECT S.[PrintReceiptLogo] AS Hotline, (SELECT p.[Value] FROM [dbo].[POSDataSetup] AS P WHERE P.[Code] = 'WEBSITE') AS Website ";
                    sql += " FROM [dbo].[Store] AS S ";
                    sql += " WHERE S.[No] = '" + storeNo + "' ";

                    try
                    {
                        using (SqlConnection connection = new SqlConnection(connectStr))
                        {
                            connection.Open();
                            SqlDataAdapter da = new SqlDataAdapter(sql, connection);
                            DataSet ds = new DataSet();
                            da.Fill(ds, "CompanyInforList");
                            DataTable dt = ds.Tables["CompanyInforList"];

                            data = (from DataRow dr in dt.Rows
                                    select new CompanyInforModel()
                                    {
                                        Hotline = dr["Hotline"].ToString(),
                                        Website = dr["Website"].ToString()
                                    }).ToList();

                            connection.Close();

                            if (data.Count == 0 || data == null) // Không có dữ liệu
                            {
                                return new List<CompanyInforModel>();
                            }

                            result = data.ToList();
                            return result;
                        }

                    }
                    catch (Exception ex)
                    {
                        VCM.BLUEPOS.Common.Helpers.LogsFile.WriteLogFile("GetCompanyInforList", "Error=" + "  " + JsonConvert.SerializeObject(ex));
                    }
                }
            }
            catch (Exception ex)
            {
                VCM.BLUEPOS.Common.Helpers.LogsFile.WriteLogFile("GetCompanyInforList", "Error=" + "  " + JsonConvert.SerializeObject(ex));
                return null;
            }
            return new List<CompanyInforModel>();
        }

        public List<PaymentEntryReturnVoucherListModel> GetReturnVoucherAmountByPrintInvoiceOrderSales(string storeNo, string posID, string orderNo)
        {
            var data = new List<PaymentEntryReturnVoucherListModel>();
            var result = new List<PaymentEntryReturnVoucherListModel>();
            var ipAddress = GetIPAddessByPOSTerminal(posID);
            try
            {
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    string connectStr = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", ipAddress, "POS", "POS@1234", "Store");

                    var sql = "SELECT P.[OrderNo], P.[StoreNo], P.[POSTerminalNo], P.[TenderType], P.[AmountTendered], P.[AmountInCurrency], P.[ReferenceNo], H.[SalesIsReturn], H.[ReturnVoucherNo], H.[ReturnVoucherExpire] ";
                    sql += " FROM [dbo].[TransPaymentEntry] AS P ";
                    sql += " INNER JOIN [dbo].[TransHeader] AS H ON P.[OrderNo] = H.[OrderNo] ";
                    sql += " WHERE H.[StoreNo] = '" + storeNo + "'";
                    sql += " AND H.[OrderNo] = '" + orderNo + "'";
                    sql += " AND P.[TenderType] = 'ZRET'  ";     // Biên nhận mua hàng

                    try
                    {
                        using (SqlConnection connection = new SqlConnection(connectStr))
                        {
                            connection.Open();
                            SqlDataAdapter da = new SqlDataAdapter(sql, connection);
                            DataSet ds = new DataSet();
                            da.Fill(ds, "ReturnVoucherNoList");
                            DataTable dt = ds.Tables["ReturnVoucherNoList"];

                            data = (from DataRow dr in dt.Rows
                                    select new PaymentEntryReturnVoucherListModel()
                                    {
                                        StoreNo = dr["StoreNo"].ToString(),
                                        POSTerminalNo = dr["POSTerminalNo"].ToString(),
                                        OrderNo = dr["OrderNo"].ToString(),
                                        SalesIsReturn = int.Parse(dr["SalesIsReturn"].ToString()),
                                        ReturnVoucherExpire = string.IsNullOrEmpty(dr["ReturnVoucherExpire"].ToString()) ? string.Empty : dr["ReturnVoucherExpire"].ToString(),
                                        TenderType = dr["TenderType"].ToString(),
                                        ReferenceNo = dr["ReferenceNo"].ToString(),
                                        ReturnVoucherNo = dr["ReturnVoucherNo"].ToString(),
                                        AmountTendered = string.IsNullOrEmpty(dr["AmountTendered"].ToString()) ? 0.0 : double.Parse(dr["AmountTendered"].ToString()),
                                        AmountInCurrency = string.IsNullOrEmpty(dr["AmountTendered"].ToString()) ? 0.0 : double.Parse(dr["AmountTendered"].ToString())
                                    }).ToList();

                            connection.Close();
                            if (data.Count == 0 || data == null) // Không có dữ liệu
                            {
                                return new List<PaymentEntryReturnVoucherListModel>();
                            }
                            result = data.ToList();
                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        VCM.BLUEPOS.Common.Helpers.LogsFile.WriteLogFile("GetReturnVoucherAmountByPrintInvoiceOrderSales", "Error=" + "  " + JsonConvert.SerializeObject(ex));
                    }
                }

            }
            catch (Exception ex)
            {
                VCM.BLUEPOS.Common.Helpers.LogsFile.WriteLogFile("GetReturnVoucherAmountByPrintInvoiceOrderSales", "Error=" + "  " + JsonConvert.SerializeObject(ex));
                return null;
            }
            return new List<PaymentEntryReturnVoucherListModel>();
        }

        public List<ReasonReturnProductModel> GetReasonCodeByPrintInvoiceOrderSales(string storeNo, string posID, string reasonCode)
        {
            var data = new List<ReasonReturnProductModel>();
            var result = new List<ReasonReturnProductModel>();
            var ipAddress = GetIPAddessByPOSTerminal(posID);
            try
            {
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    string connectStr = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", ipAddress, "POS", "POS@1234", "Store");

                    var sql = "SELECT R.[Code], R.[Description], R.[Group], R.[Pkey], R.[Status] ";
                    sql += " FROM [dbo].[ReasonCode] AS R ";
                    sql += " WHERE R.[Code] = '" + reasonCode + "'";
                    sql += " AND R.[Status] = 1  ";

                    try
                    {
                        using (SqlConnection connection = new SqlConnection(connectStr))
                        {
                            connection.Open();
                            SqlDataAdapter da = new SqlDataAdapter(sql, connection);
                            DataSet ds = new DataSet();
                            da.Fill(ds, "ReasonCodeList");
                            DataTable dt = ds.Tables["ReasonCodeList"];

                            data = (from DataRow dr in dt.Rows
                                    select new ReasonReturnProductModel()
                                    {
                                        Code = dr["Code"].ToString(),
                                        Description = dr["Description"].ToString()
                                    }).ToList();

                            connection.Close();
                            if (data.Count == 0 || data == null) // Không có dữ liệu
                            {
                                return new List<ReasonReturnProductModel>();
                            }
                            result = data.ToList();
                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        VCM.BLUEPOS.Common.Helpers.LogsFile.WriteLogFile("GetReasonCodeByPrintInvoiceOrderSales", "Error=" + "  " + JsonConvert.SerializeObject(ex));
                    }
                }

            }
            catch (Exception ex)
            {
                VCM.BLUEPOS.Common.Helpers.LogsFile.WriteLogFile("GetReasonCodeByPrintInvoiceOrderSales", "Error=" + "  " + JsonConvert.SerializeObject(ex));
                return null;
            }
            return new List<ReasonReturnProductModel>();
        }

        public List<VinIDRateByLoyatylRateModel> GetVinIDRateByLoyaltyRate(string storeNo, string posID)
        {
            var data = new List<VinIDRateByLoyatylRateModel>();
            var result = new List<VinIDRateByLoyatylRateModel>();
            var ipAddress = GetIPAddessByPOSTerminal(posID);
            try
            {
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    string connectStr = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", ipAddress, "POS", "POS@1234", "Store");
                    var sql = "SELECT L.[Code], L.[Rate], L.[FromDate], L.[ToDate], L.[Pkey] ";
                    sql += " FROM [dbo].[LoyaltyRate] AS L ";
                    sql += " WHERE L.[Code] = 'VINIDRATE' ";
                    sql += " AND L.[Enable] = 1 ";
                    sql += " AND (GetDate() >= L.[FromDate]  AND GetDate() <= L.[ToDate]) ";

                    try
                    {
                        using (SqlConnection connection = new SqlConnection(connectStr))
                        {
                            connection.Open();
                            SqlDataAdapter da = new SqlDataAdapter(sql, connection);
                            DataSet ds = new DataSet();
                            da.Fill(ds, "LoyaltyRateList");
                            DataTable dt = ds.Tables["LoyaltyRateList"];

                            data = (from DataRow dr in dt.Rows
                                    select new VinIDRateByLoyatylRateModel()
                                    {
                                        Code = dr["Code"].ToString(),
                                        Rate = string.IsNullOrEmpty(dr["Rate"].ToString()) ? 0.0 : double.Parse(dr["Rate"].ToString()),
                                        FromDate = dr["FromDate"] == null ? default(DateTime?) : Convert.ToDateTime(dr["FromDate"].ToString()),
                                        ToDate = dr["ToDate"] == null ? default(DateTime?) : Convert.ToDateTime(dr["ToDate"].ToString()),
                                        Pkey = dr["Pkey"].ToString()
                                    }).ToList();

                            connection.Close();
                            if (data.Count == 0 || data == null) // Không có dữ liệu
                            {
                                return new List<VinIDRateByLoyatylRateModel>();
                            }
                            result = data.ToList();
                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        VCM.BLUEPOS.Common.Helpers.LogsFile.WriteLogFile("GetVinIDRateByLoyaltyRate", "Error=" + "  " + JsonConvert.SerializeObject(ex));
                    }
                }

            }
            catch (Exception ex)
            {
                VCM.BLUEPOS.Common.Helpers.LogsFile.WriteLogFile("GetVinIDRateByLoyaltyRate", "Error=" + "  " + JsonConvert.SerializeObject(ex));
                return null;
            }
            return new List<VinIDRateByLoyatylRateModel>();
        }

        public List<ListPOSDataSetupModel> GetPOSDataSetup(string storeNo, string posID)
        {
            var data = new List<ListPOSDataSetupModel>();
            var result = new List<ListPOSDataSetupModel>();
            var ipAddress = GetIPAddessByPOSTerminal(posID);
            try
            {
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    string connectStr = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", ipAddress, "POS", "POS@1234", "Store");
                    var sql = "SELECT P.[Code], P.[Value], P.[Description], P.[Pkey] ";
                    sql += " FROM [dbo].[POSDataSetup] AS P ";

                    try
                    {
                        using (SqlConnection connection = new SqlConnection(connectStr))
                        {
                            connection.Open();
                            SqlDataAdapter da = new SqlDataAdapter(sql, connection);
                            DataSet ds = new DataSet();
                            da.Fill(ds, "POSDataSetupList");
                            DataTable dt = ds.Tables["POSDataSetupList"];

                            data = (from DataRow dr in dt.Rows
                                    select new ListPOSDataSetupModel()
                                    {
                                        Code = dr["Code"].ToString(),
                                        Value = dr["Value"].ToString(),
                                        Description = dr["Description"].ToString(),
                                        Pkey = dr["Pkey"].ToString()
                                    }).ToList();

                            connection.Close();
                            if (data.Count == 0 || data == null) // Không có dữ liệu
                            {
                                return new List<ListPOSDataSetupModel>();
                            }
                            result = data.ToList();
                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        VCM.BLUEPOS.Common.Helpers.LogsFile.WriteLogFile("GetPOSDataSetup", "Error=" + "  " + JsonConvert.SerializeObject(ex));
                    }
                }

            }
            catch (Exception ex)
            {
                VCM.BLUEPOS.Common.Helpers.LogsFile.WriteLogFile("GetPOSDataSetup", "Error=" + "  " + JsonConvert.SerializeObject(ex));
                return null;
            }
            return new List<ListPOSDataSetupModel>();
        }




























    }
}
