using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;


public class Log
{
    private static object _syncLogs = new object();

    public static void Debug(string content)
    {
        var path = HttpRuntime.AppDomainAppPath + "App_Data\\Logs";

        if (!System.IO.Directory.Exists(path))
            System.IO.Directory.CreateDirectory(path);

        lock (_syncLogs)
        {
            try
            {
                var dt = DateTime.Now;
                path = string.Format(@"{0}\Debug-{1:dd-MM-yyyy}.txt", path, dt.Date);
                System.IO.StreamWriter sw = new System.IO.StreamWriter(path, true);
                sw.WriteLine("{0:HH:mm:ss} => {1}", dt, content);
                sw.Flush();
                sw.Close();
            }
            catch
            {

            }
        }
    }
}