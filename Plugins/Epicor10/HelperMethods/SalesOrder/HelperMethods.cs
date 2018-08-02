using Newtonsoft.Json;
using System;
using System.Data;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Dynamic;

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

    public class SalesOrderHelperMethods
    {
        public static void writeLog(string log)
        {
            /*System.IO.StreamWriter sw = new System.IO.StreamWriter(@"c:\test\logs.txt", true);

            sw.WriteLine(DateTime.Now.ToString() + " - " + log);

            sw.Flush();
            sw.Close();*/
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
            writeLog("setting default companies");
            dynamic data = new List<dynamic>();

            foreach (string strComp in companyList)
            {
                writeLog("In for each " + strComp);

                // Establish a connection to Epicor
                object epicorConn = Common.GetEpicorConnection(request.Connection, strComp, ref response);
                writeLog("Got Epicor connection: " + epicorConn.ToString());                

                try
                {
                    foreach (var order in request.Orders)
                    {
                        dynamic orderReturn = new ExpandoObject();

                        try
                        {
                            writeLog("Deserialize original order");
                            //check and make sure that the company on the order matches the company we're working with
                            DataSet ds = (DataSet)JsonConvert.DeserializeObject(order, typeof(DataSet), new JsonSerializerSettings());

                            writeLog("Checking to ensure that that the companies match");
                            if (ds.Tables["OrderHed"].Columns.Contains("Company") && (ds.Tables["OrderHed"].Rows[0]["Company"].ToString() != strComp && ds.Tables["OrderHed"].Rows[0]["Company"].ToString().Trim() != "" && strComp != request.Company))
                                continue; //if the company ID on the sales order exists, and it doesn't match the current company, AND it's not blank AND we're nto working on the default company, so skip

                            // Load the referenced BO
                            writeLog("Getting Sales Order BO");

                            object bo = null;
                            object partBO = null;


                            try
                            {
                                bo = Common.GetBusinessObject(epicorConn, "SalesOrder", ref response);
                            }
                            catch (Exception ex)
                            {
                                response.Error = true;
                                response.ErrorText += ex.ToString();
                                writeLog(ex.ToString());
                            }
                            writeLog("Getting Part BO");
                            try
                            {
                                partBO = Common.GetBusinessObject(epicorConn, "Part", ref response);
                            }
                            catch (Exception ex)
                            {
                                response.Error = true;
                                response.ErrorText += ex.ToString();
                                writeLog(ex.ToString());
                            }

                            writeLog("Getting New Order Header Type");
                            Type t = bo.GetType().GetMethod("GetNewOrderHed").GetParameters()[0].ParameterType;

                            JsonSerializerSettings settings = new JsonSerializerSettings();
                            DataSet dss = new DataSet();

                            writeLog("New Order Defined");
                            DataSet newOrder = (DataSet)JsonConvert.DeserializeObject("{}", t, settings);

                            // Call get new order hed
                            writeLog("Getting New Order Header");
                            bo.GetType().GetMethod("GetNewOrderHed").Invoke(bo, new object[] { newOrder });

                            // Now we set the CustNum
                            newOrder.Tables["OrderHed"].Rows[0]["CustNum"] = ds.Tables["OrderHed"].Rows[0]["CustNum"];
                            newOrder.Tables["OrderHed"].Rows[0]["BTCustNum"] = ds.Tables["OrderHed"].Rows[0]["CustNum"];
                            // Invoke the ChangeCustomer method
                            writeLog("Change Customer");
                            bo.GetType().GetMethod("ChangeCustomer").Invoke(bo, new object[] { newOrder });

                            //see if we are using a one time ship to (we probably are)
                            /*if (ds.Tables["OrderHed"].Columns.Contains("UseOTS") && (bool)ds.Tables["OrdereHed"].Rows[0]["UseOTS"])
                            {
                                newOrder.Tables["OrderHed"].Rows[0]["UseOTS"] = true;
                                bo.GetType().GetMethod("ChangeHedUseOTS").Invoke(bo, new object[] { newOrder });
                            }*/

                            //fill in the rest of the information passed in the dataset
                            writeLog("Writing Additional fields");
                            foreach (DataColumn dc in ds.Tables["OrderHed"].Columns)
                                if (newOrder.Tables["OrderHed"].Columns.Contains(dc.ColumnName))
                                    try { newOrder.Tables["OrderHed"].Rows[0][dc.ColumnName] = ds.Tables["OrderHed"].Rows[0][dc.ColumnName]; }
                                    catch { }

                            writeLog("Updating Sales Order");
                            //we should now be able to save the sales order
                            bo.GetType().GetMethod("Update").Invoke(bo, new object[] { newOrder });


                            writeLog("Add Order Details");
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
                        response.ErrorText += ex.ToString();// ex.InnerException.ToString();
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
