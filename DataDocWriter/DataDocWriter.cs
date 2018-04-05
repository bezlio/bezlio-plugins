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

namespace bezlio.rdb.plugins
{
    public class DataDocWriterDataModel
    {
        //public byte[] Bytes { get; set; }
        public string OutputFileName { get; set; }
        public string FileName { get; set; }
        public string Context { get; set; }
        public string Connection { get; set; }
        public string QueryName { get; set; }
        public string SearchFormatPrefix { get; set; }
        public string SearchFormatSuffix { get; set; }
        public string ListFilter { get; set; }
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
            List<SqlFileLocation> contextLocations = bezlio.rdb.plugins.SQLServer.GetLocations();

            model.Context = bezlio.rdb.plugins.SQLServer.GetFolderNames(contextLocations);
            model.Connection = bezlio.rdb.plugins.SQLServer.GetConnectionNames();
            model.QueryName = bezlio.rdb.plugins.SQLServer.GetQueriesCascadeDefinition(contextLocations, nameof(model.Context));
            model.Parameters = new List<KeyValuePair<string, string>>();
            model.Parameters.Add(new KeyValuePair<string, string>("CustomerId", "102"));

            return model;
        }

        public static async Task<RemoteDataBrokerResponse> GetOutputFile(RemoteDataBrokerRequest rdbRequest)
        {
            var args = GetArgs();
            var text = "";
            var populateData = SQLServer.ExecuteQuery(rdbRequest);
            var fileResponse = FileSystem.GetFile(rdbRequest);
            using (var memoryStream = new MemoryStream())
            {
                using (var doc = WordprocessingDocument.Open(new MemoryStream(Convert.FromBase64String((await fileResponse).Data)), true))
                {
                    using (var reader = new StreamReader(doc.MainDocumentPart.GetStream()))
                    {
                        text = reader.ReadToEnd();
                    }

                    var row = JsonConvert.DeserializeObject<DataTable>((await populateData).Data).Rows[0];

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
                        doc.MainDocumentPart.GetStream().CopyTo(stream);
                        response.Data = JsonConvert.SerializeObject(stream.ToArray());
                    }
                    return response;
                }
            }
        }
    }
}