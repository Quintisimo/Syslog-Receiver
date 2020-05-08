using System;
using System.IO;
using System.Text;

namespace SyslogReceiver
{
    class File
    {
        private const string DIRECTORY = @"C:\Users\Administrator\Documents";
        private const string FILE_NAME = "log";

        /// <summary>
        /// Write log to file
        /// </summary>
        /// <param name="log">log row</param>
        public static void WriteLog(Log log)
        {
            string fileName = Path.Combine(DIRECTORY, $"{FILE_NAME} {DateTime.Now:dd-MM-yyyy}.txt");

            if (!Directory.Exists(DIRECTORY))
            {
                Directory.CreateDirectory(DIRECTORY);
            }

            if (!System.IO.File.Exists(fileName))
            {
                using (FileStream fsCreate = System.IO.File.Create(fileName))
                {
                    Byte[] header = new UTF8Encoding(true).GetBytes($"Created at {DateTime.Now:dd-MM-yyyy}{Environment.NewLine}{Environment.NewLine}");
                    fsCreate.Write(header, 0, header.Length);
                }
            }

            using (StreamWriter fsAppend = System.IO.File.AppendText(fileName))
            {
                fsAppend.WriteLine($"Host: {log.Hostname}");
                fsAppend.WriteLine($"Timestamp: {log.Timestamp}");
                fsAppend.WriteLine($"Process Id: {log.ProcessId}");
                fsAppend.WriteLine($"Process Name: {log.ProcessName}");
                fsAppend.WriteLine($"Severity: {log.getSeverity()}");
                fsAppend.WriteLine($"Facility: {log.getFacility()}");
                fsAppend.WriteLine($"Message: {log.Msg}{Environment.NewLine}");
            }
        }
    }
}
