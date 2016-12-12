namespace bezlio.rdb.plugins
{
    public class SqlConnectionInfo
    {
        public SqlConnectionInfo() { }

        public string ConnectionName { get; set; }
        public string ServerAddress { get; set; }
        public string DatabaseName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
