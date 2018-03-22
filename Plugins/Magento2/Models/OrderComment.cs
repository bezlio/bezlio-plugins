using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bezlio.rdb.plugins.Models
{
    public class OrderComment
    {
        /// <summary>
        /// 
        /// </summary>
        public int? is_customer_notified { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool? is_visible_on_front { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string comment { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string status { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DateTime created_at { get; set; }
    }
}
