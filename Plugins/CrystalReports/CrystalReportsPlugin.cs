using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace bezlio.rdb.plugins
{
    public class CrystalReportsDataModel
    {
        public string FolderName { get; set; }
        public string ReportName { get; set; }
        public string ConnectionName { get; set; }
        public List<KeyValuePair<string, string>> Parameters { get; set; }

        public CrystalReportsDataModel()
        {
            Parameters = new List<KeyValuePair<string, string>>();
        }
    }

    public class CrystalReports
    {
        public static object GetArgs()
        {

            CrystalReportsDataModel model = new CrystalReportsDataModel();

            model.FolderName = GetFolderNames();
            model.ReportName = "The RPT filename to run.";
            model.ConnectionName = GetConnectionNames();
            model.Parameters = new List<KeyValuePair<string, string>>();
            model.Parameters.Add(new KeyValuePair<string, string>("CustomerId", "102"));

            return model;
        }

        public static List<FileLocation> GetLocations()
        {
            string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string cfgPath = asmPath + @"\" + "CrystalReports.dll.config";
            string strLocations = "";
            if (File.Exists(cfgPath))
            {
                // Load in the cfg file
                XDocument xConfig = XDocument.Load(cfgPath);

                // Get the setting for the debug log destination
                XElement xLocations = xConfig.Descendants("bezlio.plugins.Properties.Settings").Descendants("setting").Where(a => (string)a.Attribute("name") == "rptFileLocations").FirstOrDefault();
                if (xLocations != null)
                {
                    strLocations = xLocations.Value;
                }
            }
            return JsonConvert.DeserializeObject<List<FileLocation>>(strLocations);
        }

        public static List<CrystalConnectionInfo> GetConnections()
        {
            string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string cfgPath = asmPath + @"\" + "CrystalReports.dll.config";
            string strConnections = "";
            if (File.Exists(cfgPath))
            {
                // Load in the cfg file
                XDocument xConfig = XDocument.Load(cfgPath);

                // Get the setting for the debug log destination
                XElement xConnections = xConfig.Descendants("bezlio.plugins.Properties.Settings").Descendants("setting").Where(a => (string)a.Attribute("name") == "connections").FirstOrDefault();
                if (xConnections != null)
                {
                    strConnections = xConnections.Value;
                }
            }
            return JsonConvert.DeserializeObject<List<CrystalConnectionInfo>>(strConnections);
        }

        public static string GetFolderNames()
        {
            var result = "[";
            foreach(var location in GetLocations())
            {
                result += location.LocationName + ",";
            }
            result.TrimEnd(',');
            result += "]";
            return result;
        }

        public static string GetConnectionNames()
        {
            var result = "[";
            foreach (var connection in GetConnections())
            {
                result += connection.ConnectionName + ",";
            }
            result.TrimEnd(',');
            result += "]";
            return result;
        }

        public static async Task<RemoteDataBrokerResponse> ReturnAsPDF(RemoteDataBrokerRequest rdbRequest)
        {
            CrystalReportsDataModel request = JsonConvert.DeserializeObject<CrystalReportsDataModel>(rdbRequest.Data);

            // Declare the response object
            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.Compress = rdbRequest.Compress;
            response.RequestId = rdbRequest.RequestId;
            response.DataType = "applicationJSON";

            try
            {
                string locationPath = GetLocations().Where((l) => l.LocationName.Equals(request.FolderName)).FirstOrDefault().LocationPath;

                CrystalReport cr = new CrystalReport(locationPath + request.ReportName);

                List<Tuple<string, string, string>> credentials = new List<Tuple<string, string, string>>();
                credentials.Add(new Tuple<string, string, string>("", "sa", "2A5Raspa"));
                cr.SetCredentials(credentials);

                response.Data = JsonConvert.SerializeObject(cr.GetAsPDF());

            } catch (Exception ex)
            {
                response.Error = true;
                response.ErrorText = Environment.MachineName + ": " + ex.Message;
            }

            // Return our response
            return response;
        }


    }
}
