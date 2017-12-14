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

            string SessionId = SSRS.ssrsExec.ExecutionHeaderValue.ExecutionID;

            result = SSRS.ssrsExec.Render(format, devInfo, out extension, out mimeType, out encoding, out warnings, out streamIDs);

            return result;
        }
    }
}
