using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.ModelExtensions
{
	public class UpdateFeatureRequest
	{
		[Required]
		public decimal Price { get; set; }
		public string Description { get; set; }
	}
}
