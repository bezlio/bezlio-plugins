using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bezlio.rdb.plugins.HelperMethods.Materials
{
    class IssueReturnToJob
    {
        public string Connection { get; set; }
        public string Company { get; set; }
        public bool AddMaterials { get; set; }
        public List<IssueReturnToJobTransaction> Transactions { get; set; }
        public string Plant { get; set; }

        public IssueReturnToJob()
        {
            Transactions = new List<IssueReturnToJobTransaction>();
        }
    }

    class IssueReturnToJobTransaction
    {
        public string JobNum { get; set; }
        public int AssemblySeq { get; set; }
        public int MtlSeq { get; set; }
        public string TranType { get; set; }
        public string PartNum { get; set; }
        public decimal TranQty { get; set; }
        public string UOM { get; set; }
        public string FromWarehouseCode { get; set; }
        public string FromBinNum { get; set; }
        public string ToWarehouseCode { get; set; }
        public string ToBinNum { get; set; }
        public string TranReference { get; set; }
        public string Status { get; set; }
        public string Details { get; set; }
    }

    public class MaterialsHelperMethods
    {
        public static async Task<RemoteDataBrokerResponse> Materials_IssueReturnToJob(RemoteDataBrokerRequest rdbRequest)
        {
            // Deserialize the request object
            IssueReturnToJob request = JsonConvert.DeserializeObject<IssueReturnToJob>(rdbRequest.Data);

            // Create the response object
            RemoteDataBrokerResponse response = Common.GetResponseObject(rdbRequest.RequestId, rdbRequest.Compress);

            // Establish a connection to Epicor
            object epicorConn = Common.GetEpicorConnection(request.Connection, request.Company, ref response, request.Plant);

            try
            {
                // Load the business objects
                object issueReturn = Common.GetBusinessObject(epicorConn, "IssueReturn", ref response);
                object jobEntry = Common.GetBusinessObject(epicorConn, "JobEntry", ref response);

                foreach (var t in request.Transactions)
                {
                    try
                    {
                        // If AddMaterials is set, do a GetByID on the job as well
                        if (request.AddMaterials)
                        {
                            var dsJob = jobEntry.GetType().GetMethod("GetByID").Invoke(jobEntry, new object[] { t.JobNum });
                            var mtlExists = ((DataSet)dsJob).Tables["JobMtl"].AsEnumerable().Where(m => m["PartNum"].ToString() == t.PartNum);
                            if (mtlExists.Count() <= 0)
                            {
                                jobEntry.GetType().GetMethod("GetNewJobMtl").Invoke(jobEntry, new object[] { dsJob,
                                    t.JobNum,
                                    t.AssemblySeq
                                });

                                ((DataSet)dsJob).Tables["JobMtl"].AsEnumerable().Last()["PartNum"] = t.PartNum;
                                ((DataSet)dsJob).Tables["JobMtl"].AsEnumerable().Last()["Description"] = t.PartNum;
                                ((DataSet)dsJob).Tables["JobMtl"].AsEnumerable().Last()["QtyPer"] = 1;
                                ((DataSet)dsJob).Tables["JobMtl"].AsEnumerable().Last()["AddedMtl"] = true;
                                t.MtlSeq = (int)((DataSet)dsJob).Tables["JobMtl"].AsEnumerable().Last()["MtlSeq"];

                                jobEntry.GetType().GetMethod("Update").Invoke(jobEntry, new object[] { dsJob });
                            }
                        }

                        // Get the dataset object the BO uses
                        object ds = Common.GetBusinessObjectDataSet("IssueReturn", "Erp.BO.IssueReturnDataSet", ref response);

                        // Call GeNewIssueReturnToJob to put a new row into the ds
                        string outMessage = "";
                        issueReturn.GetType().GetMethod("GetNewIssueReturnToJob").Invoke(issueReturn, new object[] { t.JobNum,
                            t.AssemblySeq,
                            t.TranType,
                            Guid.Parse("00000000-0000-0000-0000-000000000000"),
                            outMessage,
                            ds
                        });

                        // Update the ds with the values for the transaction
                        ((DataSet)ds).Tables["IssueReturn"].Rows[0]["ToJobSeq"] = t.MtlSeq;
                        ((DataSet)ds).Tables["IssueReturn"].Rows[0]["PartNum"] = t.PartNum;
                        ((DataSet)ds).Tables["IssueReturn"].Rows[0]["TranQty"] = t.TranQty;
                        ((DataSet)ds).Tables["IssueReturn"].Rows[0]["UM"] = t.UOM;
                        ((DataSet)ds).Tables["IssueReturn"].Rows[0]["FromWarehouseCode"] = t.FromWarehouseCode;
                        ((DataSet)ds).Tables["IssueReturn"].Rows[0]["FromBinNum"] = t.FromBinNum;
                        ((DataSet)ds).Tables["IssueReturn"].Rows[0]["ToWarehouseCode"] = t.ToWarehouseCode;
                        ((DataSet)ds).Tables["IssueReturn"].Rows[0]["ToBinNum"] = t.ToBinNum;
                        ((DataSet)ds).Tables["IssueReturn"].Rows[0]["ToJobSeqPartNum"] = t.PartNum;
                        ((DataSet)ds).Tables["IssueReturn"].Rows[0]["TranReference"] = t.TranReference;

                        // Pre-Perform the issue
                        bool requiresUserInput = false;
                        issueReturn.GetType().GetMethod("PrePerformMaterialMovement").Invoke(issueReturn, new object[] { ds,
                            requiresUserInput
                        });

                        // Perform the issue
                        string legalNumberMessage = "";
                        string partTranPKs = "";
                        issueReturn.GetType().GetMethod("PerformMaterialMovement").Invoke(issueReturn, new object[] { false,
                            ds,
                            legalNumberMessage,
                            partTranPKs
                        });

                        t.Status = "Success";

                    }
                    catch (Exception ex)
                    {
                        t.Status = "Error";
                        if (!string.IsNullOrEmpty(ex.InnerException.ToString()))
                        {
                            t.Details = ex.InnerException.ToString();
                        }
                        else
                        {
                            t.Details = ex.Message;
                        }
                    }
                }

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

            response.Data = JsonConvert.SerializeObject(request.Transactions);

            // Return response object
            return response;
        }
    }

}
