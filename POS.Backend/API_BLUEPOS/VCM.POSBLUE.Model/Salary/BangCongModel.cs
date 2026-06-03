using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.Salary
{
    public class BangCongModel
    {
        public DateTime? KyChamCong { get; set; }
        public string MucLuongThuongThiDua { get; set; }
        public double? CongChuan { get; set; }
        public double? CongThucTe { get; set; }
        public double? SoNgayNghiHuongLuong { get; set; }
        public double? TongCongHuongLuong { get; set; }
        public double? SoNgayNghiKhongHuongLuong { get; set; }
        public double? PhepConLai { get; set; }
        public double? GioBuConLai { get; set; }
    }
}
