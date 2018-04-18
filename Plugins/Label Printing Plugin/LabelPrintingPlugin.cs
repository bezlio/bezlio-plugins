using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace bezlio.rdb.plugins
{

    public class LabelPrintingDataModel
    {
        public string LabelFormat { get; set; }
        public string PrinterName { get; set; }
        public ushort Quantity { get; set; }
        public string Data { get; set; }

        public LabelPrintingDataModel() { }
    }

    public class LabelPrintingConfig
    {
        public LabelPrintingConfig() { }

        public string LabeLEngine { get; set; }
        public string DropFolder { get; set; }
        public string TemplateFolder { get; set; }
    }

    public class PrintJob
    {
        public PrintJob() { }

        public PrintJob(LabelPrintingDataModel dataModel, LabelPrintingConfig printingConfig)
        {
            data = dataModel;
            config = printingConfig;
        }

        public LabelPrintingDataModel data { get; set; }

        public LabelPrintingConfig config { get; set; }

        public void CreateDropFile_Bartender(string template, Dictionary<string, string> data, string printer, uint quantity, string dropFolder)
        {
            List<string> output = new List<string>();

            /* Header Section */
            /* %BTS% tells the integration service to launch BarTender */
            StringBuilder header = new StringBuilder("%BTW% ");
            /* /AF tells BarTender the location and name of the *.btw document file */
            header.Append($"/AF=\"{template}\" ");
            /* Do not replace %FilePath% or %Trigger File Name%. BarTender will recognize it as a variable and it will be automatically replaced with the label file name.
               %Trigger File Name% is used with BarTender Print Command Script
               %FilePath% is used with BarTender integration */
            header.Append("/D=\"%Trigger File Name%\" ");
            /* /PRN tells BarTender the printer */
            header.Append($"/PRN=\"{printer}\" ");
            /* /R tells BarTender the row for the data record */
            header.Append("/R=3 ");
            /* /P tells BarTender to print */
            header.Append("/P ");
            /* /DD tells BarTender to delete the label file after reading */
            header.Append("/DD ");
            /* /C tells BarTender how many copies to print */
            header.Append($"/C={quantity.ToString()}");
            /* End the header */
            output.Add(header.ToString());
            output.Add("%END%");

            /* Data Section */
            string dataLabels = string.Join(",", data.Select(x => '"' + x.Key + '"'));
            output.Add(dataLabels);
            string dataValues = string.Join(",", data.Select(x => '"' + x.Value + '"'));
            output.Add(dataValues);

            /* Write to output file */
            File.WriteAllLines(GetUniqueDropFileName(dropFolder, ".vem"), output.ToArray());
        }

        public void CreateDropFile_Loftware(string template, Dictionary<string, string> data, string printer, uint quantity, string dropFolder)
        {
            List<string> output = new List<string>();

            /* *FORMAT opens the Loftware Label Template file found in Loftware > Options > File Locations... if no path is specified */
            output.Add("*FORMAT," + template);

            /* Data section */
            foreach (KeyValuePair<string, string> entry in data)
            {
                output.Add($"{entry.Key},{entry.Value}");
            }

            /* *QUANTITY tells Loftware how many copies to print */
            output.Add("*QUANTITY," + quantity.ToString());
            /* *PRINTERNAME tells Loftware the printer name alias to print to. The Alias name is assigned to the printer in the Loftware Configure Printers Connection dialog. */
            output.Add("*PRINTERNAME," + printer);
            /* Tell Loftware to print the label */
            output.Add("*PRINTLABEL");

            /* Write to output file */
            File.WriteAllLines(GetUniqueDropFileName(dropFolder, ".pas"), output.ToArray());
        }

        private string GetUniqueDropFileName(string dropFolder, string fileExtension)
        {
            string uniqueFileName = DateTime.Now.ToString("yyyyMMddHHmmssfff") + fileExtension;
            return Path.Combine(dropFolder, uniqueFileName);
        }
    }

    public class LabelPrinting
    {
        public static LabelPrintingConfig config;

        public LabelPrinting()
        {
            //throw new InvalidOperationException("Test");
        }

        public static object GetArgs()
        {
            LabelPrintingDataModel model = new LabelPrintingDataModel();

            model.LabelFormat = "The name of the label format/template to use.";
            model.PrinterName = "The name of the printer to print to.";
            model.Quantity = 1;
            model.Data = "The data to pass to the label in JSON format.";

            return model;
        }

        public static LabelPrintingConfig LoadConfig()
        {
            string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string cfgPath = asmPath + @"\" + "LabelPrinting.dll.config";
            LabelPrintingConfig config = new LabelPrintingConfig();
            var listOfValidEngines = new List<string> {"BARTENDER", "LOFTWARE"};

            if (File.Exists(cfgPath))
            {
                // Load in the cfg file
                XDocument xConfig = XDocument.Load(cfgPath);

                // Get the setting for the backend label engine
                XElement xLabelEngine = xConfig.Descendants("bezlio.plugins.Properties.Settings").Descendants("setting").Where(a => (string)a.Attribute("name") == "labelEngine").FirstOrDefault();
                if (xLabelEngine != null)
                {
                    if (listOfValidEngines.Any(s => xLabelEngine.Value.ToUpper() == s)) {
                        config.LabeLEngine = xLabelEngine.Value.ToUpper();
                    }
                }

                // Get the setting for the drop folder
                XElement xDropFolder = xConfig.Descendants("bezlio.plugins.Properties.Settings").Descendants("setting").Where(a => (string)a.Attribute("name") == "dropFolder").FirstOrDefault();
                if (xDropFolder != null)
                {
                    if (Directory.Exists(xDropFolder.Value))
                    {
                        config.DropFolder = xDropFolder.Value;
                    }
                }

                // Get the setting for the label template folder
                XElement xTemplateFolder = xConfig.Descendants("bezlio.plugins.Properties.Settings").Descendants("setting").Where(a => (string)a.Attribute("name") == "templateFolder").FirstOrDefault();
                if (xTemplateFolder != null)
                {
                    if (Directory.Exists(xTemplateFolder.Value))
                    {
                        config.TemplateFolder = xTemplateFolder.Value;
                    }
                }
            }

            return config;
        }

#pragma warning disable 1998
        public static async Task<RemoteDataBrokerResponse> PrintLabel(RemoteDataBrokerRequest rdbRequest)
        {
            LabelPrintingDataModel request = JsonConvert.DeserializeObject<LabelPrintingDataModel>(rdbRequest.Data);

            // Get the configuration
            LabelPrintingConfig config = LoadConfig();

            // Instantiate a PrintJob object
            PrintJob printJob = new PrintJob(request, config);

            // Declare the response object
            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.RequestId = rdbRequest.RequestId;
            response.Compress = rdbRequest.Compress;
            response.DataType = "applicationJSON";

            try
            {
                //// Make sure the label engine is set
                //if (config.LabeLEngine == null)
                //{
                //    response.Error = true;
                //    response.ErrorText = "Label Printing Plugin not configured correctly on BRDB server. Label engine not set correctly.";
                //    return response;
                //}

                //// Make sure there is a proper drop folder
                //if (config.DropFolder == null)
                //{
                //    response.Error = true;
                //    response.ErrorText = "Label Printing Plugin not configured correctly on BRDB server. Drop folder is not set or doesn't point to an existing directory.";
                //    return response;
                //}

                //// Make sure there is a proper template folder
                //if (config.TemplateFolder == null)
                //{
                //    response.Error = true;
                //    response.ErrorText = "Label Printing Plugin not configured correctly on BRDB server. Template folder is not set or doesn't point to an existing directory.";
                //    return response;
                //}

                // Load the data passed in via JSON as an object we can iterate
                var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(request.Data.ToString());

                // Check that a label format was passed
                if (string.IsNullOrEmpty(request.LabelFormat))
                {
                    response.Error = true;
                    response.ErrorText = "Label format must be specified.";
                    return response;
                }

                // TODO: Check that a printer was passed
                string printer = request.PrinterName;

                // TODO: Check that a valid quantity was passed
                uint quantity = request.Quantity;

                string labelFormatAbsolutePath = Path.Combine(config.TemplateFolder, request.LabelFormat);

                // TODO: Make sure the label format file exists
                if (File.Exists(labelFormatAbsolutePath))
                {

                }

                if (config.LabeLEngine == "BARTENDER")
                {
                    printJob.CreateDropFile_Bartender(labelFormatAbsolutePath, dict, printer, quantity, config.DropFolder);
                } else if (config.LabeLEngine == "LOFTWARE")
                {
                    printJob.CreateDropFile_Loftware(labelFormatAbsolutePath, dict, printer, quantity, config.DropFolder);
                } 
            }
            catch (Exception ex)
            {
                response.Error = true;
                response.ErrorText = Environment.MachineName + ": " + ex.Message;
                return response;
            }

            response.Data = rdbRequest.Data;
            response.Error = false;

            return response;
        }
#pragma warning restore 1998

    }
}
