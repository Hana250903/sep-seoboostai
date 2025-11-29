using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.ModelExtensions
{
	public class SearchRequest
	{
		public int? CurrentPage { get; set; } = 1;
		public int? PageSize { get; set; } = 10;
	}

	public class SearchTransactionRequest : SearchRequest
	{
		public string? Keyword { get; set; }
		public string? CreatedAt { get; set; }
	}
}
