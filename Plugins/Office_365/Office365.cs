using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Exchange.WebServices.Data;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Xml.Linq;

namespace bezlio.rdb.plugins {
    class Office365_ListModel {
        public Office365_Model GetMail { get; set; }
        public Office365_Model ProcessEmail { get; set; }
        public Office365Local_Model ProcessEmailLocal { get; set; }
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
        public string From { get; set; }
        public string FromAddress { get; set; }
        public DateTime DateTimeReceived { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public bool Read { get; set; }
        public List<string> Attachments { get; set; }
        public string Id { get; set; }
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
                SubjectFilter = "Email subject filter - leave blank for none",
                AttachmentLocation = "Location to store attachments - used only for Local Processing method"
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
                    From = ((EmailMessage)msg).From.Name,
                    FromAddress = ((EmailMessage)msg).From.Address,
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
                if (request.SubjectFilter != "" && request.SubjectFilter == null) {
                    searchFilter = new SearchFilter.ContainsSubstring(ItemSchema.Subject, request.SubjectFilter);
                }
                else {
                    searchFilter = new SearchFilter.Exists(ItemSchema.Subject);
                }

                //get mail items according to filters
                FindItemsResults<Item> emails = exchange.FindItems(WellKnownFolderName.Inbox, searchFilter, view);
                List<Message_Model> emailList = new List<Message_Model>();

                //load emails and process attachments
                emails.AsParallel().ForAll(eml => {
                    eml.Load(propSet);
                    //add email to list for SQL processing client side
                    emailList.Add(new Message_Model {
                        From = ((EmailMessage)eml).From.Name,
                        FromAddress = ((EmailMessage)eml).From.Address,
                        Message = eml.Body.Text,
                        Subject = eml.Subject,
                        DateTimeReceived = eml.DateTimeReceived,
                        Read = ((EmailMessage)eml).IsRead,
                        Attachments = eml.Attachments.Select(a => a.Id.Substring(a.Id.Length - 10, 10) + a.Name.Substring(a.Name.IndexOf('.'), a.Name.Length - a.Name.IndexOf('.'))).ToList(),
                        Id = eml.Id.UniqueId
                    });
                    eml.Attachments.AsParallel().ForAll(attch => {
                        attch.Load();

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

                            //response.Data += JsonConvert.SerializeObject("Email Id: " + eml.Id + " processed!");
                            break;
                        case "Delete":
                            PropertySet propSetDel = new PropertySet(BasePropertySet.FirstClassProperties);
                            propSetDel.RequestedBodyType = BodyType.HTML;

                            //EmailMessage deleteMsg = EmailMessage.Bind(exchange, request.Id, delSet);
                            eml.Delete(DeleteMode.SoftDelete);

                            //response.Data += JsonConvert.SerializeObject("Email Id: " + eml.Id + " processed!");
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
