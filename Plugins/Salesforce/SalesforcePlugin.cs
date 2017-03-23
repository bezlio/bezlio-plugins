using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using System.Xml.Linq;
using Newtonsoft.Json;
using Salesforce.Common;
using Salesforce.Force;
using Salesforce.Common.Models;
using System.Net;
using System.Dynamic;

namespace bezlio.rdb.plugins
{
    public class SalesforceDataModel
    {
        public string Context { get; set; }
        public string Connection { get; set; }
        public string QueryName { get; set; }
        public string ObjectType { get; set; }
        public List<KeyValuePair<string, string>> Parameters { get; set; }

        public SalesforceDataModel()
        {
            Parameters = new List<KeyValuePair<string, string>>();
        }
    }

    public class SalesforceConnectionInfo
    {
        public SalesforceConnectionInfo() { }

        public string ConnectionName { get; set; }
        public string ServerAddress { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string SecurityToken { get; set; }
        public string ConsumerKey { get; set; }
        public string ConsumerSecret { get; set; }
    }

    public class SalesforceFileLocation
    {
        public SalesforceFileLocation() { }

        public string LocationName { get; set; }
        public string LocationPath { get; set; }
    }

    public class Salesforce
    {
        public static object GetArgs()
        {

            SalesforceDataModel model = new SalesforceDataModel();

            model.Context = GetFolderNames();
            model.Connection = GetConnectionNames();
            model.QueryName = "Query filename to execute.";
            model.ObjectType = "Pertains to CreateObject method only.";
            model.Parameters = new List<KeyValuePair<string, string>>();
            model.Parameters.Add(new KeyValuePair<string, string>("CustomerId", "102"));

            return model;
        }

        public static List<SalesforceFileLocation> GetLocations()
        {
            string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string cfgPath = asmPath + @"\" + "Salesforce.dll.config";
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
            return JsonConvert.DeserializeObject<List<SalesforceFileLocation>>(strConnections);
        }

        public static List<SalesforceConnectionInfo> GetConnections()
        {
            string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string cfgPath = asmPath + @"\" + "Salesforce.dll.config";
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
            return JsonConvert.DeserializeObject<List<SalesforceConnectionInfo>>(strConnections);
        }

        public static string GetFolderNames()
        {
            var result = "[";
            foreach (var location in GetLocations())
            {
                result += location.LocationName + ",";
            }
            result.TrimEnd(',');
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

        public static async Task<RemoteDataBrokerResponse> ExecuteQuery(RemoteDataBrokerRequest rdbRequest)
        {
            // Deserialize the body into the data model
            SalesforceDataModel request = JsonConvert.DeserializeObject<SalesforceDataModel>(rdbRequest.Data);

            // Declare the response object
            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.Compress = rdbRequest.Compress;
            response.RequestId = rdbRequest.RequestId;
            response.DataType = "applicationJSON";

            try
            {
                // Settings do not seem to reflect in cleanly, we will read the settings directly
                string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string cfgPath = asmPath + @"\" + "Salesforce.dll.config";
                string strLocations = "";
                string strConnections = "";

                if (File.Exists(cfgPath)) {
                    // Load in the cfg file
                    XDocument xConfig = XDocument.Load(cfgPath);

                    // Get the setting for the debug log destination
                    XElement xLocations = xConfig.Descendants("bezlio.plugins.Properties.Settings").Descendants("setting").Where(a => (string)a.Attribute("name") == "sqlFileLocations").FirstOrDefault();
                    if (xLocations != null) {
                        strLocations = xLocations.Value;
                    }
                
                    // Get the settings for the error log destination
                    XElement xConnections = xConfig.Descendants("bezlio.plugins.Properties.Settings").Descendants("setting").Where(a => (string)a.Attribute("name") == "connections").FirstOrDefault();
                    if (xConnections != null) {
                        strConnections = xConnections.Value;
                    }
                }

                // Deserialize the values from Settings
                List<SalesforceFileLocation> locations = JsonConvert.DeserializeObject<List<SalesforceFileLocation>>(strLocations);
                List<SalesforceConnectionInfo> connections = JsonConvert.DeserializeObject<List<SalesforceConnectionInfo>>(strConnections);

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
                SalesforceConnectionInfo connection = connections.Where((c) => c.ConnectionName.Equals(request.Connection)).FirstOrDefault();

                // Perform any replacements on passed variables
                if (request.Parameters != null && request.Parameters.Count > 0) {
                    FillQueryParameters(ref sql, request.Parameters);
                }

                // Create the connection
                var auth = new AuthenticationClient();
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                await auth.UsernamePasswordAsync(connection.ConsumerKey, connection.ConsumerSecret, connection.UserName, connection.Password + connection.SecurityToken, connection.ServerAddress);
                var client = new ForceClient(auth.InstanceUrl, auth.AccessToken, auth.ApiVersion);

                // Execute the query
                QueryResult<dynamic> result = await client.QueryAsync<dynamic>(sql);

                // Return the data table
                response.Data = JsonConvert.SerializeObject(result.Records);

                // Dispose
                client.Dispose();
                client = null;
                auth.Dispose();
                auth = null;
            }
            catch (Exception ex)
            {
                response.Error = true;
                response.ErrorText = Environment.MachineName + ": " + ex.Message;
            }

            // Return our response
            return response;        
        }

        public static async Task<RemoteDataBrokerResponse> CreateObject(RemoteDataBrokerRequest rdbRequest)
        {
            // Deserialize the body into the data model
            SalesforceDataModel request = JsonConvert.DeserializeObject<SalesforceDataModel>(rdbRequest.Data);

            // Declare the response object
            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.Compress = rdbRequest.Compress;
            response.RequestId = rdbRequest.RequestId;
            response.DataType = "applicationJSON";

            try
            {
                // Settings do not seem to reflect in cleanly, we will read the settings directly
                string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string cfgPath = asmPath + @"\" + "Salesforce.dll.config";
                string strConnections = "";

                if (File.Exists(cfgPath)) {
                    // Load in the cfg file
                    XDocument xConfig = XDocument.Load(cfgPath);
                
                    // Get the connections
                    XElement xConnections = xConfig.Descendants("bezlio.plugins.Properties.Settings").Descendants("setting").Where(a => (string)a.Attribute("name") == "connections").FirstOrDefault();
                    if (xConnections != null) {
                        strConnections = xConnections.Value;
                    }
                }

                // Deserialize the values from Settings
                List<SalesforceConnectionInfo> connections = JsonConvert.DeserializeObject<List<SalesforceConnectionInfo>>(strConnections);

                // Locate the connection entry specified
                SalesforceConnectionInfo connection = connections.Where((c) => c.ConnectionName.Equals(request.Connection)).FirstOrDefault();

                // Create the connection
                var auth = new AuthenticationClient();
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                await auth.UsernamePasswordAsync(connection.ConsumerKey, connection.ConsumerSecret, connection.UserName, connection.Password + connection.SecurityToken, connection.ServerAddress);
                var client = new ForceClient(auth.InstanceUrl, auth.AccessToken, auth.ApiVersion);

                // Cycle through the parameters to fill out the object type
                dynamic obj = new ExpandoObject() as IDictionary<string, object>;
                // We need an interface to dynamically add the parameters;
                var ary = obj as IDictionary<String, object>;

                foreach (var item in request.Parameters) {
                    ary.Add(item.Key, item.Value);
                }

                // Create the object
                obj.Id = await client.CreateAsync(request.ObjectType, obj);

                // Return the data table
                response.Data = JsonConvert.SerializeObject(obj);

                // Dispose
                client.Dispose();
                client = null;
                auth.Dispose();
                auth = null;
            }
            catch (Exception ex)
            {
                response.Error = true;
                response.ErrorText = Environment.MachineName + ": " + ex.Message;
            }

            // Return our response
            return response;        
        }

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
        private static void FillQueryParameters(ref string query, List<KeyValuePair<string, string>> parameters)
        {
            foreach (var parameter in parameters)
            {
                query = query.Replace('{' + parameter.Key + '}', parameter.Value.ToString().Replace("'", "''"));
            }
        }
    }
}
