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

        public static object GetEpicorConnection(string connectionName, string companyId, ref RemoteDataBrokerResponse response, string plantId = "", string userName = "", string passWord = "")
        {
            try
            {
                EpicorConnection connection = Config.GetConnectionInfo(connectionName, ref response);
                object epicorConn = null;

                if (connection != null)
                {
                    if (userName == null || userName.Length == 0)
                        userName = connection.UserName;

                    if (passWord == null || passWord.Length == 0)
                        passWord = connection.Password;

                    string clientPath = Config.GetClientPath(ref response);
                    string configPath = clientPath + @"\config\" + Config.GetConfigName(ref response);

                    Assembly sessionAssembly = null;

                    sessionAssembly = Assembly.LoadFrom(clientPath + @"\Ice.Core.Session.dll");
                    epicorConn = Activator.CreateInstance(sessionAssembly.GetType("Ice.Core.Session"), new object[] { userName, passWord, connection.AppServerUrl, null, configPath });
                    epicorConn.GetType().InvokeMember("CompanyID",
                                 BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty,
                                 Type.DefaultBinder, epicorConn, new Object[] { companyId });

                    if (!string.IsNullOrEmpty(plantId))
                    {
                        epicorConn.GetType().InvokeMember("PlantID",
                             BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty,
                             Type.DefaultBinder, epicorConn, new Object[] { plantId });
                    }
                }


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

        public static object GetBusinessObjectDataSet(string boName, string dataSetName, ref RemoteDataBrokerResponse response)
        {
            string clientPath = Config.GetClientPath(ref response);

            if (File.Exists(clientPath + @"\Erp.Contracts.BO." + boName + ".dll"))
            {
                Assembly genericAssembly = Assembly.LoadFrom(clientPath + @"\Erp.Contracts.BO." + boName + ".dll");
                return Activator.CreateInstance(genericAssembly.GetType(dataSetName));
            } else
            {
                Assembly genericAssembly = Assembly.LoadFrom(clientPath + @"\Ice.Contracts.BO." + boName + ".dll");
                return Activator.CreateInstance(genericAssembly.GetType(dataSetName));
            }

        }
    }
}
