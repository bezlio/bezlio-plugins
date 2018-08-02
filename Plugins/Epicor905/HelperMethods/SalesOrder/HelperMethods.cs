using Newtonsoft.Json;
using System;
using System.Data;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using Westwind.wwScripting;

namespace bezlio.rdb.plugins.HelperMethods.SalesOrder
{
    class SalesOrder_NewOrderByCustomerModel
    {
        public string Connection { get; set; }
        public string Company { get; set; }
        public string CustNum { get; set; }

        public SalesOrder_NewOrderByCustomerModel() { }
    }

    class SalesOrder_SubmitNewOrderModel
    {
        public string Connection { get; set; }
        public string Company { get; set; }
        public string ds { get; set; }

        public SalesOrder_SubmitNewOrderModel() { }
    }

    public class SalesOrder_MassUpdateModel
    {
        public string Connection { get; set; }
        public string Company { get; set; }
        public List<string> Orders { get; set; }

        public SalesOrder_MassUpdateModel()
        {
            this.Orders = new List<string>();
        }
    }

    public class SalesOrder_DynamicCodeModel
    {
        public string code;
        public string method;
        public List<KeyValuePair<string, string>> Parameters { get; set; }

        public SalesOrder_DynamicCodeModel()
        {
            Parameters = new List<KeyValuePair<string, string>>();
        }
    }

    public class SalesOrderHelperMethods
    {
        public static async Task<RemoteDataBrokerResponse> SalesOrder_DynamicCode(RemoteDataBrokerRequest rdbRequest)
        {
            // Deserialize the request object
            SalesOrder_DynamicCodeModel request = JsonConvert.DeserializeObject<SalesOrder_DynamicCodeModel>(rdbRequest.Data);

            // Create the response object
            RemoteDataBrokerResponse response = Common.GetResponseObject(rdbRequest.RequestId, rdbRequest.Compress);

            string code = request.code;

            var loScript = new wwScripting("CSharp");

            loScript.AddNamespace("System.IO");            

            var lcResult = loScript.ExecuteMethod(code, request.method, request.Parameters);

            response.Data = JsonConvert.SerializeObject(lcResult);

            //loScript.CallMethod(null)

            return response;
        }

        public string Testing(object[] Parameters){string writeInfo = Parameters[2] as string;long xInt = (long)Parameters[3];            string retValue = Parameters[1].GetType().ToString();			bezlio.rdb.RemoteDataBrokerResponse response = Parameters[1] as bezlio.rdb.RemoteDataBrokerResponse;            try            {                System.IO.StreamWriter sw = new System.IO.StreamWriter(@"C:\temp\dynamicTest.txt", false);                sw.WriteLine(writeInfo);				sw.WriteLine(xInt.ToString());                sw.Flush();                sw.Close();            }            catch(Exception ex)            {                retValue = ex.ToString();            }			finally{				retValue = Assembly.GetExecutingAssembly().Location;			}            return retValue;}

        public string writeFile(string writeInfo)
        {
            string retValue = "success";


            try
            {
                System.IO.StreamWriter sw = new System.IO.StreamWriter(@"C:\temp\dynamicTest.txt", false);

                sw.WriteLine(writeInfo);

                sw.Flush();
                sw.Close();
            }
            catch(Exception ex)
            {
                retValue = ex.ToString();
            }

            return retValue;
        }

        public static async Task<RemoteDataBrokerResponse> SalesOrder_MassUpdate(RemoteDataBrokerRequest rdbRequest)
        {
            // This is a helper to run new GetNewOrderHed, change the customer, run ChangeCustomer then return the ds
            // Deserialize the request object
            SalesOrder_MassUpdateModel request = JsonConvert.DeserializeObject<SalesOrder_MassUpdateModel>(rdbRequest.Data);

            // Create the response object
            RemoteDataBrokerResponse response = Common.GetResponseObject(rdbRequest.RequestId, rdbRequest.Compress);

            //there may be more than one company being updated, so lets determine the unique list of companies, if available
            List<string> companyList = new List<string>();
            foreach (var order in request.Orders)
            {
                DataSet ds = (DataSet)JsonConvert.DeserializeObject(order, typeof(DataSet), new JsonSerializerSettings());

                if (ds.Tables["OrderHed"].Columns.Contains("Company"))
                    if (ds.Tables["OrderHed"].Rows[0]["Company"].ToString().Trim().Length > 0 && !companyList.Contains(ds.Tables["OrderHed"].Rows[0]["Company"] as string))
                        companyList.Add(ds.Tables["OrderHed"].Rows[0]["Company"] as string);
            }

            if (companyList.Count == 0)
                companyList.Add(request.Company); //use the default company            

            dynamic data = new List<dynamic>();

            foreach (string strComp in companyList)
            {

                // Establish a connection to Epicor
                object epicorConn = Common.GetEpicorConnection(request.Connection, strComp, ref response);

                try
                {
                    foreach (var order in request.Orders)
                    {
                        dynamic orderReturn = new ExpandoObject();

                        try
                        {
                            //check and make sure that the company on the order matches the company we're working with
                            DataSet ds = (DataSet)JsonConvert.DeserializeObject(order, typeof(DataSet), new JsonSerializerSettings());

                            if (ds.Tables["OrderHed"].Columns.Contains("Company") && (ds.Tables["OrderHed"].Rows[0]["Company"].ToString() != strComp && ds.Tables["OrderHed"].Rows[0]["Company"].ToString().Trim() != "" && strComp != request.Company))
                                continue; //if the company ID on the sales order exists, and it doesn't match the current company, AND it's not blank AND we're nto working on the default company, so skip

                            // Load the referenced BO
                            object bo = Common.GetBusinessObject(epicorConn, "SalesOrder", ref response);
                            object partBO = Common.GetBusinessObject(epicorConn, "Part", ref response);

                            Type t = bo.GetType().GetMethod("GetNewOrderHed").GetParameters()[0].ParameterType;

                            JsonSerializerSettings settings = new JsonSerializerSettings();
                            DataSet dss = new DataSet();

                            DataSet newOrder = (DataSet)JsonConvert.DeserializeObject("{}", t, settings);                                                        

                            // Call get new order hed
                            bo.GetType().GetMethod("GetNewOrderHed").Invoke(bo, new object[] { newOrder });                            

                            // Now we set the CustNum
                            newOrder.Tables["OrderHed"].Rows[0]["CustNum"] = ds.Tables["OrderHed"].Rows[0]["CustNum"];
                            newOrder.Tables["OrderHed"].Rows[0]["BTCustNum"] = ds.Tables["OrderHed"].Rows[0]["CustNum"];
                            // Invoke the ChangeCustomer method
                            bo.GetType().GetMethod("ChangeCustomer").Invoke(bo, new object[] { newOrder });                            

                            //fill in the rest of the information passed in the dataset
                            foreach (DataColumn dc in ds.Tables["OrderHed"].Columns)
                                if (newOrder.Tables["OrderHed"].Columns.Contains(dc.ColumnName))
                                    try { newOrder.Tables["OrderHed"].Rows[0][dc.ColumnName] = ds.Tables["OrderHed"].Rows[0][dc.ColumnName]; }
                                    catch { }                            

                            //we should now be able to save the sales order
                            bo.GetType().GetMethod("Update").Invoke(bo, new object[] { newOrder });

                            //now, it is time to add in the Order details
                            foreach (DataRow orderLine in ds.Tables["OrderDtl"].Rows)
                            {
                                bo.GetType().GetMethod("GetNewOrderDtl").Invoke(bo, new object[] { newOrder, (int)newOrder.Tables["OrderHed"].Rows[0]["OrderNum"] });

                                //set the part number
                                foreach (DataRow dr in newOrder.Tables["OrderDtl"].Select("RowMod = 'A'"))
                                    dr["PartNum"] = orderLine["PartNum"];

                                //change the part number
                                bo.GetType().GetMethod("ChangePartNum").Invoke(bo, new object[] { newOrder, false, "" });                                

                                //set the selling qty
                                foreach (DataRow dr in newOrder.Tables["OrderDtl"].Select("RowMod = 'A'"))
                                {
                                    dr["SellingQuantity"] = orderLine["SellingQuantity"];

                                    //change the selling quantity
                                    bo.GetType().GetMethod("ChangeSellingQuantity").Invoke(bo, new object[] { newOrder, true, decimal.Parse(dr["SellingQuantity"].ToString()), "" });
                                }

                                //copy the rest of the fields over
                                foreach (DataRow dr in newOrder.Tables["OrderDtl"].Select("RowMod = 'A'"))
                                    foreach (DataColumn dc in orderLine.Table.Columns)
                                        if (dr.Table.Columns.IndexOf(dc.ColumnName) > -1)
                                            try { dr[dc.ColumnName] = orderLine[dc.ColumnName]; }
                                            catch { }

                                //save the new line
                                bo.GetType().GetMethod("Update").Invoke(bo, new object[] { newOrder });

                                //check and see if the added line requires us to get kit details
                                string partSearchString = "PartNum = '" + newOrder.Tables["OrderDtl"].Rows[newOrder.Tables["OrderDtl"].Rows.Count - 1]["PartNum"].ToString().Replace("'", "''") + "' AND TypeCode = 'K'";
                                DataSet partSearch = (DataSet)partBO.GetType().GetMethod("GetList").Invoke(partBO, new object[] { partSearchString, 0, 1, false });

                                //if we have a return value, then this is a kit part, and it should be expanded
                                if (partSearch.Tables[0].Rows.Count > 0)
                                {
                                    bo.GetType().GetMethod("GetKitComponents").Invoke(bo, new object[]
                                    {
                                newOrder.Tables["OrderDtl"].Rows[newOrder.Tables["OrderDtl"].Rows.Count - 1]["PartNum"] as string, //part number
                                newOrder.Tables["OrderDtl"].Rows[newOrder.Tables["OrderDtl"].Rows.Count - 1]["RevisionNum"] as string, //revision      
                                "", //alt revision
                                0, //target assembly
                                (int)newOrder.Tables["OrderDtl"].Rows[newOrder.Tables["OrderDtl"].Rows.Count - 1]["OrderNum"], //order num
                                (int)newOrder.Tables["OrderDtl"].Rows[newOrder.Tables["OrderDtl"].Rows.Count - 1]["OrderLine"], //order line
                                true,
                                false,
                                "",
                                0,
                                newOrder
                                    });
                                }

                            }

                            //lastly, add in the order header order misc records, if there are any
                            if (ds.Tables.Contains("OHOrderMsc"))
                            {
                                foreach (DataRow ohMisc in ds.Tables["OHOrderMsc"].Rows)
                                {
                                    bo.GetType().GetMethod("GetNewOHOrderMsc").Invoke(bo, new object[] {
                                newOrder,
                                (int)newOrder.Tables["OrderHed"].Rows[0]["OrderNum"],
                                0
                            });

                                    //set the misc code
                                    foreach (DataRow newRow in newOrder.Tables["OHOrderMsc"].Select("RowMod = 'A'"))
                                        newRow["MiscCode"] = ohMisc["MiscCode"];

                                    //change the misc code
                                    bo.GetType().GetMethod("ChangeMiscCode").Invoke(bo, new object[] { newOrder, "OHOrderMsc" });

                                    //set the rest of the fields
                                    foreach (DataRow newRow in newOrder.Tables["OHOrderMsc"].Select("RowMod = 'A'"))
                                        foreach (DataColumn dc in ohMisc.Table.Columns)
                                            if (newRow.Table.Columns.IndexOf(dc.ColumnName) > -1)
                                                try { newRow[dc.ColumnName] = ohMisc[dc.ColumnName]; }
                                                catch { }


                                    //finally, update the sales order
                                    bo.GetType().GetMethod("Update").Invoke(bo, new object[] { newOrder });
                                }
                            }


                            orderReturn.Order = newOrder;
                            orderReturn.IsError = false;
                            orderReturn.Errors = "";
                        }
                        catch (Exception ex)
                        {
                            orderReturn.Order = null;
                            orderReturn.IsError = true;
                            orderReturn.Errors = ex.ToString();
                        }

                        /*if (((DataSet)myReturn).Tables["BOUpdError"].Rows.Count > 0)
                        {
                            orderReturn.IsError = true;
                            response.Error = true;
                            orderReturn.Errors = ((DataSet)myReturn).Tables["BOUpdError"].Rows[0]["ErrorText"].ToString();
                            response.ErrorText += orderReturn.Errors + "<br />";
                        }*/

                        data.Add(orderReturn);
                    }

                    response.Data = JsonConvert.SerializeObject(data);

                    /*// Load the referenced BO
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
                    response.Data = JsonConvert.SerializeObject(ds);*/
                }
                catch (Exception ex)
                {
                    if (!string.IsNullOrEmpty(ex.InnerException.ToString()))
                    {
                        response.ErrorText += ex.InnerException.ToString();
                    }

                    response.Error = true;
                    response.ErrorText += ex.ToString();
                }
                finally { Common.CloseEpicorConnection(epicorConn, ref response); }

            }
            // Return response object
            return response;
        }

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
                Type t = bo.GetType().GetMethod("SubmitNewOrder").GetParameters()[0].ParameterType;

                // Serialize the passed object into the order data type
                var ds = JsonConvert.DeserializeObject(request.ds, t, new JsonSerializerSettings());
                // Call to submit the order
                bo.GetType().GetMethod("SubmitNewOrder").Invoke(bo, new object[] { ds });
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
