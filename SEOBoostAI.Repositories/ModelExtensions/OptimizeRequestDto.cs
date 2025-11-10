using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.ModelExtensions
{
	public class OptimizeRequestDto
	{
		public int UserId { get; set; }
		public string Keyword { get; set; }
		public string Content { get; set; }
		public string ContentLength { get; set; }
		public int OptimizationLevel { get; set; }
		public string ReadabilityLevel { get; set; }
		public bool IncludeCitation { get; set; }
	}
}
