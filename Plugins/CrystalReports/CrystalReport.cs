using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;

namespace bezlio.rdb.plugins
{
    class CrystalReport
    {
        public CrystalReport(string rptPath)
        {
            // Load up the assemblies needed
            this.EngineAssembly = Assembly.LoadFrom(GACUtils.GetAssemblyPath("CrystalDecisions.CrystalReports.Engine"));
            this.SharedAssembly = Assembly.LoadFrom(GACUtils.GetAssemblyPath("CrystalDecisions.Shared"));

            // Create a new instance of the ReportDocument
            Type reportDocumentType = this.EngineAssembly.GetType("CrystalDecisions.CrystalReports.Engine.ReportDocument");
            ConstructorInfo reportDocumentConstructor = reportDocumentType.GetConstructor(Type.EmptyTypes);
            this.ReportObject = reportDocumentConstructor.Invoke(new object[] { });

            // Now do a .Load on this ReportDocument
            MethodInfo reportDocumentLoadMethod = reportDocumentType.GetMethod("Load", new Type[] { typeof(string), this.SharedAssembly.GetType("CrystalDecisions.Shared.OpenReportMethod") });
            reportDocumentLoadMethod.Invoke(this.ReportObject, new object[] { rptPath, 1 }); // The 1 here = OpenReportMethod.OpenReportByTempCopy
        }

        private Assembly EngineAssembly { get; set; }
        private Assembly SharedAssembly { get; set; }
        public dynamic ReportObject { get; set; }

        public void SetCredentials(List<Tuple<string, string, string>> credentials)
        {
            Type tableLogOnInfoType = this.SharedAssembly.GetType("CrystalDecisions.Shared.TableLogOnInfo");
            ConstructorInfo tableLogOnInfoConstructor = tableLogOnInfoType.GetConstructor(Type.EmptyTypes);

            foreach (var table in this.ReportObject.Database.Tables)
            {
                dynamic tableLogOnInfo = tableLogOnInfoConstructor.Invoke(new object[] { });

                var credential = credentials.Where(c => c.Item1.Equals(table.LogOnInfo.ConnectionInfo.DatabaseName));
                if (credential.Count() > 0)
                {
                    // If we matched the DatabaseName to the first argument in the tuple, apply that specific credential
                    tableLogOnInfo.ConnectionInfo.UserID = credential.First().Item2;
                    tableLogOnInfo.ConnectionInfo.Password = credential.First().Item3;
                }
                else
                {
                    // Otherwise just apply the first one we find
                    tableLogOnInfo.ConnectionInfo.UserID = credentials.First().Item2;
                    tableLogOnInfo.ConnectionInfo.Password = credentials.First().Item3;
                }

                table.ApplyLogOnInfo(tableLogOnInfo);
            }
        }

        public dynamic GetReportDetails()
        {
            return new
            {
                ParameterFields = this.ReportObject.ParameterFields
            };
        }

        public byte[] GetAsPDF()
        {
            Type exportOptionsType = this.SharedAssembly.GetType("CrystalDecisions.Shared.ExportOptions");
            Type pdfFormatOptionsType = this.SharedAssembly.GetType("CrystalDecisions.Shared.PdfFormatOptions");
            Type diskFileDestinationOptionsType = this.SharedAssembly.GetType("CrystalDecisions.Shared.DiskFileDestinationOptions");

            ConstructorInfo pdfFormatOptionsConstructor = pdfFormatOptionsType.GetConstructor(Type.EmptyTypes);
            ConstructorInfo diskFileDestinationOptionsConstructor = diskFileDestinationOptionsType.GetConstructor(Type.EmptyTypes);

            dynamic exportOptions = this.ReportObject.ExportOptions;
            dynamic diskFileDestinationOptions = diskFileDestinationOptionsConstructor.Invoke(new object[] { });

            var pdfFormat = Enum.Parse(this.SharedAssembly.GetType("CrystalDecisions.Shared.ExportFormatType"), "PortableDocFormat");
            PropertyInfo exportFormatType = exportOptionsType.GetProperty("ExportFormatType");
            exportFormatType.SetValue(exportOptions, pdfFormat, null);

            diskFileDestinationOptions.DiskFileName = Path.GetTempFileName();
            exportOptions.ExportDestinationOptions = diskFileDestinationOptions;

            var diskFile = Enum.Parse(this.SharedAssembly.GetType("CrystalDecisions.Shared.ExportDestinationType"), "DiskFile");
            PropertyInfo exportDestinationType = exportOptionsType.GetProperty("ExportDestinationType");
            exportDestinationType.SetValue(exportOptions, diskFile, null);

            dynamic pdfFormatOptions = pdfFormatOptionsConstructor.Invoke(new object[] { });
            MethodInfo createPdfFormatOptionsMethod = exportOptionsType.GetMethod("CreatePdfFormatOptions");
            createPdfFormatOptionsMethod.Invoke(pdfFormatOptions, new object[] { });

            exportOptions.ExportFormatOptions = pdfFormatOptions;

            this.ReportObject.Export();
            byte[] b = File.ReadAllBytes(diskFileDestinationOptions.DiskFileName);
            System.IO.File.Delete(diskFileDestinationOptions.DiskFileName);
            return b;
        }
    }
}
