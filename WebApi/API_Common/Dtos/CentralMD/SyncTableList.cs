namespace TCX.API.Common.Dtos.CentralMD
{
    public class SyncTableList
    {
        public string FileName { get; set; }
        public string TableName { get; set; }
        public string Action { get; set; } = "DELETE-INSERT";
        public string ProcedureName { get; set; }
        public string ProcessID { get; set; }
        public object Data { get; set; } = null;
    }
}
