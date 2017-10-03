using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        public string GetAsType { get; set; }
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

        public static async Task<RemoteDataBrokerResponse> GetReportList(RemoteDataBrokerRequest rdbRequest)
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

                // Return the data table
                List<dynamic> result = new List<dynamic>();
                foreach (var f in Directory.GetFiles(locationPath, @"*.rpt", SearchOption.AllDirectories))
                {
                    CrystalReport cr = new CrystalReport(f);
                    FileInfo fi = new FileInfo(f);
                    result.Add(new
                    {
                        Attributes = fi.Attributes,
                        CreationTime = fi.CreationTime,
                        CreationTimeUtc = fi.CreationTimeUtc,
                        Directory = fi.Directory,
                        DirectoryName = fi.DirectoryName,
                        Exists = fi.Exists,
                        Extension = fi.Extension,
                        FullName = fi.FullName,
                        IsReadOnly = fi.IsReadOnly,
                        LastAccessTime = fi.LastAccessTime,
                        LastAccessTimeUtc = fi.LastAccessTimeUtc,
                        LastWriteTime = fi.LastWriteTime,
                        LastWriteTimeUtc = fi.LastWriteTimeUtc,
                        Length = fi.Length,
                        Name = fi.Name,
                        BaseName = Path.GetFileNameWithoutExtension(f),
                        ReportDetails = cr.GetReportDetails(),
                        obj = cr,
                        FolderName = request.FolderName
                    });
                }
                response.Data = JsonConvert.SerializeObject(result);

                foreach (var r in result)
                {
                    r.obj.Close();
                }

            }
            catch (Exception ex)
            {
                response.Error = true;
                response.ErrorText = Environment.MachineName + ": " + ex.Message;
            }

            // Return our response
            return response;
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
                // Load Crystal Report
                string locationPath = GetLocations().Where((l) => l.LocationName.Equals(request.FolderName)).FirstOrDefault().LocationPath;
                CrystalReport cr = new CrystalReport(locationPath + request.ReportName);

                // Apply Credentials
                List<Tuple<string, string, string>> credentials = new List<Tuple<string, string, string>>();
                foreach (var connection in GetConnections())
                {
                    credentials.Add(new Tuple<string, string, string>(connection.DatabaseName, connection.UserName, connection.Password));
                }
                cr.SetCredentials(credentials);

                // Apply Parameters
                var parametersObj = request.Parameters.Where(p => p.Key.Equals("ReportDetails"));
                if (parametersObj.Count() > 0)
                {
                    JContainer parameters = JObject.Parse(parametersObj.FirstOrDefault().Value);
                    //var parameters = JsonConvert.DeserializeObject(parametersObj.FirstOrDefault().Value);
                    cr.ApplyParameters(parameters);
                }

                response.Data = JsonConvert.SerializeObject(cr.GetAsPDF());

                cr.Close();

            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(ex.InnerException.ToString()))
                {
                    response.ErrorText += ex.InnerException.ToString();
                }

                response.Error = true;
                response.ErrorText += ex.Message;
            }

            // Return our response
            return response;
        }

        public static async Task<RemoteDataBrokerResponse> ReturnReportDataAs(RemoteDataBrokerRequest rdbRequest)
        {
            CrystalReportsDataModel request = JsonConvert.DeserializeObject<CrystalReportsDataModel>(rdbRequest.Data);

            // Declare the response object
            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.Compress = rdbRequest.Compress;
            response.RequestId = rdbRequest.RequestId;
            response.DataType = "applicationJSON";

            try
            {
                // Load Crystal Report
                string locationPath = GetLocations().Where((l) => l.LocationName.Equals(request.FolderName)).FirstOrDefault().LocationPath;
                CrystalReport cr = new CrystalReport(locationPath + request.ReportName);

                // Apply Credentials
                List<Tuple<string, string, string>> credentials = new List<Tuple<string, string, string>>();
                foreach (var connection in GetConnections())
                {
                    credentials.Add(new Tuple<string, string, string>(connection.DatabaseName, connection.UserName, connection.Password));
                }
                cr.SetCredentials(credentials);

                // Apply Parameters
                var parametersObj = request.Parameters.Where(p => p.Key.Equals("ReportDetails"));
                if (parametersObj.Count() > 0)
                {
                    JContainer parameters = JObject.Parse(parametersObj.FirstOrDefault().Value);
                    //var parameters = JsonConvert.DeserializeObject(parametersObj.FirstOrDefault().Value);
                    cr.ApplyParameters(parameters);
                }

                response.Data = JsonConvert.SerializeObject(cr.GetReportDataAs(request.GetAsType));

                cr.Close();

            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(ex.InnerException.ToString()))
                {
                    response.ErrorText += ex.InnerException.ToString();
                }

                response.Error = true;
                response.ErrorText += ex.Message;
            }

            // Return our response
            return response;
        }


    }
}
