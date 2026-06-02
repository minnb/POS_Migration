using System;
using System.Collections.Generic;
using System.Linq;
using TCX.API.Common.Constants;
using TCX.API.Common.Enums;
using VCM.POSBLUE.Data.DataBaseContext;
using VCM.POSBLUE.Data.Reposites;
using VCM.POSBLUE.Model.POS.Gift;

namespace VCM.POSBLUE.API.Services
{
    internal interface IPOSGiftService
    {
        bool UpdateStatusPOSGiftInfo(GiftDataRequest request);
        List<POSGiftInfo> CreatePOSGiftInfo(GiftDataRequest request);
        List<POSGiftInfo> GetByGiftCode(GiftDataRequest request);
    }
    public class POSGiftService : IPOSGiftService
    {
        private POSGiftInfoRepository _posGiftRepository { get; set; }
        public POSGiftService()
        {
            _posGiftRepository = new POSGiftInfoRepository();
        }
        public List<POSGiftInfo> CreatePOSGiftInfo(GiftDataRequest request)
        {
            string status = GiftStatusEnum.ERROR.ToString();
            if (GiftStatusConst.CheckStatusCreateGift.Contains(request.Status))
            {
                status = GiftStatusEnum.AVAILABLE.ToString();
            }

            List<POSGiftInfo> lstGift = new List<POSGiftInfo>();
            if (request.GiftCode.Count() > 0)
            {
                foreach(var giftCode in request.GiftCode)
                {
                    lstGift.Add(new POSGiftInfo()
                    {
                        GiftCode = giftCode,
                        GiftType = request.GiftType,
                        GiftStatus = status,
                        GiftValue = 0,
                        CreatedTime = DateTime.Now,
                        PosCreated = request.PosNo,
                        UsedTime = DateTime.Now,
                        PosUsed = "",
                        FromDate = DateTime.Now.Date,
                        ToDate = DateTime.Now.Date,
                        PublishDate = DateTime.Now
                    });
                }
            }
            return _posGiftRepository.InsertPOSGiftInfo(lstGift);
        }
        public List<POSGiftInfo> GetByGiftCode(GiftDataRequest request)
        {
            return _posGiftRepository.GetByGiftCode(request.GiftCode);           
        }
        public bool UpdateStatusPOSGiftInfo(GiftDataRequest request)
        {
            return _posGiftRepository.UpdateGiftStatus(request.GiftCode, request.Status, request.PosNo);
        }
    }
}
