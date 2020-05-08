using System;
using System.Data.SqlClient;
using System.Configuration;
using System.Data;

namespace SyslogReceiver
{
    class Database
    {
        /// <summary>
        /// Connect to database
        /// </summary>
        /// <returns></returns>
        private static SqlConnection connect()
        {
            ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings["DB Connection"];
            SqlConnection db = new SqlConnection(settings.ConnectionString);
            try
            {
                db.Open();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return db;
        }

        /// <summary>
        /// Save log to database
        /// </summary>
        /// <param name="log">log object</param>
        public static void SaveLog(Log log)
        {
            SqlConnection db = connect();
            SqlCommand insertLog = new SqlCommand(@"INSERT INTO SYSLOGS(severityId, facilityId, timeStamp, hostname, processId, processName, message) 
            VALUES (@severityId, @facilityId, @timeStamp, @hostname, @processId, @processName, @message)", db);

            insertLog.Parameters.Add(new SqlParameter("@severityId", SqlDbType.Int) { Value = log.Severity  } );
            insertLog.Parameters.Add(new SqlParameter("@facilityId", SqlDbType.Int) { Value = log.Facility });
            insertLog.Parameters.Add(new SqlParameter("@timeStamp", SqlDbType.DateTime2) { Value = log.Timestamp });
            insertLog.Parameters.Add(new SqlParameter("@hostname", SqlDbType.NVarChar) { Value = log.Hostname });
            insertLog.Parameters.Add(new SqlParameter("@processId", SqlDbType.Int) { Value = log.ProcessId });
            insertLog.Parameters.Add(new SqlParameter("@processName", SqlDbType.NVarChar) { Value = log.ProcessName });
            insertLog.Parameters.Add(new SqlParameter("@message", SqlDbType.NVarChar) { Value = log.Msg });

            try
            {
                insertLog.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
