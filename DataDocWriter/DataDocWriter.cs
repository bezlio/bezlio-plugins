using Newtonsoft.Json;
using System;
using System.Text;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml;
using bezlio.rdb.plugins;

namespace bezlio.rdb.plugins
{
    public class DataDocWriterDataModel
    {
        public string OutputFileName { get; set; }
        public string Context { get; set; }
        public string FileName { get; set; }
        public string Connection { get; set; }
        public string QueryName { get; set; }
        public string SearchFormatPrefix { get; set; }
        public string SearchFormatSuffix { get; set; }
        public string PopulateDataJSON { get; set; }
        public List<KeyValuePair<string, string>> Parameters { get; set; }

        public DataDocWriterDataModel()
        {
            Parameters = new List<KeyValuePair<string, string>>();
            SearchFormatPrefix = @"==";
            SearchFormatSuffix = @"==";
        }
    }

    public class DataDocWriter
    {
        public static DataDocWriterDataModel GetArgs()
        {

            DataDocWriterDataModel model = new DataDocWriterDataModel();
            List<SqlFileLocation> contextLocations = SQLServerFunctions.GetLocations();

            model.Connection = SQLServerFunctions.GetConnectionNames();
            model.QueryName = SQLServerFunctions.GetQueriesCascadeDefinition(contextLocations, nameof(model.Context));
            model.Parameters = new List<KeyValuePair<string, string>>();

            return model;
        }

        public static async Task<RemoteDataBrokerResponse> GetOutputFile(RemoteDataBrokerRequest rdbRequest)
        {
            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.Compress = rdbRequest.Compress;
            response.RequestId = rdbRequest.RequestId;
            response.DataType = "applicationJSON";

            try
            {
                var args = JsonConvert.DeserializeObject<DataDocWriterDataModel>(rdbRequest.Data);
                var text = "";
                DataTable populateData;

                var fileResponse = await getFile(rdbRequest);
                var fileContentStream = new MemoryStream();
                //var fileContents = JsonConvert.DeserializeObject<byte[]>(fileResponse.Data);
                byte[] array = Encoding.ASCII.GetBytes(fileResponse.Data);
                fileContentStream.Write(array, 0, fileResponse.Data.Length);
                using (var memoryStream = new MemoryStream())
                {
                    using (var doc = WordprocessingDocument.Open(fileContentStream, true))
                    {
                        using (var reader = new StreamReader(doc.MainDocumentPart.GetStream()))
                        {
                            text = reader.ReadToEnd();
                        }

                        if (!string.IsNullOrWhiteSpace(args.PopulateDataJSON))
                        {
                            populateData = await deserializeJSONData(rdbRequest);
                        }
                        else
                        {
                            populateData = JsonConvert.DeserializeObject<DataTable>((await SQLServerFunctions.ExecuteQuery(rdbRequest)).Data);
                        }
                        var row = populateData.Rows[0];

                        foreach (DataColumn column in row.Table.Columns)
                        {
                            var value = row[column].ToString();
                            doc.MainDocumentPart.Document.InnerXml = doc.MainDocumentPart.Document.InnerXml.Replace(args.SearchFormatPrefix + column.ColumnName + args.SearchFormatSuffix, value);
                            doc.MainDocumentPart.Document.Save();
                            doc.Save();
                        }

                        
                        using (var stream = new MemoryStream())
                        {
                            response.Data = JsonConvert.SerializeObject(fileContentStream.ToArray());
                        }
                        
                    }
                }
            }
            catch (Exception e)
            {
                //if (!string.IsNullOrEmpty(e.InnerException.ToString())) {
                //    response.ErrorText += e.InnerException.ToString();
                //}
                response.Error = true;
                response.ErrorText += e.Message;
            }

            return response;
        }

        private static async Task<DataTable> deserializeJSONData(RemoteDataBrokerRequest rdbRequest)
        {
            var deserializedTable = new DataTable();
            var populateJSON = JsonConvert.DeserializeObject<DataDocWriterDataModel>(rdbRequest.Data);
            var populateData = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(populateJSON.PopulateDataJSON);
            foreach (var item in populateData.SelectMany(i => i.Value))
            {
                if (!deserializedTable.Columns.Contains(item.Key))
                {
                    deserializedTable.Columns.Add(item.Key, item.Value.GetType());
                }
            }

            var populateDataRow = deserializedTable.NewRow();
            foreach (var item in populateData.SelectMany(i => i.Value))
            {
                populateDataRow[item.Key] = item.Value;
            }
            deserializedTable.Rows.Add(populateDataRow);
            return deserializedTable;
        }

        private static async Task<RemoteDataBrokerResponse> getFile(RemoteDataBrokerRequest rdbRequest)
        {
            var request = JsonConvert.DeserializeObject<DataDocWriterDataModel>(rdbRequest.Data);

            // Declare the response object
            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.Compress = rdbRequest.Compress;
            response.RequestId = rdbRequest.RequestId;
            response.DataType = "applicationJSON";

            try
            {
                response.Data = Encoding.ASCII.GetString(File.ReadAllBytes(request.FileName));
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