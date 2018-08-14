using Newtonsoft.Json;
using RestSharp;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace bezlio.rdb.plugins
{
    public class NotificationsModel
    {
        public string Recpient { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
    }
    

    public class SqlFileLocation
    {
        public SqlFileLocation() { }

        public string permaToken { get; set; }
    }


    public class Notifications
    {
        public static object GetArgs()
        {

            NotificationsModel model = new NotificationsModel();
            model.Recpient = "Who gets the notification";
            model.Title = "Notification Title";
            model.Body = "Notification Body";
            return model; 
        }
        public static async Task<RemoteDataBrokerResponse> sendNotification(RemoteDataBrokerRequest rdbRequest)
        {
            var test = new { Recipient = "", Title = "", Body = "" };
            try
            {
                var request = JsonConvert.DeserializeAnonymousType(rdbRequest.Data, test);

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

                restRequest.AddParameter("key", GetLocations().permaToken);

                // Execute
                IRestResponse resp = client.Execute(restRequest);

                response.Data = resp.Content;

                return response;
            }
            catch(Exception e)
            {
                var request = JsonConvert.DeserializeAnonymousType(rdbRequest.Data, test);
                var response = new RemoteDataBrokerResponse();
                response.Compress = rdbRequest.Compress;
                response.RequestId = rdbRequest.RequestId;
                response.DataType = "applicationJSON";

                response.Data =JsonConvert.SerializeObject(request);

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
