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
using DocumentFormat.OpenXml.Wordprocessing;
using System.IO.Packaging;
using Microsoft.Exchange.WebServices.Data;

namespace bezlio.rdb.plugins
{
    public class DataDocWriterDataModel
    {
        public string OutputFileName { get; set; }
        public string InputFileName { get; set; }
        public string SearchFormatPrefix { get; set; }
        public string SearchFormatSuffix { get; set; }
        public string PopulateDataJSON { get; set; }
        public string ExchangeUserName { get; set; }
        public string ExchangePassword { get; set; }
        public string FromEmailAddressFriendly { get; set; }
        public string DestinationEmailAddress { get; set; }
        public string testarg { get; set; }

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
            return model;
        }

        public static async Task<RemoteDataBrokerResponse> GetOutputFile(RemoteDataBrokerRequest rdbRequest)
        {
            var response = new RemoteDataBrokerResponse();
            response.Compress = rdbRequest.Compress;
            response.RequestId = rdbRequest.RequestId;
            response.DataType = "applicationJSON";
            try
            {
                var args = JsonConvert.DeserializeObject<DataDocWriterDataModel>(rdbRequest.Data);
                using (var templateDoc = WordprocessingDocument.Open(args.InputFileName, false))
                using (var outputDoc = WordprocessingDocument.Create("C:\\" + args.OutputFileName, WordprocessingDocumentType.Document))
                {
                    foreach (var part in templateDoc.Parts)
                        outputDoc.AddPart(part.OpenXmlPart, part.RelationshipId);

                    foreach (KeyValuePair<string, string> item in deserializeJSONData(rdbRequest))
                    {
                        var children = templateDoc.MainDocumentPart.Document.Body.Descendants<Text>().ToArray();
                        for (var i = 0; i < children.Count(); i++)
                        {
                            var child = children[i];
                            if (child.Text.Contains(args.SearchFormatPrefix + item.Key + args.SearchFormatSuffix) && child.Text.Length > (args.SearchFormatPrefix + item.Key + args.SearchFormatSuffix).Length)
                                child.Text = child.Text.Replace(args.SearchFormatPrefix + item.Key + args.SearchFormatSuffix, item.Value);

                            if (child.Text.StartsWith(args.SearchFormatPrefix) && !child.Text.EndsWith(args.SearchFormatSuffix)
                                && !children[i + 1].Text.StartsWith(args.SearchFormatPrefix) && children[i + 1].Text.EndsWith(args.SearchFormatSuffix)
                                && i < children.Length + 1)
                            {
                                var text = child.Text + children[i + 1].Text;
                                children[i + 1].Text = "";
                                child.Text = text.Replace(args.SearchFormatPrefix + item.Key + args.SearchFormatSuffix, item.Value);
                            }

                            if (child.Text == args.SearchFormatPrefix + item.Key + args.SearchFormatSuffix)
                            {

                                if (item.Value.Contains("<br/>"))
                                {
                                    child.Text = "";
                                    var splitValue = item.Value.Split(new string[] { "<br/>" }, StringSplitOptions.None);
                                    for (var x = 0; x < splitValue.Length; x++)
                                    {
                                        child.Parent.Append(new Break());
                                        child.Parent.Append(new Text(splitValue[x]));
                                    }
                                }
                                else
                                    child.Text = item.Value;
                            }
                            if (child.Text == item.Key)
                            {
                                if (children.Count() - i >= 2 && i > 0 && children[i - 1].Text == args.SearchFormatPrefix && children[i + 1].InnerXml == args.SearchFormatSuffix)
                                {
                                    if (item.Value.Contains("<br/>"))
                                    {
                                        child.Text = "";
                                        var splitValue = item.Value.Split(new string[] { "<br/>" }, StringSplitOptions.None);
                                        for (var x = 0; x < splitValue.Length; x++)
                                        {
                                            child.Parent.Append(new Break());
                                            child.Parent.Append(new Text(splitValue[x]));
                                        }
                                    }
                                    if (!item.Value.Contains("<br/>"))
                                        child.Text = item.Value;

                                    children[i - 1].Text = "";
                                    children[i + 1].Text = "";
                                }
                            }
                            else if (child.Text.Contains(args.SearchFormatPrefix)
                                && !child.Text.Substring(child.Text.IndexOf(args.SearchFormatPrefix) + args.SearchFormatPrefix.Length).Contains(args.SearchFormatSuffix))
                            {
                                var j = 1;
                                var text = child.Text.Substring(child.Text.IndexOf(args.SearchFormatPrefix) + args.SearchFormatPrefix.Length);

                                while (i < children.Length - j && !children[i + j].Text.Contains(args.SearchFormatSuffix))
                                {
                                    text += children[i + j].Text;
                                    j++;
                                }


                                if (text.Contains(item.Key))
                                {
                                    children[i + j].Text = children[i + j].Text.Replace(args.SearchFormatSuffix, "");
                                    child.Text = text.Replace(item.Key, item.Value);
                                    for (var x = j; x > 0; x--)
                                    {
                                        children[i + x].Text = "";
                                    }
                                }
                            }
                        }
                    }

                    outputDoc.MainDocumentPart.Document.RemoveAllChildren();
                    outputDoc.MainDocumentPart.Document.InnerXml = "";
                    outputDoc.MainDocumentPart.Document.Append(templateDoc.MainDocumentPart.Document.ChildElements.Select(i => i.CloneNode(true)));

                    outputDoc.MainDocumentPart.Document.Save();
                    outputDoc.Save();
                    outputDoc.Close();
                    templateDoc.Close();
                }
                File.Copy("C:\\" + args.OutputFileName, "C:\\bezlio.ddw.out." + args.OutputFileName, true);
                var fs = File.Open("C:\\bezlio.ddw.out." +  args.OutputFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using (var mem = new MemoryStream())
                {
                    fs.CopyTo(mem);
                }

                ExchangeService exchange = new ExchangeService();
                exchange.TraceEnabled = true;
                exchange.TraceFlags = TraceFlags.All;

                exchange.Credentials = new WebCredentials(args.ExchangeUserName, args.ExchangePassword);

                exchange.Url = new System.Uri("https://outlook.office365.com/EWS/Exchange.asmx");

                var email = new Microsoft.Exchange.WebServices.Data.EmailMessage(exchange);
                email.Attachments.AddFileAttachment(@"C:\" + args.OutputFileName);
                email.ToRecipients.Add(new EmailAddress(args.DestinationEmailAddress));
                email.From = new EmailAddress(args.FromEmailAddressFriendly);
                email.Body = "Your Document is attached to this message.";
                email.Send();

                response.Data = JsonConvert.SerializeObject("Your Document has been sent to the provided Email Address.");
                return response;
            }
            catch (Exception e)
            {
                response.Error = true;
                response.ErrorText = e.Message;
            };
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