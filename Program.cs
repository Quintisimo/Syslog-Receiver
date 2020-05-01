using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SyslogReceiver
{
    class Program
    {
        private const int PORT = 11000;
        private const string DIRECTORY = @"D:\User\Documents\Logs";
        private const string FILE_NAME = "log";

        private const string TEST_MESSAGE = "<100>2 1982-07-10T20:30:40.001Z myserver.com su 201 32001 - BOM 'su root' failed on /dev/pts/7";
        private const string PRIORITY_REGEX = @"(?<=\<)(.*?)(?=\>)";
        private const string VERSION_REGEX = @"(?<=\>)\d+";

        private static List<string> Queue = new List<string>();
        private static readonly object writerLock = new object();


        static void Main(string[] args)
        {
            try
            {
                Thread listenerThread = new Thread(StartServer);
                listenerThread.Start();

                Thread testerThread = new Thread(Tester);
                testerThread.Start();

                Task.Run(() => WriterTask());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Environment.Exit(-1);
            }
        }

        /// <summary>
        /// Start syslog server
        /// </summary>
        private static void StartServer()
        {
            UdpClient listener = new UdpClient(PORT);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, PORT);
            Console.WriteLine("Waiting for message");

            try
            {
                while(true)
                {
                    byte[] bytes = listener.Receive(ref endPoint);
                    string data = Encoding.ASCII.GetString(bytes, 0, bytes.Length);

                    lock(writerLock)
                    {
                        Queue.Add(data);
                    }
                }
            }
            catch (Exception e)
            {

                Console.WriteLine(e.Message);
            }
            finally
            {
                listener.Close();
            }
        }

        /// <summary>
        /// Convert queue message to Log class
        /// </summary>
        private static void WriterTask()
        {
            try
            {
                while(true)
                {
                    Task.Delay(1000).Wait();

                    lock(writerLock)
                    {
                        foreach (string message in Queue)
                        {
                            string[] messageArr = message.Split();
                            int priority = int.Parse(GetValue(PRIORITY_REGEX, messageArr[0]));

                            int facility = priority / 8;
                            int severity = priority - (facility * 8);
                            int version = int.Parse(GetValue(VERSION_REGEX, messageArr[0]));
                            DateTime timeStamp = DateTime.Parse(messageArr[1]);
                            string hostname = messageArr[2];
                            string appName = messageArr[3];
                            int procId = int.Parse(messageArr[4]);
                            int msgId = int.Parse(messageArr[5]);
                            string msg = string.Join(" ", new ArraySegment<string>(messageArr, 8, messageArr.Length - 8));

                            Log log = new Log
                            {
                                Hostname = hostname,
                                Timestamp = timeStamp,
                                Msg = msg,
                                Facility = facility,
                                Severity = severity,
                                Version = version,
                                AppName = appName,
                                ProcId = procId,
                                MsgId = msgId
                            };

                            WriteToFile(log);
                            Console.WriteLine($"Received from {hostname}");
                            Console.WriteLine($"Data: {message}{Environment.NewLine}");
                        }
                        Queue = new List<string>();
                    }
                }
            }
            catch (Exception e)
            {

                Console.WriteLine(e.Message);
                Environment.Exit(-1);
            }
        }

        /// <summary>
        /// Get field from message using regex
        /// </summary>
        /// <param name="regexPattern">regex pattern</param>
        /// <param name="message">message string</param>
        /// <returns></returns>
        private static string GetValue(string regexPattern, string message)
        {
            Regex regex = new Regex(regexPattern);
            Match match = regex.Match(message);
            return match.Value;
        }

        private static void Tester()
        {
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPAddress broadcast = IPAddress.Parse("127.0.0.1");

            byte[] send = Encoding.ASCII.GetBytes(TEST_MESSAGE);
            IPEndPoint endPoint = new IPEndPoint(broadcast, 11000);

            while (true)
            {
                s.SendTo(send, endPoint);
                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// Write log to file
        /// </summary>
        /// <param name="log">log row</param>
        private static void WriteToFile(Log log)
        {
            string fileName = Path.Combine(DIRECTORY, $"{FILE_NAME} {DateTime.Now:dd-MM-yyyy}.txt");
            
            if (!Directory.Exists(DIRECTORY))
            {
                Directory.CreateDirectory(DIRECTORY);
            }

            if (!File.Exists(fileName))
            {
                using (FileStream fsCreate = File.Create(fileName))
                {
                    Byte[] header = new UTF8Encoding(true).GetBytes($"Created at {DateTime.Now:dd-MM-yyyy}{Environment.NewLine}{Environment.NewLine}");
                    fsCreate.Write(header, 0, header.Length);
                }
            }

            using (StreamWriter fsAppend= File.AppendText(fileName))
            {
                fsAppend.WriteLine($"Host: {log.Hostname}");
                fsAppend.WriteLine($"Version: {log.Version}");
                fsAppend.WriteLine($"Timestamp: {log.Timestamp}");
                fsAppend.WriteLine($"Severity: {log.getSeverity()}");
                fsAppend.WriteLine($"Facility: {log.getFacility()}");
                fsAppend.WriteLine($"Message: {log.Msg}{Environment.NewLine}");
            }
        }
    }
}
