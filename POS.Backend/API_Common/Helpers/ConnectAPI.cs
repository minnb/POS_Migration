using System;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Script.Serialization;
using TCX.API.Common.Enums;
using TCX.API.Common.Response;

namespace TCX.API.Common.Helpers
{
    public class ConnectAPI
    {
        private readonly string baseURI = ConfigurationManager.AppSettings["BaseURI"];
        public string RelativeURI { get; set; }

        public ConnectAPI() { }
        public ConnectAPI(string baseURI, string relativeURI)
        {
            this.baseURI = baseURI;
            this.RelativeURI = relativeURI;
        }
        public ResponseModel<Response, EStatus> PostAPI<Response, TModel>(TModel model)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    try
                    {
                        client.Timeout = TimeSpan.FromMinutes(8);
                        var serialize = new JavaScriptSerializer
                        {
                            //serialize.MaxJsonLength = 50000000;
                            //serialize.MaxJsonLength = Int32.MaxValue;//2147483647
                            MaxJsonLength = int.MaxValue
                        };
                        //chuyen object sang json
                        string json = serialize.Serialize(model);

                        //tao authenzation 
                        // client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(ClientResource.Authentication_Basic, this.auth_Sim);
                        //client.DefaultRequestHeaders.Add("ClientId", "69ba1b05-0667-4e55-afa1-de962bc45731");

                        //tao url base request
                        client.BaseAddress = new Uri(this.baseURI);

                        //post request data 

                        var response = client.PostAsync(this.RelativeURI, new StringContent(json, Encoding.UTF8, "application/json")).Result;

                        if (response.IsSuccessStatusCode)
                        {
                            var data = response.Content.ReadAsStringAsync();
                            return new JavaScriptSerializer().Deserialize<ResponseModel<Response, EStatus>>(data.Result);
                        }
                        else
                        {
                            return default;
                        }
                    }
                    catch (Exception ex)
                    {
                        return new ResponseModel<Response, EStatus> { Status = EStatus.Fail, Message = ex.Message };
                    }
                }
            }
            catch (Exception ex)
            {
                return new ResponseModel<Response, EStatus> { Status = EStatus.Fail, Message = ex.Message };
            }
        }

        public ResultResponseModel PostAPIV2<Response, TModel>(TModel model)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    try
                    {
                        client.Timeout = TimeSpan.FromMinutes(8);
                        var serialize = new JavaScriptSerializer
                        {
                            MaxJsonLength = int.MaxValue
                        };
                        string json = serialize.Serialize(model);
                        //tao authenzation 
                        // client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(ClientResource.Authentication_Basic, this.auth_Sim);
                        //client.DefaultRequestHeaders.Add("ClientId", "69ba1b05-0667-4e55-afa1-de962bc45731");

                        //tao url base request
                        client.BaseAddress = new Uri(this.baseURI);
                        //post request data 
                        var response = client.PostAsync(this.RelativeURI, new StringContent(json, Encoding.UTF8, "application/json")).Result;

                        if (response.IsSuccessStatusCode)
                        {
                            var data = response.Content.ReadAsStringAsync();
                            return new JavaScriptSerializer().Deserialize<ResultResponseModel>(data.Result);
                        }
                        else
                        {
                            return default;
                        }
                    }
                    catch (Exception ex)
                    {
                        return new ResultResponseModel { Status = HttpStatusCode.InternalServerError, Message = ex.Message, Data = null };
                    }
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel { Status = HttpStatusCode.InternalServerError, Message = ex.Message, Data = null };
            }
        }

    }
}
