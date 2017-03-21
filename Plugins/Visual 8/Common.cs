using bezlio.rdb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace bezlio.rdb.plugins
{
    public class Common
    {
        public static RemoteDataBrokerResponse GetResponseObject(string requestId, bool compress)
        {
            // Declare the response object
            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.RequestId = requestId;
            response.Compress = compress;
            response.DataType = "applicationJSON";
            return response;
        }

        public static void Connect(string connectionName, ref RemoteDataBrokerResponse response)
        {
            // First we need to get the folder location
            string clientLocation = Config.GetClientPath(ref response);
            if (response.Error == true) {
                return;
            }

            VisualConnection connection = Config.GetConnectionInfo(connectionName, ref response);
            if (response.Error == true) {
                return;
            }

            if (clientLocation != "") {
                try {
                    // Load the LSA Core
                    Assembly LSACore = Assembly.LoadFrom(clientLocation + @"\LSACORE.DLL");
                    // Get the DBMS type
                    Type dbms = LSACore.GetType("Lsa.Data.Dbms");
                    // Create an instance of DBMS
                    var o = Activator.CreateInstance(dbms);
                    // We want DBMS OpenDirect method with 6 string parameters
                    MethodInfo m = dbms.GetMethod("OpenDirect", new Type[] { typeof(String), typeof(String), typeof(String), typeof(String), typeof(String), typeof(String) });
                    // Execute the method with our connection info
                    m.Invoke(o, new object[] { connection.InstanceName, connection.Provider, connection.Driver, connection.DataSource, connection.UserName, connection.Password });
                    // If this passes we are good
                    return;
                } catch (Exception e) {
                    response.Error = true;
                    response.ErrorText = e.Message;
                    return;
                }
            } else {
                response.Error = true;
                response.ErrorText = "Visual client folder not found!";
                return;
            }
        }

        public static void CloseConnection(string connectionName, ref RemoteDataBrokerResponse response)
        {
            // First we need to get the folder location
            string clientLocation = Config.GetClientPath(ref response);
            if (response.Error == true) {
                return;
            }

            VisualConnection connection = Config.GetConnectionInfo(connectionName, ref response);
            if (response.Error == true) {
                return;
            }

            try {
                // Load the LSA Core
                Assembly LSACore = Assembly.LoadFrom(clientLocation + @"\LSACORE.DLL");
                // Get the DBMS type
                Type dbms = LSACore.GetType("Lsa.Data.Dbms");
                // Create an instance of DBMS
                var o = Activator.CreateInstance(dbms);
                // We want DBMS OpenDirect method with 6 string parameters
                MethodInfo m = dbms.GetMethod("Close", new Type[] { typeof(String) });
                // Execute the method with our connection info
                m.Invoke(o, new object[] { connection.InstanceName });
                // If this passes we are good
                return;
            } catch (Exception e) {
                response.Error = true;
                response.ErrorText = e.Message;
                return;
            }
        }
    }
}
