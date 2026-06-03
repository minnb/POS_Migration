using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCX.API.Common.Dtos;
using TCX.API.Common.Enums;
using VCM.POSBLUE.Model.Dtos.ROP;

namespace TCX.API.Common.Helpers
{
    public static class ConvertHelper
    {
        public static T ToObject<T>(string json)
        {
            try
            {
                if (!string.IsNullOrEmpty(json))
                {
                    return JsonConvert.DeserializeObject<T>(json);
                }
                return (T)Convert.ChangeType(null, typeof(T));
            }
            catch (Exception ex)
            {
                FileHelper.WriteLogs($"{json} ===" +JsonConvert.SerializeObject(ex)); 
                return (T)Convert.ChangeType(null, typeof(T));
            }
        }
        public static int ConvertStringToInt(string chuoi, int errorValue = 0)
        {
            bool thanhCong = int.TryParse(chuoi, out int ketQua);
            if (thanhCong)
            {
                return ketQua;
            }
            else
            {
                return errorValue;
            }
        }
    }
}
