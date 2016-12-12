using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace bezlio.rdb.plugins
{
    class Config
    {
        public static EpicorConnection GetConnectionInfo(string ConnectionName, ref RemoteDataBrokerResponse response)
        {
            // Settings do not seem to reflect in cleanly, we will read the settings directly
            string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string cfgPath = asmPath + @"\" + "Epicor10.dll.config";
            string strConnections = "";

            if (File.Exists(cfgPath))
            {
                // Load in the cfg file
                XDocument xConfig = XDocument.Load(cfgPath);

                // Get the settings for the connections
                XElement xConnections = xConfig.Descendants("bezlio.plugins.Properties.Settings").Descendants("setting").Where(a => (string)a.Attribute("name") == "connections").FirstOrDefault();
                if (xConnections != null)
                {
                    strConnections = xConnections.Value;
                }
            }

            // Deserialize the values from Settings
            List<EpicorConnection> connections = JsonConvert.DeserializeObject<List<EpicorConnection>>(strConnections);

            // Locate the connection entry specified
            if (connections.Where((c) => c.ConnectionName.Equals(ConnectionName)).Count() == 0)
            {
                response.Error = true;
                response.ErrorText = "Could not locate a connection in the plugin config file with the name " + ConnectionName;
            }

            return connections.Where((c) => c.ConnectionName.Equals(ConnectionName)).FirstOrDefault();
        }

        public static string GetClientPath(ref RemoteDataBrokerResponse response)
        {
            // Settings do not seem to reflect in cleanly, we will read the settings directly
            string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string cfgPath = asmPath + @"\" + "Epicor10.dll.config";
            string clientPath = "";

            if (File.Exists(cfgPath))
            {
                // Load in the cfg file
                XDocument xConfig = XDocument.Load(cfgPath);

                XElement xClientPath = xConfig.Descendants("bezlio.plugins.Properties.Settings").Descendants("setting").Where(a => (string)a.Attribute("name") == "epicorClientPath").FirstOrDefault();
                if (xClientPath != null)
                {
                    clientPath = xClientPath.Value;
                }
            }

            return clientPath;
        }
    }
}
