using System;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace bezlio.DataAccessLayer
{
    public class BaseDataLayer
    {
        protected string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        protected virtual string getConnectionString()
        {
            return String.Empty;
        }
        protected SQLiteConnectionStringBuilder BaseSqlLiteConnectionBuilder(string password)
        {
            SQLiteConnectionStringBuilder connBuilder = new SQLiteConnectionStringBuilder();
            connBuilder.Version = 3;
            //Set page size to NTFS cluster size = 4096 bytes
            connBuilder.PageSize = 4096;
            connBuilder.CacheSize = 10000;
            connBuilder.JournalMode = SQLiteJournalModeEnum.Wal;
            connBuilder.Pooling = true;
            connBuilder.LegacyFormat = false;
            connBuilder.DefaultTimeout = 500;

            if (!string.IsNullOrEmpty(password))
                connBuilder.Password = password;

            return connBuilder;
        }
        protected static string SafeSqlLiteral(object sql)
        {
            try
            {
                return sql.ToString().Replace("'", "''");
            }
            catch
            {
                return String.Empty;
            }
        }

        public int ExecuteNonQuery(string queryText)
        {
            var rowsChanged = 0;
            try
            {
                using (SQLiteConnection con = new SQLiteConnection(getConnectionString()))
                {
                    con.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand(queryText, con))
                    {
                        rowsChanged = cmd.ExecuteNonQuery();
                    }
                    con.Close();
                }

            }
            catch (Exception ex)
            {
                WriteApplicationLogError(ex.Message);
            }

            return rowsChanged;
        }

        public DataTable ExecuteQuery(string sql)
        {
            DataTable dt = new DataTable("DataTable");
            try
            {
                using (SQLiteConnection con = new SQLiteConnection(getConnectionString()))
                {
                    con.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand(sql, con))
                    {
                        SQLiteDataReader reader = cmd.ExecuteReader();
                        dt.Load(reader);

                    }
                    con.Close();
                }

            }
            catch (Exception ex)
            {
                WriteApplicationLogError(ex.Message);
            }

            return dt;
        }
        public void verifyTable(string tableName, string createScript)
        {
            try
            {
                string sql = "SELECT name FROM sqlite_master WHERE type = 'table' AND name = '" + SafeSqlLiteral(tableName) + "'";
                DataTable dt = ExecuteQuery(sql);
                if (dt == null || dt.Rows.Count == 0)
                {
                    ExecuteNonQuery(createScript);
                }
            }
            catch (Exception ex)
            {
                WriteApplicationLogError(ex.Message);
            }
        }

        protected void WriteApplicationLogError(string message)
        {
            System.Diagnostics.EventLog log = new System.Diagnostics.EventLog("Application", System.Environment.MachineName, "Bezlio Data Broker");
            log.WriteEntry(message, EventLogEntryType.Error);
        }
    }
}
