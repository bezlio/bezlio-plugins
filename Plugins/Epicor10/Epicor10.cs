using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;

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

    public class Epicor10BaqDataModel
    {
        public string Connection { get; set; }
        public string Company { get; set; }
        public string BaqId { get; set; }
        public List<KeyValuePair<string, string>> Parameters { get; set; }

        public Epicor10BaqDataModel()
        {
            Parameters = new List<KeyValuePair<string, string>>();
        }
    }

    public class Epicor10
    {
        public static object GetArgs()
        {
            Epicor10BaqDataModel model = new Epicor10BaqDataModel();
            model.Connection = GetConnectionNames();
            model.Company = "Company ID";
            model.BaqId = "BAQ ID";
            model.Parameters = new List<KeyValuePair<string, string>>();
            model.Parameters.Add(new KeyValuePair<string, string>("Parameter ID", "BAQ ID"));

            return model;
        }

        public static List<EpicorConnection> GetConnections()
        {
            string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string cfgPath = asmPath + @"\" + "Epicor10.dll.config";
            string strConnections = "";
            if (File.Exists(cfgPath))
            {
                // Load in the cfg file
                XDocument xConfig = XDocument.Load(cfgPath);

                // Get the setting for the debug log destination
                XElement xConnections = xConfig.Descendants("bezlio.plugins.Properties.Settings").Descendants("setting").Where(a => (string)a.Attribute("name") == "connections").FirstOrDefault();
                if (xConnections != null)
                {
                    strConnections = xConnections.Value;
                }
            }
            return JsonConvert.DeserializeObject<List<EpicorConnection>>(strConnections);
        }

        public static string GetConnectionNames()
        {
            var result = "[";
            foreach (var connection in GetConnections())
            {
                result += connection.ConnectionName + ",";
            }
            result.TrimEnd(','); 
            result += "]";
            return result;
        }

        public static string GetBONames()
        {
            var result = "[";

            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            string clientPath = Config.GetClientPath(ref response);

            List<string> files = new List<string>();
            foreach (var f in Directory.GetFiles(clientPath, @"Erp.Contracts.BO.*.dll", SearchOption.TopDirectoryOnly))
            {
                files.Add(Path.GetFileNameWithoutExtension(f).Replace("Erp.Contracts.BO.", ""));
            }

            foreach (var f in Directory.GetFiles(clientPath, @"Ice.Contracts.BO.*.dll", SearchOption.TopDirectoryOnly))
            {
                files.Add(Path.GetFileNameWithoutExtension(f).Replace("Ice.Contracts.BO.", ""));
            }

            files.Sort();

            foreach (var file in files)
            {
                result += file + ","; ;
            }

            result.TrimEnd(',');
            result += "]";
            return result;
        }

        public static async Task<RemoteDataBrokerResponse> ExecuteBAQ(RemoteDataBrokerRequest rdbRequest)
        {
            Epicor10BaqDataModel request = JsonConvert.DeserializeObject<Epicor10BaqDataModel>(rdbRequest.Data);

            // Create the response object
            RemoteDataBrokerResponse response = Common.GetResponseObject(rdbRequest.RequestId, rdbRequest.Compress);

            // Establish a connection to Epicor
            object epicorConn = Common.GetEpicorConnection(request.Connection, request.Company, ref response);

            try
            {
                // Load the referenced BO
                object bo = Common.GetBusinessObject(epicorConn, "DynamicQuery", ref response);
                object query = Common.GetBusinessObjectDataSet("DynamicQuery", "Ice.BO.QueryExecutionDataSet", ref response);

                // Load the available parameters
                DataSet paramDS = bo.GetType().GetMethod("GetQueryExecutionParametersByID").Invoke(bo, new object[] { request.BaqId }) as DataSet;

                // Fill in the parameters
                foreach (var p in request.Parameters)
                {
                    string valueType = "string";

                    foreach(DataRow dr in paramDS.Tables["ExecutionParameter"].Select("ParameterID = '" + p.Key + "'"))
                    {
                        valueType = dr["ValueType"] as string;
                    }

                    DataRow newRow = (query as DataSet).Tables["ExecutionParameter"].NewRow();

                    newRow["ParameterID"] = p.Key;
                    newRow["ParameterValue"] = p.Value;
                    newRow["ValueType"] = valueType;
                    newRow["IsEmpty"] = false;
                    newRow["RowMod"] = "A";

                    ((DataSet)query).Tables["ExecutionParameter"].Rows.Add(newRow);
                }

                DataSet ds = (DataSet)bo.GetType().GetMethod("ExecuteByID").Invoke(bo, new object[] { request.BaqId, query });
                response.Data = JsonConvert.SerializeObject(ds.Tables["Results"]);
                //response.Data = JsonConvert.SerializeObject(query);

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

            // Return response object
            return response;

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

                        if (request.BOName.ToString() == "DynamicQuery" && (request.BOMethodName.ToString() == "ExecuteByID" || request.BOMethodName.ToString() == "Execute"))
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
