using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace bezlio.rdb.plugins
{

    public class P21DataModel
    {
        public string AuthTokenUrl { get; set; }
        public string AuthClientId { get; set; }
        public string AuthClientSecret { get; set; }
        public string Method { get; set; }
        public string DataUrl { get; set; }
        public string DataBody { get; set; }
    }

    public class TokenDataModel
    {
        public string access_token { get; set; }
        public int expires_in { get; set; }
        public string refresh_token { get; set; }
        public string token_type { get; set; }
    }

    public class P21
    {
        public static object GetArgs()
        {

            P21DataModel model = new P21DataModel();

            model.AuthTokenUrl = "http://localhost:8000/token";
            model.AuthClientId = "bezlio_client";
            model.AuthClientSecret = "password";
            model.Method = "[GET,POST,PUT,DELETE,HEAD,MERGE,OPTIONS,PATCH]";
            model.DataUrl = "http://localhost:8000/api/v2/CustomerPricings/GetCustomerPricings";
            model.DataBody = "Encoded JSON Body For Request";

            return model;
        }

        public static async Task<RemoteDataBrokerResponse> Execute(RemoteDataBrokerRequest rdbRequest)
        {
            P21DataModel request = JsonConvert.DeserializeObject<P21DataModel>(rdbRequest.Data);

            // Declare the response object
            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.Compress = rdbRequest.Compress;
            response.RequestId = rdbRequest.RequestId;
            response.DataType = "applicationJSON";

            try
            {
                // First get a token
                var client = new RestClient(request.AuthTokenUrl);
                var tokenRequest = new RestRequest(Method.POST);
                tokenRequest.AddParameter("application/x-www-form-urlencoded", "grant_type=client_credentials&client_id=" + request.AuthClientId + "&client_secret=" + request.AuthClientSecret, ParameterType.RequestBody);
                IRestResponse resp = client.Execute(tokenRequest);
                string access_token = JsonConvert.DeserializeObject<TokenDataModel>(resp.Content).access_token;

                //// Now do the actual API call
                client = new RestClient(request.DataUrl);

                // Create the object
                RestRequest dataRequest = new RestRequest();

                // Set the method based on what they requested
                switch (request.Method)
                {
                    case "DELETE":
                        dataRequest.Method = Method.DELETE;
                        break;
                    case "GET":
                        dataRequest.Method = Method.GET;
                        break;
                    case "HEAD":
                        dataRequest.Method = Method.HEAD;
                        break;
                    case "MERGE":
                        dataRequest.Method = Method.MERGE;
                        break;
                    case "OPTIONS":
                        dataRequest.Method = Method.OPTIONS;
                        break;
                    case "PATCH":
                        dataRequest.Method = Method.PATCH;
                        break;
                    case "POST":
                        dataRequest.Method = Method.POST;
                        break;
                    case "PUT":
                        dataRequest.Method = Method.PUT;
                        break;
                }

                
                dataRequest.AddHeader("Authorization", "Bearer " + access_token);
                dataRequest.AddParameter("application/json", request.DataBody, ParameterType.RequestBody);

                // Execute
                IRestResponse dataResponse = client.Execute(dataRequest);

                // Return the results
                if (dataResponse.Content.ToString().Length > 0)
                {
                    response.Data = dataResponse.Content;
                } else
                {
                    response.Data = JsonConvert.SerializeObject("Done!");
                }
            }
            catch (Exception ex)
            {
                response.Error = true;
                response.ErrorText = Environment.MachineName + ": " + ex.Message;
                if (ex.InnerException != null)
                {
                    response.ErrorText += " " + ex.InnerException.Message;
                }
            }

            // Return our response
            return response;
        }
    }
}
