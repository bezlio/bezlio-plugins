using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bezlio.rdb.plugins.Models
{
    /// <summary>
    /// 
    /// </summary>
    public enum BackOrderStatus
    {
        /// <summary>
        /// 
        /// </summary>
        NoBackorders = 0,
        /// <summary>
        /// 
        /// </summary>
        AllowQtyBelow0 = 1,
        /// <summary>
        /// 
        /// </summary>
        AllowQtyBelow0AndNotifyCustomer = 2
    }
}
