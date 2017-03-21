using Newtonsoft.Json;
using System;
using System.Data;
using System.Reflection;
using System.Threading.Tasks;

namespace bezlio.rdb.plugins.HelperMethods.SalesOrder
{
    class SalesOrder_NewOrderByCustomerModel
    {
        public string Connection { get; set; }
        public string Company { get; set; }
        public string CustNum { get; set; }

        public SalesOrder_NewOrderByCustomerModel()  { }
    }

    class SalesOrder_SubmitNewOrderModel
    {
        public string Connection { get; set; }
        public string Company { get; set; }
        public string ds { get; set; }

        public SalesOrder_SubmitNewOrderModel()  { }
    }

    public class SalesOrderHelperMethods
    {

        public static async Task<RemoteDataBrokerResponse> SalesOrder_NewOrderByCustomer(RemoteDataBrokerRequest rdbRequest)
        {
            // This is a helper to run new GetNewOrderHed, change the customer, run ChangeCustomer then return the ds
            // Deserialize the request object
            SalesOrder_NewOrderByCustomerModel request = JsonConvert.DeserializeObject<SalesOrder_NewOrderByCustomerModel>(rdbRequest.Data);

            // Create the response object
            RemoteDataBrokerResponse response = Common.GetResponseObject(rdbRequest.RequestId, rdbRequest.Compress);

            // Establish a connection to Epicor
            object epicorConn = Common.GetEpicorConnection(request.Connection, request.Company, ref response);
            
            try
            {
                // Load the referenced BO
                object bo = Common.GetBusinessObject(epicorConn, "SalesOrder", ref response);          
                Type t = bo.GetType().GetMethod("GetNewOrderHed").GetParameters()[0].ParameterType;
                // Setup a blank dataset of the orderdataset type returned from reflection
                var ds = JsonConvert.DeserializeObject("{}", t, new JsonSerializerSettings());
                // Call get new order hed
                bo.GetType().GetMethod("GetNewOrderHed").Invoke(bo, new object[] { ds });
                // Now we set the CustNum
                ((DataSet)ds).Tables["OrderHed"].Rows[0]["CustNum"] = request.CustNum;
                // Invoke the ChangeCustomer method
                bo.GetType().GetMethod("ChangeCustomer").Invoke(bo, new object[] { ds });
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

        public static async Task<RemoteDataBrokerResponse> SalesOrder_SubmitNewOrder(RemoteDataBrokerRequest rdbRequest)
        {
            // This is a helper to run new GetNewOrderHed, change the customer, run ChangeCustomer then return the ds
            // Deserialize the request object
            SalesOrder_SubmitNewOrderModel request = JsonConvert.DeserializeObject<SalesOrder_SubmitNewOrderModel>(rdbRequest.Data);

            // Create the response object
            RemoteDataBrokerResponse response = Common.GetResponseObject(rdbRequest.RequestId, rdbRequest.Compress);

            // Establish a connection to Epicor
            object epicorConn = Common.GetEpicorConnection(request.Connection, request.Company, ref response);
            
            try
            {
                // Load the referenced BO
                object bo = Common.GetBusinessObject(epicorConn, "SalesOrder", ref response);     
                // Serialize the passed object into the order data type
                Type t = bo.GetType().GetMethod("UpdateExt").GetParameters()[0].ParameterType;
                var ds = JsonConvert.DeserializeObject(request.ds, t, new JsonSerializerSettings());
                                
                // Call to submit the order
                var ret = bo.GetType().GetMethod("UpdateExt").Invoke(bo, new object[] { ds, true, false, false });
                // Return the response as success if it got this far
                response.Data = JsonConvert.SerializeObject(ret);

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
