using System.Threading.Tasks;

namespace bezlio.rdb.plugins
{
    public class Dummy
    {

#pragma warning disable 1998

        public static object GetArgs()
        {
            return null;
        }

        public static async Task<RemoteDataBrokerResponse> Execute(RemoteDataBrokerRequest rdbRequest)
        {
            // All this does is return the data sent in the request back

            //WriteDebugLog("Creating response");
            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.RequestId = rdbRequest.RequestId;

            response.Compress = rdbRequest.Compress;
            response.DataType = "applicationJSON";

            //WriteDebugLog("Adding Data");
            response.Data = rdbRequest.Data;
            response.Error = false;

            return response;
        }
#pragma warning restore 1998
    }
}
