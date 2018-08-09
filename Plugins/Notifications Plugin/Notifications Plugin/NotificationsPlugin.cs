using bezlio.rdb;
using bezlio.rdb.plugins;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace bezlio.rdb.plugins
{
    public class NotificationsModel
    {
        public string RegistrationToken { get; set; }
        public string Credential { get; set; }
    }

    public class NotificationsPlugin
    {
        public static object GetArgs()
        {

            NotificationsModel model = new NotificationsModel();
            model.RegistrationToken = "";
            model.Credential = "";
            return model;
        }
        public static object sendNotification(RemoteDataBrokerRequest rdbRequest)
        {
            RESTDataModel request = JsonConvert.DeserializeObject<RESTDataModel>(rdbRequest.Data);

            // Declare the response object
            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.Compress = rdbRequest.Compress;
            response.RequestId = rdbRequest.RequestId;
            response.DataType = "applicationJSON";

            var client = new RestClient("https://bezlio-notifications-3.azurewebsites.net/send");
            RestRequest restRequest = new RestRequest();
            restRequest.Method = Method.POST;

            // Check to see if we need to add a body
            if (request.Body != null && request.Body != "")
            {
                restRequest.AddJsonBody(request.Body);
            }

            // Execute
            IRestResponse resp = client.Execute(restRequest);

            response.Data = resp.Content;

            return response;
        }

        public static object addDevice(RemoteDataBrokerRequest rdbRequest)
        {
            RESTDataModel request = JsonConvert.DeserializeObject<RESTDataModel>(rdbRequest.Data);

            // Declare the response object
            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.Compress = rdbRequest.Compress;
            response.RequestId = rdbRequest.RequestId;
            response.DataType = "applicationJSON";

            var client = new RestClient("https://bezlio-notifications-3.azurewebsites.net/device");
            RestRequest restRequest = new RestRequest();
            restRequest.Method = Method.POST;

            // Check to see if we need to add a body
            if (request.Body != null && request.Body != "")
            {
                restRequest.AddJsonBody(request.Body);
            }

            // Execute
            IRestResponse resp = client.Execute(restRequest);

            response.Data = resp.Content;

            return response;
        }

        public static object removeDevice(RemoteDataBrokerRequest rdbRequest)
        {
            RESTDataModel request = JsonConvert.DeserializeObject<RESTDataModel>(rdbRequest.Data);

            // Declare the response object
            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.Compress = rdbRequest.Compress;
            response.RequestId = rdbRequest.RequestId;
            response.DataType = "applicationJSON";

            var client = new RestClient("https://bezlio-notifications-3.azurewebsites.net/device");
            RestRequest restRequest = new RestRequest();
            restRequest.Method = Method.DELETE;

            // Check to see if we need to add a body
            if (request.Body != null && request.Body != "")
            {
                restRequest.AddJsonBody(request.Body);
            }

            // Execute
            IRestResponse resp = client.Execute(restRequest);

            response.Data = resp.Content;

            return response;
        }
        
    }
}
