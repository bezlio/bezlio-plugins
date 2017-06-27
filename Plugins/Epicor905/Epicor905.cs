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
            model.Connection = GetConnectionNames();
            model.Company = "Company ID";
            model.BOName = GetBONames();
            model.BOMethodName = "Method Name (i.e. ExecuteByID)";
            model.Parameters = new List<KeyValuePair<string, string>>();
            model.Parameters.Add(new KeyValuePair<string, string>("pcQueryID", "BAQ ID"));

            return model;
        }

        public static List<EpicorConnection> GetConnections()
        {
            string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string cfgPath = asmPath + @"\" + "Epicor905.dll.config";
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
            foreach (var f in Directory.GetFiles(clientPath, @"Epicor.Mfg.BO.*.dll", SearchOption.TopDirectoryOnly))
            {
                files.Add(Path.GetFileNameWithoutExtension(f).Replace("Epicor.Mfg.BO.", ""));
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

        // This method allows you to execute any Epicor BO method.  This is helpful for when you want to call a
        // simple BO method, but can be tedious for transactions that require several BO calls chained together.
        // In order to make these sorts of transactions easier to call in BRDB, see HelperMethods subfolders for
        // an ever-expanding library of helper methods.
        public static async Task<RemoteDataBrokerResponse> ExecuteBOMethod(RemoteDataBrokerRequest rdbRequest)
        {
            // Cast the body to the strong type (note: I would prefer to just make the argument of 
            // the type RemoteDataBrokerRequest right off the bat but more work needs to be done to
            // cast the input as that reflected type before we can do that.

            Epicor905DataModel request = JsonConvert.DeserializeObject<Epicor905DataModel>(rdbRequest.Data);

            // Create the response object
            RemoteDataBrokerResponse response = Common.GetResponseObject(rdbRequest.RequestId, rdbRequest.Compress);

            // Establish a connection to Epicor
            object epicorConn = Common.GetEpicorConnection(request.Connection, request.Company, ref response);

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
                            var ds = JsonConvert.DeserializeObject(definedParameter.First().Value, t,settings);
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
                        Type t = bo.GetType().GetMethod(request.BOMethodName.ToString()).GetParameters()[x].ParameterType;
                        if (t.IsByRef)
                            t = t.GetElementType();

                        parameters[x] = Activator.CreateInstance(bo.GetType().GetMethod(request.BOMethodName.ToString()).GetParameters()[x].ParameterType); ;
                    }
                }

                if (bo.GetType().GetMethod(request.BOMethodName.ToString()).ReturnType != typeof(void))
                {
                    object returnObj = Activator.CreateInstance(bo.GetType().GetMethod(request.BOMethodName.ToString()).ReturnType);
                    returnObj = bo.GetType().GetMethod(request.BOMethodName.ToString()).Invoke(bo, parameters);

                    if (request.BOName.ToString() == "DynamicQuery" && request.BOMethodName.ToString() == "ExecuteByID")
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
            finally { Common.CloseEpicorConnection(epicorConn, ref response); }


            // Return response object
            return response;
        }
    }
}