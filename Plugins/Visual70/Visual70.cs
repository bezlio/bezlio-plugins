using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace bezlio.rdb.plugins
{
    class Visual70DataModel
    {
        public string Connection { get; set; }
        public string COMObject { get; set; }
        public string Context { get; set; }
        public string Method { get; set; }
        public string Data { get; set; }

        public Visual70DataModel() { }
    }

    public class Visual70
    {
        public static object GetArgs()
        {
            Visual70DataModel model = new Visual70DataModel();
            model.Connection = "The name of the server connection.";
            model.COMObject = "Visual COM Object (i.e. VMFGShf.WorkOrder.1)";
            model.Context = "COM Context (i.e. RUN_LABOR)";
            model.Method = "COM method (i.e. Save)";
            model.Data = "The data you wish to pass into this COM object in JSON format";

            return model;
        }

        public static async Task<RemoteDataBrokerResponse> ExecuteCOMCall(RemoteDataBrokerRequest rdbRequest)
        {
            // Deserialize the request object
            Visual70DataModel request = JsonConvert.DeserializeObject<Visual70DataModel>(rdbRequest.Data);

            // Create the response object
            RemoteDataBrokerResponse response = Common.GetResponseObject(rdbRequest.RequestId, rdbRequest.Compress);

            // Establish a connection to Visual
            var visualConn = Common.GetVisualConnection(request.Connection, ref response);
            string instance = Common.GetVisualInstance(request.Connection, ref response);

            try
            {
                // Create an instance of the specified COM object
                Type comType = Type.GetTypeFromProgID(request.COMObject);
                dynamic comInst = Activator.CreateInstance(comType);

                // Now load the recordset object so we can fill it
                dynamic rs = comInst.Prepare(instance, request.Context);

                // Load the data passed in via JSON as an object we can iterate through
                var data = JsonConvert.DeserializeObject(request.Data);

                foreach (JContainer cont in (JContainer)data)
                {
                    // Add a new entry to rs for each object at this level
                    rs.AddNew();

                    // Now loop through each of the properties in dr and apply their values to rs
                    foreach (JProperty prop in cont)
                    {
                        // This section needs to be enhanced to support child recordsets which
                        // would be presented as children on the given prop.  For now we only
                        // support single-level recordsets
                        rs.Fields(prop.Name).Value = prop.Value;
                    }

                    // Save the rs object
                    rs.Update();
                }

                switch (request.Method)
                {
                    case "Save":
                        comInst.Save(instance, rs);
                        response.Data = JsonConvert.SerializeObject("Success");
                        break;
                    default:
                        response.Error = true;
                        response.ErrorText = JsonConvert.SerializeObject("Unsupported COM method.");
                        break;
                }                
            }
            catch (Exception ex)
            {
                response.Error = true;
                response.ErrorText += ex.Message;
            }

            // Return response object
            return response;
        }
    }
}
