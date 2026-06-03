using System.Web.Configuration;
using TCX.API.Common.Models;

namespace VCM.POSBLUE.API.Models
{
    public class FunctionAPI
    {
        readonly string CXUrl = WebConfigurationManager.AppSettings["CXUrl"];
        readonly string CXUser = WebConfigurationManager.AppSettings["CXUser"];
        readonly string CXPassword = WebConfigurationManager.AppSettings["CXPassword"];
        public CustomerEnquiryModel CustomerEnquiry(string codeValue)
        {
            var data = new CustomerEnquiryModel();
            try
            {
                
            }
            catch
            {

            }
            return data;
        }
    }
}