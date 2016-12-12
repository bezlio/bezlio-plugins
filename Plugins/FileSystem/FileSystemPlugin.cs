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
    public class FileSystemDataModel
    {
        public string Context { get; set; }
        public string FileName { get; set; }
    }
    public class FileSystem
    {
        public static object GetArgs()
        {
            FileSystemDataModel model = new FileSystemDataModel();
            model.Context = "Location name where files are stored.";

            return model;
        }

        public static async Task<RemoteDataBrokerResponse> GetFile(RemoteDataBrokerRequest rdbRequest)
        {
            FileSystemDataModel request = JsonConvert.DeserializeObject<FileSystemDataModel>(rdbRequest.Data);

            // Declare the response object
            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.Compress = rdbRequest.Compress;
            response.RequestId = rdbRequest.RequestId;
            response.DataType = "applicationJSON";

            try
            {
                // Settings do not seem to reflect in cleanly, we will read the settings directly
                string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string cfgPath = asmPath + @"\" + "FileSystem.dll.config";
                string strLocations = "";

                if (File.Exists(cfgPath))
                {
                    // Load in the cfg file
                    XDocument xConfig = XDocument.Load(cfgPath);

                    // Get the setting for the debug log destination
                    XElement xLocations = xConfig.Descendants("bezlio.plugins.Properties.Settings").Descendants("setting").Where(a => (string)a.Attribute("name") == "fileSystemLocations").FirstOrDefault();
                    if (xLocations != null)
                    {
                        strLocations = xLocations.Value;
                    }
                }

                // Deserialize the values from Settings
                List<FileLocation> locations = JsonConvert.DeserializeObject<List<FileLocation>>(strLocations);

                // Now pick the location path by the name specified
                if (locations.Where((l) => l.LocationName.Equals(request.Context)).Count() == 0)
                {
                    response.Error = true;
                    response.ErrorText = "Could not locate a location in the plugin config file with the name " + request.Context;
                    return response;
                }
                string locationPath = locations.Where((l) => l.LocationName.Equals(request.Context)).FirstOrDefault().LocationPath;
                
                // Return the data table
                response.Data = JsonConvert.SerializeObject(File.ReadAllBytes(locationPath + request.FileName));

                //WriteDebugLog("Response created");
            }
            catch (Exception ex)
            {
                response.Error = true;
                response.ErrorText = Environment.MachineName + ": " + ex.Message;
            }

            // Return our response
            return response;
        }

        public static async Task<RemoteDataBrokerResponse> GetFileList(RemoteDataBrokerRequest rdbRequest)
        {
            FileSystemDataModel request = JsonConvert.DeserializeObject<FileSystemDataModel>(rdbRequest.Data);

            // Declare the response object
            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.Compress = rdbRequest.Compress;
            response.RequestId = rdbRequest.RequestId;
            response.DataType = "applicationJSON";

            try
            {
                // Settings do not seem to reflect in cleanly, we will read the settings directly
                string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string cfgPath = asmPath + @"\" + "FileSystem.dll.config";
                string strLocations = "";

                if (File.Exists(cfgPath))
                {
                    // Load in the cfg file
                    XDocument xConfig = XDocument.Load(cfgPath);

                    // Get the setting for the folder locations
                    XElement xLocations = xConfig.Descendants("bezlio.plugins.Properties.Settings").Descendants("setting").Where(a => (string)a.Attribute("name") == "fileSystemLocations").FirstOrDefault();
                    if (xLocations != null)
                    {
                        strLocations = xLocations.Value;
                    }
                }

                // Deserialize the values from Settings
                List<FileLocation> locations = JsonConvert.DeserializeObject<List<FileLocation>>(strLocations);
                string locationPath = locations.Where((l) => l.LocationName.Equals(request.Context)).FirstOrDefault().LocationPath;
                
                // Return the data table
                response.Data = JsonConvert.SerializeObject(Directory.GetFiles(locationPath, @"*", SearchOption.AllDirectories));
            }
            catch (Exception ex)
            {
                response.Error = true;
                response.ErrorText = Environment.MachineName + ": " + ex.Message;
            }

            // Return our response
            return response;
        }
    }
}
