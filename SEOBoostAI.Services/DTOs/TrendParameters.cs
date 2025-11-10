using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.DTOs
{
    public class TrendParameters
    {

        public string Query { get; set; } // "phở,cháo"
        public string Geolocation { get; set; } // "VN"
        public string Language { get; set; } // "vi"
        public string Timeframe { get; set; } // "today 12-m"

    }
}
