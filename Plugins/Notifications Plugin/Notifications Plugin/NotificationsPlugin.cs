using Newtonsoft.Json;
using RestSharp;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace bezlio.rdb.plugins
{
    public class NotificationsModel
    {
        public string Recipient { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
    }
    

    public class SqlFileLocation
    {
        public SqlFileLocation() { }

        public string permaLinkToken { get; set; }
    }


    public class Notifications
    {
        public static object GetArgs()
        {

            NotificationsModel model = new NotificationsModel();
            model.Recipient = "Who gets the notification";
            model.Title = "Notification Title";
            model.Body = "Notification Body";
            return model; 
        }
        public static async Task<RemoteDataBrokerResponse> sendNotification(RemoteDataBrokerRequest rdbRequest)
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                var request = JsonConvert.DeserializeObject<dynamic>(rdbRequest.Data);

                // Declare the response object
                RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
                response.Compress = rdbRequest.Compress;
                response.RequestId = rdbRequest.RequestId;
                response.DataType = "applicationJSON";

                var client = new RestClient("https://bezlio-notifications-3.azurewebsites.net/send");
                RestRequest restRequest = new RestRequest();
                restRequest.Method = Method.GET;

                // Check to see if we need to add a body
                if (request != null && request.Body != "")
                {
                    restRequest.AddParameter("recipient", request.Recipient);
                    restRequest.AddParameter("title", request.Title);
                    restRequest.AddParameter("body", request.Body);
                }

                restRequest.AddParameter("key", "eyJhbGciOiJQQkVTMi1IUzUxMitBMjU2S1ciLCJlbmMiOiJBMjU2Q0JDLUhTNTEyIiwic3ViIjoibXN2cmF5OTYrYmV6bGlvQGdtYWlsLmNvbSIsImV4cCI6MCwicDJjIjo4MTkyLCJwMnMiOiJzNm1yRWdmN2F0OGVpWTRyIn0.DxA_jovgBcrjcwPjw06hQhUggZNDuvXxJgTMNhWNDLpZCbUXxZqmjTb_csXMXjzszwXfd8pGSZO10kzJkCpJ33rLplXEm-ux.gnKofD-D-7JM5VB_LtmasQ.3iR6Op63jcALHVN4_l2HwXzwLrDT5TgTGZ_er4fW_-iUvnqx-cwIUcFLao2NvhPAbC4MGyWSZjbwQqSFc6Fl8rttyt1WKApBn30Wl3QpwEXcqYQJ2lP_Sl1t4UUI6OxRsCmpWJAcFzVxSxTjbx_UM7AEiZ8SW5gEUpbrJ1PXjulWmV4fydXetK0lz0zA7v06.cWEd5sDEgLjAVFg1GDJODT24Msy2K54ezm84_6X_wGE");

                // Execute
                IRestResponse resp = client.Execute(restRequest);

                response.Data = resp.Content;

                return response;
            }
            catch(Exception ex)
            {
                var response = new RemoteDataBrokerResponse(); response.Error = true;
                response.ErrorText = Environment.MachineName + ": " + ex.Message;
                if (ex.InnerException != null)
                {
                    response.ErrorText += " " + ex.InnerException.Message;
                }

                return response; 

            }
        }
        private static SqlFileLocation GetLocations()
        {
            string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string cfgPath = asmPath + @"\" + "Notifications.dll.config";
            string strConnections = "";
            if (File.Exists(cfgPath))
            {
                // Load in the cfg file
                XDocument xConfig = XDocument.Load(cfgPath);

                // Get the setting for the debug log destination
                XElement xConnections = xConfig.Descendants("bezlio.plugins.Properties.Settings").Descendants("setting").Where(a => (string)a.Attribute("name") == "permaLinkToken").FirstOrDefault();
                if (xConnections != null)
                {
                    strConnections = xConnections.Value;
                }
            }

            var contextLocations = JsonConvert.DeserializeObject<SqlFileLocation>(strConnections);
            

            return contextLocations;
        }


    }
}
