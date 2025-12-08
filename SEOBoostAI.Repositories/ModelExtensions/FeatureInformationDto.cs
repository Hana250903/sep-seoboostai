using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.ModelExtensions
{
	public class FeatureInformationDto
	{
		public int InformationID { get; set; }
		public int FeatureID { get; set; }
		public string InformationFeature { get; set; }
		public DateTime CreatedAt { get; set; }
	}
}
