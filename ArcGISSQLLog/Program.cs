using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Web.Script.Serialization;

namespace ArcGISSQLLog
{
    class Program
    {
        static void Main(string[] args)
        {
            bool debug = false;
            bool proxy = false;

            string logsUrl = "";
            string servicesUrl = "";
            string tokenUrl = "";
            string user = "";
            string password = "";

            string dbUser = "";
            string dbPassword = "";
            string dbName = "";
            string dbServer = "";
            string dbSchema = "";
            string requesterPublicIp = "";
            string filter = "";

            string srid = "";

            int c = args.GetUpperBound(0);

            // Loop through arguments
            for (int n = 0; n < c; n++)
            {
                string thisKey = args[n].ToLower();
                string thisVal = args[n + 1].TrimEnd().TrimStart();

                // eval the key
                switch (thisKey)
                {
                    case "-logsurl":
                        logsUrl = thisVal;
                        break;
                    case "-servicesurl":
                        servicesUrl = thisVal;
                        break;
                    case "-debug":
                        string dbg = thisVal;
                        if (dbg.ToUpper() == "Y") debug = true;
                        break;
                    case "-dbschema":
                        dbSchema = thisVal;
                        break;
                    case "-requesterpublicip":
                        requesterPublicIp = thisVal;
                        break;
                    case "-proxy":
                        string prx = thisVal;
                        if (prx.ToUpper() == "Y") proxy = true;
                        break;
                    case "-cleanlogs":
                        string clg = thisVal;
                        if (clg.ToUpper() == "Y") ;
                        break;
                    case "-user":
                        user = thisVal;
                        break;
                    case "-password":
                        password = thisVal;
                        break;
                    case "-tokenurl":
                        tokenUrl = thisVal;
                        break;
                    case "-dbuser":
                        dbUser = thisVal;
                        break;
                    case "-dbpassword":
                        dbPassword = thisVal;
                        break;
                    case "-dbname":
                        dbName = thisVal;
                        break;
                    case "-dbserver":
                        dbServer = thisVal;
                        break;
                    case "-filter":
                        filter = thisVal;
                        break;
                    case "-srid":
                        srid = thisVal;
                        break;
                    default:
                        break;
                }
            }

            if (logsUrl == "") return;

            string token = "";

            WebClient client = new WebClient();

            if (proxy)
            {
                client.Proxy = WebRequest.DefaultWebProxy;
                client.Proxy.Credentials = CredentialCache.DefaultCredentials;
            }

            token = GetToken(tokenUrl, requesterPublicIp, user, password);

            string startTime = "";
            string endTime = GetLastLogEntryTimeAgsTimestamp(dbUser,dbSchema, dbPassword, dbName, dbServer, debug);
            bool hasMore = true;

            while (hasMore)
            {
                LogMessages logMessages = GetLogMessages(logsUrl,startTime,endTime, "json",filter
                                                         , "FINE",
                                                         "1000", token);
                if (logMessages != null && logMessages.logMessages.Count > 0)
                {
                    ProcessResponse(logMessages, servicesUrl, dbUser,dbSchema, dbPassword, dbName, dbServer, debug, srid, token);
                    hasMore = logMessages.hasMore;
                    if (hasMore) startTime = logMessages.endTime.ToString();
                }
                else
                {
                    hasMore = false;
                }
                
                
            }
            Console.WriteLine("Done!");
        }

        public static string GetLastLogEntryTimeAgsTimestamp(string dbuser, string dbschema, string dbpassword, string dbname, string dbserver, bool debug)
        {
            SqlConnection myConnection = new SqlConnection("Server=" + dbserver + "; Database=" + dbname + "; User ID=" + dbuser + "; Password=" + dbpassword);

            try
            {
                myConnection.Open();
            }
            catch (Exception e)
            {
                if (debug) Console.WriteLine(e.ToString());
                System.Environment.Exit(1);
            }
            SqlCommand cmd = new SqlCommand
                {
                    CommandText = "SELECT TOP(1) * FROM " + dbschema+".RawLogs ORDER BY time DESC",
                    CommandType = CommandType.Text,
                    Connection = myConnection
                };

            SqlDataReader reader = cmd.ExecuteReader();
            if (!reader.HasRows)
                return "";
            reader.Read();
            
            DateTime lastLogEntryTime = (DateTime) reader.GetValue(3);
            string lastLogEntryTimeString = lastLogEntryTime.ToString("yyyy-MM-ddTHH:mm:ss");
            myConnection.Close();
            return lastLogEntryTimeString;
        }
        public static int ProcessResponse(LogMessages logMessages, string servicesUrl, string dbuser, string dbschema, string dbpassword, string dbname, string dbserver, bool debug, string srid, string token)
        {
            if (debug) Console.WriteLine("Retrieved " + logMessages.logMessages.Count + " records");

            SqlConnection myConnection = new SqlConnection("Server=" + dbserver + "; Database=" + dbname + "; User ID=" + dbuser + "; Password=" + dbpassword);

            try
            {
                myConnection.Open();
            }
            catch (Exception e)
            {
                if (debug) Console.WriteLine(e.ToString());
                Environment.Exit(1);
            }
            int messageCount;
            Dictionary<string,int> serviceWkids = new Dictionary<string, int>();
            for(messageCount = 0; messageCount < logMessages.logMessages.Count; messageCount++)
            {
                LogMessage currentLogMessage = logMessages.logMessages[messageCount];

                string scale = "NULL";

                string size_x = "NULL";
                string size_y = "NULL";

                string minx = "NULL";
                string miny = "NULL";
                string maxx = "NULL";
                string maxy = "NULL";

                string Shape = "NULL";

                DateTime dttime = UnixTimeStampToDateTime2(currentLogMessage.time);

                if (currentLogMessage.message.Length > 4000) currentLogMessage.message = currentLogMessage.message.Substring(0, 4000);
                if (currentLogMessage.methodName.Length > 50) currentLogMessage.methodName = currentLogMessage.methodName.Substring(0, 50);

                currentLogMessage.message = currentLogMessage.message.Replace("'", "''");

                if (currentLogMessage.message.Contains("Extent:"))
                {
                    if (!serviceWkids.ContainsKey(currentLogMessage.source))
                    {
                        MapServer serviceInfo = GetServiceInfo(servicesUrl,
                                                               currentLogMessage.source, token);
                        int wkid = serviceInfo.fullExtent.spatialReference.wkid;
                        serviceWkids.Add(currentLogMessage.source, wkid);
                    }
                    string[] vals = currentLogMessage.message.Split(';');

                    string[] tmp_extent = vals[0].Split(':');
                    string[] tmp_size = vals[1].Split(':');
                    string[] tmp_scale = vals[2].Split(':');

                    string[] tmp_sizes = tmp_size[1].Split(',');
                    string[] tmp_extents = tmp_extent[1].Split(',');

                    scale = tmp_scale[1];

                    size_x = tmp_sizes[0];
                    size_y = tmp_sizes[1];

                    minx = tmp_extents[0];
                    miny = tmp_extents[1];
                    maxx = tmp_extents[2];
                    maxy = tmp_extents[3];

                    Shape = "'POLYGON((" + minx + " " + miny + "," + minx + " " + maxy + "," + maxx + " " + maxy + "," + maxx + " " + miny + "," + minx + " " + miny + "))'";

                    Shape = "geometry::STPolyFromText(" + Shape + ", " + serviceWkids[currentLogMessage.source] + ")";
                }

                string sql = "";
                sql = sql + "INSERT INTO [" + dbname + "].["+ dbschema +"].[RawLogs]"; //TODO: handle the user schema for the table
                sql = sql + "([type]";
                sql = sql + ",[message]";
                sql = sql + ",[time]";
                sql = sql + ",[source]";
                sql = sql + ",[machine]";
                sql = sql + ",[user]";
                sql = sql + ",[code]";
                sql = sql + ",[elapsed]";
                sql = sql + ",[process]";
                sql = sql + ",[thread]";
                sql = sql + ",[methodname]";
                sql = sql + ",[mapsize_x]";
                sql = sql + ",[mapsize_y]";
                sql = sql + ",[mapscale]";
                sql = sql + ",[mapextent_minx]";
                sql = sql + ",[mapextent_miny]";
                sql = sql + ",[mapextent_maxx]";
                sql = sql + ",[mapextent_maxy]";
                sql = sql + ",[Shape])";
                sql = sql + "VALUES";
                sql = sql + "(";
                sql = sql + "'" + currentLogMessage.type + "',";
                sql = sql + "'" + currentLogMessage.message + "',";
                sql = sql + "'" + dttime.ToString("yyyy-MM-ddTHH:mm:ss.fff") + "',";
                sql = sql + "'" + currentLogMessage.source + "',";
                sql = sql + "'" + currentLogMessage.machine + "',";
                sql = sql + "'" + currentLogMessage.user + "',";
                sql = sql + currentLogMessage.code + ",";
                sql = sql + "'" + currentLogMessage.elapsed + "',";
                sql = sql + "'" + currentLogMessage.process + "',";
                sql = sql + "'" + currentLogMessage.thread + "',";
                sql = sql + "'" + currentLogMessage.methodName + "',";
                sql = sql + "" + size_x + ",";
                sql = sql + "" + size_y + ",";
                sql = sql + "" + scale + ",";
                sql = sql + "" + minx + ",";
                sql = sql + "" + miny + ",";
                sql = sql + "" + maxx + ",";
                sql = sql + "" + maxy + ",";
                sql = sql + Shape;
                sql = sql + ")";

                SqlCommand myUserCommand = new SqlCommand(sql, myConnection);
                myUserCommand.ExecuteNonQuery();

                if (debug == true) Console.WriteLine("Inserted record " + messageCount.ToString());
            }

            myConnection.Close();

            return messageCount;
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        public static double DateTimeToUnixTimestamp(DateTime dateTime)
        {
            return (dateTime - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds;
        }

        static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        static readonly double MaxUnixSeconds = (DateTime.MaxValue - UnixEpoch).TotalSeconds;

        public static DateTime UnixTimeStampToDateTime2(double unixTimeStamp)
        {
            return unixTimeStamp > MaxUnixSeconds
               ? UnixEpoch.AddMilliseconds(unixTimeStamp)
               : UnixEpoch.AddSeconds(unixTimeStamp);
        }
        public static string GetToken(string tokenUrl, string ip, string username, string password)
        {
            try
            {
                using (System.Net.WebClient client = new System.Net.WebClient())
                {
                    client.Headers["Content-type"] = "application/x-www-form-urlencoded";
                    client.Encoding = System.Text.Encoding.UTF8;
                    var collection = new System.Collections.Specialized.NameValueCollection
                    {
                        {"f", "json"},
                        {"username", username},
                        {"password", password},
                        {"client", "ip"},
                        {"expiration", "60"},
                        {"ip",ip}
                    };
                    byte[] response = client.UploadValues(tokenUrl, "POST", collection);
                    MemoryStream stream = new MemoryStream(response);
                    StreamReader reader = new StreamReader(stream);
                    string aRespStr = reader.ReadToEnd();
                    JavaScriptSerializer jss = new JavaScriptSerializer();
                    Token token = jss.Deserialize<Token>(aRespStr);
                    if (token != null)
                    {
                        return token.token;
                    }
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }
        public static LogMessages GetLogMessages(string logsUrl,string startTime, string endTime, string filterType, string filter, string level, string pageSize, string token)
        {
            try
            {
                logsUrl += "/query";
                using (System.Net.WebClient client = new System.Net.WebClient())
                {
                    client.Headers["Content-type"] = "application/x-www-form-urlencoded";
                    client.Encoding = System.Text.Encoding.UTF8;
                    var collection = new System.Collections.Specialized.NameValueCollection
                    {
                        {"f", "json"},
                        {"filterType", filterType},
                        {"filter", filter},
                        {"level", level},
                        {"pagesize", pageSize},
                        {"token",token},
                        {"startTime",startTime},
                        {"endTime",endTime}
                    };
                    byte[] response = client.UploadValues(logsUrl, "POST", collection);
                    MemoryStream stream = new MemoryStream(response);
                    StreamReader reader = new StreamReader(stream);
                    string aRespStr = reader.ReadToEnd();
                    JavaScriptSerializer jss = new JavaScriptSerializer();
                    LogMessages logMessages = jss.Deserialize<LogMessages>(aRespStr);
                    if (logMessages != null && logMessages.logMessages.Count > 0)
                    {
                        return logMessages;
                    }
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }

        public static Catalog GetCatalogInfo(string catalogUrl, string token)
        {
            try
            {
                using (System.Net.WebClient client = new System.Net.WebClient())
                {
                    client.Headers["Content-type"] = "application/x-www-form-urlencoded";
                    client.Encoding = System.Text.Encoding.UTF8;
                    var collection = new System.Collections.Specialized.NameValueCollection
                    {
                        {"f", "json"},
                        {"token",token}
                    };
                    byte[] response = client.UploadValues(catalogUrl, "POST", collection);
                    MemoryStream stream = new MemoryStream(response);
                    StreamReader reader = new StreamReader(stream);
                    string aRespStr = reader.ReadToEnd();
                    JavaScriptSerializer jss = new JavaScriptSerializer();
                    Catalog catalog = jss.Deserialize<Catalog>(aRespStr);
                    if (catalog != null)
                    {
                        return catalog;
                    }
                    return null;
                }
            }
            catch
            {
                return null;
            }
        
        }

        public static MapServer GetServiceInfo(string servicesUrl, string source, string token)
        {
            try
            {
                source = source.Replace(".", "/");
                servicesUrl += "/" + source;
                
                using (System.Net.WebClient client = new System.Net.WebClient())
                {
                    client.Headers["Content-type"] = "application/x-www-form-urlencoded";
                    client.Encoding = System.Text.Encoding.UTF8;
                    var collection = new System.Collections.Specialized.NameValueCollection
                    {
                        {"f", "json"},
                        {"token",token}
                    };
                    byte[] response = client.UploadValues(servicesUrl, "POST", collection);
                    MemoryStream stream = new MemoryStream(response);
                    StreamReader reader = new StreamReader(stream);
                    string aRespStr = reader.ReadToEnd();
                    JavaScriptSerializer jss = new JavaScriptSerializer();
                    MapServer mapServer = jss.Deserialize<MapServer>(aRespStr);
                    if (mapServer != null)
                    {
                        return mapServer;
                    }
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }
        public class Token
        {
            public string token { get; set; }
            public long expires { get; set; }
        }
        public class LogMessage
        {
            public string type { get; set; }
            public string message { get; set; }
            public double time { get; set; }
            public string source { get; set; }
            public string machine { get; set; }
            public string user { get; set; }
            public int code { get; set; }
            public string elapsed { get; set; }
            public string process { get; set; }
            public string thread { get; set; }
            public string methodName { get; set; }
        }
        public class LogMessages
        {
            public bool hasMore { get; set; }
            public long startTime { get; set; }
            public long endTime { get; set; }
            public List<LogMessage> logMessages { get; set; }
        }
    }
}