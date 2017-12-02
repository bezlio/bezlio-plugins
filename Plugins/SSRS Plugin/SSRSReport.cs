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
    class SSRSReport
    {
        public byte[] GetAsPDF()
        {
            ReportingService2010 ssrsService = new ReportingService2010();
            System.Net.NetworkCredential cred = new System.Net.NetworkCredential();
            cred.Domain = "saberlogicllc";
            cred.UserName = "administrator";
            cred.Password = "d7cGydCd014lfKHwjuuz";
            byte[] result = null;

            ssrsService.Credentials = NetworkCredentials;

            //foreach (var x in ssrsService.ListChildren("/", true))
            //{
            //    if (x.Name == "CustomReports")
            //    {
            //        foreach (var rpt in ssrsService.ListChildren(x.Path, true))
            //        {
            //            if (rpt.Name == "OrdersByState")
            //            {
            ReportExecutionService ssrsExec = new ReportExecutionService();
            ssrsExec.Credentials = cred;
            ssrsExec.Url = "http://dev-sql2014/reportserver/ReportExecution2005.asmx";

            string reportPath = @"/reports/CustomReports/OrdersByState";
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

            ssrsExec.ExecutionHeaderValue = execHeader;

            RptExecSvc.Extension[] exts = ssrsExec.ListRenderingExtensions();
            execInfo = ssrsExec.LoadReport(reportPath, historyID);

            String SessionId = ssrsExec.ExecutionHeaderValue.ExecutionID;

            //Console.WriteLine("SessionID: {0}", ssrsExec.ExecutionHeaderValue.ExecutionID);

            //try
            //{
            result = ssrsExec.Render(format, devInfo, out extension, out mimeType, out encoding, out warnings, out streamIDs);

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

            ssrsExec.Dispose();

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
