using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Data.EF.Central;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.Campaign;

namespace VCM.BLUEPOS.Data.Campaign
{
    public interface ICampaignData
    {
        Task<Tuple<List<ComboxOptionModel>, int>> GetStoreList(string searchText, int pageIndex = 0, int pageSize = 50);
        Task<List<ComboxOptionModel>> GetStoreByCodeList(List<string> listCode);
        Tuple<List<SelectionItem>, int> GetListOfferNo(CampaignOfferNoFilterModel filter);
        Tuple<List<SelectionItem>, int> GetListItem(CampaignItemFilterModel filter);
        Task<CampaignAPIResultModel<object>> Create(CampaignCreateModel req, CampaignAPIModel api);
        Task<CampaignAPIResultModel<object>> SyncCampaign(string objectId, CampaignAPIModel api);
        Task<CampaignAPIResultModel<object>> CopyCampaign(CampaignCopyModel req, CampaignAPIModel api);
        Task<CampaignAPIResultModel<CampaignDetailModel>> Detail(string objectId, CampaignAPIModel api);
        Task<CampaignAPIResultModel<List<CampaignDetailModel>>> GetListCampaign(CampaignFilterModel filter, CampaignAPIModel api);
        Task<Tuple<List<CampaignImportModel>, string>> Import(DataTable dataImport);
        Task<CampaignAPIResultModel<object>> SyncCampaignV2(string objectId, CampaignAPIModel api);

        //21/08/2025,tungnt8
        Task<CampaignAPIResultModel<List<CampaignDetailModel>>> GetListCampaignV2(CampaignFilterModel filter, CampaignAPIModel api);
        List<GetOfferTypeModel> GetListOfferType();

    }

    public class CampaignData : ICampaignData
    {
        public async Task<CampaignAPIResultModel<object>> Create(CampaignCreateModel req, CampaignAPIModel api)
        {
            var notify = new CampaignAPIResultModel<object>();
            #region valid data
            if (string.IsNullOrEmpty(req.Photo))
            {
                notify.Meta.Code = 5555;
                notify.Meta.Message = "Vui nhập ảnh Món";
            }

            if (DateTime.TryParseExact(req.StartTime, "yyyy-MM-dd HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out DateTime startDate) && startDate < DateTime.Now)
            {
                notify.Meta.Code = 5555;
                notify.Meta.Message = "Thời gian bắt đầu phải lớn hơn thời gian hiện tại";
            }
            else if (DateTime.TryParseExact(req.EndTime, "yyyy-MM-dd HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out DateTime endDate) && endDate < startDate)
            {
                notify.Meta.Code = 5555;
                notify.Meta.Message = "Thời gian bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc";
            }
            else if ((endDate - startDate).TotalHours < 2)
            {
                notify.Meta.Code = 5555;
                notify.Meta.Message = "Thời gian áp dụng khuyến mãi phải tối thiếu 2h";
            }

            var checkDiscount = req.CampaignItems.Where(x => x.Discount > x.SpecialPrice).ToList();

            if (checkDiscount.Any())
            {
                notify.Meta.Code = 5555;
                notify.Meta.Message = $"Giảm giá của Món phải nhỏ hơn hoặc bằng Giá bán tại Món {string.Join(", ", checkDiscount.Select(x => x.Sequence).ToList())}";
            }

            if (req.Price <= req.Value)
            {
                notify.Meta.Code = 5555;
                notify.Meta.Message = "Số tiền giảm giá của combo phải nhỏ hơn Tổng tiền của combo";
            }
            #endregion

            if (notify.Meta.Code != 0)
            {
                return notify;
            }

            if (req.StoreNo != null)
            {
                if (req.StoreNo.Any(x => x.ToLower() == "all")) req.StoreNo = null;
                else
                {
                    using (var db = new CentralMDPartnerContainer())
                    {
                        var getListStoreExist = db.Stores.Where(x => x.ClosingMethod == 0 && req.StoreNo.Any(a => a.Equals(x.No)))
                                                         .OrderBy(x => x.No)
                                                         .Select(x => new ComboxOptionModel
                                                         {
                                                             Value = x.No,
                                                             Text = x.Name
                                                         }).ToList();

                        var checkStoreNotExist = req.StoreNo.Where(x => !getListStoreExist.Any(y => y.Value == x)).ToList();

                        if (checkStoreNotExist.Any())
                        {
                            notify.Meta.Code = 5555;
                            notify.Meta.Message = $"Không tồn tại các Cửa hàng {string.Join(" - ", checkStoreNotExist)}";
                        }
                    }
                }
            }

            if (notify.Meta.Code != 0)
            {
                return notify;
            }

            var checkItem = req.CampaignItems.Where(x => x.Items != null && x.Items.Where(y => !string.IsNullOrEmpty(y)).Count() == 0).ToList();

            if (checkItem.Any())
            {
                notify.Meta.Code = 5555;
                notify.Meta.Message = $"Vui lòng chọn sản phẩm tại Món  {string.Join(" - ", checkItem.Select(x => x.Sequence.ToString().ToList()))}";
                return notify;
            }

            System.Net.ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            using (var httpClient = new HttpClient())
            {
                DateTime RequestDate = DateTime.Now;
                try
                {
                    httpClient.BaseAddress = new Uri(api.endPoint);
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{api.User.Trim()}:{api.Password.Trim()}")));
                    httpClient.Timeout = TimeSpan.FromSeconds(api.timeOut);
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    string jsonContent = JsonConvert.SerializeObject(req);

                    HttpContent content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await httpClient.PostAsync(api.nameAPI, content);

                    string responseData = await response.Content.ReadAsStringAsync();
                    notify = JsonConvert.DeserializeObject<CampaignAPIResultModel<object>>(responseData);

                    if (notify.Meta.Code == 200)
                    {
                        notify.Meta.Message = "Tạo CTKM thành công";
                    }
                }
                catch (Exception ex)
                {
                    notify.Meta.Code = 6666;
                    notify.Meta.Message = "Lỗi hệ thống";
                    notify.Meta.TechMssg = ex.Message;
                }
            }
            return notify;
        }

        public async Task<CampaignAPIResultModel<List<CampaignDetailModel>>> GetListCampaign(CampaignFilterModel filter, CampaignAPIModel api)
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            using (var httpClient = new HttpClient())
            {
                try
                {
                    httpClient.BaseAddress = new Uri(api.endPoint);
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{api.User.Trim()}:{api.Password.Trim()}")));
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    httpClient.Timeout = TimeSpan.FromSeconds(api.timeOut);

                    var queryParams = $"MerchantID={filter.MerchantID}&FromDate={filter.FromDate}&ToDate={filter.ToDate}";
                    string requestUri = $"{api.nameAPI}?{queryParams}";

                    HttpResponseMessage response = await httpClient.GetAsync(requestUri);
                    string responseData = await response.Content.ReadAsStringAsync();

                    var res = JsonConvert.DeserializeObject<CampaignAPIResultModel<List<CampaignDetailModel>>>(responseData);

                    if (res.Meta.Code == 200)
                    {
                        int timeWaitSync = 2; // munite
                        using (var db = new CentralMDPartnerContainer())
                        {
                            var timeSetup = db.OptionDatas.FirstOrDefault(x => x.Caption == "TIMESYNCCAMPAIGN" && x.Status == true);
                            if (timeSetup != null)
                            {
                                timeWaitSync = !string.IsNullOrEmpty(timeSetup.Code) && int.TryParse(timeSetup.Code, out timeWaitSync) ? timeWaitSync : 2;
                            }
                        }

                        DateTime now = DateTime.Now;
                        if (!string.IsNullOrEmpty(filter.SearchText))
                        {
                            string SearchText = filter.SearchText.Trim().ToLower();
                            res.Data = res.Data.Where(x => x.OfferNo.ToLower().Contains(SearchText) || (x.CampaignStores != null && x.CampaignStores.Any(y=>y.PartnerMerchantID.ToLower().Contains(SearchText)))).ToList();
                        } 
                        foreach(var item in res.Data)
                        {
                            DateTime createdDate = DateTime.Parse(item.CrtDate);
                            item.CrtDate = createdDate.ToString("MM/dd/yyyy HH:mm:ss");

                            if (item.IsSyncToGrab.ToLower() == "false")
                            {
                                if ((now - createdDate).TotalMinutes >= timeWaitSync)
                                {
                                    item.EnableSyncToGrap = true;
                                }
                                else
                                {
                                    item.EnableSyncToGrap = false;
                                }
                            }
                            else
                            {
                                item.EnableSyncToGrap = false;
                            }
                        }
                    }

                    return res;
                }
                catch (Exception ex)
                {
                    return new CampaignAPIResultModel<List<CampaignDetailModel>>
                    {
                        Meta = new Meta
                        {
                            Code = 5555,
                            Message = "Lỗi hệ thống",
                            TechMssg = ex.Message
                        }
                    };
                }
            }
        }

        // 21/08/2025,tungnt8
        public async Task<CampaignAPIResultModel<List<CampaignDetailModel>>> GetListCampaignV2(CampaignFilterModel filter, CampaignAPIModel api)
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            using (var httpClient = new HttpClient())
            {
                try
                {
                    httpClient.BaseAddress = new Uri(api.endPoint);
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{api.User.Trim()}:{api.Password.Trim()}")));
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    httpClient.Timeout = TimeSpan.FromSeconds(api.timeOut);

                    var _year = DateTime.Now.Year;
                    DateTime fromDate = new DateTime(_year, 1, 1); // ngày đầu tiên của năm hiện tại
                    DateTime aDateTime = new DateTime(_year + 1, 1, 1);
                    DateTime toDate = aDateTime.AddDays(-1); // ngày cuối cùng của năm hiện tại

                    var queryParams = $"MerchantID={filter.MerchantID}&FromDate={fromDate}&ToDate={toDate}";
                    string requestUri = $"{api.nameAPI}?{queryParams}";

                    HttpResponseMessage response = await httpClient.GetAsync(requestUri);
                    string responseData = await response.Content.ReadAsStringAsync();

                    var res = JsonConvert.DeserializeObject<CampaignAPIResultModel<List<CampaignDetailModel>>>(responseData);

                    if (res.Meta.Code == 200)
                    {
                        int timeWaitSync = 2; // munite
                        using (var db = new CentralMDPartnerContainer())
                        {
                            var timeSetup = db.OptionDatas.FirstOrDefault(x => x.Caption == "TIMESYNCCAMPAIGN" && x.Status == true);
                            if (timeSetup != null)
                            {
                                timeWaitSync = !string.IsNullOrEmpty(timeSetup.Code) && int.TryParse(timeSetup.Code, out timeWaitSync) ? timeWaitSync : 2;
                            }
                        }

                        if (!string.IsNullOrEmpty(filter.OfferType))
                        {
                            string offerType = filter.OfferType.Trim().ToLower();
                            res.Data = res.Data.Where(x => x.OfferType.ToLower().Contains(offerType)).ToList();
                        }

                        if (!string.IsNullOrEmpty(filter.Status))
                        {
                            string format = "yyyy-MM-dd";
                            DateTime nowDate = DateTime.Now.Date;
                            string sNowDate = nowDate.ToString(format);

                            if (filter.Status == "0")
                            {
                                res.Data = res.Data.Where(x => x.Blocked = false).ToList();

                                //res.Data = res.Data.Where(x => x.Blocked = false && (DateTime.Parse(x.StartTime).Date < DateTime.Parse(sNowDate).Date || DateTime.Parse(x.StartTime).Date == DateTime.Parse(sNowDate).Date)).ToList();


                            }
                            else if (filter.Status == "1")
                            {
                                res.Data = res.Data.Where(x => x.Blocked = true && (DateTime.Parse(x.StartTime) > DateTime.Parse(sNowDate) || DateTime.Parse(x.StartTime) > (DateTime.Parse(x.EndTime).Date) || DateTime.Parse(x.EndTime).Date < DateTime.Parse(sNowDate))).ToList();
                            }
                        }

                        DateTime now = DateTime.Now;
                        if (!string.IsNullOrEmpty(filter.SearchText))
                        {
                            string SearchText = filter.SearchText.Trim().ToLower();
                            res.Data = res.Data.Where(x => x.OfferNo.ToLower().Contains(SearchText) || (x.CampaignStores != null && x.CampaignStores.Any(y => y.PartnerMerchantID.ToLower().Contains(SearchText)))).ToList();
                        }

                        foreach (var item in res.Data)
                        {
                            DateTime createdDate = DateTime.Parse(item.CrtDate);
                            item.CrtDate = createdDate.ToString("MM/dd/yyyy HH:mm:ss");

                            if (item.IsSyncToGrab.ToLower() == "false")
                            {
                                if ((now - createdDate).TotalMinutes >= timeWaitSync)
                                {
                                    item.EnableSyncToGrap = true;
                                }
                                else
                                {
                                    item.EnableSyncToGrap = false;
                                }
                            }
                            else
                            {
                                item.EnableSyncToGrap = false;
                            }
                        }
                    }
                    return res;
                }
                catch (Exception ex)
                {
                    return new CampaignAPIResultModel<List<CampaignDetailModel>>
                    {
                        Meta = new Meta
                        {
                            Code = 5555,
                            Message = "Lỗi hệ thống",
                            TechMssg = ex.Message
                        }
                    };
                }
            }
        }

        public async Task<CampaignAPIResultModel<CampaignDetailModel>> Detail(string objectId, CampaignAPIModel api)
        {
            var notify = new CampaignAPIResultModel<CampaignDetailModel>();

            System.Net.ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            using (var httpClient = new HttpClient())
            {
                DateTime RequestDate = DateTime.Now;
                try
                {
                    httpClient.BaseAddress = new Uri(api.endPoint);
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{api.User.Trim()}:{api.Password.Trim()}")));
                    httpClient.Timeout = TimeSpan.FromSeconds(api.timeOut);
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var queryParams = $"Id={objectId}";
                    string requestUri = $"{api.nameAPI}?{queryParams}";

                    HttpResponseMessage response = await httpClient.GetAsync(requestUri);

                    string responseData = await response.Content.ReadAsStringAsync();
                    notify = JsonConvert.DeserializeObject<CampaignAPIResultModel<CampaignDetailModel>>(responseData);

                    if (notify.Meta.Code == 200)
                    {
                        notify.Meta.Message = "Lấy dữ liệu thành công";
                        DateTime startDate;
                        DateTime endDate;
                        DateTime.TryParseExact(notify.Data.StartTime, "MM/dd/yyyy HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out startDate);
                        DateTime.TryParseExact(notify.Data.EndTime, "MM/dd/yyyy HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out endDate);
                        notify.Data.StartTime = startDate.ToString("yyyy-MM-dd HH:mm:ss");
                        notify.Data.EndTime = endDate.ToString("yyyy-MM-dd HH:mm:ss");

                        using (var db = new CentralMDPartnerContainer())
                        {
                            var listItemNo = notify.Data.CampaignItems.Select(x => x.ItemNo).ToList();
                            var queryItem = db.Items.Where(x => listItemNo.Any(y => y == x.No));
                            var resItem = queryItem.Select(x => new { x.No, x.Description }).Distinct().ToList();

                            foreach (var item in notify.Data.CampaignItems)
                            {
                                var mapping = resItem.FirstOrDefault(x => x.No == item.ItemNo);
                                if (mapping != null) item.ItemName = mapping.Description;
                            }

                            if (notify.Data.CampaignStores != null && notify.Data.CampaignStores.Any())
                            {
                                var listStoreNo = notify.Data.CampaignStores.Select(x => x.PartnerMerchantID).ToList();
                                var queryStore = db.Stores.Where(x => x.ClosingMethod == 0 && listStoreNo.Any(y => y == x.No));
                                var resStore = queryStore.Select(x => new { x.No, x.Name }).ToList();

                                foreach (var store in notify.Data.CampaignStores)
                                {
                                    var mapping = resStore.FirstOrDefault(x => x.No == store.CampaignID);
                                    if (mapping != null) store.StoreName = mapping.Name;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    notify.Meta.Code = 6666;
                    notify.Meta.Message = "Lỗi hệ thống";
                    notify.Meta.TechMssg = ex.Message;
                }
            }
            return notify;
        }
        public async Task<CampaignAPIResultModel<object>> SyncCampaign(string objectId, CampaignAPIModel api)
        {
            var notify = new CampaignAPIResultModel<object>();
            System.Net.ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            using (var httpClient = new HttpClient())
            {
                DateTime RequestDate = DateTime.Now;
                try
                {
                    httpClient.BaseAddress = new Uri(api.endPoint);
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{api.User.Trim()}:{api.Password.Trim()}")));
                    httpClient.Timeout = TimeSpan.FromSeconds(api.timeOut);
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    string requestUri = $"{api.nameAPI.Replace("{Id}", objectId)}";

                    string jsonContent = JsonConvert.SerializeObject(objectId);

                    HttpContent content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await httpClient.PutAsync(requestUri, content);

                    string responseData = await response.Content.ReadAsStringAsync();
                    notify = JsonConvert.DeserializeObject<CampaignAPIResultModel<object>>(responseData);

                    if (notify.Meta.Code == 200)
                    {
                        notify.Meta.Message = "Đồng bộ thành công";
                    }
                }
                catch (Exception ex)
                {
                    notify.Meta.Code = 6666;
                    notify.Meta.Message = "Lỗi hệ thống";
                    notify.Meta.TechMssg = ex.Message;
                }
            }
            return notify;
        }

        // 13/08/2025,tungnt8
        public async Task<CampaignAPIResultModel<object>> SyncCampaignV2(string objectId, CampaignAPIModel api)
        {
            var notify = new CampaignAPIResultModel<object>();
            System.Net.ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            using (var httpClient = new HttpClient())
            {
                DateTime RequestDate = DateTime.Now;
                try
                {
                    httpClient.BaseAddress = new Uri(api.endPoint);
                    //httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{api.User.Trim()}:{api.Password.Trim()}")));
                    httpClient.DefaultRequestHeaders.Add("Authorization", api.Password.Trim());

                    httpClient.Timeout = TimeSpan.FromSeconds(api.timeOut);
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    string requestUri = $"{api.nameAPI.Replace("{Id}", objectId)}";
                    var model = new CampaignAPICreateV2Model()
                    {
                        StartTime = api.StartTime,
                        CampainID = objectId
                    };

                    string jsonContent = JsonConvert.SerializeObject(model);
                    HttpContent content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await httpClient.PostAsync(requestUri, content);

                    string responseData = await response.Content.ReadAsStringAsync();
                    notify = JsonConvert.DeserializeObject<CampaignAPIResultModel<object>>(responseData);

                    if (notify.Meta.Code == 200)
                    {
                        notify.Meta.Message = "Đồng bộ thành công";
                    }
                }
                catch (Exception ex)
                {
                    notify.Meta.Code = 6666;
                    notify.Meta.Message = "Lỗi hệ thống";
                    notify.Meta.TechMssg = ex.Message;
                }
            }

            return notify;
        }


        public async Task<CampaignAPIResultModel<object>> CopyCampaign(CampaignCopyModel req, CampaignAPIModel api)
        {
            var notify = new CampaignAPIResultModel<object>();

            System.Net.ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            using (var httpClient = new HttpClient())
            {
                DateTime RequestDate = DateTime.Now;
                try
                {
                    httpClient.BaseAddress = new Uri(api.endPoint);
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{api.User.Trim()}:{api.Password.Trim()}")));
                    httpClient.Timeout = TimeSpan.FromSeconds(api.timeOut);
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    string jsonContent = JsonConvert.SerializeObject(req);

                    HttpContent content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await httpClient.PostAsync(api.nameAPI, content);

                    string responseData = await response.Content.ReadAsStringAsync();
                    notify = JsonConvert.DeserializeObject<CampaignAPIResultModel<object>>(responseData);
                    if (notify.Meta.Code == 200)
                    {
                        notify.Meta.Message = "Copy CTKM thành công";
                    }
                }
                catch (Exception ex)
                {
                    notify.Meta.Code = 6666;
                    notify.Meta.Message = "Lỗi hệ thống";
                    notify.Meta.TechMssg = ex.Message;
                }
            }
            return notify;
        }

        public Tuple<List<SelectionItem>, int> GetListItem(CampaignItemFilterModel filter)
        {
            var data = new List<SelectionItem>();
            int totalCount = 0;

            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    var queryItem = db.Items.Where(x => x.Blocked == 0);
                    var queryPosGroupItem = db.PosGroupItems.Where(x => x.Status == 1);

                    if (!string.IsNullOrEmpty(filter.SearchText))
                    {
                        string searchText = filter.SearchText.Trim().ToLower();
                        queryItem = queryItem.Where(x => x.No.ToLower().Contains(searchText) || x.Description.ToLower().Contains(searchText));
                    }
                    var queryJoin = from item in queryItem
                                    join posGroup in queryPosGroupItem on item.No equals posGroup.ItemNo
                                    select new SelectionItem
                                    {
                                        Value = item.No,
                                        Name = item.Description
                                    };

                    totalCount = queryJoin.Distinct().Count();
                    int pageSize = filter.PageSize;
                    int page = filter.PageIndex;
                    int skip = (page - 1) * pageSize > totalCount ? totalCount : (page - 1) * pageSize;

                    data = queryJoin.Distinct().OrderBy(x => x.Value).Skip(skip).Take(pageSize).ToList();
                }
                catch (Exception ex)
                {

                }
            }
            return Tuple.Create(data, totalCount);
        }

        public Tuple<List<SelectionItem>, int> GetListOfferNo(CampaignOfferNoFilterModel filter)
        {
            var data = new List<SelectionItem>();
            int totalCount = 0;

            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    var query = db.OfferHeaders.Where(x => x.OfferType == filter.OfferType && x.StartingDate <= filter.StartDate && x.EndingDate >= filter.EndDate);

                    if (!string.IsNullOrEmpty(filter.SearchText))
                    {
                        string searchText = filter.SearchText.Trim().ToLower();
                        query = query.Where(x => x.No.ToLower().Contains(searchText) || x.Description.ToLower().Contains(searchText));
                    }

                    totalCount = query.Count();
                    int pageSize = filter.PageSize;
                    int page = filter.PageIndex;
                    int skip = (page - 1) * pageSize > totalCount ? totalCount : (page - 1) * pageSize;

                    data = query.Select(x => new SelectionItem { Name = x.Description, Value = x.No }).OrderBy(x => x.Value).Skip(skip).Take(pageSize).ToList();
                }
                catch (Exception ex)
                {

                }
            }
            return Tuple.Create(data, totalCount);
        }

        public async Task<Tuple<List<ComboxOptionModel>, int>> GetStoreList(string searchText, int pageIndex = 0, int pageSize = 50)
        {
            try
            {
                int totalRecord = 0;
                var skip = (pageIndex - 1) * pageSize;

                using (var db = new CentralMDPartnerContainer())
                {
                    var query = db.Stores.Where(x => x.ClosingMethod == 0);

                    if (!string.IsNullOrEmpty(searchText))
                    {
                        query = query.Where(x => x.No.ToLower().Contains(searchText) || x.Name.ToLower().Contains(searchText));
                    }

                    totalRecord = query.Count();

                    var data = query.OrderBy(x => x.No).Skip(skip).Take(pageSize).Select(x => new ComboxOptionModel { Value = x.No, Text = x.Name }).ToList();
                    return new Tuple<List<ComboxOptionModel>, int>(data, totalRecord);
                }
            }
            catch (Exception ex)
            {
                return new Tuple<List<ComboxOptionModel>, int>(new List<ComboxOptionModel>(), 0);
            }
        }

        public async Task<List<ComboxOptionModel>> GetStoreByCodeList(List<string> listCode)
        {
            try
            {
                if (listCode == null)
                    return new List<ComboxOptionModel>();

                using (var db = new CentralMDPartnerContainer())
                {
                    var query = db.Stores.Where(x => x.ClosingMethod == 0);

                    var filteredStoreList = query.Where(x => listCode.Any(a => a.Equals(x.No)))
                                                 .OrderBy(x => x.No)
                                                 .Select(x => new ComboxOptionModel
                                                 {
                                                     Value = x.No,
                                                     Text = x.Name
                                                 }).ToList();

                    return filteredStoreList;
                }
            }
            catch (Exception ex)
            {
                return new List<ComboxOptionModel>();
            }
        }
        public async Task<Tuple<List<CampaignImportModel>, string>> Import(DataTable dataImport)
        {
            try
            {
                if (dataImport.Rows.Count == 0)
                {
                    return Tuple.Create(new List<CampaignImportModel>(), "Vui lòng thêm data vào file import");
                }

                var dataCollect = new List<Tuple<string, string>>();
                foreach (DataRow row in dataImport.Rows)
                {
                    dataCollect.Add(Tuple.Create(row[0].ToString().Trim().ToUpper(), row[1].ToString().Trim().ToUpper()));
                }

                var checkGroupEmpty = dataCollect.FirstOrDefault(x => string.IsNullOrEmpty(x.Item1));

                if (checkGroupEmpty != null)
                {
                    return Tuple.Create(new List<CampaignImportModel>(), "Vui lòng nhập đầy đủ Nhóm");
                }

                var checkItemEmpty = dataCollect.FirstOrDefault(x => string.IsNullOrEmpty(x.Item2));

                if (checkItemEmpty != null)
                {
                    return Tuple.Create(new List<CampaignImportModel>(), "Vui lòng nhập đầy đủ Mã sản phẩm");
                }

                var groupDataCollect = dataCollect.GroupBy(x => x.Item1).Select(x => new
                {
                    Key = x.Key,
                    Items = x.Select(y => y.Item2).ToList()
                }).ToList();

                if(groupDataCollect.Count > 4)
                {
                    return Tuple.Create(new List<CampaignImportModel>(), "Số nhóm tối đa chỉ được 4 nhóm");
                }

                using (var db = new CentralMDPartnerContainer())
                {
                    var listCampaignImport = new List<CampaignImportModel>();

                    List<string> listItemNo = dataCollect.Select(x => x.Item2).Distinct().ToList();
                    var queryItem = db.Items.Where(x => x.Blocked == 0 && listItemNo.Any(y => y == x.No));
                    var queryPosGroupItem = db.PosGroupItems.Where(x => x.Status == 1);

                    var queryJoin = from item in queryItem
                                    join posGroup in queryPosGroupItem on item.No equals posGroup.ItemNo
                                    select new SelectionItem
                                    {
                                        Value = item.No,
                                        Name = item.Description
                                    };

                    var resQueryJoin = queryJoin.Distinct().ToList();

                    var checkItemExist = listItemNo.Where(x => !resQueryJoin.Any(y => y.Value == x)).ToList();
                    if (checkItemExist.Any())
                    {
                        return Tuple.Create(new List<CampaignImportModel>(), $"Mã sản phẩm {string.Join(", ", checkItemExist)} không tồn tại");
                    }

                    for (int i = 0; i < groupDataCollect.Count; i++)
                    {
                        listCampaignImport.Add(new CampaignImportModel
                        {
                            Sequence = i + 1,
                            Items = resQueryJoin.Where(x => groupDataCollect[i].Items.Any(y => y == x.Value)).ToList(),
                        });
                    }

                    return Tuple.Create(listCampaignImport, string.Empty);
                }
            }
            catch (Exception ex)
            {
                return Tuple.Create(new List<CampaignImportModel>(), "Lỗi hệ thống");
            }
        }

        public List<GetOfferTypeModel> GetListOfferType()
        {
            var offerType = new List<GetOfferTypeModel>();
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    offerType = db.OfferTypes.Where(d => d.Enabled == true).Select(a => new GetOfferTypeModel
                    {
                        ID = a.ID,
                        OfferType = a.OfferType1,
                        OfferName = a.OfferName,
                    }).OrderBy(d => d.OfferType).ToList();
                }
                catch (Exception ex)
                {
                }
            }
            return offerType;
        }





    }
}
