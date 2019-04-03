using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace bezlio.rdb.plugins
{
    public class LogicityDataModel
    {
        public string Context { get; set; }
        public string Filename { get; set; }
        public string Replacement { get; set; }

        public LogicityDataModel()
        {}
    }

    public class LogicityFileLocation
    {
        public LogicityFileLocation() { }

        public string LocationName { get; set; }
        public string LocationPath { get; set; }
        public List<string> ContentFileNames { get; set; } = new List<string>();
    }

    public class Logicity
    {
        public static object GetArgs()
        {

            LogicityDataModel model = new LogicityDataModel();
            model.Filename = "MyFile.rrd";
            List<LogicityFileLocation> contextLocations = GetLocations();
            model.Context = GetFolderNames(contextLocations);
            return model;
        }

        public static List<LogicityFileLocation> GetLocations()
        {
            string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string cfgPath = asmPath + @"\" + "Logicity.dll.config";
            string strConnections = "";
            if (File.Exists(cfgPath))
            {
                // Load in the cfg file
                XDocument xConfig = XDocument.Load(cfgPath);

                // Get the setting for the debug log destination
                XElement xConnections = xConfig.Descendants("bezlio.plugins.Properties.Settings").Descendants("setting").Where(a => (string)a.Attribute("name") == "logicityFileLocations").FirstOrDefault();
                if (xConnections != null)
                {
                    strConnections = xConnections.Value;
                }
            }

            var contextLocations = JsonConvert.DeserializeObject<List<LogicityFileLocation>>(strConnections);
            foreach (var context in contextLocations)
            {
                if (Directory.Exists(context.LocationPath))
                {
                    var ext = new List<string> { ".rrd" };
                    var contentFiles = Directory.GetFiles(context.LocationPath, "*.*", SearchOption.AllDirectories).Where(s => ext.Contains(Path.GetExtension(s)));
                    context.ContentFileNames = new List<string>();

                    foreach (string fileName in contentFiles)
                    {
                        context.ContentFileNames.Add(Path.GetFileNameWithoutExtension(fileName));
                    }
                }
            }

            return contextLocations;
        }

        public static string GetFolderNames(List<LogicityFileLocation> contextLocations)
        {
            var result = "[";
            foreach (var location in contextLocations)
            {
                result += location.LocationName + ",";
            }
            result.TrimEnd(',');
            result += "]";
            return result;
        }

        public static string GetClientPath()
        {
            // Settings do not seem to reflect in cleanly, we will read the settings directly
            string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string cfgPath = asmPath + @"\" + "Logicity.dll.config";
            string clientPath = "";

            if (File.Exists(cfgPath))
            {
                // Load in the cfg file
                XDocument xConfig = XDocument.Load(cfgPath);

                XElement xClientPath = xConfig.Descendants("bezlio.plugins.Properties.Settings").Descendants("setting").Where(a => (string)a.Attribute("name") == "logicityClientPath").FirstOrDefault();
                if (xClientPath != null)
                {
                    clientPath = xClientPath.Value;
                }
            }

            return clientPath;
        }

        private static List<LogicityFileLocation> getFileLocations()
        {
            string strLocations = "";
            string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string cfgPath = asmPath + @"\" + "Logicity.dll.config";

            if (File.Exists(cfgPath))
            {
                // Load in the cfg file
                XDocument xConfig = XDocument.Load(cfgPath);

                // Get the setting for the debug log destination
                XElement xLocations = xConfig.Descendants("bezlio.plugins.Properties.Settings").Descendants("setting").Where(a => (string)a.Attribute("name") == "logicityFileLocations").FirstOrDefault();
                if (xLocations != null)
                {
                    strLocations = xLocations.Value;
                }
            }

            // Deserialize the values from Settings
            return JsonConvert.DeserializeObject<List<LogicityFileLocation>>(strLocations);
        }

        public static async Task<RemoteDataBrokerResponse> ExecuteRRD(RemoteDataBrokerRequest rdbRequest)
        {
            // This function will call the Logicity executable defined in the plugin configuration and execute the passed RRD file
            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.Compress = rdbRequest.Compress;
            response.RequestId = rdbRequest.RequestId;
            response.DataType = "applicationJSON";

            // Grab our request object
            LogicityDataModel request = null;
            try
            {
                request = JsonConvert.DeserializeObject<LogicityDataModel>(rdbRequest.Data);
            } catch (Exception e)
            {
                response.Error = true;
                response.ErrorText = "Unable to convert passed data into data model";
                return response;
            }

            List<LogicityFileLocation> locations = getFileLocations();

            // Now load the requested rrd file from the specified location
            if (locations.Where((l) => l.LocationName.Equals(request.Context)).Count() == 0)
            {
                response.Error = true;
                response.ErrorText = "Could not locate a location in the plugin config file with the name " + request.Context;
                return response;
            }
            string locationPath = locations.Where((l) => l.LocationName.Equals(request.Context)).FirstOrDefault().LocationPath;
            // Now we have the location path
            string filepath = locationPath + request.Filename;
            if (!File.Exists(filepath))
            {
                response.Error = true;
                response.ErrorText = "Unable to find file at " + filepath;
                return response;
            }

            // Build out our arguments
            string args = "--quiet ";
            args += "\"" + filepath + "\" ";
            if (request.Replacement != "")
            {
                args += request.Replacement;
            }


            // Now we actually call it
            string filename = "";
            try
            {
                filename = GetClientPath() + @"\Logicity Desktop.exe";
            } catch (Exception e)
            {
                response.Error = true;
                response.ErrorText = "Unable to load Logicity executable";
                return response;
            }

            if (!File.Exists(filename))
            {
                response.Error = true;
                response.ErrorText = "Unable to find Logicity executable at " + filename;
                return response;
            }

            // Try the actual command line execution
            try
            {
                var proc = System.Diagnostics.Process.Start(filename, args);
            } catch (Exception e)
            {
                response.Error = true;
                response.ErrorText = "Unable to execute Logicity";
                return response;
            }

            // Return the response, if we get here great success
            response.Data = JsonConvert.SerializeObject("Completed!");
            return response;
        }
    }
}
