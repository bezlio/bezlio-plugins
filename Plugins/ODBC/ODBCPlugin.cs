using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Reflection;
using System.IO;

namespace bezlio.rdb.plugins
{
    public class ODBCDataModel
    {
        public string Context { get; set; }
        public string DSN { get; set; }
        public string QueryName { get; set; }
        public List<KeyValuePair<string, string>> Parameters { get; set; }

        public ODBCDataModel()
        {
            Parameters = new List<KeyValuePair<string, string>>();
        }
    }

    public class ODBC
    {
        public static object GetArgs()
        {
            ODBCDataModel model = new ODBCDataModel();

            model.Context = "Location name where ODBC query files are stored.";
            model.DSN = "The name of the ODBC connection.";
            model.QueryName = "The ODBC query filename to execute.";
            model.Parameters = new List<KeyValuePair<string, string>>();
            model.Parameters.Add(new KeyValuePair<string, string>("CustomerId", "102"));

            return model;
        }

        public static async Task<RemoteDataBrokerResponse> ExecuteQuery(RemoteDataBrokerRequest rdbRequest)
        {
            //WriteDebugLog("Running ExecuteQuery");

            // Cast the body to the strong type (note: I would prefer to just make the argument of 
            // the type RemoteDataBrokerRequest right off the bat but more work needs to be done to
            // cast the input as that reflected type before we can do that.
            // TODO: Switch argument to plugin methods to RemoteDataBrokerRequest
            //RemoteDataBrokerRequest request = JsonConvert.DeserializeObject<RemoteDataBrokerRequest>(requestJson);
            ODBCDataModel request = JsonConvert.DeserializeObject<ODBCDataModel>(rdbRequest.Data);

            // Declare the response object
            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.Compress = rdbRequest.Compress;
            response.RequestId = rdbRequest.RequestId;
            response.DataType = "applicationJSON";

            try
            {
                // Settings do not seem to reflect in cleanly, we will read the settings directly
                string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string cfgPath = asmPath + @"\" + "ODBC.dll.config";
                string strLocations = "";
                string strConnections = "";

                if (File.Exists(cfgPath))
                {
                    // Load in the cfg file
                    XDocument xConfig = XDocument.Load(cfgPath);

                    // Get the setting for the debug log destination
                    XElement xLocations = xConfig.Descendants("bezlio.plugins.Properties.Settings").Descendants("setting").Where(a => (string)a.Attribute("name") == "sqlFileLocations").FirstOrDefault();
                    if (xLocations != null)
                    {
                        strLocations = xLocations.Value;
                    }

                    // Get the settings for the error log destination
                    XElement xConnections = xConfig.Descendants("bezlio.plugins.Properties.Settings").Descendants("setting").Where(a => (string)a.Attribute("name") == "connections").FirstOrDefault();
                    if (xConnections != null)
                    {
                        strConnections = xConnections.Value;
                    }
                }

                // Deserialize the values from Settings
                List<ODBCFileLocation> locations = JsonConvert.DeserializeObject<List<ODBCFileLocation>>(strLocations);
                List<ODBCConnectionInfo> connections = JsonConvert.DeserializeObject<List<ODBCConnectionInfo>>(strConnections);

                // Now pick the location path by the name specified
                if (locations.Where((l) => l.LocationName.Equals(request.Context)).Count() == 0)
                {
                    response.Error = true;
                    response.ErrorText = "Could not locate a location in the plugin config file with the name " + request.Context;
                    return response;
                }
                string locationPath = locations.Where((l) => l.LocationName.Equals(request.Context)).FirstOrDefault().LocationPath;

                string sql = ReadAllText(locationPath + request.QueryName + ".sql");

                // Locate the connection entry specified
                ODBCConnectionInfo connection = connections.Where((c) => c.DSN.Equals(request.DSN)).FirstOrDefault();

                //WriteDebugLog("Connection Created");

                // Perform any replacements on passed variables
                if (request.Parameters != null && request.Parameters.Count > 0) {
                    FillQueryParameters(ref sql, request.Parameters);
                }

                // Now obtain a connection
                object odbcConn = getConnection("ODBC"
                            , connection.DSN);

                //WriteDebugLog("Connection Received");

                DataTable dtResponse = await executeQuery(odbcConn, sql);

                //WriteDebugLog("Query Executed");

                // Return the data table
                response.Data = JsonConvert.SerializeObject(dtResponse);

                //WriteDebugLog("Response created");

                odbcConn = null;
            }
            catch (Exception ex)
            {
                response.Error = true;
                response.ErrorText = Environment.MachineName + ": " + ex.Message;
            }

            // Return our response
            return response;
        }

        public static async Task<RemoteDataBrokerResponse> ExecuteNonQuery(RemoteDataBrokerRequest rdbRequest)
        {
            //WriteDebugLog("Running ExecuteQuery");

            // Cast the body to the strong type (note: I would prefer to just make the argument of 
            // the type RemoteDataBrokerRequest right off the bat but more work needs to be done to
            // cast the input as that reflected type before we can do that.
            // TODO: Switch argument to plugin methods to RemoteDataBrokerRequest
            //RemoteDataBrokerRequest request = JsonConvert.DeserializeObject<RemoteDataBrokerRequest>(requestJson);
            ODBCDataModel request = JsonConvert.DeserializeObject<ODBCDataModel>(rdbRequest.Data);

            // Declare the response object
            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.Compress = rdbRequest.Compress;
            response.RequestId = rdbRequest.RequestId;
            response.DataType = "applicationJSON";

            try
            {
                // Settings do not seem to reflect in cleanly, we will read the settings directly
                string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string cfgPath = asmPath + @"\" + "ODBC.dll.config";
                string strLocations = "";
                string strConnections = "";

                if (File.Exists(cfgPath))
                {
                    // Load in the cfg file
                    XDocument xConfig = XDocument.Load(cfgPath);

                    // Get the setting for the debug log destination
                    XElement xLocations = xConfig.Descendants("bezlio.plugins.Properties.Settings").Descendants("setting").Where(a => (string)a.Attribute("name") == "sqlFileLocations").FirstOrDefault();
                    if (xLocations != null)
                    {
                        strLocations = xLocations.Value;
                    }

                    // Get the settings for the error log destination
                    XElement xConnections = xConfig.Descendants("bezlio.plugins.Properties.Settings").Descendants("setting").Where(a => (string)a.Attribute("name") == "connections").FirstOrDefault();
                    if (xConnections != null)
                    {
                        strConnections = xConnections.Value;
                    }
                }

                // Deserialize the values from Settings
                List<ODBCFileLocation> locations = JsonConvert.DeserializeObject<List<ODBCFileLocation>>(strLocations);
                List<ODBCConnectionInfo> connections = JsonConvert.DeserializeObject<List<ODBCConnectionInfo>>(strConnections);

                //WriteDebugLog("Checking Location Name: " + request.Context);

                // Now load the requested .SQL file from the specified location
                string locationPath = locations.Where((l) => l.LocationName.Equals(request.Context)).FirstOrDefault().LocationPath;
                //WriteDebugLog("locationPath: " + locationPath);

                string sql = ReadAllText(locationPath + request.QueryName + ".sql");
                //WriteDebugLog("sql: " + sql);

                //WriteDebugLog("Creating Connection");

                // Locate the connection entry specified
                ODBCConnectionInfo connection = connections.Where((c) => c.DSN.Equals(request.DSN)).FirstOrDefault();

                //WriteDebugLog("Connection Created");

                // Perform any replacements on passed variables
                if (request.Parameters != null && request.Parameters.Count > 0)
                {
                    FillQueryParameters(ref sql, request.Parameters);
                }

                // Now obtain an ODBC connection
                object odbcConn = getConnection("ODBC"
                            , connection.DSN);

                //WriteDebugLog("Connection Received");

                int rowsAffected = await executeNonQuery(odbcConn, sql);

                //WriteDebugLog("Query Executed");

                // Return the data table
                response.Data = JsonConvert.SerializeObject(rowsAffected);

                //WriteDebugLog("Response created");

                odbcConn = null;
            }
            catch (Exception ex)
            {
                response.Error = true;
                response.ErrorText = Environment.MachineName + ": " + ex.Message;
            }

            // Return our response
            return response;
        }

        private static object getConnection(string _databaseType,
                                    string _dsnName)
        {
            object oConnection = new object();

            switch (_databaseType)
            {
                case "ODBC":
                    oConnection = new OdbcConnection("DSN=" + _dsnName);
                    break;
            }


            return oConnection;
        }

        private static object getCommand(string _databaseType, object _connection)
        {
            object oCommand = new object();

            switch (_databaseType)
            {
                case "ODBC":
                    oCommand = ((OdbcConnection)_connection).CreateCommand();
                    break;
            }

            return oCommand;
        }

        private static object getAdapter(string _databaseType,
                                    object _command)
        {
            object oAdapter = new object();

            switch (_databaseType)
            {
                case "ODBC":
                    oAdapter = new OdbcDataAdapter((OdbcCommand)_command);
                    break;
            }

            return oAdapter;
        }

#pragma warning disable 1998
        private async static Task<DataTable> executeQuery(object _connection,
                                    string _sql)
        {
            // TODO: Make this actually async
            object oCommand;
            object oAdapter;
            DataTable dt = new DataTable("TableName");

            switch (_connection.GetType().Name.ToString())
            {
                case "OdbcConnection":
                    oCommand = getCommand("ODBC", _connection);
                    using (((OdbcCommand)oCommand).Connection = (OdbcConnection)_connection)
                    {
                        ((OdbcCommand)oCommand).CommandText = _sql;
                        oAdapter = getAdapter("ODBC", oCommand);
                        ((OdbcDataAdapter)oAdapter).Fill(dt);
                        //((SqlCommand)oCommand).Connection.Close();
                    }
                    //((SqlCommand)oCommand).Connection = (SqlConnection)_connection;

                    break;
            }

            oCommand = null;
            oAdapter = null;
            return dt;
        }

        private async static Task<int> executeNonQuery(object _connection,
                           string _sql)
        {
            // TODO: Make this actually async
            object oCommand;
            int intRowsAffected = 0;

            switch (_connection.GetType().Name.ToString())
            {
                case "OdbcConnection":
                    oCommand = getCommand("ODBC", _connection);
                    using (((OdbcCommand)oCommand).Connection = (OdbcConnection)_connection)
                    {
                        ((OdbcCommand)oCommand).CommandText = _sql;
                        (((OdbcCommand)oCommand).Connection).Open();
                        intRowsAffected = ((OdbcCommand)oCommand).ExecuteNonQuery();
                    }
                    break;
            }

            oCommand = null;
            return intRowsAffected;
        }
        private static void FillQueryParameters(ref string query, List<KeyValuePair<string, string>> parameters)
        {
            foreach (var parameter in parameters)
            {
                query = query.Replace('{' + parameter.Key + '}', parameter.Value.ToString().Replace("'", "''"));
            }
        }
#pragma warning restore 1998

        public static string ReadAllText(string path)
        {
            string result = "";

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 0x1000, FileOptions.SequentialScan))
            using (StreamReader sr = new StreamReader(fs))
            {
                result = sr.ReadToEnd();
            }

            return result;
        }
    }
}
