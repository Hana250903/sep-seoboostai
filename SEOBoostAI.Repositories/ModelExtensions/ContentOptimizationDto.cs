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

	//public class UserRequestDto
	//{
	//	public string Keyword { get; set; }
	//	public string Content { get; set; }
	//	public string ContentLength { get; set; }
	//	public int OptimizationLevel { get; set; }
	//	public string ReadabilityLevel { get; set; }
	//	public bool IncludeCitation { get; set; }
	//}
}
