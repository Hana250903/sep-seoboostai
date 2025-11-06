using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Interfaces
{
	public interface IQuestionDataService
	{
		Task<List<QuestionData>> GetQuestionDatasAsync();
		Task<PaginationResult<List<QuestionData>>> GetQuestionDatasWithPaginateAsync(int currentPage, int pageSize);
		Task<QuestionData> GetQuestionDataByIdAsync(int id);
		Task CreateAsync(QuestionData questionData);
		Task UpdateAsync(QuestionData questionData);
		Task DeleteAsync(int id);
	}
}
