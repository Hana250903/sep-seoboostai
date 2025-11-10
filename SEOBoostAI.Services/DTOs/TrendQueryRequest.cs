using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.DTOs
{
    public class TrendQueryRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập câu hỏi.")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "Câu hỏi phải từ 10 đến 500 ký tự.")]
        public string Question { get; set; }
    }
}
