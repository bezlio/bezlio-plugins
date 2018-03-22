using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bezlio.rdb.plugins.Models
{
    public class Search_Criteria
    {
        public List<string> filter_groups { get; set; }
        public int page_size { get; set; }
    }
}
