using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.ModelExtensions
{
	public class FeatureDto
	{
		public int FeatureID { get; set; }
		public string Name { get; set; }
		public decimal Price { get; set; }
		public string Description { get; set; }

		// Danh sách các dòng lợi ích (dấu tích xanh)
		public List<string> Benefits { get; set; }
	}
}
