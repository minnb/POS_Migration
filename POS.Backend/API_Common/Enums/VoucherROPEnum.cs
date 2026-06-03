using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Enums
{
    public enum VoucherTypeOneUEnum
    {
        FIXED = 1,
        PERCENTAGE = 2
    }
    public enum VoucherTypePOS_SAP
    {
        R,
        V
    }
    
    public enum ArticleTypeEnum
    {
        ZVCN = 2,
        ZCPN = 3,
        ZPOS = 1,
        POINTS_VOUCHER = 4,
        VOUCHER = 5
    }
    public enum VoucherStatusEnum
    {
        NOTEXIST = 0,
        NEW = 10,
        AVL = 20,
        PEDDINGPRINT = 30,
        INSTOCK = 40,
        SOLD = 50,
        RDM = 60,
        CANCELLED = 70,
        EXPIRED = 80,
        EXP = 90
    }
    public enum VoucherROPStatusEnum
    {
        [Description("Mới tạo")]
        NotExist = 0,

        [Description("Mới tạo")]
        New = 10,

        [Description("Đã phát hành")]
        Created = 20,

        [Description("Chờ in phôi")]
        PendingPrint = 30,

        [Description("Đang tồn kho")]
        InStock = 40,

        [Description("Đã kích hoạt")]
        Actived = 50,

        [Description("Đã sử dụng")]
        Used = 60,

        [Description("Đã hủy")]
        Cancelled = 70,

        [Description("Đã hết hạn")]
        Expired = 80,

        [Description("Hủy kích hoạt")]
        Deactivate = 90,
    }
}
