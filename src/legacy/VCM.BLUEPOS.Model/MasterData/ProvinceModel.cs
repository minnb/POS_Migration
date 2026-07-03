using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.MasterData
{

    public class ProvinceListResponseModel
    {
        public int ID { get; set; }
        public string ProvinceCode { get; set; }
        public string ProvinceName { get; set; }
        public string Status { get; set; }
        public string CreatedDateStr { get; set; }
        public string CreatedUser { get; set; }
        public string LastUpdateDateStr { get; set; }
        public string LastUpdateUser { get; set; }
        public int Total { get; set; }
    }

    public class CreateProvinceResponseModel
    {
        public int? ID { get; set; }
        public string ProvinceCode { get; set; }
        public string ProvinceName { get; set; }
        public bool? Status { get; set; }
        public string CreatedDateStr { get; set; }
        public string CreatedUser { get; set; }
        public string LastUpdateDateStr { get; set; }
        public string LastUpdateUser { get; set; }
        public long Counter { get; set; }
        public string Pkey { get; set; }
    }

    public class UpdateProvinceResponseModel
    {
        public int? ID { get; set; }
        public string ProvinceCode { get; set; }
        public string ProvinceName { get; set; }
        public bool? Status { get; set; }
        public string CreatedDateStr { get; set; }
        public string CreatedUser { get; set; }
        public string LastUpdateDateStr { get; set; }
        public string LastUpdateUser { get; set; }
        public long Counter { get; set; }
        public string Pkey { get; set; }
    }

    public class DeleteProvinceResponseModel
    {
        public int? ID { get; set; }
        public string ProvinceCode { get; set; }
        public string ProvinceName { get; set; }
        public bool? Status { get; set; }
        public string CreatedDateStr { get; set; }
        public string CreatedUser { get; set; }
        public string LastUpdateDateStr { get; set; }
        public string LastUpdateUser { get; set; }
        public long Counter { get; set; }
        public string Pkey { get; set; }
    }

    public class CheckProvinceResponseModel
    {
        public string ProvinceCode { get; set; }
        public bool Status { get; set; }
        public string Message { get; set; }

    }







}
