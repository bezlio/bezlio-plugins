using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Exchange.WebServices.Data;
using System.Collections.Generic;
using System.Linq;

namespace bezlio.rdb.plugins {
    #region 
    class Office365_Model {
        public string UserName { get; set; }
        public string Password { get; set; }
        public int MsgCnt { get; set; }
        public string SubjectFilter { get; set; }
    }
    #endregion

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
        public List<Attachment> Attachments { get; set; }
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

    public class Office365 {
        public static object GetArgs() {
            Office365_Model model = new Office365_Model {
                UserName = "Email Address",
                Password = "Email password",
                MsgCnt = 0,
                SubjectFilter = "Email subject filter - leave blank for none"
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
                propSet.RequestedBodyType = BodyType.HTML;

                ItemView view = new ItemView(request.MsgCnt);
                view.PropertySet = propSet;

                SearchFilter searchFilter;
                if (request.SubjectFilter != "") {
                    searchFilter = new SearchFilter.ContainsSubstring(ItemSchema.Subject, request.SubjectFilter);
                } else {
                    searchFilter = new SearchFilter.Exists(ItemSchema.Subject);
                }

                FindItemsResults <Item> emails = exchange.FindItems(WellKnownFolderName.Inbox, searchFilter, view);
                //foreach(EmailMessage msg in emails) {
                //    msg.Load(propSet);
                //    foreach(Attachment attch in msg.Attachments) {
                //        attch.Load();
                //    }
                //}

                List<Message_Model> emailList = new List<Message_Model>();
                emailList = emails.Select(msg => new Message_Model {
                    From = ((EmailMessage)msg).From.Name,
                    FromAddress = ((EmailMessage)msg).From.Address,
                    Message = msg.Body.Text,
                    Subject = msg.Subject,
                    DateTimeReceived = msg.DateTimeReceived,
                    Read = ((EmailMessage)msg).IsRead,
                    Attachments = msg.Attachments.ToList(),
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
