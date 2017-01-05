using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace bezlio.rdb.plugins
{
    class Epicor10DataModel
    {
        public string Connection { get; set; }
        public string Company { get; set; }
        public string BOName { get; set; }
        public string BOMethodName { get; set; }
        public List<KeyValuePair<string, string>> Parameters { get; set; }

        public Epicor10DataModel()
        {
            Parameters = new List<KeyValuePair<string, string>>();
        }
    }


    public class Epicor10
    {
        public static object GetArgs()
        {
            Epicor10DataModel model = new Epicor10DataModel();
            model.Connection = "The name of the server connection.";
            model.Company = "Company ID";
            model.BOName = "Epicor BO (i.e. DynamicQuery)";
            model.BOMethodName = "Method Name (i.e. ExecuteByID)";
            model.Parameters = new List<KeyValuePair<string, string>>();
            model.Parameters.Add(new KeyValuePair<string, string>("queryID", "BAQ ID"));

            return model;
        }

        // This method allows you to execute any Epicor BO method.  This is helpful for when you want to call a
        // simple BO method, but can be tedious for transactions that require several BO calls chained together.
        // In order to make these sorts of transactions easier to call in BRDB, see HelperMethods subfolders for
        // an ever-expanding library of helper methods.
        public static async Task<RemoteDataBrokerResponse> ExecuteBOMethod(RemoteDataBrokerRequest rdbRequest)
        {
            // Deserialize the request object
            Epicor10DataModel request = JsonConvert.DeserializeObject<Epicor10DataModel>(rdbRequest.Data);

            // Create the response object
            RemoteDataBrokerResponse response = Common.GetResponseObject(rdbRequest.RequestId, rdbRequest.Compress);

            // Establish a connection to Epicor
            object epicorConn = Common.GetEpicorConnection(request.Connection, request.Company, ref response);

            if (epicorConn != null)
            {
                try
                {
                    // Load the referenced BO
                    object bo = Common.GetBusinessObject(epicorConn, request.BOName.ToString(), ref response);

                    // Develop parameters object
                    int parameterCount = bo.GetType().GetMethod(request.BOMethodName.ToString()).GetParameters().Count();
                    object[] parameters = new object[parameterCount];
                    for (int x = 0; x < parameterCount; x++)
                    {
                        string parameterName = bo.GetType().GetMethod(request.BOMethodName.ToString()).GetParameters()[x].Name;
                        var definedParameter = request.Parameters.Where((p) => p.Key.Equals(parameterName));
                        if (definedParameter.Count() > 0)
                        {
                            // If this is a dataset parameter we need to deserialize it first, otherwise a straight convert should be fine
                            if (bo.GetType().GetMethod(request.BOMethodName.ToString()).GetParameters()[x].ParameterType.ToString().Contains("DataSet"))
                            {
                                Type t = bo.GetType().GetMethod(request.BOMethodName.ToString()).GetParameters()[x].ParameterType;
                                JsonSerializerSettings settings = new JsonSerializerSettings();
                                var ds = JsonConvert.DeserializeObject(definedParameter.First().Value, t, settings);
                                parameters[x] = ds;
                            }
                            else
                            {
                                Type t = bo.GetType().GetMethod(request.BOMethodName.ToString()).GetParameters()[x].ParameterType;
                                if (t.IsByRef)
                                    t = t.GetElementType();

                                parameters[x] = Convert.ChangeType(definedParameter.First().Value, t);
                            }
                        }
                        else if (bo.GetType().GetMethod(request.BOMethodName.ToString()).GetParameters()[x].ParameterType.ToString().Contains("DataSet"))
                        {
                            parameters[x] = Activator.CreateInstance(bo.GetType().GetMethod(request.BOMethodName.ToString()).GetParameters()[x].ParameterType); ;
                        }
                    }

                    if (bo.GetType().GetMethod(request.BOMethodName.ToString()).ReturnType != typeof(void))
                    {
                        object returnObj = Activator.CreateInstance(bo.GetType().GetMethod(request.BOMethodName.ToString()).ReturnType);
                        returnObj = bo.GetType().GetMethod(request.BOMethodName.ToString()).Invoke(bo, parameters);

                        if (request.BOName.ToString() == "DynamicQuery")
                        {
                            DataTable dt = ((DataSet)returnObj).Tables["Results"];
                            response.Data = JsonConvert.SerializeObject(dt);
                        }
                        else
                        {
                            response.Data = JsonConvert.SerializeObject(returnObj);
                        }
                    }
                    else
                    {
                        bo.GetType().GetMethod(request.BOMethodName.ToString()).Invoke(bo, parameters);
                        response.Data = JsonConvert.SerializeObject("Update Successful");
                    }


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
                finally { Common.CloseEpicorConnection(epicorConn, ref response); }
            }

            // Return response object
            return response;
        }
    }
}
