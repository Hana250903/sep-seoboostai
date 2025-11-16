using SEOBoostAI.Repository.ModelExtensions.GeminiAIModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.ModelExtensions
{
	public class ContentOptimizationDto
	{
		public int ContentOptimizationID { get; set; }
		public int UserID { get; set; }
		public string Model { get; set; }
		public string UserRequest { get; set; }
		public AiOptimizationResponse AiData { get; set; }
		public DateTime? CreatedAt { get; set; }
	}
}
