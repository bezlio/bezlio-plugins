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
            try
            {
                var args = JsonConvert.DeserializeObject<DataDocWriterDataModel>(rdbRequest.Data);
                var text = "";
                DataTable populateData;

                var fileResponse = await getFile(rdbRequest);
                var fileContentStream = new MemoryStream();
                var fileContents = Convert.FromBase64String(JsonConvert.DeserializeObject<string>(fileResponse.Data));
                fileContentStream.Write(fileContents, 0, fileContents.Length);
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
                            //populateData = JsonConvert.DeserializeObject<DataTable>(JsonConvert.DeserializeObject<DataDocWriterDataModel>(rdbRequest.Data).PopulateDataJSON);
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

                        RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
                        response.Compress = rdbRequest.Compress;
                        response.RequestId = rdbRequest.RequestId;
                        response.DataType = "applicationJSON";
                        using (var stream = new MemoryStream())
                        {
                            response.Data = JsonConvert.SerializeObject(fileContentStream.ToArray());
                        }
                        return response;
                    }
                }
            }
            catch (Exception e)
            {
                var x = e;
            }
            return new RemoteDataBrokerResponse();
        }

        private static async Task<DataTable> deserializeJSONData(RemoteDataBrokerRequest rdbRequest)
        {
            var x = new Dictionary<string, object>();
            x.Add("test", "col");

            var deserializedTable = new DataTable();
            var populateJSON = JsonConvert.DeserializeObject<DataDocWriterDataModel>(rdbRequest.Data);
            var populateData = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(populateJSON.PopulateDataJSON);
            foreach (var item in populateData.First().Value)
            {
                if (!deserializedTable.Columns.Contains(item.Key))
                {
                    deserializedTable.Columns.Add(item.Key, item.Value.GetType());
                }
            }
            foreach (var row in populateData)
            {
                var populateDataRow = deserializedTable.NewRow();
                foreach (var item in row.Value)
                {
                    populateDataRow[item.Key] = item.Value;
                }
                deserializedTable.Rows.Add(populateDataRow);
            }
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
                //if (Directory.GetAccessControl(request.FileName.Split('\\').Take(request.FileName.Count(c => c == '\\') - 1).Aggregate((a, b) => a + b)) == null)
                //throw new UnauthorizedAccessException("User does not have access to directory at location. Please use full and exact paths and file names. \n" + request.FileName);
                //if (!File.Exists(request.FileName))
                //throw new FileNotFoundException("File at location not found. Please use full and exact paths and file names. \n" + request.FileName);
                // Return the data table
                response.Data = JsonConvert.SerializeObject(File.ReadAllBytes(request.FileName));

                //WriteDebugLog("Response created");
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