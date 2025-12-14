using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.ModelExtensions
{
	public class OptimizeRequestDto
	{
		[Required(ErrorMessage = "Từ khóa không được để trống")]
		public string Keyword { get; set; }
		[Required(ErrorMessage = "Nội dung không được để trống")]
		[MinLength(10, ErrorMessage = "Nội dung quá ngắn (tối thiểu 10 ký tự)")]
		[MaxLength(1000, ErrorMessage ="Nội dung không được quá 1000 ký tự")]
		public string Content { get; set; }
		public string ContentLength { get; set; }
		public int OptimizationLevel { get; set; }
		public string ReadabilityLevel { get; set; }
		public int FeatureId { get; set; }
	}
}
