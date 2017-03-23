using System.Data.SQLite;
using System.Data;
using System;
using System.IO;

namespace bezlio.DataAccessLayer
{
    public class ConfigDataLayer : BaseDataLayer
    {
        public ConfigDataLayer(string pluginName)
        {
            PluginName = pluginName;
            checkForDatabase();
        }

        public string Password {get;set;}
        public string PluginName { get; set; }

        private void checkForDatabase()
        {
            try
            {
                // Initialize the settings database, if necessary
                if (!File.Exists(asmPath + @"\" + PluginName + ".db"))
                {
                    SQLiteConnection.CreateFile(asmPath + @"\" + PluginName + ".db");
                }

            }
            catch (Exception ex)
            {
                WriteApplicationLogError(ex.Message);
            }
        }

        protected override string getConnectionString()
        {
            SQLiteConnectionStringBuilder connBuilder = BaseSqlLiteConnectionBuilder(Password);
            connBuilder.DataSource = asmPath + @"\" + PluginName + ".db";

            return connBuilder.ToString();
        }
    }
}
