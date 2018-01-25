using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace bezlio.rdb.plugins
{
    class Visual8DataModel
    {
        public string Connection { get; set; }
        public string BOName { get; set; }
        public List<KeyValuePair<string, string>> Parameters { get; set; }

        public Visual8DataModel()
        {
            Parameters = new List<KeyValuePair<string, string>>();
        }
    }

    public class Visual8
    {
         public static object GetArgs()
        {
            Visual8DataModel model = new Visual8DataModel();
            model.Connection = "The name of the server connection.";
            model.BOName = "Lsa.Vmfg.Sales.Customer";
            model.Parameters = new List<KeyValuePair<string, string>>();
            model.Parameters.Add(new KeyValuePair<string, string>("Load", "JSON Parsed Customer ID"));

            return model;
        }

        // This is a base method for handling Visual 8 support. This will extensively use helper methods to handle the bulk of the work but this
        // would work for a generic load or save of any type of transaction or document. Loading a blank value will return an empty dataset which
        // the user can pass back after updating for a save method
        public static async Task<RemoteDataBrokerResponse> ExecuteBOMethod(RemoteDataBrokerRequest rdbRequest)
        {
            Visual8DataModel request = JsonConvert.DeserializeObject<Visual8DataModel>(rdbRequest.Data);

            // Create the response object
            RemoteDataBrokerResponse response = Common.GetResponseObject(rdbRequest.RequestId, rdbRequest.Compress);

            // Establish a connection to Epicor
            Common.Connect(request.Connection, ref response);

            try
            {
                // Find the assembly
                string[] boNameAry = request.BOName.Split('.');
                string asmName = boNameAry[1] + boNameAry[2] + ".DLL";
                string clientLocation = Config.GetClientPath(ref response);
                VisualConnection connection = Config.GetConnectionInfo(request.Connection, ref response);
                // Load the assembly
                Assembly asm = Assembly.LoadFrom(clientLocation + @"\" + asmName);
                // Find the type
                Type type = asm.GetType(request.BOName);
                // Set the constructor
                ConstructorInfo ctor = type.GetConstructor(new Type[] { typeof(string) });
                // Create an instance using thes constructor
                var obj = ctor.Invoke(new object[] { connection.InstanceName });

                // Because of how the .net objects work we are going to use the parameters as an execution plan and return the final DS when we are done
                foreach (var action in request.Parameters) {
                    if (action.Key == "MergeDataSet") {
                        // Merge dataset runs through the value associated and automatically updates the local dataset
                        DataSet mergeDs = JsonConvert.DeserializeObject<DataSet>(action.Value);

                        // Get the tables property from the object
                        PropertyInfo p = type.GetProperty("Tables");
                        DataTableCollection tables = (DataTableCollection)p.GetValue(obj);

                        // Cycle through and update the local dataset to the values passed
                        foreach (DataTable mergeTable in mergeDs.Tables) {
                            for (var i = 0; i < mergeTable.Rows.Count; i++) {
                                foreach(DataColumn mergeColumn in mergeTable.Columns) {
                                    tables[mergeTable.TableName].Rows[i][mergeColumn.ColumnName] = mergeTable.Rows[i][mergeColumn.ColumnName];
                                }
                            }
                        }
                    } else {
                        // If it is not merge, we are calling these actions
                        // Start by parsing the value we received
                        JContainer o = JObject.Parse(action.Value);
                        // Now we are running get methods (since there are multiple signitures per call
                        foreach (var method in type.GetMethods()) {
                            // First filter by name
                            if (method.Name == action.Key) {
                                // Now we need to check the parameters
                                var parms = method.GetParameters();

                                // Check to see if the signature of this method matches how many things we've passed
                                if (parms.Length == (int)o.GetType().GetProperty("Count").GetValue(o)) {
                                    // It does, now we need to create an object array
                                    List<object> objList = new List<object>();
                                    var i = 0;
                                    var values = o.Values().ToList();
                                    foreach (var p in parms) {
                                        // Add the parameter to the objects we will pass to the method
                                        objList.Add(
                                            Convert.ChangeType(
                                                ((JValue)(values[i])).Value,
                                                p.ParameterType
                                            )
                                        );
                                        i++;
                                    }
                                    // Invoke our method
                                    method.Invoke(obj, objList.ToArray());
                                }
                            }
                        }
                    }
                }

                // Once we have executed the execution plan we are going to return the dataset for the object
                PropertyInfo propDataSet = type.GetProperty("DataSet");
                response.Data = JsonConvert.SerializeObject(propDataSet.GetValue(obj));
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
            finally { Common.CloseConnection(request.Connection, ref response); }

            // Return response object
            return response;
        }
    }
}
