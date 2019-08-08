using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using Newtonsoft.Json;

namespace bezlio.rdb.plugins {
    class FileSystemDynamicDataModel {
        public string FileName { get; set; }
    }

    public class FileSystemDynamic {
        public static object GetArgs() {
            FileSystemDynamicDataModel model = new FileSystemDynamicDataModel();
            return model;
        }

        public static async Task<RemoteDataBrokerResponse> GetFile(RemoteDataBrokerRequest rdbRequest) {
            FileSystemDynamicDataModel request = JsonConvert.DeserializeObject<FileSystemDynamicDataModel>(rdbRequest.Data);

            // Declare the response object
            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.Compress = rdbRequest.Compress;
            response.RequestId = rdbRequest.RequestId;
            response.DataType = "applicationJSON";

            try {
                //if (Directory.GetAccessControl(request.FileName.Split('\\').Take(request.FileName.Count(c => c == '\\') - 1).Aggregate((a, b) => a + b)) == null)
                    //throw new UnauthorizedAccessException("User does not have access to directory at location. Please use full and exact paths and file names. \n" + request.FileName);
                //if (!File.Exists(request.FileName))
                    //throw new FileNotFoundException("File at location not found. Please use full and exact paths and file names. \n" + request.FileName);
                // Return the data table
                response.Data = JsonConvert.SerializeObject(File.ReadAllBytes(request.FileName));

                //WriteDebugLog("Response created");
            }
            catch (Exception ex) {
                response.Error = true;
                response.ErrorText = Environment.MachineName + ": " + ex.Message;
            }

            // Return our response
            return response;
        }
    }
}