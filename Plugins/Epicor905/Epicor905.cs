using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Linq;

namespace bezlio.rdb.plugins
{
    public class Epicor905DataModel
    {
        public string Connection { get; set; }
        public string Company { get; set; }
        public string BOName { get; set; }
        public string BOMethodName { get; set; }
        public List<KeyValuePair<string, string>> Parameters { get; set; }

        public Epicor905DataModel()
        {
            Parameters = new List<KeyValuePair<string, string>>();
        }
    }


    public class Epicor905
    {
        public static object GetArgs()
        {
            Epicor905DataModel model = new Epicor905DataModel();
            model.Connection = "The name of the server connection.";
            model.Company = "Company ID";
            model.BOName = "Epicor BO (i.e. DynamicQuery)";
            model.BOMethodName = "Method Name (i.e. ExecuteByID)";
            model.Parameters = new List<KeyValuePair<string, string>>();
            model.Parameters.Add(new KeyValuePair<string, string>("pcQueryID", "BAQ ID"));

            return model;
        }

        public static async Task<RemoteDataBrokerResponse> ExecuteBOMethod(RemoteDataBrokerRequest rdbRequest)
        {
            // Cast the body to the strong type (note: I would prefer to just make the argument of 
            // the type RemoteDataBrokerRequest right off the bat but more work needs to be done to
            // cast the input as that reflected type before we can do that.

            Epicor905DataModel request = JsonConvert.DeserializeObject<Epicor905DataModel>(rdbRequest.Data);

            // Declare the response object
            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.RequestId = rdbRequest.RequestId;
            response.Compress = rdbRequest.Compress;
            response.DataType = "applicationJSON";

            try
            {
                // Settings do not seem to reflect in cleanly, we will read the settings directly
                string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string cfgPath = asmPath + @"\" + "Epicor905.dll.config";
                string strConnections = "";
                string clientPath = "";

                if (File.Exists(cfgPath))
                {
                    // Load in the cfg file
                    XDocument xConfig = XDocument.Load(cfgPath);

                    // Get the settings for the connections
                    XElement xConnections = xConfig.Descendants("bezlio.plugins.Properties.Settings").Descendants("setting").Where(a => (string)a.Attribute("name") == "connections").FirstOrDefault();
                    if (xConnections != null)
                    {
                        strConnections = xConnections.Value;
                    }

                    XElement xClientPath = xConfig.Descendants("bezlio.plugins.Properties.Settings").Descendants("setting").Where(a => (string)a.Attribute("name") == "epicorClientPath").FirstOrDefault();
                    if (xClientPath != null)
                    {
                        clientPath = xClientPath.Value;
                    }
                }

                // Deserialize the values from Settings
                List<EpicorConnection> connections = JsonConvert.DeserializeObject<List<EpicorConnection>>(strConnections);

                // Locate the connection entry specified
                if (connections.Where((c) => c.ConnectionName.Equals(request.Connection)).Count() == 0)
                {
                    response.Error = true;
                    response.ErrorText = "Could not locate a connection in the plugin config file with the name " + request.Connection;
                    return response;
                }
                EpicorConnection connection = connections.Where((c) => c.ConnectionName.Equals(request.Connection)).FirstOrDefault();

                // Connect to Epicor
                object epicorConn = getEpicorConnection(clientPath, connection.AppServerUrl, connection.UserName, connection.Password, request.Company);

                try
                {
                    // Now attempt to perform the BO call defined within the body
                    Assembly sessionAssembly = Assembly.LoadFrom(clientPath + @"\Epicor.Mfg.Core.Session.dll");
                    Assembly genericAssembly = Assembly.LoadFrom(clientPath + @"\Epicor.Mfg.BO." + request.BOName.ToString() + ".dll");
                    Assembly genericIFAssembly = Assembly.LoadFrom(clientPath + @"\Epicor.Mfg.IF.I" + request.BOName.ToString() + ".dll");
                    object bo = Activator.CreateInstance(genericAssembly.GetType("Epicor.Mfg.BO." + request.BOName.ToString()), new object[] { epicorConn.GetType().InvokeMember("ConnectionPool", BindingFlags.GetProperty, null, epicorConn, null) });

                    // Develop parameters object
                    int parameterCount = ((Type[])genericAssembly.ExportedTypes)[0].GetMethod(request.BOMethodName.ToString()).GetParameters().Count();
                    object[] parameters = new object[parameterCount];
                    for (int x = 0; x < parameterCount; x++)
                    {
                        string parameterName = ((Type[])genericAssembly.ExportedTypes)[0].GetMethod(request.BOMethodName.ToString()).GetParameters()[x].Name;
                        var definedParameter = request.Parameters.Where((p) => p.Key.Equals(parameterName));
                        if (definedParameter.Count() > 0)
                        {
                            // If this is a dataset parameter we need to deserialize it first, otherwise a straight convert should be fine
                            if (((Type[])genericAssembly.ExportedTypes)[0].GetMethod(request.BOMethodName.ToString()).GetParameters()[x].ParameterType.ToString().Contains("DataSet"))
                            {
                                Type t = ((Type[])genericAssembly.ExportedTypes)[0].GetMethod(request.BOMethodName.ToString()).GetParameters()[x].ParameterType;
                                JsonSerializerSettings settings = new JsonSerializerSettings();
                                var ds = JsonConvert.DeserializeObject(definedParameter.First().Value, t,settings);
                                parameters[x] = ds;
                            }
                            else
                            {
                                parameters[x] = Convert.ChangeType(definedParameter.First().Value, ((Type[])genericAssembly.ExportedTypes)[0].GetMethod(request.BOMethodName.ToString()).GetParameters()[x].ParameterType);
                            }
                        }
                        else if (bo.GetType().GetMethod(request.BOMethodName.ToString()).GetParameters()[x].ParameterType.ToString().Contains("DataSet"))
                        {
                            Type t = bo.GetType().GetMethod(request.BOMethodName.ToString()).GetParameters()[x].ParameterType;
                            if (t.IsByRef)
                                t = t.GetElementType();

                            parameters[x] = Activator.CreateInstance(bo.GetType().GetMethod(request.BOMethodName.ToString()).GetParameters()[x].ParameterType); ;
                        }
                    }

                    if (((Type[])genericAssembly.ExportedTypes)[0].GetMethod(request.BOMethodName.ToString()).ReturnType != typeof(void))
                    {
                        object returnObj = Activator.CreateInstance(((Type[])genericAssembly.ExportedTypes)[0].GetMethod(request.BOMethodName.ToString()).ReturnType);
                        returnObj = bo.GetType().GetMethod(request.BOMethodName.ToString()).Invoke(bo, parameters);

                        if (request.BOName.ToString() == "DynamicQuery")
                        {
                            DataTable dt = ((DataSet)returnObj).Tables["Results"];
                            foreach (DataRow displayFields in ((DataSet)returnObj).Tables["DisplayFields"].Rows)
                            {
                                if (dt.Columns.IndexOf(displayFields["FieldLabel"].ToString()) == -1)
                                    dt.Columns[displayFields["FieldName"].ToString()].ColumnName = displayFields["FieldLabel"].ToString();
                            }

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
                finally
                {
                    // Disconnect from Epicor
                    closeEpicorConnection(clientPath, epicorConn);
                }
            }
            catch (Exception ex)
            {
                response.Error = true;

                if (ex.InnerException != null)
                {
                    response.ErrorText += ex.InnerException.Message;
                } else
                {
                    response.ErrorText += ex.Message;
                }
            }

            // Return response object
            return response;
        }

        private static object getEpicorConnection(string clientPath, string appServerUrl, string userName, string password, string companyId)
        {
            object epicorConn = null;
            Assembly sessionAssembly = null;

            sessionAssembly = Assembly.LoadFrom(clientPath + @"\Epicor.Mfg.Core.Session.dll");
            epicorConn = Activator.CreateInstance(sessionAssembly.GetType("Epicor.Mfg.Core.Session"), new object[] { userName, password, appServerUrl });
            epicorConn.GetType().InvokeMember("CompanyID",
                         BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty,
                         Type.DefaultBinder, epicorConn, new Object[] { companyId });

            return epicorConn;
        }

        public static void closeEpicorConnection(string clientPath, object epicorConn)
        {
            Assembly sessionAssembly = null;
            sessionAssembly = Assembly.LoadFrom(clientPath + @"\Epicor.Mfg.Core.Session.dll");
            // Dispose of the connection
            sessionAssembly.GetType("Epicor.Mfg.Core.Session").GetMethod("Dispose").Invoke(epicorConn, null);
        }
    }
}
