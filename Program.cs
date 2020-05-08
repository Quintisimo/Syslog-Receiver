using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Globalization;

namespace SyslogReceiver
{
    class Program
    {
        private const int PORT = 514;

        private const string TEST_MESSAGE_1 = @"<30>May  7 11:43:55 dhcpd[56209]: DHCPACK on 192.168.199.2 to 50:c7:bf:9e:c6:6c via em4";
        private const string TEST_MESSAGE_2 = @"<30>May  7 11:03:36 unbound: [43078:0] info: resolving www.tm.a.prd.aadg.akadns.net. A IN";
        private const string TEST_MESSAGE_3 = @"<30>May  7 12:27:40 iked[80712]: spi=0x146e2464e6c8c522: sa_state: ESTABLISHED -> CLOSED from 110.232.116.36:4500 to 172.31.255.255:4500 policy 'PharmX Stub Tunnel'";

        private const string PRIORITY_REGEX = @"(?<=\<)(.*?)(?=\>)";

        private const string DATE_REGEX = @"(?<=\>)(\w+\s+\d+\s[^\s]+)";
        private const string DATE_FORMAT = @"MMM  d HH:mm:ss";

        private const string PROCESS_NAME_REGEX = @"(\w+)(?=([:\s]*\[))";
        private const string PROCESS_ID_REGEX = @"(?<=\[)(\d+)(?=[:\d]*\])";

        private const string MESSAGE_REGEX = @"(?<=]:*\s)(.*)";

        private static List<string> Queue = new List<string>();
        private static readonly object writerLock = new object();

        private static IPEndPoint endPoint;


        static void Main(string[] args)
        {
            try
            {
                Thread listenerThread = new Thread(StartServer);
                listenerThread.Start();

                //Thread testerThread = new Thread(Tester);
                //testerThread.Start();

                Task.Run(() => WriterTask());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Start syslog server
        /// </summary>
        private static void StartServer()
        {
            UdpClient listener = new UdpClient(PORT);
            endPoint = new IPEndPoint(IPAddress.Any, PORT);
            Console.WriteLine("Waiting for message");

            try
            {
                while (true)
                {
                    byte[] bytes = listener.Receive(ref endPoint);
                    string data = Encoding.ASCII.GetString(bytes, 0, bytes.Length);
                    Console.WriteLine(data);
                    lock (writerLock)
                    {
                        Queue.Add(data);
                    }
                }
            }
            catch (Exception e)
            {

                Console.WriteLine($"Server Error: {e.Message}");
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
                while (true)
                {
                    Task.Delay(1000).Wait();

                    lock (writerLock)
                    {
                        foreach (string message in Queue)
                        {
                            if (!message.Contains("last message repeated"))
                            {
                                if (!DateTime.TryParseExact(GetValue(DATE_REGEX, message), DATE_FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime timeStamp))
                                {
                                    Console.WriteLine("Date received was invalid using current date");
                                    timeStamp = DateTime.Now;
                                }

                                if (!int.TryParse(GetValue(PROCESS_ID_REGEX, message), out int processId))
                                {
                                    processId = -1;
                                }

                                if (int.TryParse(GetValue(PRIORITY_REGEX, message), out int priority))
                                {
                                    string msg = GetValue(MESSAGE_REGEX, message);
                                    string hostname = endPoint.Address.ToString();

                                    string processName = GetValue(PROCESS_NAME_REGEX, message);

                                    int facility = priority / 8;
                                    int severity = priority - (facility * 8);

                                    Log log = new Log
                                    {
                                        Hostname = hostname,
                                        Timestamp = timeStamp,
                                        Msg = msg,
                                        ProcessId = processId,
                                        ProcessName = processName,
                                        Facility = facility,
                                        Severity = severity,
                                    };

                                    //File.WriteLog(log);
                                    Database.SaveLog(log);
                                    Console.WriteLine($"Received from {hostname}");
                                    Console.WriteLine($"Data: {message}{Environment.NewLine}");
                                }


                            }
                        }
                        Queue = new List<string>();
                    }
                }
            }
            catch (Exception e)
            {

                Console.WriteLine($"Write Error: {e.Message}");
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

            byte[] send = Encoding.ASCII.GetBytes(TEST_MESSAGE_3);
            IPEndPoint endPoint = new IPEndPoint(broadcast, 514);

            while (true)
            {
                s.SendTo(send, endPoint);
                Thread.Sleep(1000);
            }
        }

    }
}
