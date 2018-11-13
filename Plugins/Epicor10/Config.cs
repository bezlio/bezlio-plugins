using Newtonsoft.Json;
using System;
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
            try
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
            catch (Exception ex)
            {
                response.Error = true;
                response.ErrorText = ex.Message;
                return null;
            }
        }

        public static string GetConfigName(ref RemoteDataBrokerResponse response) {
            try {
                string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string cfgPath = asmPath + @"\Epicor10.dll.config";
                string configName = "";

                if (File.Exists(cfgPath)) {
                    XDocument xConfig = XDocument.Load(cfgPath);

                    XElement xConfigName = xConfig.Descendants("bezlio.plugins.Properties.Settings").Descendants("setting").Where(a => (string)a.Attribute("name") == "epicorConfigName").FirstOrDefault();
                    if (xConfigName != null) {
                        configName = xConfigName.Value;
                    }
                }

                return configName;
            } catch (Exception ex) {
                response.Error = true;
                response.ErrorText = ex.Message;
                return null;
            }
        }
    }
}
