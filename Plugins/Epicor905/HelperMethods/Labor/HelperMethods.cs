﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
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

        public Labor_ClockIn()  { }
    }

    class Labor_StartActivity
    {
        public string Connection { get; set; }
        public string Company { get; set; }
        public List<int> LaborHedSeq { get; set; }
        public string JobNum { get; set; }
        public int JobAsm { get; set; }
        public int JobOp { get; set; }

        public Labor_StartActivity()  { }
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
                response.ErrorText = ex.Message;
            }
            finally { Common.CloseEpicorConnection(epicorConn, ref response); }

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


                foreach (int laborHed in request.LaborHedSeq) {
                    // First we need to do a get by ID on the laborHedSeq
                    var ds = bo.GetType().GetMethod("GetByID").Invoke(bo, new object[] { laborHed });

                    // We are going to cycle through each laborhedseq to start production
                    bo.GetType().GetMethod("StartActivity").Invoke(bo, new object[] { laborHed, "P", ds });

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
                if (!string.IsNullOrEmpty(ex.InnerException.ToString())) {
                    response.ErrorText += ex.InnerException.ToString();
                }
                response.Error = true;
                response.ErrorText = ex.Message;
            }
            finally { Common.CloseEpicorConnection(epicorConn, ref response); }

            // Return response object
            return response;
        }
    }
}
