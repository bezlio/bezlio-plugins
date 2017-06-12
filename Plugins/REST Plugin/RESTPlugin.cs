using bezlio.rdb;
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
    public class RESTDataModel
    {
        public string Url { get; set; }
        public string Method { get; set; }
        public string Authentication { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Headers { get; set; }
        public string Body { get; set; }

        public RESTDataModel()  { }
    }

    public class HeaderModel
    {
        public string Header { get; set; }
        public string Value { get; set; }
    }
    public class REST
    {
        public static object GetArgs()
        {

            RESTDataModel model = new RESTDataModel();
            model.Url = "http://twitter.com";
            model.Method = "[GET,POST,PUT,DELETE,HEAD,MERGE,OPTIONS,PATCH]";
            model.Authentication = "[None,Basic]";
            model.Username = "User";
            model.Password = "Pass";
            model.Headers = "Array of headers";
            model.Body = "JSON string of object";
            return model;
        }

        public static async Task<RemoteDataBrokerResponse> Execute(RemoteDataBrokerRequest rdbRequest)
        {
            RESTDataModel request = JsonConvert.DeserializeObject<RESTDataModel>(rdbRequest.Data);

            // Declare the response object
            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.Compress = rdbRequest.Compress;
            response.RequestId = rdbRequest.RequestId;
            response.DataType = "applicationJSON";

            try
            {
                // Set TLS, the OR should mean that we can use any of them
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                var client = new RestClient(request.Url);

                // Determine and set authentication
                switch (request.Authentication) {
                    case "Basic":
                        client.Authenticator = new HttpBasicAuthenticator(request.Username, request.Password);
                        break;
                    case "None":
                        break;
                    default:
                        break;
                }

                // Create the object
                RestRequest restRequest = new RestRequest();

                // Set the method based on what they requested
                switch (request.Method) {
                    case "DELETE":
                        restRequest.Method = Method.DELETE;
                        break;
                    case "GET":
                        restRequest.Method = Method.GET;
                        break;
                    case "HEAD":
                        restRequest.Method = Method.HEAD;
                        break;
                    case "MERGE":
                        restRequest.Method = Method.MERGE;
                        break;
                    case "OPTIONS":
                        restRequest.Method = Method.OPTIONS;
                        break;
                    case "PATCH":
                        restRequest.Method = Method.PATCH;
                        break;
                    case "POST":
                        restRequest.Method = Method.POST;
                        break;
                    case "PUT":
                        restRequest.Method = Method.PUT;
                        break;
                }

                // Cycle through the headers
                if (request.Headers != null && request.Headers != "") {
                    List<HeaderModel> headers = JsonConvert.DeserializeObject<List<HeaderModel>>(request.Headers);
                    headers.ForEach(h =>
                    {
                        if (h.Header != "" && h.Value != "") {
                            restRequest.AddHeader(h.Header, h.Value);
                        }                  
                    });
                }

                // Check to see if we need to add a body
                if (request.Body != null && request.Body != "") {
                    restRequest.AddJsonBody(request.Body);
                }

                // Execute
                IRestResponse resp = client.Execute(restRequest);

                // Return the results
                response.Data = resp.Content;
                //response.Data = JsonConvert.SerializeObject(resp.Content);
            }
            catch (Exception ex)
            {
                response.Error = true;
                response.ErrorText = Environment.MachineName + ": " + ex.Message;
                if (ex.InnerException != null) {
                    response.ErrorText += " " + ex.InnerException.Message;
                }
            }

            // Return our response
            return response;
        }
    }
}
