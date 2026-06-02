
using System.Collections.Generic;
using System.Data;
using System;
using TCX.API.Common.Dtos.Loyalty.WinCode;
using TCX.WebApiCore.EntityFrameworkCore.Dapper;
using Dapper;
using VCM.POSBLUE.Model.WinLife;
using System.Linq;
using Newtonsoft.Json;
using TCX.API.Common.Helpers;

namespace TCX.WebApiCore
{
    public class WincodeRepository
    {
        private readonly KibanaService _kibanaService;
        public WincodeRepository()
        {
            _kibanaService = new KibanaService();
        }
        public List<WinCodeCustomerDto> GetWinCodeCustomer(string phoneNumber)
        {
            try
            {
                using (IDbConnection db = DapperCentralMDFactory.CreateConnection())
                {
                    db.Open();
                    return db.Query<WinCodeCustomerDto>("SELECT * FROM WinCodeCustomer (NOLOCK) WHERE MemberCard = '" + phoneNumber + "';").ToList();
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("GetWinCodeCustomer", ex);
                return null;
            }
        }
        public Tuple<bool, string> UpdateWincodeCustomer(WinLife_UpdatePromotions_POS_Request request)
        {
            bool flag = true;
            string message = "OK";
            string query = @"SELECT * FROM WinCodeCustomer (NOLOCK) " +
                            " WHERE OrderNo = '" + request.orderNo + "'" +
                            " AND MemberCard = '" + request.phoneNumber + "'";

            using (IDbConnection db = DapperCentralMDFactory.CreateConnection())
            {
                db.Open();
                var updateWinCodeCustomerDto = db.Query<WinCodeCustomerDto>(query).ToList();
                using (var transaction = db.BeginTransaction())
                {
                    try
                    {
                        if (updateWinCodeCustomerDto != null && updateWinCodeCustomerDto.Count > 0)
                        {
                            var insertWinCodeCustomerDto = new List<WinCodeCustomerDto>();
                            foreach (var item in request.appliedCodes)
                            {
                                insertWinCodeCustomerDto.Add(new WinCodeCustomerDto()
                                {
                                    ID = Guid.NewGuid(),
                                    WinCode = item.winCode,
                                    MemberCard = request.phoneNumber,
                                    Csn = request.csn,
                                    QuantityReciepted = item.quantity * (-1),
                                    QuantityRecieptedSum = item.quantity * (-1),
                                    OrderNo = request.orderNo + "9",
                                    StoreNo = request.storeCode,
                                    PosNo = request.posCode,
                                    IsDeleted = true,
                                    TransDate = DateTime.Now.Date,
                                    CreatedBy = request.posCode,
                                    UpdatedBy = request.posCode,
                                    CreatedDate = DateTime.Now,
                                    UpdatedDate = DateTime.Now
                                });
                            }
                            string queryIns = @"INSERT INTO [dbo].[WinCodeCustomer]
                                            ([ID],[WinCode],[MemberCard],[Csn],[QuantityRecieptedSum],[QuantityReciepted],[OrderNo],[StoreNo],[PosNo],[IsDeleted]
                                            ,[TransDate],[CreatedDate],[UpdatedDate],[CreatedBy],[UpdatedBy]) " +
                                                " VALUES (@ID,@WinCode,@MemberCard,@Csn,@QuantityRecieptedSum,@QuantityReciepted,@OrderNo,@StoreNo,@PosNo,@IsDeleted" +
                                                " ,@TransDate,@CreatedDate,@UpdatedDate,@CreatedBy,@UpdatedBy) ";
                            db.Execute(queryIns, insertWinCodeCustomerDto, transaction);
                        }
                        else
                        {
                            flag = false;
                            message = string.Format("Đơn hàng {0} không tồn tại ", request.orderNo);
                        }

                        transaction.Commit();
                        return new Tuple<bool, string>(flag, message);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        FileHelper.WriteExpLogs("Rollback UpdateWincodeCustomer", ex);
                        _kibanaService.LogException("UpdateWincodeCustomer", request.posCode, 0, "", JsonConvert.SerializeObject(ex));
                        if(ex.Message.ToString().Contains("UNIQUE KEY") || ex.Message.ToString().Contains("Cannot insert duplicate key in object"))
                        {
                            message = string.Format("Đơn hàng {0} đã thực hiện hoàn trả số lượng", request.orderNo);
                        }
                        else
                        {
                            message = "Exception: " + ex.Message.ToString();
                        }
                        return new Tuple<bool, string>(false, message);
                    }
                }
            }
        }
        public Tuple<bool, string> InsertWincodeCustomer(WinLife_UpdatePromotions_POS_Request request)
        {
            bool flag = true;
            string message = "OK";
            using (IDbConnection db = DapperCentralMDFactory.CreateConnection())
            {
                db.Open();
                using (var transaction = db.BeginTransaction())
                {
                    try
                    {
                        var insertWinCodeCustomerDto = new List<WinCodeCustomerDto>();
                        foreach (var item in request.appliedCodes)
                        {
                            insertWinCodeCustomerDto.Add(new WinCodeCustomerDto()
                            {
                                ID = Guid.NewGuid(),
                                WinCode = item.winCode,
                                MemberCard = request.phoneNumber,
                                Csn = request.csn,
                                QuantityReciepted = item.quantity,
                                QuantityRecieptedSum = item.quantity,
                                OrderNo = request.orderNo,
                                StoreNo = request.storeCode,
                                PosNo = request.posCode,
                                IsDeleted = false,
                                TransDate = DateTime.Now.Date,
                                CreatedBy = request.posCode,
                                UpdatedBy = request.posCode,
                                CreatedDate = DateTime.Now,
                                UpdatedDate = DateTime.Now
                            });
                        }
                        string queryIns = @"INSERT INTO [dbo].[WinCodeCustomer]
                                            ([ID],[WinCode],[MemberCard],[Csn],[QuantityRecieptedSum],[QuantityReciepted],[OrderNo],[StoreNo],[PosNo],[IsDeleted]
                                            ,[TransDate],[CreatedDate],[UpdatedDate],[CreatedBy],[UpdatedBy]) " +
                                            " VALUES (@ID,@WinCode,@MemberCard,@Csn,@QuantityRecieptedSum,@QuantityReciepted,@OrderNo,@StoreNo,@PosNo,@IsDeleted" +
                                            " ,@TransDate,@CreatedDate,@UpdatedDate,@CreatedBy,@UpdatedBy) ";
                        db.Execute(queryIns, insertWinCodeCustomerDto, transaction);
                        transaction.Commit();
                        return new Tuple<bool, string>(flag, message);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        FileHelper.WriteExpLogs("Rollback InsertWincodeCustomer", ex);
                        _kibanaService.LogException("InsertWincodeCustomer", request.posCode, 0, "", JsonConvert.SerializeObject(ex));

                        if (ex.Message.ToString().Contains("UNIQUE KEY") || ex.Message.ToString().Contains("Cannot insert duplicate key in object"))
                        {
                            message = string.Format("Đơn hàng {0} đã thực hiện nhận {1}", request.orderNo, request.appliedCodes.FirstOrDefault().winCode);
                        }
                        else
                        {
                            message = "Exception: " + ex.Message.ToString();
                        }

                        return new Tuple<bool, string>(flag, message);
                    }
                }
            }
        }
    }
}