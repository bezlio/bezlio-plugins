using Magento.RestApi;
using Magento.RestApi.Json;
using Magento.RestApi.Models;
using Newtonsoft.Json;
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
    // Note: This plugin reference Magento-RestApi (https://github.com/nickvane/Magento-RestApi).  For Newtonsoft compatibility
    // I had to download that source manually and revise the Newtonsoft versioning to 8.0.3.  Also, line 26 was commented out
    // on BaseConverter.cs as it impacted which elements were available for serialization.  The compiled version of this DLL
    // has been placed into the Compiled folder.

    public class MagentoDataModel
    {
        public string Connection { get; set; }
        public List<KeyValuePair<string, string>> Filters { get; set; }

        public MagentoDataModel()
        {
            Filters = new List<KeyValuePair<string, string>>();
        }
    }

    public class Magento
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
            public string ConsumerKey { get; set; }
            public string ConsumerSecret { get; set; }
            public string MagentoUserName { get; set; }
            public string MagentoUserPassword { get; set; }
        }

        public static List<MagentoConnectionInfo> GetConnections()
        {
            string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string cfgPath = asmPath + @"\" + "Magento.dll.config";
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
                } else
                {
                    return connections.Where((c) => c.ConnectionName.Equals(name)).FirstOrDefault();
                }
            } catch (Exception ex)
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

        private static Filter GetFilterObject(List<KeyValuePair<string, string>> filters)
        {
            var filter = new Filter();

            foreach (var f in filters)
            {
                if (f.Value.Contains(":"))
                {
                    switch (f.Value.Split(':')[0])
                    {
                        case ">":
                            filter.FilterExpressions.Add(new FilterExpression(f.Key, ExpressionOperator.gt, f.Value.Replace(">:", "")));
                            break;
                        case "in":
                            filter.FilterExpressions.Add(new FilterExpression(f.Key, ExpressionOperator.@in, f.Value.Replace("in:", "")));
                            break;
                        case "like":
                            filter.FilterExpressions.Add(new FilterExpression(f.Key, ExpressionOperator.like, f.Value.Replace("like:", "")));
                            break;
                        case "<":
                            filter.FilterExpressions.Add(new FilterExpression(f.Key, ExpressionOperator.lt, f.Value.Replace("<:", "")));
                            break;
                        case "!=":
                            filter.FilterExpressions.Add(new FilterExpression(f.Key, ExpressionOperator.neq, f.Value.Replace("!=:", "")));
                            break;
                        case "<>":
                            filter.FilterExpressions.Add(new FilterExpression(f.Key, ExpressionOperator.neq, f.Value.Replace("<>:", "")));
                            break;
                        case "nin":
                            filter.FilterExpressions.Add(new FilterExpression(f.Key, ExpressionOperator.@in, f.Value.Replace("nin:", "")));
                            break;
                        default:
                            filter.FilterExpressions.Add(new FilterExpression(f.Key, ExpressionOperator.like, f.Value));
                            break;
                    }
                }
                else
                {
                    filter.FilterExpressions.Add(new FilterExpression(f.Key, ExpressionOperator.like, f.Value));
                }

            }

            return filter;
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
                var client = new MagentoApi()
                    .Initialize(connection.SiteUrl, connection.ConsumerKey, connection.ConsumerSecret)
                    .AuthenticateAdmin(connection.MagentoUserName, connection.MagentoUserPassword);

                var filter = GetFilterObject(request.Filters);
                var magentoResponse = await client.GetCustomers(filter);

                if (!magentoResponse.HasErrors)
                {
                    response.Data = JsonConvert.SerializeObject(magentoResponse.Result);
                }

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
                var client = new MagentoApi()
                    .Initialize(connection.SiteUrl, connection.ConsumerKey, connection.ConsumerSecret)
                    .AuthenticateAdmin(connection.MagentoUserName, connection.MagentoUserPassword);

                var filter = GetFilterObject(request.Filters);
                var magentoResponse = await client.GetProducts(filter);

                if (!magentoResponse.HasErrors)
                {
                    response.Data = JsonConvert.SerializeObject(magentoResponse.Result);
                }

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
                var client = new MagentoApi()
                    .Initialize(connection.SiteUrl, connection.ConsumerKey, connection.ConsumerSecret)
                    .AuthenticateAdmin(connection.MagentoUserName, connection.MagentoUserPassword);

                var filter = GetFilterObject(request.Filters);
                var magentoResponse = await client.GetOrders(filter);

                if (!magentoResponse.HasErrors)
                {
                    response.Data = JsonConvert.SerializeObject(magentoResponse.Result);
                }

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
