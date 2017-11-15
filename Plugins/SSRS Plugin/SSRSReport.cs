using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;

using ReportService;
using RptExecSvc;

namespace bezlio.rdb.plugins
{
    class SSRSReportParameters
    {
        public string name { get; set; }
        public string type { get; set; }
    }
    class SSRSReport
    {
        
        public object GetParameters(string folderPath, string reportName)
        {
            string reportPath = folderPath + "/" + reportName;
            string historyID = null;

            ExecutionInfo execInfo = new ExecutionInfo();
            ExecutionHeader execHeader = new ExecutionHeader();

            SSRS.ssrsExec.ExecutionHeaderValue = execHeader;

            execInfo = SSRS.ssrsExec.LoadReport(reportPath, historyID);

            if (execInfo.ParametersRequired)
            {
                var x = execInfo.Parameters;
            }

            object parameters = execInfo.Parameters.Select(param => new SSRSReportParameters
            {
                name = param.Name,
                type = param.ToString()
            });

            return parameters;
        }

        public byte[] GetAsPDF(string folderPath, string reportName)
        {
            byte[] result = null;

            string reportPath = folderPath + "/" + reportName;
            string format = "HTML4.0";
            string historyID = null;
            string devInfo = @"<DeviceInfo><Toolbar>False</Toolbar></DeviceInfo>";

            string encoding;
            string mimeType;
            string extension;
            RptExecSvc.Warning[] warnings = null;
            string[] streamIDs = null;

            ExecutionInfo execInfo = new ExecutionInfo();
            ExecutionHeader execHeader = new ExecutionHeader();

            SSRS.ssrsExec.ExecutionHeaderValue = execHeader;

            RptExecSvc.Extension[] exts = SSRS.ssrsExec.ListRenderingExtensions();
            execInfo = SSRS.ssrsExec.LoadReport(reportPath, historyID);

            if (execInfo.ParametersRequired)
            {
                var x = execInfo.Parameters;
            }

            string SessionId = SSRS.ssrsExec.ExecutionHeaderValue.ExecutionID;

            //Console.WriteLine("SessionID: {0}", ssrsExec.ExecutionHeaderValue.ExecutionID);

            //try
            //{
            result = SSRS.ssrsExec.Render(format, devInfo, out extension, out mimeType, out encoding, out warnings, out streamIDs);

            //return result;
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.Message);
            //}

            //string fileName = @"C:\Users\rschn\documents\samplereport.pdf";

            //using (FileStream stream = File.OpenWrite(fileName))
            //{
            //    stream.Write(result, 0, result.Length);
            //    //Console.WriteLine("Report done");
            //}

            //SSRS.ssrsExec.Dispose();

            //byte[] b = File.ReadAllBytes(fileName);
            //System.IO.File.Delete(fileName);
            //return b;
            //            }
            //        }
            //    }
            //}

            return result;
        }

        public System.Net.ICredentials NetworkCredentials
        {
            get
            {
                return new NetworkCredential("administrator", "d7cGydCd014lfKHwjuuz", "saberlogicllc");
            }
        }
    }
}
