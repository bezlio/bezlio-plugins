using Newtonsoft.Json;
using System;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace bezlio.rdb.plugins
{
    public class RSS
    {
        public class RSSDataModel
        {
            public string Url { get; set; }

            public RSSDataModel() { }
        }

        public static object GetArgs()
        {
            RSSDataModel model = new RSSDataModel();
            model.Url = "URL of RSS feed";
            return model;
        }

        public static async Task<RemoteDataBrokerResponse> GetFeed(RemoteDataBrokerRequest rdbRequest)
        {
            RSSDataModel request = JsonConvert.DeserializeObject<RSSDataModel>(rdbRequest.Data);

            // Declare the response object
            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.Compress = rdbRequest.Compress;
            response.RequestId = rdbRequest.RequestId;
            response.DataType = "applicationJSON";

            try
            {
                var r = XmlReader.Create(request.Url);
                var data = SyndicationFeed.Load(r);

                // Extract encoded content into an attribute extenstion so it is accessible as text
                foreach (var item in data.Items)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (SyndicationElementExtension extension in item.ElementExtensions)
                    {
                        XElement ele = extension.GetObject<XElement>();
                        if (ele.Name.LocalName == "encoded" && ele.Name.Namespace.ToString().Contains("content"))
                        {
                            sb.Append(ele.Value);
                        }
                    }
                    item.AttributeExtensions.Add(new XmlQualifiedName("encodedContent", ""), sb.ToString());
                }

                // Return the data table
                response.Data = JsonConvert.SerializeObject(data.Items);
            }
            catch (Exception ex)
            {
                response.Error = true;
                response.ErrorText = ex.Message;
            }

            // Return our response
            return response;
        }
    }
}
