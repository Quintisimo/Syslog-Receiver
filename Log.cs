using System;
using System.Net;

namespace SyslogReceiver
{
    class Log
    {
        private readonly string[] FACILITIES = {
            "kernal message",
            "user-level message",
            "Mail System",
            "system daemons",
            "security/authorization messages",
            "messages generated internally by syslogd",
            "line printer subsytem",
            "network news subsystem",
            "UUCP subsystem",
            "clock daemon",
            "security/authorization messages",
            "FTP daemon",
            "NTP subsystem",
            "log audit",
            "log alert",
            "clock daemon (note 2)",
            "local use 0  (local0)",
            "local use 1  (local1)",
            "local use 2  (local2)",
            "local use 3  (local3)",
            "local use 4  (local4)",
            "local use 5  (local5)",
            "local use 6  (local6)",
            "local use 7  (local7)"
        };


        private readonly string[] SEVERITIES = {
              "system is unusable",
              "action must be taken immediately",
              "critical conditions",
              "error conditions",
              "warning conditions",
              "normal but significant condition",
              "informational messages",
              "debug-level messages",
        };

        private int facility;
        private int severity;

        public int Facility {
            set {
                if (value > FACILITIES.Length) throw new ArgumentException("Invalid facility code");
                facility = value;
            }
        }

        public string getFacility()
        {
            return FACILITIES[facility];
        }

        public int Severity {
            set {
                if (value > SEVERITIES.Length) throw new ArgumentException("Invalid severity code");
                severity = value;
            }
        }

        public string getSeverity()
        {
            return SEVERITIES[severity];
        }

        public int Version { get; set; }

        public DateTime Timestamp { get; set; }

        public string Hostname { get; set; }

        public string Msg { get; set; }

        public string Type { get; set; }
    }
}
