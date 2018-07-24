using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace bezlio.rdb.plugins
{
    public class WCFDataModel
    {
        public string URI { set; get; }
        public string Contract { set; get; }
        public string MethodName { set; get; }
        public List<string> Parameters { get; set; }

        public WCFDataModel()
        {
            this.Parameters = new List<string>();
        }
    }

    public class WCF
    {
        public static object GetArgs()
        {
            WCFDataModel model = new WCFDataModel();

            model.URI = "The URI (with WSDL of the service contract";
            model.Contract = "The contract (interface) name that will be used";
            model.MethodName = "The method name to be called";
            model.Parameters = new List<string>();
            return model;
        }

        public static async Task<RemoteDataBrokerResponse> Execute(RemoteDataBrokerRequest rdbRequest)
        {
            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();

            WCFDataModel request = JsonConvert.DeserializeObject<WCFDataModel>(rdbRequest.Data);

            object results = null;

            try
            {
                DynamicProxyLibrary.DynamicProxyFactory factory = new DynamicProxyLibrary.DynamicProxyFactory(request.URI);

                DynamicProxyLibrary.DynamicProxy proxy = factory.CreateProxy(request.Contract);

                if (request.Parameters.Count > 0)
                {
                    object[] parameters = new object[request.Parameters.Count];

                    for (int i = 0; i <= request.Parameters.Count - 1; i++)
                    {
                        parameters[i] = request.Parameters[i];
                    }

                    results = proxy.CallMethod(request.MethodName, parameters);
                }
                else
                    results = proxy.CallMethod(request.MethodName, null);

                if (results != null)
                    response.Data = JsonConvert.SerializeObject(results);
                else
                    response.Data = JsonConvert.SerializeObject("No Response");

                proxy.Close();

            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(ex.InnerException.ToString()))
                {
                    response.ErrorText += ex.InnerException.ToString();
                }

                response.Error = true;
                response.ErrorText += ex.Message;
            }

            return response;
        }
    }
}
