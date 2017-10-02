using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace bezlio.rdb.plugins.HelperMethods.Labor
{
    class Labor_ClockIn
    {
        public string Connection { get; set; }
        public string Company { get; set; }
        public List<string> EmployeeNum { get; set; }
        public int Shift { get; set; }

        public Labor_ClockIn() { }
    }

    class Labor_ClockOut
    {
        public string Connection { get; set; }
        public string Company { get; set; }
        public List<string> EmployeeNum { get; set; }

        public Labor_ClockOut() { }
    }

    class Labor_StartActivity
    {
        public string Connection { get; set; }
        public string Company { get; set; }
        public List<int> LaborHedSeq { get; set; }
        public string JobNum { get; set; }
        public int JobAsm { get; set; }
        public int JobOp { get; set; }
        public bool Setup { get; set; }

        public Labor_StartActivity()  { }
    }

    class Labor_StartIndirect
    {
        public string Connection { get; set; }
        public string Company { get; set; }
        public List<int> LaborHedSeq { get; set; }
        public string IndirectCode { get; set; }
        public string WCCode { get; set; }

        public Labor_StartIndirect() { }
    }

    class Labor_EndActivities
    {
        public string Connection { get; set; }
        public string Company { get; set; }
        public string LaborDataSet { get; set; }

        public Labor_EndActivities() { }
    }

    public class LaborHelperMethods
    {
        public static async Task<RemoteDataBrokerResponse> Labor_ClockIn(RemoteDataBrokerRequest rdbRequest)
        {
            // Deserialize the request object
            Labor_ClockIn request = JsonConvert.DeserializeObject<Labor_ClockIn>(rdbRequest.Data);

            // Create the response object
            RemoteDataBrokerResponse response = Common.GetResponseObject(rdbRequest.RequestId, rdbRequest.Compress);

            // Establish a connection to Epicor
            object epicorConn = Common.GetEpicorConnection(request.Connection, request.Company, ref response);

            try
            {
                // Load the referenced BO
                object bo = Common.GetBusinessObject(epicorConn, "Labor", ref response);
                object boEmpBasic = Common.GetBusinessObject(epicorConn, "EmpBasic", ref response);

                // We are going to start by running a GetList of active transactions for that shift so that we only clock the user in once
                // Build out our LaborHed filter
                var laborHedWhere = "ActiveTrans=true and Shift='" + request.Shift + "' and (";
                for (var i=0; i<request.EmployeeNum.Count; i++) {
                    if (i == 0) {
                        laborHedWhere += "EmployeeNum='" + request.EmployeeNum[i] + "'";
                    } else {
                        laborHedWhere += " or EmployeeNum='" + request.EmployeeNum[i] + "'";
                    }
                }

                laborHedWhere += ")";
                var ds = bo.GetType().GetMethod("GetRows").Invoke(bo, new object[] { laborHedWhere, "", "", "", "", "", "", "", "", "", "", "", 0, 1, false });

                // Now we need to clock in anyone that is not currently clocked in
                var mod = false;
                foreach (string emp in request.EmployeeNum) {
                    if (((DataSet)ds).Tables["LaborHed"].AsEnumerable().Where(r => r.Field<string>("EmployeeNum") == emp).Count() == 0) {
                        // This employee isnt clocked in 
                        // So we know that we need to requery
                        mod = true;
                        boEmpBasic.GetType().GetMethod("ClockIn").Invoke(boEmpBasic, new object[] { emp, request.Shift });
                    }
                }

                if (mod == true) {
                    // We had to clock someone in so lets requery before we return
                    ds = bo.GetType().GetMethod("GetRows").Invoke(bo, new object[] { laborHedWhere, "", "", "", "", "", "", "", "", "", "", "", 0, 1, false });
                }

                response.Data = JsonConvert.SerializeObject(ds);
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(ex.InnerException.ToString())) {
                    response.ErrorText += ex.InnerException.ToString();
                }
                response.Error = true;
                response.ErrorText += ex.Message;
            }
            finally { Common.CloseEpicorConnection(epicorConn, ref response); }

            // Return response object
            return response;
        }

        public static async Task<RemoteDataBrokerResponse> Labor_ClockOut(RemoteDataBrokerRequest rdbRequest)
        {
            // Deserialize the request object
            Labor_ClockOut request = JsonConvert.DeserializeObject<Labor_ClockOut>(rdbRequest.Data);

            // Create the response object
            RemoteDataBrokerResponse response = Common.GetResponseObject(rdbRequest.RequestId, rdbRequest.Compress);

            // Establish a connection to Epicor
            object epicorConn = Common.GetEpicorConnection(request.Connection, request.Company, ref response);

            // Load the referenced BO
            object bo = Common.GetBusinessObject(epicorConn, "Labor", ref response);
            object boEmpBasic = Common.GetBusinessObject(epicorConn, "EmpBasic", ref response);
            List<dynamic> clockOutResults = new List<dynamic>();

            foreach (string emp in request.EmployeeNum)
            {
                dynamic clockOutResult = new ExpandoObject();
                clockOutResult.EmployeeNum = emp;

                try
                {
                    // First end any activities this employee might be clocked onto
                    object laborHed = Common.GetBusinessObjectDataSet("Labor", "Epicor.Mfg.BO.LaborHedListDataSet", ref response);
                    laborHed = bo.GetType().GetMethod("GetList").Invoke(bo, new object[] { "ActiveTrans = true and EmployeeNum = '" + emp + "'", 0, 1, false });
                    foreach (DataRow lh in ((DataSet)laborHed).Tables[0].Rows)
                    {
                        // First we need to do a get by ID on the laborHedSeq
                        var ds = bo.GetType().GetMethod("GetByID").Invoke(bo, new object[] { Convert.ToInt32(lh["LaborHedSeq"]) });

                        // Now mark each LaborDtl in this dataset with a RowMod U
                        int laborDtlUpdated = 0;
                        foreach (DataRow dr in ((DataSet)ds).Tables["LaborDtl"].Rows)
                        {
                            if ((bool)dr["ActiveTrans"] == true)
                            {
                                dr["EndActivity"] = true;
                                dr["RowMod"] = "U";
                                laborDtlUpdated += 1;
                            }
                        }

                        if (laborDtlUpdated > 0)
                        {
                            // Now call EndActivity 
                            bo.GetType().GetMethod("EndActivity").Invoke(bo, new object[] { ds });

                            // Update
                            bo.GetType().GetMethod("Update").Invoke(bo, new object[] { ds });
                        }
                    }

                    boEmpBasic.GetType().GetMethod("ClockOut").Invoke(boEmpBasic, new object[] { emp });
                    clockOutResult.Error = false;
                } catch (Exception ex)
                {
                    if (!string.IsNullOrEmpty(ex.InnerException.ToString()))
                    {
                        clockOutResult.ErrorText += ex.InnerException.ToString();
                    }
                    clockOutResult.Error = true;
                    clockOutResult.ErrorText += ex.Message;
                }

                clockOutResults.Add(clockOutResult);
            }

            response.Data = JsonConvert.SerializeObject(clockOutResults);

            Common.CloseEpicorConnection(epicorConn, ref response);

            // Return response object
            return response;
        }

        public static async Task<RemoteDataBrokerResponse> Labor_StartActivity(RemoteDataBrokerRequest rdbRequest)
        {
            // Deserialize the request object
            Labor_StartActivity request = JsonConvert.DeserializeObject<Labor_StartActivity>(rdbRequest.Data);

            // Create the response object
            RemoteDataBrokerResponse response = Common.GetResponseObject(rdbRequest.RequestId, rdbRequest.Compress);

            // Establish a connection to Epicor
            object epicorConn = Common.GetEpicorConnection(request.Connection, request.Company, ref response);

            DataSet returnDs = new DataSet();

            try
            {
                // Load the referenced BO
                object bo = Common.GetBusinessObject(epicorConn, "Labor", ref response);

                foreach (int laborHed in request.LaborHedSeq)
                {
                    // First we need to do a get by ID on the laborHedSeq
                    var ds = bo.GetType().GetMethod("GetByID").Invoke(bo, new object[] { laborHed });

                    // Now end any activities this employee may currently be clocked onto
                    int laborDtlUpdated = 0;
                    foreach (DataRow dr in ((DataSet)ds).Tables["LaborDtl"].Rows)
                    {
                        if ((bool)dr["ActiveTrans"] == true)
                        {
                            dr["EndActivity"] = true;
                            dr["RowMod"] = "U";
                            laborDtlUpdated += 1;
                        }
                    }

                    if (laborDtlUpdated > 0)
                    {
                        // Now call EndActivity 
                        bo.GetType().GetMethod("EndActivity").Invoke(bo, new object[] { ds });

                        // Update
                        bo.GetType().GetMethod("Update").Invoke(bo, new object[] { ds });
                    }

                    // Now put them onto the new job
                    if (request.Setup)
                    {
                        // We are going to cycle through each laborhedseq to start production
                        bo.GetType().GetMethod("StartActivity").Invoke(bo, new object[] { laborHed, "S", ds });
                    }
                    else
                    {
                        // We are going to cycle through each laborhedseq to start production
                        bo.GetType().GetMethod("StartActivity").Invoke(bo, new object[] { laborHed, "P", ds });
                    }


                    // JobNum
                    bo.GetType().GetMethod("DefaultJobNum").Invoke(bo, new object[] { ds, request.JobNum });

                    // JobAsm
                    bo.GetType().GetMethod("DefaultAssemblySeq").Invoke(bo, new object[] { ds, request.JobAsm });

                    // JobOp
                    bo.GetType().GetMethod("DefaultOprSeq").Invoke(bo, new object[] { ds, request.JobOp, "" });

                    // Update
                    bo.GetType().GetMethod("Update").Invoke(bo, new object[] { ds });

                    // Merge the data into our return
                    returnDs.Merge((DataSet)ds);
                }

                response.Data = JsonConvert.SerializeObject(returnDs);
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

        public static async Task<RemoteDataBrokerResponse> Labor_StartIndirect(RemoteDataBrokerRequest rdbRequest)
        {
            // deserialize the request object
            Labor_StartIndirect request = JsonConvert.DeserializeObject<Labor_StartIndirect>(rdbRequest.Data);

            // create the response object
            RemoteDataBrokerResponse response = Common.GetResponseObject(rdbRequest.RequestId, rdbRequest.Compress);

            // connect to epicor
            object epicorConn = Common.GetEpicorConnection(request.Connection, request.Company, ref response);

            DataSet returnDs = new DataSet();

            try
            {
                object bo = Common.GetBusinessObject(epicorConn, "Labor", ref response);

                foreach (int laborHed in request.LaborHedSeq)
                {
                    // First we need to do a get by ID on the laborHedSeq
                    var ds = bo.GetType().GetMethod("GetByID").Invoke(bo, new object[] { laborHed });

                    // Now end any activities this employee may currently be clocked onto
                    int laborDtlUpdated = 0;
                    foreach (DataRow dr in ((DataSet)ds).Tables["LaborDtl"].Rows)
                    {
                        if ((bool)dr["ActiveTrans"] == true)
                        {
                            dr["EndActivity"] = true;
                            dr["RowMod"] = "U";
                            laborDtlUpdated += 1;
                        }
                    }

                    if (laborDtlUpdated > 0)
                    {
                        // Now call EndActivity 
                        bo.GetType().GetMethod("EndActivity").Invoke(bo, new object[] { ds });

                        // Update
                        bo.GetType().GetMethod("Update").Invoke(bo, new object[] { ds });
                    }

                    //start indirect activity
                    bo.GetType().GetMethod("StartActivity").Invoke(bo, new object[] { laborHed, "I", ds });

                    //default indirect code
                    bo.GetType().GetMethod("DefaultIndirect").Invoke(bo, new object[] { ds, request.IndirectCode });

                    //default resource group
                    string vMsg = "";
                    bo.GetType().GetMethod("DefaultWCCode").Invoke(bo, new object[] { ds, request.WCCode, vMsg });

                    //update
                    bo.GetType().GetMethod("Update").Invoke(bo, new object[] { ds });

                    // Merge the data into our return
                    returnDs.Merge((DataSet)ds);
                }

                response.Data = JsonConvert.SerializeObject(returnDs);
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

        public static async Task<RemoteDataBrokerResponse> Labor_EndActivities(RemoteDataBrokerRequest rdbRequest)
        {
            // Deserialize the request object
            Labor_EndActivities request = JsonConvert.DeserializeObject<Labor_EndActivities>(rdbRequest.Data);

            // Create the response object
            RemoteDataBrokerResponse response = Common.GetResponseObject(rdbRequest.RequestId, rdbRequest.Compress);

            // Establish a connection to Epicor
            object epicorConn = Common.GetEpicorConnection(request.Connection, request.Company, ref response);

            DataSet returnDs = new DataSet();

            try
            {
                // Load the referenced BO
                object bo = Common.GetBusinessObject(epicorConn, "Labor", ref response);

                // Deserialize the provided LaborDataSet
                JsonSerializerSettings settings = new JsonSerializerSettings();
                var laborDs = JsonConvert.DeserializeObject(request.LaborDataSet, typeof(DataSet), settings);
                
                // Now loop through each LaborDtl and process them
                foreach (DataRow laborDtl in ((DataSet)laborDs).Tables["LaborDtl"].Rows)
                {
                    // First we need to do a get by ID on the laborHedSeq
                    var ds = bo.GetType().GetMethod("GetByID").Invoke(bo, new object[] { Convert.ToInt32(laborDtl["LaborHedSeq"]) });

                    // Now mark each LaborDtl in this dataset with a RowMod U
                    foreach (DataRow dr in ((DataSet)ds).Tables["LaborDtl"].Rows)
                    {
                        var drParts = ((DataSet)ds).Tables["LaborDtl"].Select("LaborDtlSeq = " + dr["LaborDtlSeq"]);

                        if ((bool)dr["ActiveTrans"] == true) {
                            dr["RowMod"] = "U";                         
                        }
                    }

                    // Now call EndActivity and DefaultLaborQty
                    bo.GetType().GetMethod("EndActivity").Invoke(bo, new object[] { ds });

                    // Now mark each LaborDtl in this dataset with a RowMod U
                    foreach (DataRow dr in ((DataSet)ds).Tables["LaborDtl"].Rows)
                    {
                        var drParts = ((DataSet)ds).Tables["LaborDtl"].Select("LaborDtlSeq = " + dr["LaborDtlSeq"]);

                        if ((bool)dr["ActiveTrans"] == true)
                        {
                            dr["EndActivity"] = true;


                            if (string.IsNullOrEmpty(laborDtl["RequestMove"].ToString()))
                                dr["RequestMove"] = false;
                            else
                                dr["RequestMove"] = laborDtl["RequestMove"];

                            if (drParts.Count() == 0)
                                dr["LaborQty"] = laborDtl["LaborQty"];
                            else
                                dr["LaborQty"] = Convert.ToDecimal(laborDtl["LaborQty"].ToString()) * drParts.Count();

                            // If this is a setup, fill in the setup percentage complete
                            if (dr["LaborType"].ToString() == "S")
                                dr["SetupPctComplete"] = laborDtl["SetupPctComplete"];

                            // If there are LaborPart rows in ds, apply the specified LaborQty to each.
                            // At some point we may wish to add LaborPart support to a Bezl, in which case
                            // we will want to revise this logic
                            foreach (DataRow drPart in ((DataSet)ds).Tables["LaborPart"].Select("LaborDtlSeq = " + dr["LaborDtlSeq"]))
                            {
                                drPart["PartQty"] = laborDtl["LaborQty"];
                                drPart["RowMod"] = "U";
                            }
                        }

                    }

                    // Update
                    bo.GetType().GetMethod("Update").Invoke(bo, new object[] { ds });

                    // Merge the data into our return
                    returnDs.Merge((DataSet)ds);
                }

                response.Data = JsonConvert.SerializeObject(returnDs);
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
