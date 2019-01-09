using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bezlio.Logging
{
    public class BezlioLog
    {
        // ERROR is logged to log file as well as EventViewer
        // INFO is logged to log file only
        // Log file sizes are maintained, send as much info there as possible

        // BezlioLog uses .NET 4 Lazy Initialization, so that exactly one instance of the logger is created by the application
        // This circumvents log file locking issues and unforeseen effects
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly Lazy<BezlioLog> lazy =
            new Lazy<BezlioLog>(() => new BezlioLog());

        public static BezlioLog Instance { get { return lazy.Value; } }

        private BezlioLog()
        {
            
        }

        public void Info(string message)
        {
            logger.Info(message);
        }

        public void Info(Exception ex, string message)
        {
            logger.Info(ex, message);
        }

        public void Info(Exception ex)
        {
            logger.Info(ex, String.Empty);
        }

        public void Error(string message)
        {
            logger.Error(message);
        }

        public void Error(Exception ex, string message)
        {
            logger.Error(ex, message);
        }

        public void Error(Exception ex)
        {
            logger.Error(ex, String.Empty);
        }
    }
}
