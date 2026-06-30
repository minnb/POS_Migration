using System;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using VCM.BLUEPOS.Model.Account;


namespace VCM.BLUEPOS.Models.Account
{
    public class LapConnection
    {
        public static ADUserModel ADUser = new ADUserModel();
        public static ADConnectionStatus CheckUsesLoginAd(string lapIp, string lapPath, string userNameAdmin, string passwordAdmin, string userNameEntry, string passwordEntry)
        {
            try
            {
                using (DirectoryEntry entry = new DirectoryEntry(lapPath, userNameAdmin, passwordAdmin, AuthenticationTypes.Secure))
                {
                    DirectorySearcher ds = new DirectorySearcher(entry);
                    ds.SearchScope = SearchScope.Subtree;
                    SearchResult result = ds.FindOne();

                    if (result != null)
                    {
                        using (DirectoryEntry checkPass = new DirectoryEntry(lapPath, userNameEntry, passwordEntry, AuthenticationTypes.Secure))
                        {
                            DirectorySearcher dsUser = new DirectorySearcher(checkPass);
                            dsUser.SearchScope = SearchScope.Subtree;
                            SearchResult resultUser = dsUser.FindOne();
                            if (resultUser != null)
                            {
                                var user = GetADUserInfo(lapIp, userNameAdmin, passwordAdmin, userNameEntry);
                                if (user == null)
                                {
                                    return ADConnectionStatus.LoginADFail;
                                }

                                ADUser = user;
                                return ADConnectionStatus.LoginADSuccess;
                            }
                            else
                            {
                                return ADConnectionStatus.LoginADSuccess;
                            }
                        }
                    }
                    else
                    {
                        return ADConnectionStatus.ConnectADFail;
                    }
                }
            }
            catch (Exception ex)
            {
                return ADConnectionStatus.ConnectADFail;
            }
        }

        public static bool IsActiveUser(DirectoryEntry de)
        {
            if (de.NativeGuid == null) return false;
            int flags = (int)de.Properties["userAccountControl"].Value;
            return !Convert.ToBoolean(flags & 0x0002);
        }

        public static ADUserModel GetADUserInfo(string lapIp, string userNameAdmin, string passwordAdmin, string userNameEntry)
        {
            try
            {
                var userAd = new ADUserModel();
                string sValue = "";
                string fullName = "";
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
                                if (!IsActiveUser(de))
                                {
                                    return null;
                                }

                                sValue = de.Properties["displayName"].Value != null ? (string)de.Properties["displayName"].Value : "";
                                //Không có thông tin displayNam thì lấy thông tin khác

                                if (sValue == "")
                                {
                                    sValue = de.Properties["givenName"].Value != null ? (string)de.Properties["givenName"].Value : "";
                                    sValue += de.Properties["sn"].Value != null ? (string)de.Properties["sn"].Value : "";
                                }
                                fullName = de.Properties["sn"].Value != null ? (string)de.Properties["sn"].Value : "";
                                fullName += " " + (de.Properties["givenName"].Value != null ? (string)de.Properties["givenName"].Value : "");

                                userAd.FullName = fullName;
                                userAd.FullNameDep = sValue;

                                userAd.Department = de.Properties["department"].Value != null ? (string)de.Properties["department"].Value : "";
                                userAd.Company = de.Properties["company"].Value != null ? (string)de.Properties["company"].Value : "";
                                //Thông tin email
                                userAd.Email = de.Properties["mail"].Value != null ? (string)de.Properties["mail"].Value : "";
                                //Thông tin số điện thoại
                                userAd.Phone = de.Properties["mobile"].Value != null ? (string)de.Properties["mobile"].Value : "";
                                userAd.UserName = de.Properties["sAMAccountName"].Value != null ? (string)de.Properties["sAMAccountName"].Value : "";
                                userAd.StaffCode = de.Properties["employeeID"].Value != null ? (string)de.Properties["employeeID"].Value : "";
                                userAd.Title = de.Properties["title"].Value != null ? (string)de.Properties["title"].Value : "";

                            }
                        }
                    }
                }
                return userAd;
            }
            catch (Exception)
            {
                return null;
            }
        }
       
    }
}