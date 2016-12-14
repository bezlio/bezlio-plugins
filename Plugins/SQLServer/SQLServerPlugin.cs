using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Reflection;
using System.IO;

namespace bezlio.rdb.plugins
{
    public class SQLServerDataModel
    {
        public string Context { get; set; }
        public string Connection { get; set; }
        public string QueryName { get; set; }
        public List<KeyValuePair<string, string>> Parameters { get; set; }

        public SQLServerDataModel()
        {
            Parameters = new List<KeyValuePair<string, string>>();
        }
    }

    public class SQLServer
    {
        public static object GetArgs()
        {

            SQLServerDataModel model = new SQLServerDataModel();

            model.Context = "Location name where .SQL files are stored.";
            model.Connection = "The name of the SQL Server connection.";
            model.QueryName = "The SQL query filename or Stored Procedure name to execute.";
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
            SQLServerDataModel request = JsonConvert.DeserializeObject<SQLServerDataModel>(rdbRequest.Data);

            // Declare the response object
            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.Compress = rdbRequest.Compress;
            response.RequestId = rdbRequest.RequestId;
            response.DataType = "applicationJSON";

            try
            {
                List<SqlFileLocation> locations = getFileLocations();
                List<SqlConnectionInfo> connections = getSqlConnections();

                // Now load the requested .SQL file from the specified location
                if (locations.Where((l) => l.LocationName.Equals(request.Context)).Count() == 0)
                {
                    response.Error = true;
                    response.ErrorText = "Could not locate a location in the plugin config file with the name " + request.Context;
                    return response;
                }
                string locationPath = locations.Where((l) => l.LocationName.Equals(request.Context)).FirstOrDefault().LocationPath;
                //WriteDebugLog("locationPath: " + locationPath);

                string sql = ReadAllText(locationPath + request.QueryName + ".sql");

                // Locate the connection entry specified
                if (connections.Where((c) => c.ConnectionName.Equals(request.Connection)).Count() == 0)
                {
                    response.Error = true;
                    response.ErrorText = "Could not locate a connection in the plugin config file with the name " + request.Connection;
                    return response;
                }
                SqlConnectionInfo connection = connections.Where((c) => c.ConnectionName.Equals(request.Connection)).FirstOrDefault();

                // Perform any replacements on passed variables
                if (request.Parameters != null && request.Parameters.Count > 0) {
                    FillQueryParameters(ref sql, request.Parameters);
                }

                // Now obtain a SQL connection
                object sqlConn = getConnection("SQL Server"
                            , connection.ServerAddress
                            , connection.DatabaseName
                            , connection.UserName
                            , connection.Password);

                //WriteDebugLog("Connection Received");

                DataTable dtResponse = await executeQuery(sqlConn, sql, null);

                //WriteDebugLog("Query Executed");

                // Return the data table
                response.Data = JsonConvert.SerializeObject(dtResponse);

                //WriteDebugLog("Response created");

                sqlConn = null;
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
            SQLServerDataModel request = JsonConvert.DeserializeObject<SQLServerDataModel>(rdbRequest.Data);

            // Declare the response object
            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.Compress = rdbRequest.Compress;
            response.RequestId = rdbRequest.RequestId;
            response.DataType = "applicationJSON";

            try
            {
                // Deserialize the values from Settings
                List<SqlFileLocation> locations = getFileLocations();
                List<SqlConnectionInfo> connections = getSqlConnections();

                //WriteDebugLog("Checking Location Name: " + request.Context);

                // Now load the requested .SQL file from the specified location
                string locationPath = locations.Where((l) => l.LocationName.Equals(request.Context)).FirstOrDefault().LocationPath;
                //WriteDebugLog("locationPath: " + locationPath);

                string sql = ReadAllText(locationPath + request.QueryName + ".sql");
                //WriteDebugLog("sql: " + sql);

                //WriteDebugLog("Creating Connection");

                // Locate the connection entry specified
                SqlConnectionInfo connection = connections.Where((c) => c.ConnectionName.Equals(request.Connection)).FirstOrDefault();

                //WriteDebugLog("Connection Created");

                // Perform any replacements on passed variables
                if (request.Parameters != null && request.Parameters.Count > 0) {
                    FillQueryParameters(ref sql, request.Parameters);
                }
                
                // Now obtain a SQL connection
                object sqlConn = getConnection("SQL Server"
                            , connection.ServerAddress
                            , connection.DatabaseName
                            , connection.UserName
                            , connection.Password);

                //WriteDebugLog("Connection Received");

                int rowsAffected = await executeNonQuery(sqlConn, sql, null);

                //WriteDebugLog("Query Executed");

                // Return the data table
                response.Data = JsonConvert.SerializeObject(rowsAffected);

                //WriteDebugLog("Response created");

                sqlConn = null;
            }
            catch (Exception ex)
            {
                response.Error = true;
                response.ErrorText = Environment.MachineName + ": " + ex.Message;
            }

            // Return our response
            return response;
        }

        public static async Task<RemoteDataBrokerResponse> ExecuteStoredProcedure(RemoteDataBrokerRequest rdbRequest)
        {
            SQLServerDataModel request = JsonConvert.DeserializeObject<SQLServerDataModel>(rdbRequest.Data);

            // Declare the response object
            RemoteDataBrokerResponse response = getResponseForRequest(rdbRequest);

            try
            {
                // Deserialize the values from Settings
                List<SqlConnectionInfo> connections = getSqlConnections();
                string sql = request.QueryName;

                // Locate the connection entry specified
                SqlConnectionInfo connection = connections.Where((c) => c.ConnectionName.Equals(request.Connection)).FirstOrDefault();

                // Now obtain a SQL connection
                object sqlConn = getConnection("SQL Server"
                            , connection.ServerAddress
                            , connection.DatabaseName
                            , connection.UserName
                            , connection.Password);

                DataTable dtResponse = await executeQuery(sqlConn, sql, request.Parameters);
                response.Data = JsonConvert.SerializeObject(dtResponse);

                sqlConn = null;
            }
            catch (Exception ex)
            {
                response.Error = true;
                response.ErrorText = Environment.MachineName + ": " + ex.Message;
            }

            // Return our response
            return response;
        }

        public static async Task<RemoteDataBrokerResponse> ExecuteStoredProcedureNonQuery(RemoteDataBrokerRequest rdbRequest)
        {
            SQLServerDataModel request = JsonConvert.DeserializeObject<SQLServerDataModel>(rdbRequest.Data);

            // Declare the response object
            RemoteDataBrokerResponse response = getResponseForRequest(rdbRequest);

            try
            {
                // Deserialize the values from Settings
                List<SqlConnectionInfo> connections = getSqlConnections();
                string sql = request.QueryName;

                // Locate the connection entry specified
                SqlConnectionInfo connection = connections.Where((c) => c.ConnectionName.Equals(request.Connection)).FirstOrDefault();

                // Now obtain a SQL connection
                object sqlConn = getConnection("SQL Server"
                            , connection.ServerAddress
                            , connection.DatabaseName
                            , connection.UserName
                            , connection.Password);

                int rowsAffected = await executeNonQuery(sqlConn, sql, request.Parameters);
                response.Data = JsonConvert.SerializeObject(rowsAffected);

                sqlConn = null;
            }
            catch (Exception ex)
            {
                response.Error = true;
                response.ErrorText = Environment.MachineName + ": " + ex.Message;
            }

            // Return our response
            return response;
        }

        private static RemoteDataBrokerResponse getResponseForRequest(RemoteDataBrokerRequest rdbRequest)
        {
            // Declare the response object
            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.Compress = rdbRequest.Compress;
            response.RequestId = rdbRequest.RequestId;
            response.DataType = "applicationJSON";

            return response;
        }

        private static List<SqlFileLocation> getFileLocations()
        {
            string strLocations = "";
            string cfgPath = getCfgPath();

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
            }

            // Deserialize the values from Settings
            return JsonConvert.DeserializeObject<List<SqlFileLocation>>(strLocations);
        }

        private static List<SqlConnectionInfo> getSqlConnections()
        {
            string strConnections = "";
            string cfgPath = getCfgPath();

            if (File.Exists(cfgPath))
            {
                // Load in the cfg file
                XDocument xConfig = XDocument.Load(cfgPath);

                // Get the settings for the error log destination
                XElement xConnections = xConfig.Descendants("bezlio.plugins.Properties.Settings").Descendants("setting").Where(a => (string)a.Attribute("name") == "connections").FirstOrDefault();
                if (xConnections != null)
                {
                    strConnections = xConnections.Value;
                }
            }

            // Deserialize the values from Settings
            return JsonConvert.DeserializeObject<List<SqlConnectionInfo>>(strConnections);
        }

        private static string getCfgPath()
        {
            string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string cfgPath = asmPath + @"\" + "SQLServer.dll.config";

            return cfgPath;
        }

        private static object getConnection(string _databaseType,
                                    string _serverAddress,
                                    string _databaseName,
                                    string _userName,
                                    string _password)
        {
            object oConnection = new object();

            switch (_databaseType)
            {
                case "SQL Server":
                    oConnection = new SqlConnection("server=" + _serverAddress + ";uid=" + _userName + ";pwd=" + _password + ";database=" + _databaseName + ";Connection Timeout=150");
                    break;
            }


            return oConnection;
        }

        private static object getCommand(string _databaseType)
        {
            object oCommand = new object();

            switch (_databaseType)
            {
                case "SQL Server":
                    oCommand = new SqlCommand();
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
                case "SQL Server":
                    oAdapter = new SqlDataAdapter((SqlCommand)_command);
                    break;
            }

            return oAdapter;
        }

        #pragma warning disable 1998
        private async static Task<DataTable> executeQuery(object _connection,
                                    string _sql, List<KeyValuePair<string, string>> spParams)
        {
            // TODO: Make this actually async
            object oCommand;
            object oAdapter;
            DataTable dt = new DataTable("TableName");

            switch (_connection.GetType().Name.ToString())
            {
                case "SqlConnection":
                    oCommand = getCommand("SQL Server");
                    SqlCommand command = (SqlCommand)oCommand;

                    using (command.Connection = (SqlConnection)_connection) {
                        if (spParams != null && spParams.Count > 0)
                        {
                            command.CommandType = CommandType.StoredProcedure;
                            FillSPParameters(ref command, spParams);
                        }

                        command.CommandText = _sql;
                        oAdapter = getAdapter("SQL Server", oCommand);                  
                        ((SqlDataAdapter)oAdapter).Fill(dt);
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
                                   string _sql, List<KeyValuePair<string, string>> spParams)
        {
            // TODO: Make this actually async
            object oCommand;
            int intRowsAffected = 0;

            switch (_connection.GetType().Name.ToString())
            {
                case "SqlConnection":
                    oCommand = getCommand("SQL Server");
                    SqlCommand command = (SqlCommand)oCommand;

                    using (command.Connection = (SqlConnection)_connection)
                    {
                        if (spParams != null && spParams.Count > 0)
                        {
                            command.CommandType = CommandType.StoredProcedure;
                            FillSPParameters(ref command, spParams);
                        }

                        command.CommandText = _sql;
                        command.Connection.Open();
                        intRowsAffected = ((SqlCommand)oCommand).ExecuteNonQuery();
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

        private static void FillSPParameters(ref SqlCommand command, List<KeyValuePair<string, string>> parameters)
        {
            foreach (var parameter in parameters)
            {
                string value = parameter.Value.ToString().Replace("'", "''");
                double number;
                SqlParameter param;

                if (!(value.StartsWith("\"") && value.EndsWith("\"")) && double.TryParse(value, out number))
                {
                    param = new SqlParameter(parameter.Key, number);
                    param.DbType = DbType.Double;
                } else
                {
                    param = new SqlParameter(parameter.Key, value);
                    param.DbType = DbType.String;
                }            

                param.Direction = ParameterDirection.Input;
                command.Parameters.Add(param);
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
