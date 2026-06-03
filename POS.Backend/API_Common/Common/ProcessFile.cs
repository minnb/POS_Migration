using System.IO;

namespace TCX.API.Common.Common
{
    public class ProcessFile
    {
        public static void CreateFolder(string folder)
        {
            try
            {
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);
            }
            catch
            {

            }
        }

        public static bool DowloadFile(string file_name, string _pathSaveFile, string _str)
        {
            CreateFolder(_pathSaveFile);
            var _filename = _pathSaveFile +"\\"+ file_name;
            try
            {
                if (_str != null)
                {
                    File.Create(_filename).Dispose();
                    using (TextWriter tw = new StreamWriter(_filename))
                    {
                        tw.WriteLine(_str);
                        tw.Close();
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
