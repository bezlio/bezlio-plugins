using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Reflection;
using System.Xml.Linq;
using Newtonsoft.Json;
using MarketplaceWebServiceOrders;
using MarketplaceWebServiceOrders.Model;
using MWSRecommendationsSectionService;
using MWSRecommendationsSectionService.Model;
using System.Dynamic;

namespace bezlio.rdb.plugins
{
    class Amazon_ListModel {
        public GetOpenOrders_Model GetOpenOrdersList { get; set; }
        public GetOrder_Model GetOrderByID { get; set; }
        public GetOrders_Model GetOrdersByID { set; get; }
    }

    class GetOpenOrders_Model {
        public string orderStatus { get; set; }
        public DateTime createdAfter { get; set; }
    }

    class GetOrder_Model {
        public string orderId { get; set; }
    }

    class GetOrders_Model
    {
        public List<string> Orders { set; get; }
    }

    public class Amazon
    {
        static MarketplaceWebServiceOrdersClient ordersClient;
        static MWSRecommendationsSectionServiceClient recommendationClient;
        static string sellerId;
        static string mwsAuthToken;
        static string marketplaceId;

        public Amazon() {
            AuthenticateClient();
        }

        public static object GetArgs() {
            Amazon_ListModel model = new Amazon_ListModel();

            model.GetOpenOrdersList = new GetOpenOrders_Model() {
                createdAfter = DateTime.Today,
                orderStatus = "Order Status Filter (comma separated - Pending, Unshipped, PartiallyShipped, Shipped, Canceled, Unfulfillable, PendingAvailability)"
            };

            model.GetOrderByID = new GetOrder_Model() {
                orderId = "Order ID (comma separated)"
            };

            model.GetOrdersByID = new GetOrders_Model
            {
                Orders = new List<string>()
            };

            return model;
        }

        private static string GetCfgPath(){
            string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return asmPath + @"\" + "Amazon.dll.config";
        }

        private static void AuthenticateClient() {
            string cfgPath = GetCfgPath();
            XDocument xConfig = XDocument.Load(cfgPath);
            XElement xDeveloper = xConfig.Descendants("bezlio.rdb.plugins.Properties.Settings").Descendants("setting").Where(setting => (string)setting.Attribute("name") == "developerInfo").FirstOrDefault();
            XElement xSeller = xConfig.Descendants("bezlio.rdb.plugins.Properties.Settings").Descendants("setting").Where(setting => (string)setting.Attribute("name") == "sellerInfo").FirstOrDefault();

            List<AmazonConnection_Developer> developerInfo = JsonConvert.DeserializeObject<List<AmazonConnection_Developer>>(xDeveloper.Value);
            List<AmazonConnection_Seller> sellerInfo = JsonConvert.DeserializeObject<List<AmazonConnection_Seller>>(xSeller.Value);

            //developer info
            string accessKey = developerInfo[0].accessKey;
            string secretKey = developerInfo[0].secretKey;
            string appName = developerInfo[0].appName;
            string appVersion = developerInfo[0].appVersion;

            //client info
            sellerId = sellerInfo[0].sellerId;
            mwsAuthToken = sellerInfo[0].mwsAuthToken;
            marketplaceId = sellerInfo[0].marketplaceId;

            string serviceUrl = "https://mws.amazonservices.com";

            MarketplaceWebServiceOrdersConfig ordersConfig = new MarketplaceWebServiceOrdersConfig() {
                ServiceURL = serviceUrl
            };

            MWSRecommendationsSectionServiceConfig recommendationConfig = new MWSRecommendationsSectionServiceConfig() {
                ServiceURL = serviceUrl
            };

            ordersClient = new MarketplaceWebServiceOrdersClient(accessKey, secretKey, appName, appVersion, ordersConfig);
            recommendationClient = new MWSRecommendationsSectionServiceClient(accessKey, secretKey, appName, appVersion, recommendationConfig);
        }

        public static RemoteDataBrokerResponse GetResponseObject(string requestId, bool compress) {
            // Declare the response object
            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.RequestId = requestId;
            response.Compress = compress;
            response.DataType = "applicationJSON";
            return response;
        }

        public static async Task<RemoteDataBrokerResponse> GetStatus (RemoteDataBrokerRequest rdbRequest) {
            RemoteDataBrokerResponse response = GetResponseObject(rdbRequest.RequestId, true);
            
            try {
                MarketplaceWebServiceOrders.Model.GetServiceStatusRequest getStatus = new MarketplaceWebServiceOrders.Model.GetServiceStatusRequest();
                getStatus.SellerId = sellerId;
                getStatus.MWSAuthToken = mwsAuthToken;
                
                var data = ordersClient.GetServiceStatus(getStatus);

                response.Data = JsonConvert.SerializeObject(data);
            }
            catch (Exception ex) {
                if (!string.IsNullOrEmpty(ex.InnerException.ToString()))
                    response.ErrorText += ex.InnerException.ToString();

                response.Error = true;
                response.ErrorText = ex.Message;
            }

            return response;
        }

        public static async Task<RemoteDataBrokerResponse> GetOpenOrdersList (RemoteDataBrokerRequest rdbRequest) {
            GetOpenOrders_Model request = JsonConvert.DeserializeObject<GetOpenOrders_Model>(rdbRequest.Data);

            RemoteDataBrokerResponse response = GetResponseObject(rdbRequest.RequestId, true);

            try {

                ListOrdersRequest ordersRequest = new ListOrdersRequest();
                ordersRequest.SellerId = sellerId;
                ordersRequest.MWSAuthToken = mwsAuthToken;
                ordersRequest.MarketplaceId = new List<string>(new string[] { marketplaceId });

                ordersRequest.CreatedAfter = request.createdAfter;
                ordersRequest.OrderStatus = request.orderStatus.Split(',').ToList();                

                var data = ordersClient.ListOrders(ordersRequest);                

                    response.Data = JsonConvert.SerializeObject(data);
            }
            catch (Exception ex) {
                if (!string.IsNullOrEmpty(ex.InnerException.ToString()))
                    response.ErrorText += ex.InnerException.ToString();

                response.Error = true;
                response.ErrorText = ex.Message;
            }

            return response;
        }

        public static async Task<RemoteDataBrokerResponse> GetOrdersById(RemoteDataBrokerRequest rdbRequest)
        {
            RemoteDataBrokerResponse response = GetResponseObject(rdbRequest.RequestId, true);

            GetOrders_Model request = null;

            try
            {
                request = JsonConvert.DeserializeObject<GetOrders_Model>(rdbRequest.Data);
            }
            catch
            {
                response.Error = true;
                response.ErrorText = "Testing!!";
            }

            dynamic data = new List<dynamic>();

            //obtain all of the order information
            try
            {
                //create a list of all of the order IDs
                StringBuilder sb = new StringBuilder();
                foreach (var order in request.Orders)
                    sb.Append(order).Append(',');

                //now, get ALL of the requested orders
                GetOrderRequest orderRequest = new GetOrderRequest(sellerId, sb.ToString().TrimEnd(',').Split(',').ToList());
                orderRequest.MWSAuthToken = mwsAuthToken;

                var orderData = ordersClient.GetOrder(orderRequest);
                
                //go through each of the order results and obtain their order items
                for (int i = 0; i < orderData.GetOrderResult.Orders.Count; i++)
                {
                    dynamic orderObject = new ExpandoObject();
                    orderObject.Order = orderData.GetOrderResult.Orders[i];                    

                    ListOrderItemsRequest orderItemRequest = new ListOrderItemsRequest(sellerId, orderData.GetOrderResult.Orders[i].AmazonOrderId);
                    orderItemRequest.MWSAuthToken = mwsAuthToken;

                    var orderItemData = ordersClient.ListOrderItems(orderItemRequest);

                    orderObject.OrderItems = orderItemData.ListOrderItemsResult.OrderItems;                    

                    data.Add(orderObject);

                    //determine if we need to put a wait in, depending on the max quota and if there are more requests
                    if (orderItemData.ResponseHeaderMetadata.QuotaRemaining == 0 && i + 1 < orderData.GetOrderResult.Orders.Count && orderItemData.ResponseHeaderMetadata.QuotaResetsAt != null)
                    {
                        //we need to wait until the reset time
                        var timeUntil = ((DateTime)orderItemData.ResponseHeaderMetadata.QuotaResetsAt).Subtract(DateTime.Now);
                        System.Threading.Thread.Sleep(timeUntil);
                    }
                }

                response.Data = JsonConvert.SerializeObject(data);
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(ex.InnerException.ToString()))
                {
                    response.ErrorText += ex.InnerException.ToString();
                }

                response.Error = true;
                response.ErrorText = ex.ToString();
            }

            return response;
        }

        public static async Task<RemoteDataBrokerResponse> GetOrderById (RemoteDataBrokerRequest rdbRequest) {
            GetOrder_Model request = JsonConvert.DeserializeObject<GetOrder_Model>(rdbRequest.Data);

            RemoteDataBrokerResponse response = GetResponseObject(rdbRequest.RequestId, true);

            try {
                GetOrderRequest orderRequest = new GetOrderRequest(sellerId, request.orderId.Split(',').ToList());
                orderRequest.MWSAuthToken = mwsAuthToken;
               
                var orderData = ordersClient.GetOrder(orderRequest);

                ListOrderItemsRequest orderItemRequest = new ListOrderItemsRequest(sellerId, request.orderId);
                orderItemRequest.MWSAuthToken = mwsAuthToken;

                var orderItemData = ordersClient.ListOrderItems(orderItemRequest);

                dynamic data = new ExpandoObject();                
                data.Order = orderData.GetOrderResult.Orders[0];
                data.OrderItems = orderItemData.ListOrderItemsResult.OrderItems;
                               
                response.Data = JsonConvert.SerializeObject(data);
            }
            catch (Exception ex) {
                if (!string.IsNullOrEmpty(ex.InnerException.ToString())) {
                    response.ErrorText += ex.InnerException.ToString();
                }

                response.Error = true;
                response.ErrorText = ex.Message;
            }

            return response;
        }

        public static async Task<RemoteDataBrokerResponse> GetRecommendations (RemoteDataBrokerRequest rdbRequest) {
            RemoteDataBrokerResponse response = GetResponseObject(rdbRequest.RequestId, true);

            try {
                ListRecommendationsRequest recommendationRequest = new ListRecommendationsRequest();
                recommendationRequest.SellerId = sellerId;
                recommendationRequest.MWSAuthToken = mwsAuthToken;
                recommendationRequest.MarketplaceId = marketplaceId;

                var data = recommendationClient.ListRecommendations(recommendationRequest);

                response.Data = JsonConvert.SerializeObject(data);
            }
            catch (Exception ex) {
                if(!string.IsNullOrEmpty(ex.InnerException.ToString())) {
                    response.ErrorText += ex.InnerException.ToString();
                }

                response.Error = true;
                response.ErrorText = ex.Message;
            }

            return response;
        }
    }
}
