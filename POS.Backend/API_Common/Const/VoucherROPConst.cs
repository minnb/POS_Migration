using System.Collections.Generic;
using TCX.API.Common.Enums;

namespace TCX.API.Common.Constants
{
    public class VoucherROPConst
    {
        public static readonly int[] StatusVoucherROP = { 10,20,30,40,50,60,70,80,90 };
        public static readonly int[] StatusVoucherInCheckROP = { 50};
        public static readonly int[] StatusVoucherSerialCheckROP = { 40 };
    }

    public static class VoucherROPHelper
    {
        public static Dictionary<string, string> VoucherTypeCAP = new Dictionary<string, string>
        {
            { "VOUCHER", "ZVCN" },
            { "COUPON", "ZCPN" },
            { "POINTS_VOUCHER", "POINTS_VOUCHER" },
            { "", "ZVCN"}
        };
        public static Dictionary<string, int> GetVoucheTypeName()
        {
            Dictionary<string, int> openWith = new Dictionary<string, int>
            {
                { ArticleTypeEnum.ZCPN.ToString(),(int)ArticleTypeEnum.ZCPN},
                { ArticleTypeEnum.ZVCN.ToString(),(int)ArticleTypeEnum.ZVCN},
                { ArticleTypeEnum.ZPOS.ToString(),(int)ArticleTypeEnum.ZPOS}
            };
            return openWith;
        }
        public static Dictionary<int, string> GetCodeStatusByIdVoucher()
        {
            return new Dictionary<int, string>
                    {
                        { (int)VoucherStatusEnum.NOTEXIST, VoucherStatusEnum.NOTEXIST.ToString() },
                        { (int)VoucherStatusEnum.NEW, VoucherStatusEnum.NEW.ToString() },
                        { (int)VoucherStatusEnum.PEDDINGPRINT, VoucherStatusEnum.PEDDINGPRINT.ToString() },
                        { (int)VoucherStatusEnum.AVL, VoucherStatusEnum.AVL.ToString() },
                        { (int)VoucherStatusEnum.INSTOCK, VoucherStatusEnum.INSTOCK.ToString() },
                        { (int)VoucherStatusEnum.SOLD, VoucherStatusEnum.SOLD.ToString() },
                        { (int)VoucherStatusEnum.CANCELLED, VoucherStatusEnum.CANCELLED.ToString() },
                        { (int)VoucherStatusEnum.RDM, VoucherStatusEnum.RDM.ToString() },
                        { (int)VoucherStatusEnum.EXPIRED, VoucherStatusEnum.EXPIRED.ToString() },
                        { (int)VoucherStatusEnum.EXP , VoucherStatusEnum.EXP.ToString()}
                    };
        }
        public static Dictionary<string, int> GetStatusIdByCodeVoucher()
        {
            return new Dictionary<string, int>
            {
                { VoucherStatusEnum.NOTEXIST.ToString(), (int)VoucherStatusEnum.NOTEXIST },
                { VoucherStatusEnum.NEW.ToString(), (int)VoucherStatusEnum.NEW },
                { VoucherStatusEnum.PEDDINGPRINT.ToString(), (int)VoucherStatusEnum.PEDDINGPRINT },
                { VoucherStatusEnum.AVL.ToString(), (int)VoucherStatusEnum.AVL },
                { VoucherStatusEnum.INSTOCK.ToString(), (int)VoucherStatusEnum.INSTOCK },
                { VoucherStatusEnum.SOLD.ToString(), (int)VoucherStatusEnum.SOLD },
                { VoucherStatusEnum.CANCELLED.ToString(), (int)VoucherStatusEnum.CANCELLED },
                { VoucherStatusEnum.RDM.ToString(), (int)VoucherStatusEnum.RDM },
                { VoucherStatusEnum.EXP.ToString(), (int)VoucherStatusEnum.EXP },
                 { VoucherStatusEnum.EXPIRED.ToString(), (int)VoucherStatusEnum.EXPIRED }
            };
        }
        public static Dictionary<int, string> GetNameStatusVoucher()
        {
            Dictionary<int, string> openWith = new Dictionary<int, string>
            {
                {(int)VoucherROPStatusEnum.NotExist, VoucherROPStatusEnum.NotExist.ToString() },
                {(int)VoucherROPStatusEnum.New, VoucherROPStatusEnum.New.ToString() },
                {(int)VoucherROPStatusEnum.Created, VoucherROPStatusEnum.Created.ToString() },
                {(int)VoucherROPStatusEnum.PendingPrint, VoucherROPStatusEnum.PendingPrint.ToString() },
                {(int)VoucherROPStatusEnum.InStock, VoucherROPStatusEnum.InStock.ToString() },
                {(int)VoucherROPStatusEnum.Actived, VoucherROPStatusEnum.Actived.ToString()},
                {(int)VoucherROPStatusEnum.Used, VoucherROPStatusEnum.Used.ToString() },
                {(int)VoucherROPStatusEnum.Cancelled, VoucherROPStatusEnum.Cancelled.ToString() },
                {(int)VoucherROPStatusEnum.Expired, VoucherROPStatusEnum.Expired.ToString() },
                {(int)VoucherROPStatusEnum.Deactivate, VoucherROPStatusEnum.Deactivate.ToString() }
            };
            return openWith;
        }
    }
}
//[Description("Mới tạo")]
//NotExist = 0,
//        [Description("Mới tạo")]
//New = 10,
//        [Description("Đã phát hành")]
//Created = 20,
//        [Description("Chờ in phôi")]
//PendingPrint = 30,
//        [Description("Đang tồn kho")]
//InStock = 40,
//        [Description("Đã kích hoạt")]
//Actived = 50,
//        [Description("Đã sử dụng")]
//Used = 60,
//        [Description("Đã hủy")]
//Cancelled = 70,
//        [Description("Đã hết hạn")]
//Expired = 80,
//        [Description("Hủy kích hoạt")]
//Deactivate = 90,