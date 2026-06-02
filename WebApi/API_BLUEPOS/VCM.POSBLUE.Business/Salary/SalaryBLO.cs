using System;
using TCX.API.Common.Enums;
using VCM.POSBLUE.Data.Salary;
using VCM.POSBLUE.Model.Salary;

namespace VCM.POSBLUE.Business.Salary
{
    public interface ISalaryBLO
    {
        Tuple<ADConnectionStatus, ADUserModel> Login(LoginViewModel req);
        SalaryResponse Get_BangLuongThang_ByEmp(string MaNV, DateTime Date);
        Tuple<bool, string> GetEmplCodeFromHCM(string companyCode, string userAD);
    }
    public class SalaryBLO : ISalaryBLO
    {

        private SalaryData data { get; set; }
        public SalaryBLO()
        {
            data = new SalaryData();
        }
        public SalaryResponse Get_BangLuongThang_ByEmp(string MaNV, DateTime Date)
        {
            return data.Get_BangLuongThang_ByEmp(MaNV, Date);
        }

        public Tuple<ADConnectionStatus, ADUserModel> Login(LoginViewModel req)
        {
            return data.Login(req);
        }

        public Tuple<bool, string> GetEmplCodeFromHCM(string companyCode, string userAD)
        {
            return data.GetEmplCode(companyCode, userAD);   
        }
    }
}
