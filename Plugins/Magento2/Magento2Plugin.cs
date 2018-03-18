using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace bezlio.rdb.plugins
{
    public class MagentoDataModel
    {
        public string Connection { get; set; }
        public List<KeyValuePair<string, string>> Filters { get; set; }

        public MagentoDataModel()
        {
            Filters = new List<KeyValuePair<string, string>>();
        }
    }
    public class Magento2
    {
        #region Common
        public static object GetArgs()
        {

            MagentoDataModel model = new MagentoDataModel();

            model.Connection = GetConnectionNames();
            model.Filters.Add(new KeyValuePair<string, string>("status", "pending"));

            return model;
        }

        public class MagentoConnectionInfo
        {
            public MagentoConnectionInfo() { }

            public string ConnectionName { get; set; }
            public string SiteUrl { get; set; }
            public string MagentoUserName { get; set; }
            public string MagentoUserPassword { get; set; }
        }

        public static List<MagentoConnectionInfo> GetConnections()
        {
            string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string cfgPath = asmPath + @"\" + "Magento2.dll.config";
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
            return JsonConvert.DeserializeObject<List<MagentoConnectionInfo>>(strConnections);
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

        private static MagentoConnectionInfo GetConnectionByName(string name, RemoteDataBrokerResponse response)
        {
            try
            {
                List<MagentoConnectionInfo> connections = GetConnections();

                // Locate the connection entry specified
                if (connections.Where((c) => c.ConnectionName.Equals(name)).Count() == 0)
                {
                    response.Error = true;
                    response.ErrorText = "Could not locate a connection in the plugin config file with the name " + name;
                    return new MagentoConnectionInfo();
                }
                else
                {
                    return connections.Where((c) => c.ConnectionName.Equals(name)).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                response.Error = true;
                response.ErrorText = ex.Message;
                return new MagentoConnectionInfo();
            }
        }

        private static RemoteDataBrokerResponse GetResponseObject(bool compress, string requestId)
        {
            // Declare the response object
            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.Compress = compress;
            response.RequestId = requestId;
            response.DataType = "applicationJSON";

            return response;
        }

        private static string GetFilterString(List<KeyValuePair<string, string>> filters)
        {
            var filter = "searchCriteria";

            for (int i = 0; i < filters.Count; i++)
            {
                var fieldName = "";
                var fieldOperator = "eq";
                var fieldValue = "";

                if (filters[i].Value.Contains(":"))
                {
                    fieldName = filters[i].Key;
                    fieldOperator = filters[i].Value.Split(':')[0];
                    fieldValue = filters[i].Value.Split(':')[1];
                }
                else
                {
                    fieldName = filters[i].Key;
                    fieldValue = filters[i].Value;
                }
                filter += $"[filterGroups][{i}][filters][{i}][field]={fieldName}&searchCriteria[filterGroups][{i}][filters][{i}][value]={fieldValue}&searchCriteria[filterGroups][{i}][filters][{i}][condition_type]={fieldOperator}";
            }

            return filter;
        }

        public static string GetBearerToken(string siteUrl, string userName, string password)
        {
            var client = new RestClient { BaseUrl = new Uri(siteUrl) };
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/json");
            request.Resource = "rest/V1/integration/admin/token";
            request.AddJsonBody(new { username = userName, password = password });
            var response = client.Execute(request);
            return response.Content;
        }
        #endregion

        #region Get List Methods
        public static async Task<RemoteDataBrokerResponse> GetCustomers(RemoteDataBrokerRequest rdbRequest)
        {
            MagentoDataModel request = JsonConvert.DeserializeObject<MagentoDataModel>(rdbRequest.Data);
            var response = GetResponseObject(rdbRequest.Compress, rdbRequest.RequestId);
            var connection = GetConnectionByName(request.Connection, response);
            if (response.Error) { return response; }

            try
            {
                var token = GetBearerToken(connection.SiteUrl, connection.MagentoUserName, connection.MagentoUserPassword).Trim('"');
                var client = new RestClient { BaseUrl = new Uri(connection.SiteUrl) };
                var magentoRequest = new RestRequest(Method.GET);
                magentoRequest.AddHeader("Content-Type", "application/json");
                magentoRequest.AddHeader("Authorization", "Bearer " + token);
                magentoRequest.Resource = "rest/V1/customers/search?" + GetFilterString(request.Filters);
                var magentoResponse = client.Execute<Models.Customers>(magentoRequest);
                response.Data = JsonConvert.SerializeObject(magentoResponse.Data.Items);
            }
            catch (Exception ex)
            {
                response.Error = true;
                response.ErrorText = ex.Message;
            }

            return response;
        }
        public static async Task<RemoteDataBrokerResponse> GetProducts(RemoteDataBrokerRequest rdbRequest)
        {
            MagentoDataModel request = JsonConvert.DeserializeObject<MagentoDataModel>(rdbRequest.Data);
            var response = GetResponseObject(rdbRequest.Compress, rdbRequest.RequestId);
            var connection = GetConnectionByName(request.Connection, response);
            if (response.Error) { return response; }

            try
            {
                var token = GetBearerToken(connection.SiteUrl, connection.MagentoUserName, connection.MagentoUserPassword).Trim('"');
                var client = new RestClient { BaseUrl = new Uri(connection.SiteUrl) };
                var magentoRequest = new RestRequest(Method.GET);
                magentoRequest.AddHeader("Content-Type", "application/json");
                magentoRequest.AddHeader("Authorization", "Bearer " + token);
                magentoRequest.Resource = "rest/V1/products?" + GetFilterString(request.Filters);
                var magentoResponse = client.Execute<Models.Products>(magentoRequest);
                response.Data = JsonConvert.SerializeObject(magentoResponse.Data.Items);
            }
            catch (Exception ex)
            {
                response.Error = true;
                response.ErrorText = ex.Message;
            }

            return response;
        }
        public static async Task<RemoteDataBrokerResponse> GetOrders(RemoteDataBrokerRequest rdbRequest)
        {
            MagentoDataModel request = JsonConvert.DeserializeObject<MagentoDataModel>(rdbRequest.Data);
            var response = GetResponseObject(rdbRequest.Compress, rdbRequest.RequestId);
            var connection = GetConnectionByName(request.Connection, response);
            if (response.Error) { return response; }

            try
            {
                var token = GetBearerToken(connection.SiteUrl, connection.MagentoUserName, connection.MagentoUserPassword).Trim('"');
                var client = new RestClient { BaseUrl = new Uri(connection.SiteUrl) };
                var magentoRequest = new RestRequest(Method.GET);
                magentoRequest.AddHeader("Content-Type", "application/json");
                magentoRequest.AddHeader("Authorization", "Bearer " + token);
                magentoRequest.Resource = "rest/V1/orders?" + GetFilterString(request.Filters);
                var magentoResponse = client.Execute<Models.Orders>(magentoRequest);
                response.Data = JsonConvert.SerializeObject(magentoResponse.Data.Items);
            }
            catch (Exception ex)
            {
                response.Error = true;
                response.ErrorText = ex.Message;
            }

            return response;
        }
        #endregion
    }
}
