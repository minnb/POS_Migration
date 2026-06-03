using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.Salary
{
    public class NhanVienModel
    {
        public string MaNhanVien { get; set; }
        public DateTime? NgayBatDauLamViec { get; set; }
        public string MasterCode { get; set; }
        public string HoTen { get; set; }
        public string KhuVucNhanSu { get; set; }
        public string PhongBan { get; set; }
        public string ChucDanh { get; set; }
        public string SoTaiKhoan { get; set; }
        public string NganHang { get; set; }
    }
}
