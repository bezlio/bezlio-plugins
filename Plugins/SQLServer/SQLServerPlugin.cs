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
using System.Text.RegularExpressions;

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

    public class SQLServerDynamicQueryDataModel
    {
        public string Connection { get; set; }
        public List<KeyValuePair<string, string>> Parameters { get; set; }
        public List<KeyValuePair<string, string>> Queries { get; set; }

        public SQLServerDynamicQueryDataModel()
        {
            this.Parameters = new List<KeyValuePair<string, string>>();
            this.Queries = new List<KeyValuePair<string, string>>();
        }
    }

    public class SQLServer
    {
        public static object GetArgs()
        {

            SQLServerDataModel model = new SQLServerDataModel();
            List<SqlFileLocation> contextLocations = GetLocations();

            model.Context = GetFolderNames(contextLocations);
            model.Connection = GetConnectionNames();
            model.QueryName = GetQueriesCascadeDefinition(contextLocations, nameof(model.Context));
            model.Parameters = new List<KeyValuePair<string, string>>();
            model.Parameters.Add(new KeyValuePair<string, string>("CustomerId", "102"));         

            return model;
        }

        public static List<SqlFileLocation> GetLocations()
        {
            string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string cfgPath = asmPath + @"\" + "SQLServer.dll.config";
            string strConnections = "";
            if (File.Exists(cfgPath))
            {
                // Load in the cfg file
                XDocument xConfig = XDocument.Load(cfgPath);

                // Get the setting for the debug log destination
                XElement xConnections = xConfig.Descendants("bezlio.plugins.Properties.Settings").Descendants("setting").Where(a => (string)a.Attribute("name") == "sqlFileLocations").FirstOrDefault();
                if (xConnections != null)
                {
                    strConnections = xConnections.Value;
                }
            }

            var contextLocations = JsonConvert.DeserializeObject<List<SqlFileLocation>>(strConnections);
            foreach (var context in contextLocations)
            {
                if (Directory.Exists(context.LocationPath))
                {
                    var ext = new List<string> { ".sql" };
                    var contentFiles = Directory.GetFiles(context.LocationPath, "*.*", SearchOption.AllDirectories).Where(s => ext.Contains(Path.GetExtension(s)));
                    context.ContentFileNames = new List<string>();

                    foreach (string fileName in contentFiles)
                    {
                        context.ContentFileNames.Add(Path.GetFileNameWithoutExtension(fileName));
                    }
                }
            }

            return contextLocations;
        }

        public static List<SqlConnectionInfo> GetConnections()
        {
            string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string cfgPath = asmPath + @"\" + "SQLServer.dll.config";
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
            return JsonConvert.DeserializeObject<List<SqlConnectionInfo>>(strConnections);
        }

        public static string GetFolderNames(List<SqlFileLocation> contextLocations)
        {
            var result = "[";
            foreach (var location in contextLocations)
            {
                result += location.LocationName + ",";
            }
            result.TrimEnd(',');
            result += "]";
            return result;
        }

        public static string GetQueriesCascadeDefinition(List<SqlFileLocation> contextLocations, string contextPropertyName)
        {
            var result = "[";
            foreach (var context in contextLocations)
            {
                result += contextPropertyName + ":" + context.LocationName + "[";
                foreach (var fileName in context.ContentFileNames)
                {
                    result += fileName + ",";
                }
                result.TrimEnd(new char[]{','});
                result += "],";
            }
            result.TrimEnd(new char[]{','});
            result += "]";
            return result;
        }

        public static string GetConnectionNames()
        {
            var result = "[";
            foreach (var connection in GetConnections())
            {
                result += connection.ConnectionName + ",";
            }
            result.TrimEnd(',');
            result += "]";
            return result;
        }

        public static async Task<RemoteDataBrokerResponse> ExecuteDynamicQuery(RemoteDataBrokerRequest rdbRequest)
        {
            //WriteDebugLog("Running ExecuteQuery");

            // Cast the body to the strong type (note: I would prefer to just make the argument of 
            // the type RemoteDataBrokerRequest right off the bat but more work needs to be done to
            // cast the input as that reflected type before we can do that.
            // TODO: Switch argument to plugin methods to RemoteDataBrokerRequest
            //RemoteDataBrokerRequest request = JsonConvert.DeserializeObject<RemoteDataBrokerRequest>(requestJson);
            SQLServerDynamicQueryDataModel request = JsonConvert.DeserializeObject<SQLServerDynamicQueryDataModel>(rdbRequest.Data);

            // Declare the response object
            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.Compress = rdbRequest.Compress;
            response.RequestId = rdbRequest.RequestId;
            response.DataType = "applicationJSON";

            if (!allowDynamicQuery())
            {
                response.Error = true;
                response.ErrorText = Environment.MachineName + ": Dynamic query execution not currently allowed. Please contact your Bezlio administrator for more information.";

                return response;
            }

            try
            {
                List<string> tableNames = new List<string>();

                //List<SqlFileLocation> locations = getFileLocations();
                List<SqlConnectionInfo> connections = getSqlConnections();                                

                // Locate the connection entry specified
                if (connections.Where((c) => c.ConnectionName.Equals(request.Connection)).Count() == 0)
                {
                    response.Error = true;
                    response.ErrorText = "Could not locate a connection in the plugin config file with the name " + request.Connection;
                    return response;
                }
                SqlConnectionInfo connection = connections.Where((c) => c.ConnectionName.Equals(request.Connection)).FirstOrDefault();

                // Now obtain a SQL connection
                object sqlConn = getConnection("SQL Server"
                            , connection.ServerAddress
                            , connection.DatabaseName
                            , connection.UserName
                            , connection.Password);

                //create the sql text
                System.Text.StringBuilder sb = new System.Text.StringBuilder();

                //go through and combine the queries
                foreach (var query in request.Queries)
                    sb.Append(query.Value).Append(";");

                DataSet results = await executeQuery(sqlConn, sb.ToString(), request.Parameters, false);

                //ensure that the results are the same between the two
                if(results.Tables.Count == request.Queries.Count)
                {
                    for(int i = 0; i < results.Tables.Count; i++)
                    {
                        if (request.Queries[i].Key == "")
                            results.Tables[i].TableName = "TableName" + i.ToString();
                        else
                            results.Tables[i].TableName = request.Queries[i].Key;
                    }
                }

                if (results.Tables.Count == 1)
                {
                    DataTable dt = results.Tables[0].Copy();
                    dt.TableName = "";
                    response.Data = JsonConvert.SerializeObject(dt);
                }
                else if (results.Tables.Count > 1)
                {
                    response.Data = JsonConvert.SerializeObject(results);
                }
                else
                    response.Data = JsonConvert.SerializeObject(request.Connection = sb.ToString());

            }
            catch (Exception ex)
            {
                response.Error = true;
                response.ErrorText = Environment.MachineName + ": " + ex.Message;
            }

            // Return our response
            return response;
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
                if (request.Parameters != null && request.Parameters.Count > 0)                
                    request.Parameters = FillQueryParameters(ref sql, request.Parameters);                                   

                // Now obtain a SQL connection
                object sqlConn = getConnection("SQL Server"
                            , connection.ServerAddress
                            , connection.DatabaseName
                            , connection.UserName
                            , connection.Password);

                //WriteDebugLog("Connection Received");

                DataSet dsResults = await executeQuery(sqlConn, sql, request.Parameters, false); ;

                //DataTable dtResponse = await executeQuery(sqlConn, sql, request.Parameters, false);

                //WriteDebugLog("Query Executed");

                int tableIndex = 1;

                foreach (DataTable dt in dsResults.Tables)
                {
                    dt.TableName = "TableName" + tableIndex.ToString();

                    tableIndex++;
                }


                // Return the data table
                if (dsResults.Tables.Count == 1)
                {
                    DataTable dt = dsResults.Tables[0].Copy();
                    dt.TableName = "";

                    response.Data = JsonConvert.SerializeObject(dt);
                }
                else
                    response.Data = JsonConvert.SerializeObject(dsResults);

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

                // Perform any replacements on passed variables, this also includes replacing the parameter values themselves, if required
                if (request.Parameters != null && request.Parameters.Count > 0)
                    request.Parameters = FillQueryParameters(ref sql, request.Parameters);
                
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

                DataSet dsResults = await executeQuery(sqlConn, sql, request.Parameters, true); ;

                //DataTable dtResponse = await executeQuery(sqlConn, sql, request.Parameters, false);

                //WriteDebugLog("Query Executed");

                int tableIndex = 1;

                foreach (DataTable dt in dsResults.Tables)
                {
                    dt.TableName = "TableName" + tableIndex.ToString();

                    tableIndex++;
                }


                // Return the data table
                if (dsResults.Tables.Count == 1)
                {
                    DataTable dt = dsResults.Tables[0].Copy();
                    dt.TableName = "";

                    response.Data = JsonConvert.SerializeObject(dt);
                }
                else
                    response.Data = JsonConvert.SerializeObject(dsResults);

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

        private static bool allowDynamicQuery()
        {
            string cfgPath = getCfgPath();

            bool retValue = false;

            if (File.Exists(cfgPath))
            {
                // Load in the cfg file
                XDocument xConfig = XDocument.Load(cfgPath);

                // Get the settings for the error log destination
                XElement xConnections = xConfig.Descendants("bezlio.plugins.Properties.Settings").Descendants("setting").Where(a => (string)a.Attribute("name") == "allowDynamicQueries").FirstOrDefault();
                if (xConnections != null)
                {
                    if (xConnections.Value.ToLower() == "true")
                        retValue = true;
                }
            }

            return retValue;
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
                    if(_userName != null && _userName.Trim().Length > 0)
                        oConnection = new SqlConnection("server=" + _serverAddress + ";uid=" + _userName + ";pwd=" + _password + ";database=" + _databaseName + ";Connection Timeout=150");
                    else //assume integrated authentication
                        oConnection = new SqlConnection("server=" + _serverAddress + ";database=" + _databaseName + ";Connection Timeout=150;integrated security = SSPI");
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
        private async static Task<DataSet> executeQuery(object _connection,
                                    string _sql, List<KeyValuePair<string, string>> spParams, bool storedProcedure)
        {
            // TODO: Make this actually async
            object oCommand;
            object oAdapter;
            DataSet ds = new DataSet();
            ds.DataSetName = "QueryExecution";

            DataTable dt = new DataTable("TableName");

            switch (_connection.GetType().Name.ToString())
            {
                case "SqlConnection":
                    oCommand = getCommand("SQL Server");
                    SqlCommand command = (SqlCommand)oCommand;

                    using (command.Connection = (SqlConnection)_connection) {
                        if (storedProcedure)                        
                            command.CommandType = CommandType.StoredProcedure;

                        if(spParams != null && spParams.Count > 0)
                            FillSPParameters(ref command, spParams);

                        command.CommandText = _sql;
                        oAdapter = getAdapter("SQL Server", oCommand);                  
                        ((SqlDataAdapter)oAdapter).Fill(ds);
                        //((SqlCommand)oCommand).Connection.Close();
                    }
                    //((SqlCommand)oCommand).Connection = (SqlConnection)_connection;

                    break;
            }

            oCommand = null;
            oAdapter = null;
            return ds;
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
        private static List<KeyValuePair<string, string>> FillQueryParameters(ref string query, List<KeyValuePair<string, string>> parameters)
        {
            List<KeyValuePair<string, string>> newParams = new List<KeyValuePair<string, string>>();

            //using regex against original query to find parameters that are in the old format
            MatchCollection mc = Regex.Matches(query, "(['%]{0,}\\{)(.*?)(\\}['%]{0,})");
            
            foreach(Match m in mc)
            {
                //group 2 contains the actual name of the parameter
                var param = (from v in parameters where v.Key == m.Groups[2].Value select v).FirstOrDefault();

                //if a value is found, replace the found parameter with the true parameter name in the query
                if (param.Key != null && param.Key.Length > 0)
                {
                    query = query.Replace(m.Value, "@" + param.Key);

                    if (m.Groups[1].Value.IndexOf('%') > -1)
                        param = new KeyValuePair<string, string>(param.Key, "%" + param.Value);

                    if (m.Groups[3].Value.IndexOf('%') > -1)
                        param = new KeyValuePair<string, string>(param.Key, param.Value + "%");

                    newParams.Add(param);
                }
            }

            return newParams;
        }

        private static void FillSPParameters(ref SqlCommand command, List<KeyValuePair<string, string>> parameters)
        {
            foreach (var parameter in parameters)
            {
                string useKey = parameter.Key;

                if (!parameter.Key.StartsWith("@")) //if the parameter name doesn't contain the "@" symbol, we will convert it to the proper parameter type
                    useKey = "@" + parameter.Key;

                string value = parameter.Value.ToString().Replace("'", "''");
                double number;
                SqlParameter param;

                if (!(value.StartsWith("\"") && value.EndsWith("\"")) && double.TryParse(value, out number))
                {
                    param = new SqlParameter(useKey, number);
                    param.DbType = DbType.Double;
                } else
                {
                    param = new SqlParameter(useKey, value);
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
