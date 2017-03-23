using bezlio.DataAccessLayer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
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
        public List<KeyValuePair<string, string>> Parameters { get; set; }

        public CrystalReportsDataModel()
        {
            Parameters = new List<KeyValuePair<string, string>>();
        }
    }

    public class CrystalReports
    {
        public static ConfigDataLayer config;
        public CrystalReports() {
            config = new ConfigDataLayer("CrystalReports");
            config.verifyTable("CrystalReports", @"create table CrystalReports (ID INTEGER PRIMARY KEY
                                                    , DisplayName text
                                                    , Path text
                                                    , Type text)");

            config.verifyTable("CrystalReportsCredentials", @"create table CrystalReportsCredentials (ID INTEGER PRIMARY KEY
                                                    , CrystalReportId integer
                                                    , Database text
                                                    , UserName text
                                                    , Password text)");

            config.verifyTable("CrystalReportsAuthorization", @"create table CrystalReportsAuthorization (ID INTEGER PRIMARY KEY
                                                    , CrystalReportId integer
                                                    , ConnectionId text)");
        }

        public static object GetArgs()
        {

            CrystalReportsDataModel model = new CrystalReportsDataModel();

            model.FolderName = GetFolderNames();
            model.ReportName = "The RPT filename to run.";
            model.Parameters = new List<KeyValuePair<string, string>>();
            model.Parameters.Add(new KeyValuePair<string, string>("CustomerId", "102"));

            return model;
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
            DataTable dt = config.ExecuteQuery("SELECT * FROM CrystalReports where Type = 'Folder'");
            var result = "[";
            foreach(DataRow dr in dt.Rows)
            {
                result += dr["DisplayName"].ToString() + ",";
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
                DataTable dt = new DataTable();

                if (!string.IsNullOrEmpty(request.FolderName)) 
                    dt = config.ExecuteQuery("SELECT * FROM CrystalReports WHERE Type = 'Folder' and DisplayName = '" + request.FolderName + "'");
                else
                    dt = config.ExecuteQuery("SELECT * FROM CrystalReports ");

                if (dt.Rows.Count > 0)
                {
                    // Return the data table
                    List<dynamic> result = new List<dynamic>();

                    foreach (DataRow dr in dt.Rows)
                    {
                        if (dr["Type"].ToString() == "Folder")
                        {
                            foreach (var f in Directory.GetFiles(dt.Rows[0]["Path"].ToString(), @"*", SearchOption.AllDirectories))
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
                        } else
                        {
                            CrystalReport cr = new CrystalReport(dr["Path"].ToString());
                            FileInfo fi = new FileInfo(dr["Path"].ToString());
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
                                BaseName = Path.GetFileNameWithoutExtension(dr["Path"].ToString()),
                                ReportDetails = cr.GetReportDetails(),
                                obj = cr,
                                FolderName = request.FolderName
                            });
                        }
                    }

                    response.Data = JsonConvert.SerializeObject(result);

                    foreach (var r in result)
                    {
                        r.obj.Close();
                    }
                } else
                {
                    response.Error = true;
                    response.ErrorText = "No reports found.";
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
                DataTable dt = new DataTable();

                if (!string.IsNullOrEmpty(request.FolderName))
                    dt = config.ExecuteQuery("SELECT * FROM CrystalReports where Type = 'Folder' and DisplayName = '" + request.FolderName + "'");
                else
                    dt = config.ExecuteQuery("SELECT * FROM CrystalReports where Type = 'File' and DisplayName = '" + request.ReportName + "'");

                if (dt.Rows.Count > 0)
                {
                    string fullPath = "";

                    if (dt.Rows[0]["Type"].ToString() == "Folder")
                        fullPath = dt.Rows[0]["Path"].ToString() + "/" + request.ReportName;
                    else
                        fullPath = dt.Rows[0]["Path"].ToString();

                    // Load Crystal Report
                    CrystalReport cr = new CrystalReport(fullPath);

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
                        cr.ApplyParameters(parameters);
                    }

                    response.Data = JsonConvert.SerializeObject(cr.GetAsPDF());

                    cr.Close();
                } else
                {
                    response.Error = true;
                    response.ErrorText = "Could not load report.";
                }

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
