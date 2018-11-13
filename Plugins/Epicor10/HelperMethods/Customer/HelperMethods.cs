using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Data;

namespace bezlio.rdb.plugins.HelperMethods.Customer
{
    class Customer_CreateCustomerModel
    {
        public string Connection { get; set; }
        public string Company { get; set; }
        public string CustDS { get; set; }

        public Customer_CreateCustomerModel() { }
    }

    public class CustomerHelperMethods
    {
        public static async Task<RemoteDataBrokerResponse> Customer_CreateCustomer(RemoteDataBrokerRequest rdbRequest)
        {
            Customer_CreateCustomerModel request = JsonConvert.DeserializeObject<Customer_CreateCustomerModel>(rdbRequest.Data);

            RemoteDataBrokerResponse response = Common.GetResponseObject(rdbRequest.RequestId, rdbRequest.Compress);

            // Establish a connection to Epicor
            object epicorConn = Common.GetEpicorConnection(request.Connection, request.Company, ref response);

            try
            {
                object bo = Common.GetBusinessObject(epicorConn, "Customer", ref response);
                Type t = bo.GetType().GetMethod("GetNewCustomer").GetParameters()[0].ParameterType;
                DataSet newDS = (DataSet)JsonConvert.DeserializeObject("{}", t, new JsonSerializerSettings());
                DataSet custDS = (DataSet)JsonConvert.DeserializeObject(request.CustDS, typeof(DataSet), new JsonSerializerSettings());                

                if(custDS.Tables.IndexOf("Customer") > -1)
                    foreach(DataRow dr in custDS.Tables["Customer"].Rows)
                    {
                        newDS.Clear();

                        bo.GetType().GetMethod("GetNewCustomer").Invoke(bo, new object[] { newDS });

                        //set all of the fields passed, if they exist
                        foreach (DataColumn dc in custDS.Tables["Customer"].Columns)
                            if (newDS.Tables["Customer"].Columns.IndexOf(dc.ColumnName) > -1)
                                try { newDS.Tables["Customer"].Rows[0][dc.ColumnName] = dr[dc.ColumnName]; } //just in case there is a direct cast error with NULLs, which can happen
                                catch { }

                        bo.GetType().GetMethod("GetCustomerTerritory").Invoke(bo, new object[] { newDS, 0 });

                        bo.GetType().GetMethod("Update").Invoke(bo, new object[] { newDS });
                    }

                response.Data = JsonConvert.SerializeObject(newDS);
                
            }
            catch(Exception ex)
            {
                if (!string.IsNullOrEmpty(ex.InnerException.ToString()))
                {
                    response.ErrorText += ex.InnerException.ToString();
                }

                response.Error = true;
                response.ErrorText += ex.Message;
            }
            finally { Common.CloseEpicorConnection(epicorConn, ref response); }

            return response;
        }
    }
}
