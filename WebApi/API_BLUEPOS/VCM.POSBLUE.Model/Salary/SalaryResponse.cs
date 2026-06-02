using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.Salary
{
    public class SalaryResponse
    {
        public NhanVienModel InfoStaff { get; set; }
        public BangCongModel InfoWork { get; set; }
        public List<BangLuongModel> InfoSalary { get; set; }
    }
    public class SalaryRequest
    {
        //public string companyCode { get; set; }
        [Required]
        public string userAD { get; set; }
        public string passAD { get; set; }
        [Required]
        public string dateView { get; set; }//yyyy-MM-dd

    }
}
