using System;

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

        public static LSADATA.Dbms GetVisualConnection(string connectionName, ref RemoteDataBrokerResponse response)
        {
            try
            {
                // Pull the connection info from the config file
                VisualConnection connection = Config.GetConnectionInfo(connectionName, ref response);

                // Establish a connection and return the DBMC object
                LSADATA.Dbms dbms = new LSADATA.Dbms();
                dbms.OpenLocal(connection.Instance, connection.UserName, connection.Password);
                return dbms;
            }
            catch (Exception ex)
            {
                response.Error = true;

                if (ex.InnerException != null) { response.ErrorText += ex.InnerException.Message; }
                else { response.ErrorText += ex.Message; }

                return null;
            }
        }

        public static string GetVisualInstance(string connectionName, ref RemoteDataBrokerResponse response)
        {
            try
            {
                // Pull the connection info from the config file
                VisualConnection connection = Config.GetConnectionInfo(connectionName, ref response);
                return connection.Instance;
            }
            catch (Exception ex)
            {
                response.Error = true;

                if (ex.InnerException != null) { response.ErrorText += ex.InnerException.Message; }
                else { response.ErrorText += ex.Message; }

                return null;
            }
        }
    }
}
