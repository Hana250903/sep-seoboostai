using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.ModelExtensions
{
	public class CreateFeatureInfoRequest
	{
		public int FeatureID { get; set; }
		public string InformationFeature { get; set; }
	}

	public class UpdateFeatureInfoRequest
	{
		public string InformationFeature { get; set; }
	}
}
