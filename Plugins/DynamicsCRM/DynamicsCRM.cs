using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using bezlio.rdb;
using Microsoft.Xrm.Sdk.Client;
using System.ServiceModel.Description;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.IO;
using System.Reflection;
using System.Xml.Linq;

namespace bezlio.rdb.plugins
{
    #region Models
    public class DynamicsCRM_Connection_Model
    {
        public string ConnectionName { get; set; }
        public string OrganizationUri { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
    public class DynamicsCRM_Model
    {
        public string ConnectionName { get; set; }
        public string Type { get; set; }
        public string Columns { get; set; }
        public string AllColumns { get; set; }
        public string Conditions { get; set; }
    }
    public class DynamicsCRM_Condition_Model
    {
        public string AttributeName { get; set; }
        public string Operator { get; set; }
        public string Value { get; set; }
    }
    public class DynamicsCRM_GetByID_Model
    {
        public string ConnectionName { get; set; }
        public string Type { get; set; }
        public string Id { get; set; }
        public string Columns { get; set; }
        public string AllColumns { get; set; }
    }
    #endregion

    public class DynamicsCRM
    {
        #region GetArgs()
        public static object GetArgs()
        {

            DynamicsCRM_Model model = new DynamicsCRM_Model();

            model.ConnectionName = "Name of the CRM connection to use";
            model.Type = "Crm Sdk Type";
            model.Columns = "JSON string array of columns";
            model.AllColumns = "[true,false]";
            model.Conditions = "JSON array of object { AttributeName, Operator, Value }";
            return model;
        }
        #endregion

        #region ResponseObject
        public static RemoteDataBrokerResponse GetResponseObject(string requestId, bool compress)
        {
            // Declare the response object
            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.RequestId = requestId;
            response.Compress = compress;
            response.DataType = "applicationJSON";
            return response;
        }
        #endregion

        #region GenerateProxy
        public static IOrganizationService GenerateService(string connectionName)
        {
            // Get the connections
            List<DynamicsCRM_Connection_Model> cons = GetConnections();
            // Find the one we want
            DynamicsCRM_Connection_Model connection = cons.Where(c => c.ConnectionName == connectionName).FirstOrDefault();
 
            Uri organizationUri = new Uri(connection.OrganizationUri);

            //Define the connection credentials
            ClientCredentials creds = new ClientCredentials();
            creds.UserName.UserName = connection.Username;
            creds.UserName.Password = connection.Password;

            //Create the service proxy
            OrganizationServiceProxy serviceProxy = new OrganizationServiceProxy(organizationUri, null, creds, null);
            serviceProxy.EnableProxyTypes();

            //Here we will use the interface instead of the proxy object.
            IOrganizationService service = (IOrganizationService)serviceProxy;

            return service;
        }
        #endregion

        public static List<DynamicsCRM_Connection_Model> GetConnections()
        {
            // This returns all of the available connections
            string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string cfgPath = asmPath + @"\" + "DynamicsCRM.dll.config";
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
            return JsonConvert.DeserializeObject<List<DynamicsCRM_Connection_Model>>(strConnections);
        }

        #region Query
        public static async Task<RemoteDataBrokerResponse> Query(RemoteDataBrokerRequest rdbRequest)
        {
            // Declare the response object
            RemoteDataBrokerResponse response = GetResponseObject(rdbRequest.RequestId, rdbRequest.Compress);

            // Deserialize the request
            DynamicsCRM_Model request = JsonConvert.DeserializeObject<DynamicsCRM_Model>(rdbRequest.Data);
        
            try {
                //Here we will use the interface instead of the proxy object.
                IOrganizationService service = GenerateService(request.ConnectionName);

                //Create the query expression
                QueryExpression query = new QueryExpression(request.Type);

                // Column set holder
                ColumnSet colSet = null;

                // Check if we want all the columns
                bool allCols = bool.Parse(request.AllColumns);

                if (allCols) {
                    // We want all the columns
                    colSet = new ColumnSet(true);
                } else {
                    // Deserialize the columns we would like
                    string[] cols = JsonConvert.DeserializeObject<string[]>(request.Columns);

                    // Add them to the requested columns
                    colSet = new ColumnSet(cols);
                }

                // Add the column set to the query
                query.ColumnSet = colSet;

                // Deserialize Conditions
                DynamicsCRM_Condition_Model[] conditions = JsonConvert.DeserializeObject<DynamicsCRM_Condition_Model[]>(request.Conditions);
                foreach (DynamicsCRM_Condition_Model condition in conditions) {
                    // Get the operator from the passed request
                    ConditionOperator co = (ConditionOperator)System.Enum.Parse(typeof(ConditionOperator), condition.Operator);
                    // Add the condition to the query
                    query.Criteria.AddCondition(new ConditionExpression(condition.AttributeName, co, condition.Value));
                }

                // Call the service
                var res = service.RetrieveMultiple(query);

                // Return the response
                response.Data = JsonConvert.SerializeObject(res);
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(ex.InnerException.ToString()))
                    response.ErrorText += ex.InnerException.ToString();

                response.Error = true;
                response.ErrorText = ex.Message;
            }

            return response;
        }
        #endregion

        #region GetById
        public static async Task<RemoteDataBrokerResponse> GetById(RemoteDataBrokerRequest rdbRequest)
        {
            // Declare the response object
            RemoteDataBrokerResponse response = GetResponseObject(rdbRequest.RequestId, rdbRequest.Compress);

            // Deserialize the request
            DynamicsCRM_GetByID_Model request = JsonConvert.DeserializeObject<DynamicsCRM_GetByID_Model>(rdbRequest.Data);

            try {
                //Here we will use the interface instead of the proxy object.
                IOrganizationService service = GenerateService(request.ConnectionName);

                //Create the query expression
                QueryExpression query = new QueryExpression(request.Type);

                // Column set holder
                ColumnSet colSet = null;

                // Check if we want all the columns
                bool allCols = bool.Parse(request.AllColumns);

                if (allCols) {
                    // We want all the columns
                    colSet = new ColumnSet(true);
                } else {
                    // Deserialize the columns we would like
                    string[] cols = JsonConvert.DeserializeObject<string[]>(request.Columns);

                    // Add them to the requested columns
                    colSet = new ColumnSet(cols);
                }

                // Parse the guid
                Guid gid = Guid.Parse(request.Id);

                // Call the service
                var res = service.Retrieve(request.Type, gid, colSet);

                // Return the response
                response.Data = JsonConvert.SerializeObject(res);
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(ex.InnerException.ToString()))
                    response.ErrorText += ex.InnerException.ToString();

                response.Error = true;
                response.ErrorText = ex.Message;
            }

            return response;
        }
        #endregion
    }
}
