using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Reflection;
using System.IO;
using System.Data.SQLite;

namespace bezlio.rdb.plugins
{
    public class SQLiteDataModel
    {
        public string Context { get; set; }
        public string Connection { get; set; }
        public string QueryName { get; set; }
        public List<KeyValuePair<string, string>> Parameters { get; set; }

        public SQLiteDataModel()
        {
            Parameters = new List<KeyValuePair<string, string>>();
        }
    }

    public class SQLite
    {
        public static object GetArgs()
        {

            SQLiteDataModel model = new SQLiteDataModel();
            List<SQLiteFileLocations> contextLocations = GetLocations();

            model.Context = GetFolderNames(contextLocations);
            model.Connection = GetConnectionNames();
            model.QueryName = GetQueriesCascadeDefinition(contextLocations, nameof(model.Context));
            model.Parameters = new List<KeyValuePair<string, string>>();
            model.Parameters.Add(new KeyValuePair<string, string>("CustomerId", "102"));

            return model;
        }

        public static List<SQLiteFileLocations> GetLocations()
        {
            string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string cfgPath = asmPath + @"\" + "SQLite.dll.config";
            string strConnections = "";
            if (File.Exists(cfgPath))
            {
                // Load in the cfg file
                XDocument xConfig = XDocument.Load(cfgPath);

                // Get the setting for the debug log destination
                XElement xConnections = xConfig.Descendants("bezlio.plugins.Properties.Settings").Descendants("setting").Where(a => (string)a.Attribute("name") == "SQLiteFileLocations").FirstOrDefault();
                if (xConnections != null)
                {
                    strConnections = xConnections.Value;
                }
            }

            var contextLocations = JsonConvert.DeserializeObject<List<SQLiteFileLocations>>(strConnections);
            if (contextLocations != null) {
                foreach (var context in contextLocations) {
                    if (Directory.Exists(context.LocationPath)) {
                        var ext = new List<string> { ".sql" };
                        var contentFiles = Directory.GetFiles(context.LocationPath, "*.*", SearchOption.AllDirectories).Where(s => ext.Contains(Path.GetExtension(s)));
                        context.ContentFileNames = new List<string>();

                        foreach (string fileName in contentFiles) {
                            context.ContentFileNames.Add(Path.GetFileNameWithoutExtension(fileName));
                        }
                    }
                }
            }

            return contextLocations;
        }

        public static List<SQLiteConnectionInfo> GetConnections()
        {
            string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string cfgPath = asmPath + @"\" + "SQLite.dll.config";
            string strConnections = "";
            if (File.Exists(cfgPath))
            {
                // Load in the cfg file
                XDocument xConfig = XDocument.Load(cfgPath);

                // Get the setting for the debug log destination
                XElement xConnections = xConfig.Descendants("bezlio.plugins.Properties.Settings").Descendants("setting").Where(a => (string)a.Attribute("name") == "connections").FirstOrDefault();
                if (xConnections != null)
                {
                    strConnections = xConnections.Value;
                }
            }
            return JsonConvert.DeserializeObject<List<SQLiteConnectionInfo>>(strConnections);
        }

        public static string GetFolderNames(List<SQLiteFileLocations> contextLocations)
        {
            string result = "";
            if (contextLocations != null) {
                if (contextLocations != null) {
                    result = "[";
                    foreach (var location in contextLocations) {
                        result += location.LocationName + ",";
                    }
                    result.TrimEnd(',');
                    result += "]";
                }
            }

            return result;
        }

        public static string GetQueriesCascadeDefinition(List<SQLiteFileLocations> contextLocations, string contextPropertyName)
        {
            string result = "[";
            if (contextLocations != null) {
                foreach (var context in contextLocations) {
                    result += contextPropertyName + ":" + context.LocationName + "[";
                    foreach (var fileName in context.ContentFileNames) {
                        result += fileName + ",";
                    }
                    result.TrimEnd(new char[] { ',' });
                    result += "],";
                }
                result.TrimEnd(new char[] { ',' });
                result += "]";
            }
            return result;
        }

        public static string GetConnectionNames()
        {
            string result = "";
            var con = GetConnections();
            if (con != null) {
                result = "[";
                foreach (var connection in GetConnections()) {
                    result += connection.ConnectionName + ",";
                }
                result.TrimEnd(',');
                result += "]";
            }
            return result;
        }

        public static async Task<RemoteDataBrokerResponse> ExecuteQuery(RemoteDataBrokerRequest rdbRequest)
        {
            //WriteDebugLog("Running ExecuteQuery");

            // Cast the body to the strong type (note: I would prefer to just make the argument of 
            // the type RemoteDataBrokerRequest right off the bat but more work needs to be done to
            // cast the input as that reflected type before we can do that.
            // TODO: Switch argument to plugin methods to RemoteDataBrokerRequest
            //RemoteDataBrokerRequest request = JsonConvert.DeserializeObject<RemoteDataBrokerRequest>(requestJson);
            SQLiteDataModel request = JsonConvert.DeserializeObject<SQLiteDataModel>(rdbRequest.Data);

            // Declare the response object
            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.Compress = rdbRequest.Compress;
            response.RequestId = rdbRequest.RequestId;
            response.DataType = "applicationJSON";

            try
            {
                List<SQLiteFileLocations> locations = getFileLocations();
                List<SQLiteConnectionInfo> connections = getSqlConnections();

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
                SQLiteConnectionInfo connection = connections.Where((c) => c.ConnectionName.Equals(request.Connection)).FirstOrDefault();

                // Perform any replacements on passed variables
                if (request.Parameters != null && request.Parameters.Count > 0)
                {
                    FillQueryParameters(ref sql, request.Parameters);
                }

                // Now obtain a SQL connection
                object sqlConn = getConnection("SQLite"
                            , connection.DatabaseLocation
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
            SQLiteDataModel request = JsonConvert.DeserializeObject<SQLiteDataModel>(rdbRequest.Data);

            // Declare the response object
            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.Compress = rdbRequest.Compress;
            response.RequestId = rdbRequest.RequestId;
            response.DataType = "applicationJSON";

            try
            {
                // Deserialize the values from Settings
                List<SQLiteFileLocations> locations = getFileLocations();
                List<SQLiteConnectionInfo> connections = getSqlConnections();

                //WriteDebugLog("Checking Location Name: " + request.Context);

                // Now load the requested .SQL file from the specified location
                string locationPath = locations.Where((l) => l.LocationName.Equals(request.Context)).FirstOrDefault().LocationPath;
                //WriteDebugLog("locationPath: " + locationPath);

                string sql = ReadAllText(locationPath + request.QueryName + ".sql");
                //WriteDebugLog("sql: " + sql);

                //WriteDebugLog("Creating Connection");

                // Locate the connection entry specified
                SQLiteConnectionInfo connection = connections.Where((c) => c.ConnectionName.Equals(request.Connection)).FirstOrDefault();

                //WriteDebugLog("Connection Created");

                // Perform any replacements on passed variables
                if (request.Parameters != null && request.Parameters.Count > 0)
                {
                    FillQueryParameters(ref sql, request.Parameters);
                }

                // Now obtain a SQL connection
                object sqlConn = getConnection("SQLite"
                            , connection.DatabaseLocation
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
            SQLiteDataModel request = JsonConvert.DeserializeObject<SQLiteDataModel>(rdbRequest.Data);

            // Declare the response object
            RemoteDataBrokerResponse response = getResponseForRequest(rdbRequest);

            try
            {
                // Deserialize the values from Settings
                List<SQLiteConnectionInfo> connections = getSqlConnections();
                string sql = request.QueryName;

                // Locate the connection entry specified
                SQLiteConnectionInfo connection = connections.Where((c) => c.ConnectionName.Equals(request.Connection)).FirstOrDefault();

                // Now obtain a SQL connection
                object sqlConn = getConnection("SQLite"
                            , connection.DatabaseLocation
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
            SQLiteDataModel request = JsonConvert.DeserializeObject<SQLiteDataModel>(rdbRequest.Data);

            // Declare the response object
            RemoteDataBrokerResponse response = getResponseForRequest(rdbRequest);

            try
            {
                // Deserialize the values from Settings
                List<SQLiteConnectionInfo> connections = getSqlConnections();
                string sql = request.QueryName;

                // Locate the connection entry specified
                SQLiteConnectionInfo connection = connections.Where((c) => c.ConnectionName.Equals(request.Connection)).FirstOrDefault();

                // Now obtain a SQL connection
                object sqlConn = getConnection("SQLite"
                            , connection.DatabaseLocation
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

        private static List<SQLiteFileLocations> getFileLocations()
        {
            string strLocations = "";
            string cfgPath = getCfgPath();

            if (File.Exists(cfgPath))
            {
                // Load in the cfg file
                XDocument xConfig = XDocument.Load(cfgPath);

                // Get the setting for the debug log destination
                XElement xLocations = xConfig.Descendants("bezlio.plugins.Properties.Settings").Descendants("setting").Where(a => (string)a.Attribute("name") == "SQLiteFileLocationss").FirstOrDefault();
                if (xLocations != null)
                {
                    strLocations = xLocations.Value;
                }
            }

            // Deserialize the values from Settings
            return JsonConvert.DeserializeObject<List<SQLiteFileLocations>>(strLocations);
        }

        private static List<SQLiteConnectionInfo> getSqlConnections()
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
            return JsonConvert.DeserializeObject<List<SQLiteConnectionInfo>>(strConnections);
        }

        private static string getCfgPath()
        {
            string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string cfgPath = asmPath + @"\" + "SQLite.dll.config";

            return cfgPath;
        }

        private static object getConnection(string _databaseType,
                                    string _DatabaseLocation,
                                    string _password)
        {
            object oConnection = new object();

            switch (_databaseType)
            {
                case "SQLite":
                    oConnection = new SQLiteConnection("Data Source=" + _DatabaseLocation + "Version=3;Password=" + _password + ";");
                    break;
            }


            return oConnection;
        }

        private static object getCommand(string _databaseType)
        {
            object oCommand = new object();

            switch (_databaseType)
            {
                case "SQLite":
                    oCommand = new SQLiteCommand();
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
                case "SQLite":
                    oAdapter = new SQLiteDataAdapter((SQLiteCommand)_command);
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
                case "SQLiteConnection":
                    oCommand = getCommand("SQLite");
                    SQLiteCommand command = (SQLiteCommand)oCommand;

                    using (command.Connection = (SQLiteConnection)_connection)
                    {
                        if (spParams != null && spParams.Count > 0)
                        {
                            command.CommandType = CommandType.StoredProcedure;
                            FillSPParameters(ref command, spParams);
                        }

                        command.CommandText = _sql;
                        oAdapter = getAdapter("SQLite", oCommand);
                        ((SQLiteDataAdapter)oAdapter).Fill(dt);
                        //((SQLiteCommand)oCommand).Connection.Close();
                    }
                    //((SQLiteCommand)oCommand).Connection = (SqlConnection)_connection;

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
                case "SQLiteConnection":
                    oCommand = getCommand("SQLite");
                    SQLiteCommand command = (SQLiteCommand)oCommand;

                    using (command.Connection = (SQLiteConnection)_connection)
                    {
                        if (spParams != null && spParams.Count > 0)
                        {
                            command.CommandType = CommandType.StoredProcedure;
                            FillSPParameters(ref command, spParams);
                        }

                        command.CommandText = _sql;
                        command.Connection.Open();
                        intRowsAffected = ((SQLiteCommand)oCommand).ExecuteNonQuery();
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

        private static void FillSPParameters(ref SQLiteCommand command, List<KeyValuePair<string, string>> parameters)
        {
            foreach (var parameter in parameters)
            {
                string value = parameter.Value.ToString().Replace("'", "''");
                double number;
                SQLiteParameter param;

                if (!(value.StartsWith("\"") && value.EndsWith("\"")) && double.TryParse(value, out number))
                {
                    param = new SQLiteParameter(parameter.Key, number);
                    param.DbType = DbType.Double;
                }
                else
                {
                    param = new SQLiteParameter(parameter.Key, value);
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
