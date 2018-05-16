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
using System.Xml;
using System.Text.RegularExpressions;

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
            var exportParams = deserializeJSONData(rdbRequest);
            try
            {
                var args = JsonConvert.DeserializeObject<DataDocWriterDataModel>(rdbRequest.Data);
                using (var templateDoc = WordprocessingDocument.Open(args.InputFileName, false))
                using (var outputDoc = WordprocessingDocument.Create("C:\\" + args.OutputFileName, WordprocessingDocumentType.Document))
                {
                    foreach (var part in templateDoc.Parts)
                        outputDoc.AddPart(part.OpenXmlPart, part.RelationshipId);

                    foreach (KeyValuePair<string, string> item in exportParams)
                    {
                        var children = templateDoc.MainDocumentPart.Document.Body.Descendants<Text>().ToArray();
                        for (var i = 0; i < children.Count(); i++)
                        {
                            var child = children[i];
                            if (child.Text.Contains(args.SearchFormatPrefix + item.Key + args.SearchFormatSuffix) && child.Text.Length > (args.SearchFormatPrefix + item.Key + args.SearchFormatSuffix).Length)
                                ReplaceWithFormattedText(child, child.Text.Replace(args.SearchFormatPrefix + item.Key + args.SearchFormatSuffix, item.Value));

                            if (child.Text.StartsWith(args.SearchFormatPrefix) && !child.Text.EndsWith(args.SearchFormatSuffix)
                                && !children[i + 1].Text.StartsWith(args.SearchFormatPrefix) && children[i + 1].Text.EndsWith(args.SearchFormatSuffix)
                                && i < children.Length + 1)
                            {
                                var text = child.Text + children[i + 1].Text;
                                children[i + 1].Text = "";
                                ReplaceWithFormattedText(child, text.Replace(args.SearchFormatPrefix + item.Key + args.SearchFormatSuffix, item.Value));
                            }

                            if (child.Text == args.SearchFormatPrefix + item.Key + args.SearchFormatSuffix)
                            {
                                ReplaceWithFormattedText(child, item.Value);
                            }
                            if (child.Text == item.Key)
                            {
                                if (children.Count() - i >= 2 && i > 0 && children[i - 1].Text == args.SearchFormatPrefix && children[i + 1].InnerXml == args.SearchFormatSuffix)
                                {
                                    ReplaceWithFormattedText(child, item.Value);
                                    children[i - 1].Text = "";
                                    children[i + 1].Text = "";
                                }
                            }
                            else if (child.Text.Contains(args.SearchFormatPrefix)
                                && i > 0
                                && !exportParams.Keys.Contains(children[i - 1].Text)
                                && !child.Text.Substring(child.Text.IndexOf(args.SearchFormatPrefix) + args.SearchFormatPrefix.Length).Contains(args.SearchFormatSuffix))
                            {
                                var aaa = children[i - 1].Text;
                                var bbb = child.Text;
                                var ccc = children[i + 1].Text;
                                var j = 1;
                                var text = child.Text.Substring(child.Text.IndexOf(args.SearchFormatPrefix) + args.SearchFormatPrefix.Length);

                                while (children.Length > i + j && !children[i + j].Text.Contains(args.SearchFormatSuffix))
                                {
                                    text += children[i + j].Text;
                                    j++;
                                }


                                if (text.Contains(item.Key))
                                {
                                    children[i + j].Text = children[i + j].Text.Replace(args.SearchFormatSuffix, "");
                                    ReplaceWithFormattedText(child, text.Replace(item.Key, item.Value));
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
                var timestamp = DateTime.UtcNow.ToFileTimeUtc().ToString();
                File.Delete("C:\\bezlio.ddw.out." + timestamp + "." + args.OutputFileName);
                File.Copy("C:\\" + args.OutputFileName, "C:\\bezlio.ddw.out." +  timestamp + "." + args.OutputFileName, true);
                var fs = File.Open("C:\\bezlio.ddw.out." + timestamp + "." + args.OutputFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
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

                File.Delete("C:\\bezlio.ddw.out." + timestamp + "." + args.OutputFileName);
                File.Delete(@"C:\" + args.OutputFileName);

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

        private static void ReplaceWithFormattedText(Text target, string replaceValue)
        {
            if (replaceValue.Contains("<br/>") || replaceValue.Contains("<b>"))
            {
                target.Text = "";
                var splitValue = replaceValue.Split(new string[] { "<br/>" }, StringSplitOptions.None);
                for (var x = 0; x < splitValue.Length; x++)
                {
                    //target.Parent.Append(new Break());
                    //target.Parent.Append(new Text(splitValue[x]));
                }
                var xml = Regex.Replace(replaceValue, "<(?!text)([0-9A-Za-z]+?)>", "</text><$1>");
                xml = Regex.Replace(xml, "<(?!/text)(/[0-9A-Za-z]+?)>", "<$1><text>");
                xml = Regex.Replace(xml, "<([B|b][R|r] */)>", "</text><$1><text>");
                xml = "<body><text>" + xml + "</text></body>";
                var xmlDoc = XDocument.Parse(xml);
                var descendants = xmlDoc.Descendants().ToArray();
                var paragraph = new Paragraph();
                var paragraphProperties = new ParagraphProperties();
                var formattedRun = new Run();
                var runProperties = new RunProperties();
                var runText = new Text();
                for (var i = 1; i < descendants.Count(); i++)
                {
                    XElement child = descendants[i];
                    runText = new Text() { Text = child.Value, Space = SpaceProcessingModeValues.Preserve };
                    if(!string.IsNullOrWhiteSpace(child.Value))
                    {
                        paragraphProperties = new ParagraphProperties();
                        formattedRun = new Run();
                        //runProperties = new RunProperties();
                    }
                    else
                    {
                        if (formattedRun.Parent.Descendants().OfType<RunProperties>().Count() > 0)
                        {
                            //runProperties = formattedRun.Parent.Descendants().OfType<RunProperties>().ToArray()[0];
                            //formattedRun.RemoveChild<RunProperties>(formattedRun.GetFirstChild<RunProperties>());
                        }
                        //if (formattedRun.Parent.Descendants().OfType<ParagraphProperties>().Count() > 0)
                        //{
                        //    paragraphProperties = formattedRun.Parent.Descendants().OfType<ParagraphProperties>().ToArray()[0];
                        //    formattedRun.Parent.RemoveChild<ParagraphProperties>(formattedRun.GetFirstChild<ParagraphProperties>());
                        //}
                        //if (formattedRun.Descendants().OfType<Text>().Count() > 0)
                        //{
                        //    runText = formattedRun.Descendants().OfType<Text>().ToArray()[0];
                        //    formattedRun.RemoveChild<Text>(formattedRun.GetFirstChild<Text>());
                        //}
                    }
                    var newParagraph = false;
                    switch (child.Name.LocalName.ToUpper())
                    {
                        case "B":
                            runProperties.Append(new Bold());
                            break;
                        case "I":
                            runProperties.Append(new Italic());
                            break;
                        case "U":
                            runProperties.Append(new Underline());
                            break;
                        case "LEFT":
                            paragraph = new Paragraph();
                            paragraphProperties = new ParagraphProperties();
                            paragraphProperties.Append(new Justification() { Val = JustificationValues.Left });
                            paragraph.Append(paragraphProperties);
                            newParagraph = true;
                            break;
                        case "RIGHT":
                            paragraph = new Paragraph();
                            paragraphProperties = new ParagraphProperties();
                            paragraphProperties.Append(new Justification() { Val = JustificationValues.Right });
                            paragraph.Append(paragraphProperties);
                            paragraphProperties.Append(new Bold());
                            newParagraph = true;
                            break;
                        case "CENTER":
                        case "CENTRE":
                            paragraph = new Paragraph();
                            paragraphProperties = new ParagraphProperties();
                            paragraphProperties.Append(new Justification() { Val = JustificationValues.Center });
                            paragraph.Append(paragraphProperties);
                            newParagraph = true;
                            break;
                        case "BR":
                            formattedRun.Append(new Break());
                            break;
                    }


                    var fontSize = 11;
                    if(int.TryParse(child.Name.LocalName, out fontSize))
                        formattedRun.Append(new FontSize() { Val = fontSize.ToString() });

                    if(!newParagraph && runProperties.Parent == null)
                        formattedRun.Append(runProperties);

                    formattedRun.Append(runText);

                    if(!newParagraph && !string.IsNullOrWhiteSpace(child.Value))
                        target.Parent.Parent.Append(formattedRun);

                    if (newParagraph)
                    {
                        var defaultFormatParagraph = new Paragraph();
                        var defaultFormatParagraphRun = new Run(); 
                        var defaultFormatParagraphText = new Text();
                        defaultFormatParagraphRun.Append(defaultFormatParagraphText);
                        defaultFormatParagraph.Append(defaultFormatParagraphRun);
                        target.Parent.Parent.Parent.InsertAfter(defaultFormatParagraph, target.Parent.Parent);

                        paragraph.Append(formattedRun);
                        runProperties = new RunProperties();
                        formattedRun.RunProperties = runProperties;

                        target.Parent.Parent.Parent.InsertAfter(paragraph, target.Parent.Parent);
                        target = defaultFormatParagraphText;

                        formattedRun = defaultFormatParagraphRun;
                        runText = defaultFormatParagraphText;

                        newParagraph = false;
                    }
                }

            }
            else
                target.Text = replaceValue;
        }

        private static Dictionary<string, string> deserializeJSONData(RemoteDataBrokerRequest rdbRequest)
        {
            var deserializedTable = new Dictionary<string, string>();
            var populateJSON = JsonConvert.DeserializeObject<DataDocWriterDataModel>(rdbRequest.Data);
            var populateData = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(populateJSON.PopulateDataJSON.ToString());

            return populateData.SelectMany(i => i.Value).Select(item => new KeyValuePair<string, string>(item.Key, item.Value.ToString())).ToDictionary(key => key.Key, val => val.Value);
        }
    }
}