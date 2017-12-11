﻿using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

using System.Net;
using System.Xml.Linq;
using System.IO;
using System.Reflection;

using RptExecSvc;
using ReportService;

namespace bezlio.rdb.plugins
{
    //model used to deserialize incoming request
    class SSRSDataModel
    {
        public string FolderName { get; set; }
        public string ReportName { get; set; }
    }

    //main class, crux of work done here
    public class SSRS
    {
        public static ReportingService2010 ssrsSvc;
        public static ReportExecutionService ssrsExec;
        static NetworkCredential creds;

        public SSRS()
        {
            Authenticate();
        }

        public static object GetArgs()
        {
            SSRSDataModel model = new SSRSDataModel();
            model.FolderName = GetFolderNames();
            model.ReportName = "The report filename to run.";

            return model;
        }

        public void Authenticate()
        {
            string cfgPath = getCfgPath();
            XDocument xConfig = XDocument.Load(cfgPath);
            XElement xConnection = xConfig.Descendants("bezlio.plugins.Properties.Settings").Descendants("setting").Where(a => (string)a.Attribute("name") == "ssrsConnections").FirstOrDefault();

            List<SSRSConnectionInfo> ssrsConn = JsonConvert.DeserializeObject<List<SSRSConnectionInfo>>(xConnection.Value);

            ssrsSvc = new ReportingService2010();
            ssrsExec = new ReportExecutionService();
            creds = new NetworkCredential();

            creds.Domain = ssrsConn[0].Domain;
            creds.UserName = ssrsConn[0].UserName;
            creds.Password = ssrsConn[0].Password;

            ssrsSvc.Credentials = creds;
            ssrsExec.Credentials = creds;

            ssrsExec.Url = ssrsConn[0].ExecUrl;
        }

        public static string GetFolderNames()
        {
            string result = "[";
            foreach (var rpt in ssrsSvc.ListChildren("/reports", true))
            {
                if (rpt.TypeName == "Folder")
                {
                    result += (result == "[") ? rpt.Name : "," + rpt.Name;
                }
            }
            result += "]";

            return result;
        }

        public static async Task<RemoteDataBrokerResponse> GetReportList(RemoteDataBrokerRequest rdbRequest)
        {
            SSRSDataModel request = JsonConvert.DeserializeObject<SSRSDataModel>(rdbRequest.Data);

            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.RequestId = rdbRequest.RequestId;
            response.Compress = rdbRequest.Compress;
            response.DataType = "applicationJSON";

            try
            {
                List<dynamic> result = new List<dynamic>();
                if(request.FolderName.IndexOf("/reports") == -1)
                {
                    request.FolderName = "/reports/" + request.FolderName;
                }
                foreach (var rpt in ssrsSvc.ListChildren(request.FolderName, true))
                {
                    result.Add(new
                    {
                        Name = rpt.Name,
                        Type = rpt.TypeName
                    });
                }

                response.Data = JsonConvert.SerializeObject(result);
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

        public static async Task<RemoteDataBrokerResponse> GetReportParameters(RemoteDataBrokerRequest rdbRequest)
        {
            SSRSDataModel request = JsonConvert.DeserializeObject<SSRSDataModel>(rdbRequest.Data);

            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.Compress = true;
            response.RequestId = rdbRequest.RequestId;
            response.DataType = "applicationJSON";

            try
            {
                SSRSReport ssrs = new SSRSReport();

                response.Data = JsonConvert.SerializeObject(ssrs.GetParameters(request.FolderName, request.ReportName));
            }
            catch (Exception ex)
            {
                if (!String.IsNullOrEmpty(ex.Message))
                    response.ErrorText += ex.InnerException;

                response.Error = true;
                response.ErrorText += ex.Message;
            }

            return response;
        }

        public static async Task<RemoteDataBrokerResponse> ReturnAsPDF(RemoteDataBrokerRequest rdbRequest)
        {
            SSRSDataModel request = JsonConvert.DeserializeObject<SSRSDataModel>(rdbRequest.Data);

            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.Compress = true;
            response.RequestId = rdbRequest.RequestId;
            response.DataType = "applicationJSON";

            try
            {
                SSRSReport ssrs = new SSRSReport();

                response.Data = JsonConvert.SerializeObject(ssrs.GetAsPDF(request.FolderName, request.ReportName));
            }
            catch (Exception ex)
            {
                if (!String.IsNullOrEmpty(ex.Message))
                {
                    response.ErrorText += ex.InnerException;
                }

                response.Error = true;
                response.ErrorText += ex.Message;
            }

            return response;
        }

        //any other custom methods
        private static string getCfgPath()
        {
            string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string cfgPath = asmPath + @"\" + "SSRS.dll.config";

            return cfgPath;
        }
    }
}