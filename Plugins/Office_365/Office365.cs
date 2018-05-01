using Microsoft.Exchange.WebServices.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace bezlio.rdb.plugins {
    class Office365_ListModel {
        public Office365_Model GetMail { get; set; }
        public Office365_Model ProcessEmail { get; set; }
        public Office365Local_Model ProcessEmailLocal { get; set; }
        public SendEmailAttachment_Model SendEmailAttachment { get; set; }
    }
    #region 
    class Office365_Model {
        public string UserName { get; set; }
        public string Password { get; set; }
        public int MsgCnt { get; set; }
        public string SubjectFilter { get; set; }
    }
    #endregion

    class Office365Local_Model {
        public string UserName { get; set; }
        public string Password { get; set; }
        public int MsgCnt { get; set; }
        public string SubjectFilter { get; set; }
        public string AttachmentLocation { get; set; }
        public string ProcessType { get; set; }
        public string SubFolderLocation { get; set; }
    }

    #region MessageRequestModel
    class MessageRequest_Model {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Id { get; set; }
    }
    #endregion

    #region MessageModel
    class Message_Model {
        public string EmailFrom { get; set; }
        public string EmailFromAddress { get; set; }
        public string EmailTo { get; set; }
        public string EmailToAddress { get; set; }
        public string EmailCC { get; set; }
        public string EmailCCAddress { get; set; }
        public string EmailBCC { get; set; }
        public string EmailBCCAddress { get; set; }
        public DateTime DateTimeReceived { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public bool Read { get; set; }
        public List<string> Attachments { get; set; }
        public string Id { get; set; }
    }
    #endregion

    #region SendEmailAttachmentModel


    class SendEmailAttachment_Model
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Destination { get; set; }
        public string EmailBody { get; set; }
        public string FileFullName { get; set; }
    }

    #endregion

    class ProcessMsg_Model {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Id { get; set; }
        public string ProcessType { get; set; }
        public string Destination { get; set; }
    }
    
    #region Appointment_Model
    class Apt_Model {
        public string Subject { get; set; }
        public string Body { get; set; }
        public DateTime ItemDate { get; set; }
    }
    #endregion

    class FileLocation {
        public string LocationName { get; set; }
        public string LocationPath { get; set; }
    }

    public class Office365 {
        public static object GetArgs() {
            Office365_ListModel model = new Office365_ListModel();

            model.GetMail = new Office365_Model {
                UserName = "Email addresss",
                Password = "Email password",
                MsgCnt = 0,
                SubjectFilter = "Email subject filter - leave blank for none"
            };

            model.ProcessEmail = new Office365_Model {
                UserName = "Email addresss",
                Password = "Email password",
                MsgCnt = 0,
                SubjectFilter = "Email subject filter - leave blank for none"
            };

            model.ProcessEmailLocal = new Office365Local_Model {
                UserName = "Email Address",
                Password = "Email password",
                MsgCnt = 0,
                SubjectFilter = "Email subject filter - leave blank for none (EXC- for exclude, INC- for include)",
                AttachmentLocation = "Location to store attachments - used only for Local Processing method"
            };

            model.SendEmailAttachment = new SendEmailAttachment_Model
            {
                UserName = "Email Address",
                Password = "Email password",
                Destination = "To Email Address",
                EmailBody = "Outbound Email Body Text",
                FileFullName = "Full File Path and File Name of File Attachment"
            };


            return model;
        }

        #region ResponseObject
        public static RemoteDataBrokerResponse GetResponseObject(string requestId, bool compress) {
            // Declare the response object
            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.RequestId = requestId;
            response.Compress = compress;
            response.DataType = "applicationJSON";
            return response;
        }
        #endregion

        #region GetMail
        public static async Task<RemoteDataBrokerResponse> GetMail(RemoteDataBrokerRequest rdbRequest) {
            Office365_Model request = JsonConvert.DeserializeObject<Office365_Model>(rdbRequest.Data);

            RemoteDataBrokerResponse response = GetResponseObject(rdbRequest.RequestId, true);

            try {
                ExchangeService exchange = new ExchangeService();
                exchange.TraceEnabled = true;
                exchange.TraceFlags = TraceFlags.All;

                exchange.Credentials = new WebCredentials(request.UserName, request.Password);

                exchange.Url = new System.Uri("https://outlook.office365.com/EWS/Exchange.asmx");

                PropertySet propSet = new PropertySet(BasePropertySet.FirstClassProperties);
                propSet.RequestedBodyType = BodyType.Text;

                ItemView view = new ItemView(request.MsgCnt);
                view.PropertySet = propSet;

                SearchFilter searchFilter;
                if (request.SubjectFilter != "" && request.SubjectFilter != null) {
                    searchFilter = new SearchFilter.ContainsSubstring(ItemSchema.Subject, request.SubjectFilter);
                } else {
                    searchFilter = new SearchFilter.Exists(ItemSchema.Subject);
                }

                FindItemsResults <Item> emails = exchange.FindItems(WellKnownFolderName.Inbox, searchFilter, view);
                emails.AsParallel().ForAll(eml => eml.Load(propSet));

                List<Message_Model> emailList = new List<Message_Model>();
                emailList = emails.Select(msg => new Message_Model {
                    EmailFrom = ((EmailMessage)msg).From.Name,
                    EmailFromAddress = ((EmailMessage)msg).From.Address,
                    //EmailTo = ((EmailMessage)msg)
                    Message = msg.Body.Text,
                    Subject = msg.Subject,
                    DateTimeReceived = msg.DateTimeReceived,
                    Read = ((EmailMessage)msg).IsRead,
                    Id = msg.Id.UniqueId,
                }).ToList();

                response.Data = JsonConvert.SerializeObject(emailList);
            }
            catch (Exception ex) {
                if (!string.IsNullOrEmpty(ex.InnerException.ToString()))
                    response.ErrorText += ex.InnerException.ToString();

                response.Error = true;
                response.ErrorText = ex.Message;
            }

            return response;
        }
        #endregion

        #region SendEmailAttachment


        public static async Task<RemoteDataBrokerResponse> SendEmailAttachment(RemoteDataBrokerRequest rdbRequest)
        {
            SendEmailAttachment_Model request = JsonConvert.DeserializeObject<SendEmailAttachment_Model>(rdbRequest.Data);

            var response = GetResponseObject(rdbRequest.RequestId, true);

            try
            {
                var exchange = new ExchangeService();
                exchange.TraceEnabled = true;
                exchange.TraceFlags = TraceFlags.All;

                exchange.Credentials = new WebCredentials(request.UserName, request.Password);
                exchange.Url = new Uri("https://outlook.office365.com/EWS/Exchange.asmx");

                var email = new Microsoft.Exchange.WebServices.Data.EmailMessage(exchange);
                email.Attachments.AddFileAttachment(request.FileFullName);
                email.ToRecipients.Add(new EmailAddress(request.Destination));
                email.From = new EmailAddress(request.UserName);
                email.Body = request.EmailBody;
                email.Send();

                response.Data = JsonConvert.SerializeObject("SUCCESS. EMAIL SENT.");
                response.Error = false;
                response.ErrorText = "SUCCESS";

            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(ex.InnerException.ToString()))
                {
                    response.ErrorText += ex.InnerException.ToString();
                }

                response.Error = true;
                response.ErrorText = ex.Message;
            }

            return response;
        }

        #endregion

        #region GetBody
        public static async Task<RemoteDataBrokerResponse> GetBody(RemoteDataBrokerRequest rdbRequest) {
            MessageRequest_Model request = JsonConvert.DeserializeObject<MessageRequest_Model>(rdbRequest.Data);

            RemoteDataBrokerResponse response = GetResponseObject(rdbRequest.RequestId, true);

            try {
                ExchangeService exchange = new ExchangeService();
                exchange.TraceEnabled = true;
                exchange.TraceFlags = TraceFlags.All;

                exchange.Credentials = new WebCredentials(request.UserName, request.Password);

                exchange.Url = new System.Uri("https://outlook.office365.com/EWS/Exchange.asmx");

                PropertySet propSet = new PropertySet(BasePropertySet.FirstClassProperties);
                propSet.RequestedBodyType = BodyType.HTML;

                EmailMessage textMsg = EmailMessage.Bind(exchange, request.Id, propSet);
                textMsg.Load();

                response.Data = JsonConvert.SerializeObject(textMsg.Body.Text.Substring(textMsg.Body.Text.IndexOf("<body"), (textMsg.Body.Text.Length - textMsg.Body.Text.IndexOf("<body") - 1)));
            }
            catch (Exception ex) {

                if (!string.IsNullOrEmpty(ex.InnerException.ToString()))
                    response.ErrorText += ex.InnerException.ToString();

                response.Error = true;
                response.ErrorText = ex.Message;
            }

            return response;
        }
        #endregion

        public static async Task<RemoteDataBrokerResponse> ProcessEmail(RemoteDataBrokerRequest rdbRequest){
            ProcessMsg_Model request = JsonConvert.DeserializeObject<ProcessMsg_Model>(rdbRequest.Data);

            RemoteDataBrokerResponse response = GetResponseObject(rdbRequest.RequestId, true);

            try {
                ExchangeService exchange = new ExchangeService();
                exchange.TraceEnabled = true;
                exchange.TraceFlags = TraceFlags.All;

                exchange.Credentials = new WebCredentials(request.UserName, request.Password);
                exchange.Url = new Uri("https://outlook.office365.com/EWS/Exchange.asmx");

                //delete or move switch
                switch(request.ProcessType){
                    case "Move":
                        //create subfolder if it doesn't exist
                        FolderView folderView = new FolderView(1);
                        SearchFilter folderFilter = new SearchFilter.ContainsSubstring(FolderSchema.DisplayName, request.Destination);
                        FindFoldersResults destinationResults = exchange.FindFolders(WellKnownFolderName.Inbox, folderFilter, folderView);

                        Folder destinationFolder;
                        if (destinationResults.TotalCount == 0) {
                            destinationFolder = new Folder(exchange);
                            destinationFolder.DisplayName = request.Destination;
                            destinationFolder.Save(WellKnownFolderName.Inbox);
                        } else {
                            destinationFolder = Folder.Bind(exchange, destinationResults.Folders.Single().Id);
                        }

                        //move items to specified folder
                        PropertySet propSet = new PropertySet(BasePropertySet.FirstClassProperties);
                        propSet.RequestedBodyType = BodyType.HTML;

                        EmailMessage emailMsg = EmailMessage.Bind(exchange, request.Id, propSet);
                        emailMsg.Move(destinationFolder.Id);

                        response.Data = JsonConvert.SerializeObject("Email Id: " + request.Id + " processed!");
                        break;
                    case "Delete":
                        PropertySet delSet = new PropertySet(BasePropertySet.FirstClassProperties);
                        delSet.RequestedBodyType = BodyType.HTML;

                        EmailMessage deleteMsg = EmailMessage.Bind(exchange, request.Id, delSet);
                        deleteMsg.Delete(DeleteMode.SoftDelete);

                        response.Data = JsonConvert.SerializeObject("Email Id: " + request.Id + " processed!");
                        break;
                }

            } catch (Exception ex) {
                if (!string.IsNullOrEmpty(ex.InnerException.ToString())) {
                    response.ErrorText += ex.InnerException.ToString();
                }

                response.Error = true;
                response.ErrorText = ex.Message;
            }

            return response;
        }

        public static async Task<RemoteDataBrokerResponse> ProcessEmailLocal(RemoteDataBrokerRequest rdbRequest){
            Office365Local_Model request = JsonConvert.DeserializeObject<Office365Local_Model>(rdbRequest.Data);

            RemoteDataBrokerResponse response = GetResponseObject(rdbRequest.RequestId, true);

            try {
                ExchangeService exchange = new ExchangeService();
                exchange.TraceEnabled = true;
                exchange.TraceFlags = TraceFlags.All;

                exchange.Credentials = new WebCredentials(request.UserName, request.Password);

                exchange.Url = new System.Uri("https://outlook.office365.com/EWS/Exchange.asmx");

                PropertySet propSet = new PropertySet(BasePropertySet.FirstClassProperties);
                propSet.RequestedBodyType = BodyType.Text;

                ItemView view = new ItemView(request.MsgCnt);
                view.PropertySet = propSet;

                SearchFilter searchFilter;
                if (request.SubjectFilter != "" && request.SubjectFilter != null) {
                    if(request.SubjectFilter.Contains("EXC")) {
                        searchFilter = new SearchFilter.IsNotEqualTo(ItemSchema.Subject, request.SubjectFilter.Substring(request.SubjectFilter.IndexOf("-") + 1, (request.SubjectFilter.Length - (request.SubjectFilter.IndexOf("-") + 1))));
                    } else if(request.SubjectFilter.Contains("INC")){
                        searchFilter = new SearchFilter.ContainsSubstring(ItemSchema.Subject, request.SubjectFilter.Substring(request.SubjectFilter.IndexOf("-") + 1, (request.SubjectFilter.Length - (request.SubjectFilter.IndexOf("-") + 1))));
                    } else {
                        searchFilter = new SearchFilter.ContainsSubstring(ItemSchema.Subject, request.SubjectFilter);
                    }
                }
                else {
                    searchFilter = new SearchFilter.Exists(ItemSchema.Subject);
                }

                //INC-Bezlio Day

                //get mail items according to filters
                FindItemsResults<Item> emails = exchange.FindItems(WellKnownFolderName.Inbox, searchFilter, view);
                List<Message_Model> emailList = new List<Message_Model>();

                //load emails and process attachments
                emails.AsParallel().ForAll(eml => {
                    eml.Load(propSet);
                    //add email to list for SQL processing client side
                    emailList.Add(new Message_Model {
                        EmailFrom = ((EmailMessage)eml).From.Name,
                        EmailFromAddress = ((EmailMessage)eml).From.Address,
                        EmailToAddress = ((EmailMessage)eml).ToRecipients.Count > 0 ? ((EmailMessage)eml).ToRecipients.Aggregate((a, b) => a + "~" + b).ToString() : "",
                        EmailCCAddress = ((EmailMessage)eml).CcRecipients.Count > 0 ? ((EmailMessage)eml).CcRecipients.Aggregate((a, b) => a + "~" + b).ToString() : "",
                        EmailBCCAddress = ((EmailMessage)eml).BccRecipients.Count > 0 ? ((EmailMessage)eml).BccRecipients.Aggregate((a, b) => a + "~" + b).ToString() : "",
                        Message = eml.Body.Text,
                        Subject = eml.Subject,
                        DateTimeReceived = eml.DateTimeReceived,
                        Read = ((EmailMessage)eml).IsRead,
                        Attachments = eml.Attachments.Select(a => a.Id.Substring(a.Id.Length - 10, 10) + a.Name.Substring(a.Name.IndexOf('.'), a.Name.Length - a.Name.IndexOf('.'))).ToList(),
                        Id = eml.Id.UniqueId
                    });
                    eml.Attachments.AsParallel().ForAll(attch => {
                        attch.Load();

                        if (request.SubFolderLocation != null && request.SubFolderLocation != "") {
                            //copied in from file system plugin for ease of use
                            string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                            string cfgPath = asmPath + @"\" + "Office365.dll.config";
                            string strLocations = "";

                            if (File.Exists(cfgPath)) {
                                // Load in the cfg file
                                XDocument xConfig = XDocument.Load(cfgPath);

                                // Get the setting for the debug log destination
                                XElement xLocations = xConfig.Descendants("bezlio.plugins.Properties.Settings").Descendants("setting").Where(a => (string)a.Attribute("name") == "localFileStorage").FirstOrDefault();
                                if (xLocations != null) {
                                    strLocations = xLocations.Value;
                                }
                            }

                            // Deserialize the values from Settings
                            List<FileLocation> locations = JsonConvert.DeserializeObject<List<FileLocation>>(strLocations);

                            // Now pick the location path by the name specified
                            string locationPath = locations.Where((l) => l.LocationName.Equals(request.AttachmentLocation)).FirstOrDefault().LocationPath;

                            File.WriteAllBytes(locationPath + "/" + attch.Id.Substring(attch.Id.Length - 10, 10) + attch.Name.Substring(attch.Name.IndexOf('.'), attch.Name.Length - attch.Name.IndexOf('.')), ((FileAttachment)attch).Content);
                        }
                    });

                    //process email to avoid running through again
                    switch (request.ProcessType) {
                        case "Move":
                            //create subfolder if it doesn't exist
                            FolderView folderView = new FolderView(1);
                            SearchFilter folderFilter = new SearchFilter.ContainsSubstring(FolderSchema.DisplayName, request.SubFolderLocation);
                            FindFoldersResults destinationResults = exchange.FindFolders(WellKnownFolderName.Inbox, folderFilter, folderView);

                            Folder destinationFolder;
                            if (destinationResults.TotalCount == 0) {
                                destinationFolder = new Folder(exchange);
                                destinationFolder.DisplayName = request.SubFolderLocation;
                                destinationFolder.Save(WellKnownFolderName.Inbox);
                            }
                            else {
                                destinationFolder = Folder.Bind(exchange, destinationResults.Folders.Single().Id);
                            }

                            //move items to specified folder
                            PropertySet propSetMove = new PropertySet(BasePropertySet.FirstClassProperties);
                            propSet.RequestedBodyType = BodyType.HTML;

                            eml.Move(destinationFolder.Id);
                            break;
                        case "Delete":
                            PropertySet propSetDel = new PropertySet(BasePropertySet.FirstClassProperties);
                            propSetDel.RequestedBodyType = BodyType.HTML;

                            eml.Delete(DeleteMode.SoftDelete);
                            break;

                        default:
                            break;
                    }
                });

                response.Data += JsonConvert.SerializeObject(emailList);
            }
            catch (Exception ex) {
                if (!string.IsNullOrEmpty(ex.InnerException.ToString()))
                    response.ErrorText += ex.InnerException.ToString();

                response.Error = true;
                response.ErrorText = ex.Message;
            }

            return response;
        }

        #region GetCalendar
        public static async Task<RemoteDataBrokerResponse> GetCalendar(RemoteDataBrokerRequest rdbRequest) {
            Office365_Model request = JsonConvert.DeserializeObject<Office365_Model>(rdbRequest.Data);

            RemoteDataBrokerResponse response = GetResponseObject(rdbRequest.RequestId, true);

            try {
                ExchangeService exchange = new ExchangeService();
                exchange.TraceEnabled = true;
                exchange.TraceFlags = TraceFlags.All;

                exchange.Credentials = new WebCredentials(request.UserName, request.Password);

                exchange.Url = new System.Uri("https://outlook.office365.com/EWS/Exchange.asmx");

                ItemView view = new ItemView(1000);

                FindItemsResults<Item> calendar = exchange.FindItems(WellKnownFolderName.Calendar, view);
                List<Apt_Model> calList = new List<Apt_Model>();

                calList = calendar.Select(cal => new Apt_Model {
                    Subject = cal.Subject,
                    ItemDate = ((Appointment)cal).Start
                }).ToList();

                response.Data = JsonConvert.SerializeObject(calList);

            }
            catch (Exception ex) {
                if (!string.IsNullOrEmpty(ex.InnerException.ToString()))
                    response.ErrorText += ex.InnerException.ToString();

                response.Error = true;
                response.ErrorText = ex.Message;
            }

            return response;
        }
        #endregion
    }
}
