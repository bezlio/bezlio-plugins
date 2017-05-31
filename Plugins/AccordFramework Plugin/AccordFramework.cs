using Accord.Statistics.Distributions.Univariate;
using bezlio.rdb;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bezlio.rdb.plugins
{
    public class AccordFrameworkDataModel
    {
        public string Input { get; set; }

        public AccordFrameworkDataModel()  { }
    }
    public class AccordFramework
    {
        public static object GetArgs()
        {

            AccordFrameworkDataModel model = new AccordFrameworkDataModel();
            model.Input = "[0.25, 0.33, 0.75]";
            return model;
        }

        public static async Task<RemoteDataBrokerResponse> NormalDistribution(RemoteDataBrokerRequest rdbRequest)
        {
            AccordFrameworkDataModel request = JsonConvert.DeserializeObject<AccordFrameworkDataModel>(rdbRequest.Data);

            // Declare the response object
            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.Compress = rdbRequest.Compress;
            response.RequestId = rdbRequest.RequestId;
            response.DataType = "applicationJSON";

            try
            {
                // Deserialize the input into a double array
                double[] input = JsonConvert.DeserializeObject<double[]>(request.Input);
                // Create a normal distribution
                var normal = new NormalDistribution();
                // Fit the inputs
                normal.Fit(input);
                // Return the data table
                response.Data = JsonConvert.SerializeObject(normal);
            }
            catch (Exception ex)
            {
                response.Error = true;
                response.ErrorText = Environment.MachineName + ": " + ex.Message;
            }

            // Return our response
            return response;
        }
    }
}
