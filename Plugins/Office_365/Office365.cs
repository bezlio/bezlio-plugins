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
        public DateTime DateTimeReceived { get; set; }
        public string Subject { get; set; }
        public bool Read { get; set; }
        public string Id { get; set; }
    }
    #endregion

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

                ItemView view = new ItemView(request.MsgCnt);

                SearchFilter searchFilter;
                if (request.SubjectFilter != null) {
                    searchFilter = new SearchFilter.ContainsSubstring(ItemSchema.Subject, request.SubjectFilter);
                } else {
                    searchFilter = new SearchFilter.Exists(ItemSchema.Subject);
                }

                FindItemsResults <Item> emails = exchange.FindItems(WellKnownFolderName.Inbox, searchFilter, view);
                List<Message_Model> emailList = new List<Message_Model>();

                emailList = emails.Select(msg => new Message_Model {
                    From = ((EmailMessage)msg).From.Name,
                    Subject = msg.Subject,
                    DateTimeReceived = msg.DateTimeReceived,
                    Read = ((EmailMessage)msg).IsRead,
                    Id = msg.Id.UniqueId
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
