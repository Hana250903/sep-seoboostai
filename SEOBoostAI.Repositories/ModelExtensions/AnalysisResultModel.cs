using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.ModelExtensions
{
    public class AnalysisResultModel
    {
        public PageSpeedMetrics PageSpeedMetrics { get; set; }
        public ComparisonModel ComparisonModel { get; set; }
    }

    public class ComparisonModel
    {
        public int ScoreChange { get; set; }
        public double? FcpChange { get; set; }
        public double? LcpChange { get; set; }
        public double? ClsChange { get; set; }
        public double? TbtChange { get; set; }
        public double? SiChange { get; set; }
        public double? TtiChange { get; set; }
    }
}
