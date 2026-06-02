using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using VCM.POSBLUE.Data.DataBaseContext;
using VCM.POSBLUE.Model.SAP;

namespace VCM.POSBLUE.Data.POS
{
    public interface ISAPData
    {
        //VoucherModel CheckVoucher(VoucherRequest model);
        //VoucherModel CheckReturnVoucher(VoucherRequest model);
        //UpdateDataResponse<List<VoucherStatusResponseModel>> UpdateVourcher(VoucherUpdateRequest model);
        //UpdateDataResponse<List<VoucherStatusResponseModel>> UpdateReturVourcher(VoucherUpdateRequest model);
        //CreateDataResponse<List<VoucherStatusResponseModel>> CreateVoucher(List<CreateVoucherModel> model);
        //CreateDataResponse<List<VoucherStatusResponseModel>> CreateReturnVoucher(List<CreateVoucherModel> model);
        void WriteLogSAP(LogSAPModel model);
        Tuple<bool, HttpStatusCode, string, CpnVchCodeSendModel> SendCodeToPOS(CpnVchCodeSendRequest model);
    }
    public class SAPData : ISAPData
    {
        private readonly object lockRequest = new object();
        public void WriteLogSAP(LogSAPModel model)
        {
            using (var db = new LoyaltyEntityContainer())
            {
                try
                {
                    if (model != null)
                    {
                        var itemSave = new LogSAP
                        {
                            StoreNo = model.StoreNo,
                            POSTerminal = model.POSTerminal,
                            VoucherNumber = model.VoucherNumber,
                            ActionType = model.ActionType,
                            RequestPOS = model.RequestPOS,
                            RequestSAP = model.RequestSAP,
                            ResponseSAP = model.ResponseSAP,
                            CreatedDate = model.CreatedDate
                        };
                        db.LogSAPs.Add(itemSave);
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message.ToString());
                }
            }
        }
        public Tuple<bool, HttpStatusCode, string, CpnVchCodeSendModel> SendCodeToPOS(CpnVchCodeSendRequest model)
        {
            var modelData = new CpnVchCodeSendModel();
            var result = new Tuple<bool, HttpStatusCode, string, CpnVchCodeSendModel>(false, HttpStatusCode.BadRequest, "", modelData);
            try
            {
                lock (lockRequest)
                {
                    using (var db = new CentralMDContainer())
                    {
                        db.Database.CommandTimeout = 60;
                        using (var transaction = db.Database.BeginTransaction())
                        {
                            try
                            {
                                HttpStatusCode statusCode = HttpStatusCode.OK;
                                string mess = "Success";
                                modelData = db.Database.SqlQuery<CpnVchCodeSendModel>("[dbo].[SP_CpnVchCodeSend] @ItemNo,@StoreNo,@PosID,@OrderNo,@PhoneNumber,@QtyOfDay,@LimitQty",
                                        new SqlParameter("@ItemNo", model.ItemNo ?? ""),
                                        new SqlParameter("@StoreNo", model.StoreNo ?? ""),
                                        new SqlParameter("@PosID", model.PosID ?? ""),
                                        new SqlParameter("@OrderNo", model.OrderNo ?? ""),
                                        new SqlParameter("@PhoneNumber", model.PhoneNumber ?? ""),
                                        new SqlParameter("@QtyOfDay", model.QtyOfDay),
                                        new SqlParameter("@LimitQty", model.LimitQty)
                                        ).FirstOrDefault();

                                if (modelData == null)
                                {
                                    result = new Tuple<bool, HttpStatusCode, string, CpnVchCodeSendModel>(false, HttpStatusCode.NotFound, $"Không tìm thấy mã Coupon/voucher cho item {model.ItemNo} này", modelData);
                                    return result;
                                }

                                if (modelData != null && !string.IsNullOrEmpty(modelData.ItemNo) && string.IsNullOrEmpty(modelData.SerialNumber)) 
                                {
                                    statusCode = HttpStatusCode.NotFound;
                                    if (modelData.ItemNo == "OUT")
                                    {
                                        mess = $"Số điện thoại {model.PhoneNumber} đã hết hạn mức nhận {model.LimitQty} voucher";
                                    }
                                    else if (modelData.ItemNo == "OUT_OF_DAY")
                                    {
                                        mess = $"Số điện thoại {model.PhoneNumber} đã hết hạn mức nhận {model.QtyOfDay} voucher của ngày hôm nay";
                                    }
                                }
                                else
                                {
                                    var insertCodeSend = new CpnVchCodeSend
                                    {
                                        StoreNo = model.StoreNo,
                                        PosID = model.PosID,
                                        Date = model.BussinessDate,
                                        OrderNo = model.OrderNo,
                                        ItemNo = model.ItemNo,
                                        Code = modelData.SerialNumber,
                                        PhoneNumber = model.PhoneNumber ?? "",
                                        CreatedDate = DateTime.Now
                                    };
                                    db.CpnVchCodeSends.Add(insertCodeSend);
                                    db.SaveChanges();
                                }
                                transaction.Commit();
                                result = new Tuple<bool, HttpStatusCode, string, CpnVchCodeSendModel>(true, statusCode, mess, modelData);
                            }
                            catch (Exception ex)
                            {
                                result = new Tuple<bool, HttpStatusCode, string, CpnVchCodeSendModel>(false, HttpStatusCode.ExpectationFailed, JsonConvert.SerializeObject(ex), null);
                                transaction.Rollback();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result = new Tuple<bool, HttpStatusCode, string, CpnVchCodeSendModel>(false, HttpStatusCode.InternalServerError, JsonConvert.SerializeObject(ex), null);
            }
            return result;
        }
    }
}
