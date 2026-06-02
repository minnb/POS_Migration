using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Globalization;
using System.Linq;
using System.Text;
using TCX.API.Common.Enums;
using TCX.API.Common.Helpers;
using VCM.POSBLUE.Data.DataBaseContext;
using VCM.POSBLUE.Model.Salary;

namespace VCM.POSBLUE.Data.Salary
{
    public interface ISalaryData
    {
        Tuple<ADConnectionStatus, ADUserModel> Login(LoginViewModel req);
        SalaryResponse Get_BangLuongThang_ByEmp(string MaNV, DateTime Date);
        Tuple<bool, string> GetEmplCode(string companyCode, string userAD);
    }
    public class SalaryData: ISalaryData
    {
        public SalaryResponse Get_BangLuongThang_ByEmp(string MaNV, DateTime Date)
        {
            try
            {
                var context = new SalaryContainer();
                DataTable table = new DataTable();
                DataSet ds = new DataSet();
                using (var con = new SqlConnection(context.Database.Connection.ConnectionString))
                using (var cmd = new SqlCommand("Get_BangLuongThang_ByEmp", con))
                using (var da = new SqlDataAdapter(cmd))
                {
                    cmd.Parameters.AddWithValue("@PERNR", MaNV);
                    cmd.Parameters.AddWithValue("@DATE", Date);
                    cmd.CommandType = CommandType.StoredProcedure;
                    da.Fill(ds);
                    var t1 = ds.Tables[0];//Thông tin nhân viên
                    var t2 = ds.Tables[1];//Thông tin bảng công
                    var t3 = ds.Tables[2];//Thông tin lương
                    NhanVienModel nv = new NhanVienModel();
                    var lstNV = new List<NhanVienModel>();
                    for (int i = 0; i <= t1.Rows.Count - 1; i++)
                    {
                        nv.MaNhanVien = t1.Rows[i]["MaNhanVien"].ToString();
                        nv.NgayBatDauLamViec = Convert.ToDateTime(t1.Rows[i]["NgayBatDauLamViec"]);
                        nv.MasterCode = t1.Rows[i]["MasterCode"].ToString();
                        nv.HoTen = t1.Rows[i]["HoTen"].ToString();
                        nv.KhuVucNhanSu = t1.Rows[i]["KhuVucNhanSu"].ToString();
                        nv.PhongBan = t1.Rows[i]["PhongBan"].ToString();
                        nv.ChucDanh = t1.Rows[i]["ChucDanh"].ToString();
                        nv.SoTaiKhoan = t1.Rows[i]["SoTaiKhoan"].ToString();
                        nv.NganHang = t1.Rows[i]["NganHang"].ToString();
                        lstNV.Add(nv);
                    }

                    BangCongModel bc = new BangCongModel();
                    var lstBC = new List<BangCongModel>();
                    for (int i = 0; i <= t2.Rows.Count - 1; i++)
                    {
                        bc.KyChamCong = Convert.ToDateTime(t2.Rows[i]["KyChamCong"]);
                        bc.MucLuongThuongThiDua = Decode(t2.Rows[i]["MucLuongThuongThiDua"].ToString());
                        bc.CongChuan = Convert.ToDouble(t2.Rows[i]["CongChuan"].ToString());
                        bc.CongThucTe = Convert.ToDouble(t2.Rows[i]["CongThucTe"].ToString());
                        bc.SoNgayNghiHuongLuong = Convert.ToDouble(t2.Rows[i]["SoNgayNghiHuongLuong"].ToString());
                        bc.TongCongHuongLuong = Convert.ToDouble(t2.Rows[i]["TongCongHuongLuong"].ToString());
                        bc.SoNgayNghiKhongHuongLuong = Convert.ToDouble(t2.Rows[i]["SoNgayNghiKhongHuongLuong"].ToString());
                        bc.PhepConLai = Convert.ToDouble(t2.Rows[i]["PhepConLai"].ToString());
                        bc.GioBuConLai = Convert.ToDouble(t2.Rows[i]["GioBuConLai"].ToString());
                        lstBC.Add(bc);
                    }

                    var lstBL = new List<BangLuongModel>();
                    for (int i = 0; i <= t3.Rows.Count - 1; i++)
                    {
                        BangLuongModel bl = new BangLuongModel
                        {
                            //bl.BEGDA = Convert.ToDateTime(t3.Rows[i]["BEGDA"]);
                            //bl.ENDDA = Convert.ToDateTime(t3.Rows[i]["ENDDA"]);
                            TYPE = t3.Rows[i]["TYPE"].ToString().Trim(),
                            CONTENT = t3.Rows[i]["CONTENT"].ToString(),
                            FOMULA1 = String.Format("{0:#,#}", decimal.Parse(Decode(t3.Rows[i]["FOMULA1"].ToString()) == "" ? "0" : Decode(t3.Rows[i]["FOMULA1"].ToString()), CultureInfo.InvariantCulture))
                        };
                        //bl.FOMULA2 = t3.Rows[i]["FOMULA2"].ToString();
                        //bl.FOMULA3 = t3.Rows[i]["FOMULA3"].ToString();
                        //bl.FOMULA4 = t3.Rows[i]["FOMULA4"].ToString();
                        lstBL.Add(bl);
                    }

                    var result = new SalaryResponse()
                    {
                        InfoStaff = lstNV.FirstOrDefault(),
                        InfoWork = lstBC.FirstOrDefault(),
                        InfoSalary = lstBL
                    };
                    return result;
                }
            }
            catch
            {
                return default;
            }
        }
        public Tuple<ADConnectionStatus, ADUserModel> Login(LoginViewModel req)
        {
            try
            {
                var pathLapAd_MSN = ConfigurationManager.AppSettings["PathLapAd_MSN"];
                var lapIp_MSN = ConfigurationManager.AppSettings["PathLapIP_MSN"];
                var usernameLapAdmin_MSN = ConfigurationManager.AppSettings["LapAdUserNameAdmin_MSN"];
                var passwordLapAdmin_MSN = ConfigurationManager.AppSettings["LapAdPasswordAdmin_MSN"];

                var status_MSN = CheckUsesLoginAd(lapIp_MSN, pathLapAd_MSN, usernameLapAdmin_MSN, passwordLapAdmin_MSN, req.UserName, req.PassWord);

                return new Tuple<ADConnectionStatus, ADUserModel>(status_MSN.Item1, status_MSN.Item2);
            }
            catch (Exception ex) 
            {
                FileHelper.WriteExpLogs("ADLogin", ex);
                return new Tuple<ADConnectionStatus, ADUserModel>(ADConnectionStatus.ConnectADFail, null);
            }
        }
        public Tuple<ADConnectionStatus, ADUserModel> CheckUsesLoginAd(string lapIp, string lapPath, string userNameAdmin, string passwordAdmin, string userNameEntry, string passwordEntry)
        {
            var adStatus = new ADConnectionStatus();
            var adUser = new ADUserModel();
            var resultAD = new Tuple<ADConnectionStatus, ADUserModel>(adStatus, adUser);
            try
            {
                using (DirectoryEntry entry = new DirectoryEntry(lapPath, userNameAdmin, passwordAdmin, AuthenticationTypes.Secure))
                {
                    DirectorySearcher ds = new DirectorySearcher(entry)
                    {
                        SearchScope = SearchScope.Subtree
                    };
                    SearchResult result = ds.FindOne();

                    if (result != null)
                    {
                        using (DirectoryEntry checkPass = new DirectoryEntry(lapPath, userNameEntry, passwordEntry, AuthenticationTypes.Secure))
                        {
                            DirectorySearcher dsUser = new DirectorySearcher(checkPass)
                            {
                                SearchScope = SearchScope.Subtree
                            };
                            SearchResult resultUser = dsUser.FindOne();
                            if (resultUser != null)
                            {
                                var user = GetADUserInfo(lapIp, userNameAdmin, passwordAdmin, userNameEntry);
                                if (user == null)
                                {
                                    resultAD = new Tuple<ADConnectionStatus, ADUserModel>(ADConnectionStatus.LoginADFail, null);
                                    //return ADConnectionStatus.LoginADFail;
                                }
                               // ADUser = user;
                                resultAD = new Tuple<ADConnectionStatus, ADUserModel>(ADConnectionStatus.LoginADSuccess, user);
                                //return ADConnectionStatus.LoginADSuccess;
                            }
                            else
                            {
                                resultAD = new Tuple<ADConnectionStatus, ADUserModel>(ADConnectionStatus.LoginADFail, null);
                               // return ADConnectionStatus.LoginADSuccess;
                            }
                        }

                    }
                    else
                    {
                        resultAD = new Tuple<ADConnectionStatus, ADUserModel>(ADConnectionStatus.ConnectADFail, null);
                        //return ADConnectionStatus.ConnectADFail;
                    }
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("CheckUsesLoginAd", ex);
                //return ADConnectionStatus.ConnectADFail;
                resultAD = new Tuple<ADConnectionStatus, ADUserModel>(ADConnectionStatus.ConnectADFail, null);

            }
            return resultAD;
        }
        public static ADUserModel GetADUserInfo(string lapIp, string userNameAdmin, string passwordAdmin, string userNameEntry)
        {
            try
            {
                var userAd = new ADUserModel();
                string sValue = "";
                //string x = "";
                using (var context = new PrincipalContext(ContextType.Domain, lapIp, userNameAdmin, passwordAdmin))
                {
                    using (UserPrincipal user = new UserPrincipal(context))
                    {
                        user.SamAccountName = userNameEntry;

                        using (var searcher = new PrincipalSearcher(user))
                        {
                            var result = searcher.FindOne();
                            if (result != null)
                            {
                                DirectoryEntry de = result.GetUnderlyingObject() as DirectoryEntry;
                                sValue = de.Properties["displayName"].Value != null ? (string)de.Properties["displayName"].Value : "";
                                //Không có thông tin displayNam thì lấy thông tin khác
                                if (sValue == "")
                                {
                                    sValue = de.Properties["givenName"].Value != null ? (string)de.Properties["givenName"].Value : "";
                                    sValue += de.Properties["sn"].Value != null ? (string)de.Properties["sn"].Value : "";
                                }

                                userAd.FullName = sValue;
                                userAd.Department = de.Properties["department"].Value != null ? (string)de.Properties["department"].Value : "";
                                userAd.Company = de.Properties["company"].Value != null ? (string)de.Properties["company"].Value : "";
                                //Thông tin email
                                userAd.Email = de.Properties["mail"].Value != null ? (string)de.Properties["mail"].Value : "";
                                //Thông tin số điện thoại
                                //01699595456
                                userAd.Phone = de.Properties["mobile"].Value != null ? (string)de.Properties["mobile"].Value : "";
                                userAd.UserName = de.Properties["sAMAccountName"].Value != null ? (string)de.Properties["sAMAccountName"].Value : ""; ;
                                userAd.MasterCode = de.Properties["employeeID"].Value != null ? (string)de.Properties["employeeID"].Value : "";
                                userAd.Title = de.Properties["title"].Value != null ? (string)de.Properties["title"].Value : "";

                                using (var db = new SalaryContainer())
                                {
                                    var data = db.Database.SqlQuery<PernrByUserAD>("[dbo].[Get_pernr_by_UsernameAD] @UsernameAD ", new SqlParameter("@UsernameAD", userAd.UserName)).FirstOrDefault();
                                    userAd.MaNV = data.PERNR;                                   
                                }
                            }
                            else
                            {
                                return null;
                            }
                        }
                    }
                }
                return userAd;
            }
            catch
            {
                return null;
            }
        }
        public string Decode(String base64code)
        {
            if (string.IsNullOrEmpty(base64code))
                return string.Empty;
            var lv1 = Encoding.UTF8.GetString(Convert.FromBase64String(base64code.Trim()));
            lv1 = lv1.Replace("dmluZ3JvdXA=", string.Empty);
            var lv2 = Encoding.UTF8.GetString(Convert.FromBase64String(lv1));
            return lv2;
        }
        public Tuple<bool, string> GetEmplCode(string companyCode, string userAD)
        {
            try
            {
                using (var db = new SalaryContainer())
                {
                    var data = db.Database.SqlQuery<PernrByUserAD>("[dbo].[Get_pernr_by_UsernameAD] @UsernameAD ", new SqlParameter("@UsernameAD", companyCode +"."+ userAD)).FirstOrDefault();
                    if(data != null && !string.IsNullOrEmpty(data.PERNR))
                    {
                        return new Tuple<bool, string>(true, data.PERNR);
                    }
                    else
                    {
                        return new Tuple<bool, string>(false, @"Không tìm thấy mã nhân viên");
                    }
                }
            }
            catch(Exception ex)
            {
                FileHelper.WriteExpLogs("GetEmplCode", ex);
                return new Tuple<bool, string>(false, @"Không tìm thấy mã nhân viên");
            }
        }
    }
}
