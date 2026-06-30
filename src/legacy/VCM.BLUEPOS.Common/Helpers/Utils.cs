using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Sockets;
using System.Web;
using System.Reflection;


namespace VCM.BLUEPOS.Common.Helpers
{
    public static class Utils
    {
        public static bool CheckPort(string IPAddress, int port, ref string Result)
        {
            try
            {
                var client = new TcpClient();
                if (!client.ConnectAsync(IPAddress, port).Wait(5000))
                {
                    Result = client.Connected.ToString();
                    client.Close();
                    return false;
                }
                else
                {
                    Result = client.Connected.ToString();
                    client.Close();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Result = ex.Message;
                return false;
            }
        }

        public static DataTable ToDataTable<T>(this IEnumerable<T> list)
        {
            Type elementType = typeof(T);
            using (DataTable t = new DataTable())
            {
                PropertyInfo[] _props = elementType.GetProperties();
                foreach (PropertyInfo propInfo in _props)
                {
                    Type _pi = propInfo.PropertyType;
                    Type ColType = Nullable.GetUnderlyingType(_pi) ?? _pi;
                    t.Columns.Add(propInfo.Name, ColType);
                }
                foreach (T item in list)
                {
                    DataRow row = t.NewRow();
                    foreach (PropertyInfo propInfo in _props)
                    {
                        row[propInfo.Name] = propInfo.GetValue(item, null) ?? DBNull.Value;
                    }
                    t.Rows.Add(row);
                }
                return t;
            }
        }
        public static DataSet ToDataSet<T>(this IEnumerable<T> list)
        {
            using (DataSet ds = new DataSet())
            {
                ds.Tables.Add(list.ToDataTable());
                return ds;
            }
        }



    }
}
