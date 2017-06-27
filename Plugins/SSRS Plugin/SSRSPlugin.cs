using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using SSRS_Plugin.ReportService2010;

namespace bezlio.rdb.plugins
{
    //model used to deserialize incoming request
    class SSRSDataModel
    {
        public string FolderName { get; set; }
        public string ReportName { get; set; }
    }

    //main class, crux of work done here
    public class SSRS //when renaming, your DLL name must match this  
    {
        public static object GetArgs()
        {
            SSRSDataModel model = new SSRSDataModel();

            model.FolderName = GetFolderNames();

            object x = new object();

            return x;
        }

        public static string GetFolderNames()
        {
            ReportingService2010 ssrsService = new ReportingService2010();


            var result = "[";

            return result;
        }             

        public static async Task<RemoteDataBrokerResponse> GetData(RemoteDataBrokerRequest rdbRequest)
        {
            RemoteDataBrokerRequest request = JsonConvert.DeserializeObject<RemoteDataBrokerRequest>(rdbRequest.Data);

            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.RequestId = rdbRequest.RequestId;
            response.Compress = rdbRequest.Compress;
            response.DataType = "applicationJSON";

            try
            {
                //logic here - ability to interact with any .NET compatible libraries, or other third party applications

                response.Data = JsonConvert.SerializeObject("Complete");
            }
            catch (Exception ex) //catch any errors
            {
                if (!string.IsNullOrEmpty(ex.InnerException.ToString()))
                    response.ErrorText += ex.InnerException.ToString();

                response.Error = true;
                response.ErrorText = ex.Message;
            }

            return response;
        }

        //any other custom methods
    }
}
