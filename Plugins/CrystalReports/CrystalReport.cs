using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json.Linq;

namespace bezlio.rdb.plugins
{
    class CrystalReport
    {
        public CrystalReport(string rptPath)
        {
            // Load up the assemblies needed
            EngineAssembly = Assembly.LoadFrom(GACUtils.GetAssemblyPath("CrystalDecisions.CrystalReports.Engine"));
            SharedAssembly = Assembly.LoadFrom(GACUtils.GetAssemblyPath("CrystalDecisions.Shared"));

            // Create a new instance of the ReportDocument
            reportDocumentType = EngineAssembly.GetType("CrystalDecisions.CrystalReports.Engine.ReportDocument");
            ConstructorInfo reportDocumentConstructor = reportDocumentType.GetConstructor(Type.EmptyTypes);
            ReportObject = reportDocumentConstructor.Invoke(new object[] { });

            // Now do a .Load on this ReportDocument
            MethodInfo reportDocumentLoadMethod = reportDocumentType.GetMethod("Load", new Type[] { typeof(string), SharedAssembly.GetType("CrystalDecisions.Shared.OpenReportMethod") });
            reportDocumentLoadMethod.Invoke(ReportObject, new object[] { rptPath, 1 }); // The 1 here = OpenReportMethod.OpenReportByTempCopy
        }

        private Assembly EngineAssembly { get; set; }
        private Assembly SharedAssembly { get; set; }
        private Type reportDocumentType { get; set; }
        public dynamic ReportObject { get; set; }

        public void SetCredentials(List<Tuple<string, string, string>> credentials)
        {
            Type tableLogOnInfoType = SharedAssembly.GetType("CrystalDecisions.Shared.TableLogOnInfo");
            ConstructorInfo tableLogOnInfoConstructor = tableLogOnInfoType.GetConstructor(Type.EmptyTypes);

            foreach (var table in ReportObject.Database.Tables)
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

        public void ApplyParameters(JContainer parameters)
        {
            // Declare the ParameterValues object we need to use to hold the values
            Type parameterValuesType = SharedAssembly.GetType("CrystalDecisions.Shared.ParameterValues");
            Type parameterFieldDefinitionType = EngineAssembly.GetType("CrystalDecisions.CrystalReports.Engine.ParameterFieldDefinition");
            ConstructorInfo parameterValuesConstructor = parameterValuesType.GetConstructor(Type.EmptyTypes);
            MethodInfo parameterValuesAddMethod = parameterValuesType.GetMethod("Add", new Type[] { typeof(object) });
            MethodInfo parameterFieldDefinitionApplyMethod = parameterFieldDefinitionType.GetMethod("ApplyCurrentValues", new Type[] { parameterValuesType });

            // Also declare the individual parameter value types we will be using
            Type parameterDiscreteValueType = SharedAssembly.GetType("CrystalDecisions.Shared.ParameterDiscreteValue");
            ConstructorInfo parameterDiscreteValueConstructor = parameterDiscreteValueType.GetConstructor(Type.EmptyTypes);
            Type parameterRangeValueType = SharedAssembly.GetType("CrystalDecisions.Shared.ParameterRangeValue");
            ConstructorInfo parameterRangeValueConstructor = parameterRangeValueType.GetConstructor(Type.EmptyTypes);

            // Now loop through each parameter specified and add to this ParameterValues object
            foreach (JObject parameter in ((JProperty)parameters.First).Value.Children())
            {
                string parameterName = "";
                dynamic parameterValue = new object();
                int parameterValueKind = 0;
                int discreteOrRangeKind = 0;
                bool enableAllowMultipleValue = false;
                string reportName = "";

                foreach (JProperty prop in parameter.Children())
                {
                    switch (prop.Name)
                    {
                        case "Name":
                            parameterName = prop.Value.ToString();
                            break;
                        case "Value":
                            parameterValue = prop.Value;
                            break;
                        case "ParameterValueKind":
                            parameterValueKind = (int)prop.Value;
                            break;
                        case "DiscreteOrRangeKind":
                            discreteOrRangeKind = (int)prop.Value;
                            break;
                        case "EnableAllowMultipleValue":
                            enableAllowMultipleValue = (bool)prop.Value;
                            break;
                        case "ReportName":
                            reportName = prop.Value.ToString();
                            break;
                        default:
                            break;
                    }
                }

                // Only do anything if this is a main report parameter
                if (reportName == "")
                {
                    dynamic parameterValues = parameterValuesConstructor.Invoke(new object[] { });
                    dynamic parameterFieldDefinition = ReportObject.DataDefinition.ParameterFields[parameterName];

                    // First determine whether we are constructing a range parameter value or not
                    if (discreteOrRangeKind == 0)
                    {
                        // Next determine whether this is multi-value or not
                        if (enableAllowMultipleValue)
                        {
                            foreach (var val in parameterValue.Children())
                            {
                                dynamic parameterAddValue = parameterDiscreteValueConstructor.Invoke(new object[] { });
                                parameterAddValue.Value = GetParameterValueTyped(parameterValueKind, val.Value.ToString());
                                parameterValuesAddMethod.Invoke(parameterValues, new object[] { parameterAddValue });
                            }
                        }
                        else
                        {
                            dynamic parameterAddValue = parameterDiscreteValueConstructor.Invoke(new object[] { });
                            parameterAddValue.Value = GetParameterValueTyped(parameterValueKind, parameterValue.ToString());
                            parameterValuesAddMethod.Invoke(parameterValues, new object[] { parameterAddValue });
                        }
                    }
                    else
                    {
                        // Next determine whether this is multi-value or not
                        if (enableAllowMultipleValue)
                        {
                            foreach (var val in parameterValue.Children())
                            {
                                dynamic parameterAddRangeValue = parameterRangeValueConstructor.Invoke(new object[] { });
                                parameterAddRangeValue.StartValue = GetParameterValueTyped(parameterValueKind, val.Value.StartValue.ToString());
                                parameterAddRangeValue.EndValue = GetParameterValueTyped(parameterValueKind, val.Value.EndValue.ToString());
                                parameterValuesAddMethod.Invoke(parameterValues, new object[] { parameterAddRangeValue });
                            }
                        }
                        else
                        {
                            dynamic parameterAddRangeValue = parameterRangeValueConstructor.Invoke(new object[] { });
                            parameterAddRangeValue.StartValue = GetParameterValueTyped(parameterValueKind, parameterValue.StartValue.ToString());
                            parameterAddRangeValue.EndValue = GetParameterValueTyped(parameterValueKind, parameterValue.EndValue.ToString());
                            parameterValuesAddMethod.Invoke(parameterValues, new object[] { parameterAddRangeValue });
                        }
                    }

                    parameterFieldDefinitionApplyMethod.Invoke(parameterFieldDefinition, new object[] { parameterValues });
                }
            }
        }

        public dynamic GetParameterValueTyped(int parameterValueKind, string parameterValue)
        {
            if (parameterValueKind == 3 || parameterValueKind == 5 || parameterValueKind == 6)
            {
                return DateTime.Parse((parameterValue));
            } else {
                return parameterValue;
            }
        }

        public dynamic GetReportDetails()
        {            
            return new
            {
                ParameterFields = ReportObject.ParameterFields
            };
        }

        public void Close()
        {
            reportDocumentType.GetMethod("Close").Invoke(ReportObject, null);
            reportDocumentType.GetMethod("Dispose").Invoke(ReportObject, null);
        }

        public byte[] GetAsPDF()
        {
            Type exportOptionsType = SharedAssembly.GetType("CrystalDecisions.Shared.ExportOptions");
            Type pdfFormatOptionsType = SharedAssembly.GetType("CrystalDecisions.Shared.PdfFormatOptions");
            Type diskFileDestinationOptionsType = SharedAssembly.GetType("CrystalDecisions.Shared.DiskFileDestinationOptions");

            ConstructorInfo pdfFormatOptionsConstructor = pdfFormatOptionsType.GetConstructor(Type.EmptyTypes);
            ConstructorInfo diskFileDestinationOptionsConstructor = diskFileDestinationOptionsType.GetConstructor(Type.EmptyTypes);

            dynamic exportOptions = ReportObject.ExportOptions;
            dynamic diskFileDestinationOptions = diskFileDestinationOptionsConstructor.Invoke(new object[] { });

            var pdfFormat = Enum.Parse(SharedAssembly.GetType("CrystalDecisions.Shared.ExportFormatType"), "PortableDocFormat");
            PropertyInfo exportFormatType = exportOptionsType.GetProperty("ExportFormatType");
            exportFormatType.SetValue(exportOptions, pdfFormat, null);

            diskFileDestinationOptions.DiskFileName = Path.GetTempFileName();
            exportOptions.ExportDestinationOptions = diskFileDestinationOptions;

            var diskFile = Enum.Parse(SharedAssembly.GetType("CrystalDecisions.Shared.ExportDestinationType"), "DiskFile");
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
