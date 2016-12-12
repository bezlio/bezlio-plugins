using Newtonsoft.Json;
using System;
using System.Data;
using System.Threading.Tasks;

namespace bezlio.rdb.plugins.HelperMethods.JobAdjustment
{
    class JobAdjustment_LaborAdjModel
    {
        public string Connection { get; set; }
        public string Company { get; set; }
        public string JobNum { get; set; }
        public int AssemblySeq { get; set; }
        public int OprSeq { get; set; }
        public string EmployeeNum { get; set; }
        public decimal LaborQty { get; set; }
        public decimal LaborHrs { get; set; }
        public bool Complete { get; set; }
        public bool OpComplete { get; set; }

        public JobAdjustment_LaborAdjModel()  { }
    }
    
    public class JobAdjustmentHelperMethods
    {
        public static async Task<RemoteDataBrokerResponse> JobAdjustment_LaborAdj(RemoteDataBrokerRequest rdbRequest)
        {
            // Deserialize the request object
            JobAdjustment_LaborAdjModel request = JsonConvert.DeserializeObject<JobAdjustment_LaborAdjModel>(rdbRequest.Data);

            // Create the response object
            RemoteDataBrokerResponse response = Common.GetResponseObject(rdbRequest.RequestId, rdbRequest.Compress);

            // Establish a connection to Epicor
            object epicorConn = Common.GetEpicorConnection(request.Connection, request.Company, ref response);

            try
            {
                // Load the referenced BO
                object bo = Common.GetBusinessObject(epicorConn, "JobAdjustment", ref response);

                // Create an instance of the JobAdjustmentDataSet and load it up with info on our job
                Type t = bo.GetType().GetMethod("StartAdjustments").GetParameters()[0].ParameterType;
                var ds = JsonConvert.DeserializeObject(@"{ ""Jobs"": [ 
                                                            { 
                                                                ""Company"": """ + request.Company + @"""
                                                              , ""JobNum"": """ + request.JobNum + @"""
                                                              , ""RowMod"": ""A"" 
                                                            }  
                                                           ]}", t, new JsonSerializerSettings());

                bo.GetType().GetMethod("StartAdjustments").Invoke(bo, new object[] { ds });

                // Now ds has been updated with a JALaborDtl row for us to fill in and commit
                ((DataSet)ds).Tables["JALaborDtl"].Rows[0]["AssemblySeq"] = request.AssemblySeq;
                ((DataSet)ds).Tables["JALaborDtl"].Rows[0]["OprSeq"] = request.OprSeq;
                ((DataSet)ds).Tables["JALaborDtl"].Rows[0]["EmployeeNum"] = request.EmployeeNum;
                ((DataSet)ds).Tables["JALaborDtl"].Rows[0]["LaborQty"] = request.LaborQty;
                ((DataSet)ds).Tables["JALaborDtl"].Rows[0]["LaborHrs"] = request.LaborHrs;
                ((DataSet)ds).Tables["JALaborDtl"].Rows[0]["Complete"] = request.Complete;
                ((DataSet)ds).Tables["JALaborDtl"].Rows[0]["OpComplete"] = request.OpComplete;

                // Commit the adjustment
                bo.GetType().GetMethod("CommitLaborAdj").Invoke(bo, new object[] { ds });

                // Return the response as success if it got this far
                response.Data = JsonConvert.SerializeObject("Update Successful");
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
