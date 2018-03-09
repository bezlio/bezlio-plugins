using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Threading.Tasks;

namespace bezlio.rdb.plugins.HelperMethods.Customer
{
    class Customer_CreateContactModel
    {
        public string Connection { get; set; }
        public string Company { get; set; }
        public int CustNum { get; set; }
        public string ShipToNum { get; set; }
        public List<KeyValuePair<string, string>> Parameters { get; set; }

        public Customer_CreateContactModel()  {
            Parameters = new List<KeyValuePair<string, string>>();
        }
    }
    
    public class CustomerHelperMethods
    {
        public static async Task<RemoteDataBrokerResponse> Customer_CreateContactModel(RemoteDataBrokerRequest rdbRequest)
        {
            // Deserialize the request object
            Customer_CreateContactModel request = JsonConvert.DeserializeObject<Customer_CreateContactModel>(rdbRequest.Data);

            // Create the response object
            RemoteDataBrokerResponse response = Common.GetResponseObject(rdbRequest.RequestId, rdbRequest.Compress);

            // Establish a connection to Epicor
            object epicorConn = Common.GetEpicorConnection(request.Connection, request.Company, ref response);

            try
            {
                // Load the referenced BO
                object bo = Common.GetBusinessObject(epicorConn, "CustCnt", ref response);

                // Create an instance of the Customer_CreateContactModel and load it up with info on our job
                Type t = bo.GetType().GetMethod("GetNewCustCnt").GetParameters()[0].ParameterType;
                var ds = JsonConvert.DeserializeObject(@"{}", t, new JsonSerializerSettings());
                bo.GetType().GetMethod("GetNewCustCnt").Invoke(bo, new object[] { ds, request.CustNum, request.ShipToNum });

                // Cycle through the passed parameters and update the new ds with those values
                request.Parameters.ForEach(p => {
                    ((DataSet)ds).Tables["CustCnt"].Rows[0][p.Key] = p.Value;
                });

                ((DataSet)ds).Tables["CustCnt"].Rows[0]["RowMod"] = "U";
                // Commit the transaction
                bo.GetType().GetMethod("Update").Invoke(bo, new object[] { ds });

                // Return the response as success if it got this far
                response.Data = JsonConvert.SerializeObject(ds);
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
