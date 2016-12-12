using System;
using System.IO;
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

                sessionAssembly = Assembly.LoadFrom(clientPath + @"\Ice.Core.Session.dll");
                epicorConn = Activator.CreateInstance(sessionAssembly.GetType("Ice.Core.Session"), new object[] { connection.UserName, connection.Password, connection.AppServerUrl });
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
            sessionAssembly = Assembly.LoadFrom(clientPath + @"\Ice.Core.Session.dll");
            // Dispose of the connection
            sessionAssembly.GetType("Ice.Core.Session").GetMethod("Dispose").Invoke(epicorConn, null);
        }

        public static object GetBusinessObject(object connection, string boName, ref RemoteDataBrokerResponse response)
        {
            string clientPath = Config.GetClientPath(ref response);

            // Now attempt to perform the BO call defined within the body
            Assembly sessionAssembly = Assembly.LoadFrom(clientPath + @"\Ice.Core.Session.dll");
            Assembly serviceModelAssembly = Assembly.LoadFrom(clientPath + @"\Epicor.ServiceModel.dll");
            object wcfSupport = Activator.CreateInstance(sessionAssembly.GetType("Ice.Lib.Framework.WCFServiceSupport"));
            var createImpl = sessionAssembly.GetType("Ice.Lib.Framework.WCFServiceSupport").GetMethod("CreateImpl");
            MethodInfo createImplDC; ;
            object bo;

            Assembly genericAssembly;
            if (File.Exists(clientPath + @"\Erp.Contracts.BO." + boName + ".dll"))
            {
                genericAssembly = Assembly.LoadFrom(clientPath + @"\Erp.Contracts.BO." + boName + ".dll");
                createImplDC = createImpl.MakeGenericMethod(genericAssembly.GetType("Erp.Proxy.BO." + boName + "Impl"));
                bo = createImplDC.Invoke(wcfSupport, new object[] { connection, "Erp/BO/" + boName });
            }
            else
            {
                genericAssembly = Assembly.LoadFrom(clientPath + @"\Ice.Contracts.BO." + boName + ".dll");
                createImplDC = createImpl.MakeGenericMethod(genericAssembly.GetType("Ice.Proxy.BO." + boName + "Impl"));
                bo = createImplDC.Invoke(wcfSupport, new object[] { connection, "Ice/BO/" + boName });
            }

            return bo;
        }
    }
}
