using System;
using System.Reflection;

namespace bezlio.rdb.plugins
{
    class Common
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

        public static object GetEpicorConnection(string connectionName, string companyId, ref RemoteDataBrokerResponse response)
        {
            try
            {
                EpicorConnection connection = Config.GetConnectionInfo(connectionName, ref response);

                string clientPath = Config.GetClientPath(ref response);
                object epicorConn = null;
                Assembly sessionAssembly = null;

                sessionAssembly = Assembly.LoadFrom(clientPath + @"\Epicor.Mfg.Core.Session.dll");
                epicorConn = Activator.CreateInstance(sessionAssembly.GetType("Epicor.Mfg.Core.Session"), new object[] { connection.UserName, connection.Password, connection.AppServerUrl });
                epicorConn.GetType().InvokeMember("CompanyID",
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty,
                            Type.DefaultBinder, epicorConn, new Object[] { companyId });

                return epicorConn;
            }
            catch (Exception ex)
            {
                response.Error = true;

                if (ex.InnerException != null) { response.ErrorText += ex.InnerException.Message; }
                else { response.ErrorText += ex.Message; }

                return null;
            }
        }

        public static void CloseEpicorConnection(object epicorConn, ref RemoteDataBrokerResponse response)
        {
            string clientPath = Config.GetClientPath(ref response);
            Assembly sessionAssembly = null;
            sessionAssembly = Assembly.LoadFrom(clientPath + @"\Epicor.Mfg.Core.Session.dll");
            // Dispose of the connection
            sessionAssembly.GetType("Epicor.Mfg.Core.Session").GetMethod("Dispose").Invoke(epicorConn, null);
        }

        public static object GetBusinessObject(object connection, string boName, ref RemoteDataBrokerResponse response)
        {
            string clientPath = Config.GetClientPath(ref response);

            // Now attempt to perform the BO call defined within the body
            Assembly sessionAssembly = Assembly.LoadFrom(clientPath + @"\Epicor.Mfg.Core.Session.dll");
            Assembly genericAssembly = Assembly.LoadFrom(clientPath + @"\Epicor.Mfg.BO." + boName + ".dll");
            Assembly genericIFAssembly = Assembly.LoadFrom(clientPath + @"\Epicor.Mfg.IF.I" + boName + ".dll");
            object bo = Activator.CreateInstance(genericAssembly.GetType("Epicor.Mfg.BO." + boName), new object[] { connection.GetType().InvokeMember("ConnectionPool", BindingFlags.GetProperty, null, connection, null) });

            return bo;
        }
    }
}
