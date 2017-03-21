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
    class Config
    {
        public static VisualConnection GetConnectionInfo(string ConnectionName, ref RemoteDataBrokerResponse response)
        {
            try
            {
                // Settings do not seem to reflect in cleanly, we will read the settings directly
                string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string cfgPath = asmPath + @"\" + "Visual8.dll.config";
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
                List<VisualConnection> connections = JsonConvert.DeserializeObject<List<VisualConnection>>(strConnections);

                // Locate the connection entry specified
                if (connections.Where((c) => c.ConnectionName.Equals(ConnectionName)).Count() == 0)
                {
                    response.Error = true;
                    response.ErrorText = "Could not locate a connection in the plugin config file with the name " + ConnectionName;
                }

                return connections.Where((c) => c.ConnectionName.Equals(ConnectionName)).FirstOrDefault();
            } catch (Exception ex)
            {
                response.Error = true;
                response.ErrorText = ex.Message;
                return null;
            }
        }

        public static string GetClientPath(ref RemoteDataBrokerResponse response)
        {
            try
            {
                // Settings do not seem to reflect in cleanly, we will read the settings directly
                string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string cfgPath = asmPath + @"\" + "Visual8.dll.config";
                string clientPath = "";

                if (File.Exists(cfgPath))
                {
                    // Load in the cfg file
                    XDocument xConfig = XDocument.Load(cfgPath);

                    XElement xClientPath = xConfig.Descendants("bezlio.plugins.Properties.Settings").Descendants("setting").Where(a => (string)a.Attribute("name") == "visualClientPath").FirstOrDefault();
                    if (xClientPath != null)
                    {
                        clientPath = xClientPath.Value;
                    }
                }

                return clientPath;
            }
            catch (Exception ex)
            {
                response.Error = true;
                response.ErrorText = ex.Message;
                return null;
            }
        }
    }
}
