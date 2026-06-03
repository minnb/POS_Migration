using System;
using System.Data.SqlClient;
using System.Reflection.Emit;
using TCX.API.Common.Enums;
using TCX.WebApiCore;
using static System.Net.Mime.MediaTypeNames;
public static class DapperLoyaltyFactory
{
    private static string _connectionString;

    public static void Initialize(string connectionString)
    {
        _connectionString = connectionString;
    }

    public static SqlConnection CreateConnection()
    {
        try
        {
            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new InvalidOperationException("DapperLoyaltyFactory Connection string is not initialized.");
            }

            return new SqlConnection(_connectionString);
        }
        catch (Exception ex)
        {
            HttpClientService.SendMessageSMS("Error creating SQL connection: " + ex.Message, MessageTypeEnum.Warning.ToString(), "Lỗi kết nối database Loyalty", "", "", appCode: AppCodeMessageEnum.BLUEPOS_SQL_EXP.ToString());
            throw new Exception("Error creating SQL connection: " + ex.Message, ex);
        }
    }
}
public static class DapperCentralMDFactory
{
    private static string _connectionString;

    public static void Initialize(string connectionString)
    {
        _connectionString = connectionString;
    }

    public static SqlConnection CreateConnection()
    {
        try
        {
            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new InvalidOperationException("DapperCentralMDFactory Connection string is not initialized.");
            }

            return new SqlConnection(_connectionString);
        }
        catch(Exception ex)
        {
            HttpClientService.SendMessageSMS("Error creating SQL connection: " + ex.Message, MessageTypeEnum.Warning.ToString(), "Lỗi kết nối database CentralMD", "", "", appCode: AppCodeMessageEnum.BLUEPOS_SQL_EXP.ToString());
            throw new Exception("Error creating SQL connection: " + ex.Message, ex);
        }
    }
}