using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using Newtonsoft.Json;
using VCM.BLUEPOS.Common.Helpers;
using VCM.BLUEPOS.Data.EF;
using VCM.BLUEPOS.Data.EF.Central;
using VCM.BLUEPOS.Data.EF.IFSAP;
using VCM.BLUEPOS.Model.Authen;
using VCM.BLUEPOS.Model.OptionModel;
using VCM.BLUEPOS.Model.Common;
using VCM.BLUEPOS.Model.SyncData;
using System.Data.Entity;
using VCM.BLUEPOS.Model.StoreActivities;

namespace VCM.BLUEPOS.Data.SyncData
{
    public interface ISyncIData
    {
        List<SAPLogModel> ListLogSAP(string processID);
        List<SalePosMisModel> ListOrderCentral(string storeNo, string posID, DateTime bussinessDate);
        bool UpdateEOD(POSEODModel model);

        // 21/11/2024, tungnt8
        List<IPAddressPOSTerminalByStoreModel> GetPOSTerminalByStore(string storeNo);
        List<MissSalePosModel> ListOrderCentralV2(string storeNo, string posID, DateTime bussinessDate);

    }

    public class SyncIData : ISyncIData
    {
        public List<SAPLogModel> ListLogSAP(string processID)
        {
            try
            {
                var result = new List<SAPLogModel>();
                using (var db = new IFSAPContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    result = db.Logs.Where(d => d.ProcessID == processID).Select(a => new SAPLogModel
                    {
                        TransType = a.TransType,
                        Message = a.MessageError,
                        ProcessID = processID
                    }).OrderBy(d => d.TransType).OrderBy(d => d.Message).ToList();
                    return result;
                }
            }
            catch (Exception Ex)
            {
                return (default(List<SAPLogModel>));
            }
        }

        public List<SalePosMisModel> ListOrderCentral(string storeNo, string posID, DateTime bussinessDate)
        {
            var result = new List<SalePosMisModel>();
            try
            {
                var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
                // 27/10/2022, Tài : đề nghị lấy từ CentralSales
                using (var db = new CentralSalesContainer(ipServer))
                {
                    result = db.TransHeaders.Where(d => d.StoreNo == storeNo && d.POSTerminalNo == posID && DbFunctions.TruncateTime(d.OrderDate) == bussinessDate)
                        .Select(a => new SalePosMisModel
                        {
                            OrderNo = a.OrderNo,
                            CashierID = a.CashierID,
                            CustomerName = a.CustomerName,
                            Amount = a.AmountInclVAT
                        }).ToList();
                }

                // Code cũ :

                //using (var db = new CentralSalesStagingContainer(ipServer))
                //{
                //    result = db.TransHeaderStagings.Where(d => d.StoreNo == storeNo && d.POSTerminalNo == posID && DbFunctions.TruncateTime(d.OrderDate) == bussinessDate)
                //        .Select(a => new SalePosMisModel
                //        {
                //            OrderNo = a.OrderNo,
                //            CashierID = a.CashierID,
                //            CustomerName = a.CustomerName,
                //            Amount = a.AmountInclVAT
                //        }).ToList();
                //}

            }
            catch
            {
            }
            return result;
        }

        public bool UpdateEOD(POSEODModel model)
        {
            var result = true;
            try
            {
                var ipServer = ServerIPConnection.GetIPServerByStore(model.StoreNo);
                using (var db = new CentralSalesContainer(ipServer))
                {

                    var checkUpd = db.POSEODs.FirstOrDefault(d => d.StoreNo == model.StoreNo && d.POSTerminalNo == model.POSTerminalNo && d.BussinessDate == model.BussinessDate);
                    if (checkUpd != null)
                    {
                        checkUpd.TotalSale = checkUpd.TotalSale + (model.TotalSale ?? 0);
                    }
                    else
                    {
                        var insert = new POSEOD
                        {
                            StoreNo = model.StoreNo,
                            POSTerminalNo = model.POSTerminalNo,
                            BussinessDate = model.BussinessDate,
                            TotalSale = model.TotalSale,
                            TotalAmount = model.TotalAmount,
                            MaxBillNumber = model.MaxBillNumber,
                            CreatedDate = model.CreatedDate
                        };
                        db.POSEODs.Add(insert);
                    }
                    db.SaveChanges();
                }
            }
            catch
            {
                result = false;
            }
            return result;
        }

        // 21/11/2024, tungnt8
        public List<IPAddressPOSTerminalByStoreModel> GetPOSTerminalByStore(string storeNo)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = new List<IPAddressPOSTerminalByStoreModel>();
                    if (storeNo == "" || storeNo == "ALL")
                    {
                        data = (from a in db.POSTerminals
                                    join b in db.Stores on a.No equals b.No
                                    where a.Status == true && (a.IPAddress != null || a.IPAddress != "")
                                    select new IPAddressPOSTerminalByStoreModel
                                    {
                                        StoreNo = a.StoreNo,
                                        IpAddress = a.IPAddress,
                                        PosID = a.No,
                                        PosName = a.No
                                    }).OrderBy(x => x.PosID).ToList();
                    }
                    else
                    {
                        data = (from a in db.POSTerminals
                                    where a.StoreNo == storeNo && a.Status == true && (a.IPAddress != null || a.IPAddress != "")
                                    select new IPAddressPOSTerminalByStoreModel
                                    {
                                        StoreNo = a.StoreNo,
                                        IpAddress = a.IPAddress,
                                        PosID = a.No,
                                        PosName = a.No
                                    }).OrderBy(x => x.PosID).ToList();
                    }
                   
                    if (data == null || data.Count == 0)
                    {
                        return new List<IPAddressPOSTerminalByStoreModel>();
                    }
                    return data;                  
                }
            }
            catch (Exception ex)
            {
                return new List<IPAddressPOSTerminalByStoreModel>();
            }
        }

        // 21/11/2024, tungnt8
        public List<MissSalePosModel> ListOrderCentralV2(string storeNo, string posID, DateTime bussinessDate)
        {
            var result = new List<MissSalePosModel>();
            try
            {
                    var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
                    using (var db = new CentralSalesContainer(ipServer))
                    {
                            result = db.TransHeaders.Where(d => d.StoreNo == storeNo && d.POSTerminalNo == posID && DbFunctions.TruncateTime(d.OrderDate) == bussinessDate)
                            .Select(a => new MissSalePosModel
                            {
                                TotalOrderCentral = a.OrderNo.Count(),
                                StoreNo = a.StoreNo,
                                POSTerminal = a.POSTerminalNo,
                                OrderDate = a.OrderDate.ToString()
                            }).ToList();
                }
            }
            catch(Exception ex)
            {
            }
            return result;
        }




    }
}
