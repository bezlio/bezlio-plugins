namespace bezlio.rdb.plugins
{
    public class SmtpFromAddresses
    {
        public SmtpFromAddresses() {}

        public string FromAddress { get; set; }
        public string DisplayName { get; set; }
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpUser { get; set; }
        public string SmtpPassword { get; set; }
        public bool UseSSL { get; set; }

    }
}
