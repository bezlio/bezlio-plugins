﻿using Newtonsoft.Json;
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
using DocumentFormat.OpenXml.Drawing;
using System.IO.Packaging;

namespace bezlio.rdb.plugins
{
    public class DataDocWriterDataModel
    {
        public string OutputFileName { get; set; }
        public string InputFileName { get; set; }
        public string SearchFormatPrefix { get; set; }
        public string SearchFormatSuffix { get; set; }
        public string PopulateDataJSON { get; set; }

        public DataDocWriterDataModel()
        {
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


            return model;
        }
        // To search and replace content in a document part.
        public static void SearchAndReplace(string document, Dictionary<string, string> dict)
        {
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

                var docText = "";
                using (var templateDoc = WordprocessingDocument.Open(args.InputFileName, false))
                using (var outputDoc = WordprocessingDocument.Create("C:\\" + args.OutputFileName, WordprocessingDocumentType.Document))
                {
                    foreach (var part in templateDoc.Parts)
                        if (part.GetType() != typeof(MainDocumentPart))
                            outputDoc.AddPart(part.OpenXmlPart, part.RelationshipId);
                    using (StreamReader sr = new StreamReader(templateDoc.MainDocumentPart.GetStream(FileMode.Open)))
                    {
                        docText = sr.ReadToEnd();
                    }
                    foreach (KeyValuePair<string, string> item in deserializeJSONData(rdbRequest))
                    {
                        docText = docText.Replace(args.SearchFormatPrefix + item.Key + args.SearchFormatSuffix, item.Value);
                        //var elements = templateDoc.MainDocumentPart.Document.Elements<DocumentFormat.OpenXml.Wordprocessing.Text>().ToArray();
                        //var newElements = new List<DocumentFormat.OpenXml.Wordprocessing.Text>();
                        //for (var i = 0; i < elements.Count(); i++)
                        //{
                        //    if (elements[i].Text != "==" && elements[i])
                        //    {

                        //    }
                        //    else
                        //    {

                        //    }
                        //}
                    }

                    using (StreamWriter sw = new StreamWriter(outputDoc.MainDocumentPart.GetStream(FileMode.Create)))
                    {
                        sw.Write(docText);
                    }
                    outputDoc.MainDocumentPart.Document.Save();
                    outputDoc.Save();
                    outputDoc.Close();
                    templateDoc.Close();
                }
                var fs = File.Open("C:\\" + args.OutputFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using (var mem = new MemoryStream())
                {
                    fs.CopyTo(mem);
                    response.Data = Convert.ToBase64String(mem.ToArray());
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

        private static Dictionary<string, string> deserializeJSONData(RemoteDataBrokerRequest rdbRequest)
        {
            var deserializedTable = new Dictionary<string, string>();
            var populateJSON = JsonConvert.DeserializeObject<DataDocWriterDataModel>(rdbRequest.Data);
            var populateData = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(populateJSON.PopulateDataJSON.ToString());

            return populateData.SelectMany(i => i.Value).Select(item => new KeyValuePair<string, string>(item.Key, item.Value.ToString())).ToDictionary(key => key.Key, val => val.Value);
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
                response.Data = Encoding.ASCII.GetString(File.ReadAllBytes(request.InputFileName));
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