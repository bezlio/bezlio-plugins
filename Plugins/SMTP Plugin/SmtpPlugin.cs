using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Reflection;
using System.IO;
using bezlio.rdb.plugins;
using bezlio.rdb;
using System.Net.Mail;

namespace bezlio.rdb.plugins
{
    public class SmtpDataModel
    {
        public string From { get; set; }
        public string To { get; set; }
        public string Cc { get; set; }
        public string Bcc { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string BodyIsHTML { get; set; }

        public SmtpDataModel()  { }
    }

    public class SMTP
    {
        public static object GetArgs()
        {

            SmtpDataModel model = new SmtpDataModel();

            model.From = GetFromAddressesArgs();
            model.To = "Email recipient(s).  Semi-colon separated if multiple.";
            model.Cc = "Email CC recipient(s).  Semi-colon separated if multiple.";
            model.Bcc = "Email BCC recipient(s).  Semi-colon separated if multiple.";
            model.Subject = "Email Subject";
            model.Body = "Email Body";
            model.BodyIsHTML = "[Yes,No]";

            return model;
        }

        public static List<SmtpFromAddresses> GetFromAddresses()
        {
            string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string cfgPath = asmPath + @"\" + "SMTP.dll.config";
            string strConnections = "";
            if (File.Exists(cfgPath))
            {
                // Load in the cfg file
                XDocument xConfig = XDocument.Load(cfgPath);

                // Get the setting for the debug log destination
                XElement xConnections = xConfig.Descendants("bezlio.plugins.Properties.Settings").Descendants("setting").Where(a => (string)a.Attribute("name") == "fromAddresses").FirstOrDefault();
                if (xConnections != null)
                {
                    strConnections = xConnections.Value;
                }
            }
            return JsonConvert.DeserializeObject<List<SmtpFromAddresses>>(strConnections);
        }

        public static string GetFromAddressesArgs()
        {
            var result = "[";
            foreach (var addr in GetFromAddresses())
            {
                result += addr.FromAddress + ",";
            }
            result.TrimEnd(',');
            result += "]";
            return result;
        }

        public static async Task<RemoteDataBrokerResponse> SendEmail(RemoteDataBrokerRequest rdbRequest)
        {
            SmtpDataModel request = JsonConvert.DeserializeObject<SmtpDataModel>(rdbRequest.Data);

            // Declare the response object
            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.Compress = rdbRequest.Compress;
            response.RequestId = rdbRequest.RequestId;
            response.DataType = "applicationJSON";

            try
            {
                List<SmtpFromAddresses> addresses = GetFromAddresses();

                // Locate the from address specified
                if (addresses.Where((a) => a.FromAddress.Equals(request.From)).Count() == 0)
                {
                    response.Error = true;
                    response.ErrorText = "Could not locate a from address in the plugin config file with the name " + request.From;
                    return response;
                }
                SmtpFromAddresses address = addresses.Where((a) => a.FromAddress.Equals(request.From)).FirstOrDefault();

                MailMessage message = new MailMessage();
                message.From = new MailAddress(address.FromAddress, address.DisplayName);
                
                if (request.To.Contains(";"))
                {
                    foreach (var a in request.To.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        message.To.Add(a);
                    }
                } else if (!string.IsNullOrEmpty(request.To))
                {
                    message.To.Add(request.To);
                }

                if (request.Cc.ToString().Contains(";"))
                {
                    foreach (var a in request.Cc.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        message.CC.Add(a);
                    }
                }
                else if (!string.IsNullOrEmpty(request.Cc))
                {
                    message.CC.Add(request.Cc);
                }

                if (request.Bcc.ToString().Contains(";"))
                {
                    foreach (var a in request.Bcc.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        message.Bcc.Add(a);
                    }
                }
                else if (!string.IsNullOrEmpty(request.Bcc))
                {
                    message.Bcc.Add(request.Bcc);
                }

                message.Subject = request.Subject;
                message.Body = request.Body;
                if (request.BodyIsHTML == "Yes")
                    message.IsBodyHtml = true;

                SmtpClient client = new SmtpClient(address.SmtpServer, address.SmtpPort);

                if (address.UseSSL)
                    client.EnableSsl = true;

                client.Credentials = new System.Net.NetworkCredential(address.SmtpUser, address.SmtpPassword);

                client.Send(message);

                // Return the data table
                response.Data = JsonConvert.SerializeObject("Message Sent");
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
