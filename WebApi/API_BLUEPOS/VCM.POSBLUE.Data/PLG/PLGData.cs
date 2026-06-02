using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using TCX.API.Common.Models;
using VCM.POSBLUE.Data.DataBaseContext;
using VCM.POSBLUE.Data.DataBaseContext.PLG;
using VCM.POSBLUE.Model.Reward;

namespace VCM.POSBLUE.Data.PLG
{
    public interface IPLGData
    {
        ResultResponse InforVoucher(string seriNo);
        ResultResponse SaleVoucher(SaleVoucherRequestPOS model);
        ResultResponse RedeemVoucher(ReedemVoucherRequestPOS model);
        void LogVoucher(VoucherCardLogModel model);
        void VoucherTrans(List<VoucherCardTransModel> listmodel);
        Tuple<bool, int, string, RewardCodeSendModel> SendCodeReWardToPOS(RewardCodeRequest model);
    }
    public class PLGData : IPLGData
    {
        private readonly object lockRequest = new object();
        public ResultResponse InforVoucher(string seriNo)
        {
            var result = new ResultResponse { Status = HttpStatusCode.OK, Data = null };
            var data = new InforVoucherResponse();
            try
            {
                var date = DateTime.Now.Date;
                using (var db = new PLGContextContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var checkData = db.PL_VoucherCard.Where(d => d.SeriNo == seriNo).ToList();
                    if (checkData.Count() == 0)
                    {
                        result = new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Mã voucher này {seriNo} không tồn tại" };
                    }
                    else
                    {
                        if (checkData.Any(d => d.RemainValue <= 0))
                        {
                            result = new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Mã voucher {seriNo} có giá trị = 0" };
                        }
                        else
                        {
                            var checkValid = checkData.Any(d => d.ValidTo.Value.Date < date);
                            if (checkValid)
                            {
                                result = new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Mã voucher {seriNo} đã hết hạn" };
                            }
                            else
                            {
                                data = checkData.Select(a => new InforVoucherResponse
                                {
                                    TypeVoucher = a.CardType, //Employee: ưu đãi nhân viên,Prepaid: thẻ trả trước,Voucher:thẻ Voucher
                                    StatusVoucher = a.Status,
                                    Value = a.RemainValue

                                }).FirstOrDefault();
                                result = new ResultResponse { Status = HttpStatusCode.OK, Message = "OK", Data = data };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result = new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Lỗi data {JsonConvert.SerializeObject(ex)}" };
            }
            return result;
        }

        public ResultResponse SaleVoucher(SaleVoucherRequestPOS model)
        {
            var result = new ResultResponse { Status = HttpStatusCode.OK, Data = null };
            var data = new InforVoucherResponse();
            try
            {
                var date = DateTime.Now.Date;
                using (var db = new PLGContextContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var getSerial = db.PL_VoucherCard.Where(d => d.SeriNo == model.seriNo);
                    if (getSerial.Count() > 0)
                    {
                        if (getSerial.Any(d => d.Value <= 0))
                        {
                            result = new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Voucher này {model.seriNo} không hợp lệ khi có value={getSerial.FirstOrDefault().Value}" };

                        }
                        else if (getSerial.Any(d => d.Status == ConstStatus.A))
                        {
                            result = new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Voucher này {model.seriNo} đã được kích hoạt" };

                        }
                        else if (getSerial.Any(d => d.Status == ConstStatus.R))
                        {
                            result = new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Voucher này {model.seriNo} đã được sử dụng" };

                        }
                        else if (getSerial.Any(d => d.Status == ConstStatus.E))
                        {
                            result = new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Voucher này {model.seriNo} đã hết hạn" };

                        }
                        else if (getSerial.Any(d => d.Status == ConstStatus.D))
                        {
                            result = new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Voucher này {model.seriNo} đã được hủy" };
                        }
                        else
                        {
                            var dateTime = DateTime.Now;
                            var updateData = getSerial.FirstOrDefault(d => d.Status == ConstStatus.N);
                            updateData.Status = ConstStatus.A;
                            updateData.ActivateDate = dateTime;
                            updateData.ValidFrom = dateTime;
                            updateData.ValidTo = dateTime.AddDays((int)updateData.ValidDays);
                            updateData.UserName = model.staffCode;

                            db.SaveChanges();
                            //var value = getSerial.FirstOrDefault().Value;
                            var dataCard = new VoucherCardTransResponse { StoreNo = model.storeNo, PosID = model.posID, SeriNo = model.seriNo, StatusVoucher = ConstStatus.A, Value = model.salePrice };

                            var listVoucherTrans = new List<VoucherCardTransModel>
                            {
                                new VoucherCardTransModel
                                {
                                    Partner = model.partner,
                                    SeriNo = model.seriNo,
                                    StoreNo = model.storeNo,
                                    PosID = model.posID,
                                    Status = true,
                                    Value = model.salePrice,
                                    SalePrice = model.salePrice,
                                    DiscountAmount = model.discountAmount,
                                    FunctionName = "SaleVoucher",
                                    UserName = model.staffCode
                                }
                            };
                            VoucherTrans(listVoucherTrans);

                            result = new ResultResponse { Status = HttpStatusCode.OK, Message = $"Cập nhật thành công voucher {model.seriNo}", Data = dataCard };
                        }
                    }
                    else
                    {
                        result = new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Voucher này {model.seriNo} không tồn tại trong hệ thống" };
                    }
                }
            }
            catch (Exception ex)
            {
                result = new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Lỗi data {JsonConvert.SerializeObject(ex)}" };
            }
            return result;
        }

        public ResultResponse RedeemVoucher(ReedemVoucherRequestPOS model)
        {
            var result = new ResultResponse { Status = HttpStatusCode.OK, Data = null };
            var data = new InforVoucherResponse();
            try
            {
                var date = DateTime.Now.Date;
                using (var db = new PLGContextContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var listVoucher = model.listSeriNo;
                    var check = true;
                    foreach (var code in listVoucher)
                    {
                        var getCode = db.PL_VoucherCard.Where(d => d.SeriNo == code.seriNo);
                        if (getCode.Count() > 0)
                        {
                            if (getCode.Any(d => d.Status == ConstStatus.R))
                            {
                                result = new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Voucher này {code.seriNo} đã được sử dụng" };
                                check = false;
                            }
                            else if (getCode.Any(d => d.RemainValue <= 0))
                            {
                                result = new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Voucher này {code.seriNo} không hợp lệ khi có giá trị ={getCode.FirstOrDefault().RemainValue}" };
                                check = false;
                            }
                            else if (getCode.Any(d => d.RemainValue < code.value))
                            {
                                result = new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Không được phép sử dụng giá trị lớn hơn giá trị voucher {getCode.FirstOrDefault().RemainValue}" };
                                check = false;
                            }
                            else if (getCode.Any(d => d.Status == ConstStatus.N))
                            {
                                result = new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Voucher này {code.seriNo} chưa được kích hoạt" };
                                check = false;
                            }
                            else if (getCode.Any(d => d.Status == ConstStatus.E))
                            {
                                result = new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Voucher này {code.seriNo} đã hết hạn" };
                                check = false;
                            }
                            else if (getCode.Any(d => d.Status == ConstStatus.D))
                            {
                                result = new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Voucher này {code.seriNo} đã được hủy" };
                                check = false;
                            }
                        }
                        else
                        {
                            result = new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Voucher này {code.seriNo} không tồn tại trong hệ thống" };
                            check = false;
                        }
                    }
                    if (check)
                    {
                        var dateTime = DateTime.Now;
                        foreach (var code in listVoucher)
                        {
                            var getCode = db.PL_VoucherCard.Where(d => d.SeriNo == code.seriNo);
                            var updateData = getCode.FirstOrDefault(d => d.Status == ConstStatus.A || d.Status == ConstStatus.R);
                            updateData.Status = ConstStatus.R;
                            updateData.RemainValue -= code.value;
                            updateData.LastUsedStore = model.storeNo;
                            updateData.LastUsedPosID = model.posID;
                            updateData.LastUsedDate = dateTime;
                            updateData.UserName = model.staffCode;
                        }
                        db.SaveChanges();

                        var listVoucherTrans = new List<VoucherCardTransModel>();
                        listVoucherTrans.AddRange(listVoucher.Select(a => new VoucherCardTransModel
                        {
                            Partner = model.partner,
                            SeriNo = a.seriNo,
                            StoreNo = model.storeNo,
                            PosID = model.posID,
                            Status = true,
                            Value = a.value,
                            FunctionName = "RedeemVoucher",
                            UserName = model.staffCode
                        }));
                        VoucherTrans(listVoucherTrans);
                        result = new ResultResponse { Status = HttpStatusCode.OK, Message = $"Sử dụng thành công {listVoucher.Count} thành công" };
                    }
                }
            }
            catch (Exception ex)
            {
                result = new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Lỗi data {JsonConvert.SerializeObject(ex)}" };
            }
            return result;
        }

        public void VoucherTrans(List<VoucherCardTransModel> listmodel)
        {
            try
            {
                using (var db = new PLGContextContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var insertVoucherTrans = listmodel.Select(model => new PL_VoucherCardTrans
                    {
                        Partner = model.Partner,
                        SeriNo = model.SeriNo,
                        StoreNo = model.StoreNo,
                        PosID = model.PosID,
                        Value = model.Value,
                        SalePrice = model.SalePrice,
                        DiscountAmount = model.DiscountAmount,
                        Status = model.Status,
                        FunctionName = model.FunctionName,
                        CreatedDate = DateTime.Now,
                        UserName = model.UserName
                    });
                    db.PL_VoucherCardTrans.AddRange(insertVoucherTrans);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                LogVoucher(new VoucherCardLogModel
                {
                    Partner = listmodel.FirstOrDefault().Partner,
                    StoreNo = listmodel.FirstOrDefault().StoreNo,
                    PosID = listmodel.FirstOrDefault().PosID,
                    FunctionName = "Trans_" + listmodel.FirstOrDefault().FunctionName,
                    SeriNo = string.Join(",", listmodel.Select(a => a.SeriNo)),
                    PosRequest = JsonConvert.SerializeObject(listmodel),
                    MsgError = JsonConvert.SerializeObject(ex)
                });
            }
        }

        public void LogVoucher(VoucherCardLogModel model)
        {
            try
            {
                using (var db = new PLGContextContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var insertVoucherTrans = new VoucherCardLog
                    {
                        Partner = model.Partner,
                        SeriNo = model.SeriNo,
                        StoreNo = model.StoreNo,
                        PosID = model.PosID,
                        Value = model.Value,
                        Status = model.Status,
                        FunctionName = model.FunctionName,
                        PosRequest = model.PosRequest,
                        PosResponse = model.PosResponse,
                        PartnerRequest = model.PartnerRequest,
                        PartnerResponse = model.PartnerResponse,
                        CreatedDate = DateTime.Now,
                        UserName = model.UserName,
                        MsgError = model.MsgError,
                        StatusCode = model.StatusCode,
                        TimeResponse = model.TimeResponse

                    };
                    db.VoucherCardLogs.Add(insertVoucherTrans);
                    db.SaveChanges();
                }
            }
            catch 
            {
                throw;
            }
        }

        public Tuple<bool, int, string, RewardCodeSendModel> SendCodeReWardToPOS(RewardCodeRequest model)
        {
            var modelData = new RewardCodeSendModel();
            var result = new Tuple<bool, int, string, RewardCodeSendModel>(false, 400, "", modelData);
            try
            {
                lock (lockRequest)
                {
                    using (var db = new CentralMDContainer())
                    {
                        db.Database.CommandTimeout = 2 * 60;
                        using (var transaction = db.Database.BeginTransaction())
                        {
                            try
                            {
                                modelData = db.Database.SqlQuery<RewardCodeSendModel>("[dbo].[RewardCodeIssue] @OfferNo",
                                    new SqlParameter("@OfferNo", model.OfferNo)
                                    ).FirstOrDefault();

                                if (modelData == null)
                                {
                                    result = new Tuple<bool, int, string, RewardCodeSendModel>(true, 204, $"Không tìm thấy Code cho Offer {model.OfferNo} này", modelData);
                                    return result;
                                }

                                var dateTime = DateTime.Now;
                                if (modelData.ToDate.Value.Date < dateTime.Date)
                                {
                                    result = new Tuple<bool, int, string, RewardCodeSendModel>(false, 400, $"Đã hết thời hạn phát hành mã dự thưởng", modelData);
                                    return result;
                                }

                                if (!string.IsNullOrEmpty(modelData.Code))
                                {
                                    var insertCodeSend = new RewardCodeSend
                                    {
                                        StoreNo = model.StoreNo,
                                        PosID = model.PosID,
                                        Date = model.BussinessDate,
                                        OrderNo = model.OrderNo,
                                        OfferNo = model.OfferNo,
                                        Code = modelData.Code,
                                        CreatedDate = dateTime,
                                        IPServer = model.IPServer,
                                    };

                                    db.RewardCodeSends.Add(insertCodeSend);
                                    db.SaveChanges();
                                    transaction.Commit();
                                }
                                result = new Tuple<bool, int, string, RewardCodeSendModel>(true, 200, "Success", modelData);

                            }
                            catch (Exception ex)
                            {
                                result = new Tuple<bool, int, string, RewardCodeSendModel>(false, 500, JsonConvert.SerializeObject(ex), modelData);
                                transaction.Rollback();
                            }
                        }
                    }
                }            
               
            }
            catch (Exception ex)
            {
                result = new Tuple<bool, int, string, RewardCodeSendModel>(false, 500, JsonConvert.SerializeObject(ex), modelData);
            }
            return result;
        }
    }
}
