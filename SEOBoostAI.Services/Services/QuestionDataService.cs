using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories.Interfaces;
using SEOBoostAI.Repository.UnitOfWork;
using SEOBoostAI.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services
{
	public class QuestionDataService : IQuestionDataService
	{
		private readonly IQuestionDataRepository _questionDataRepository;
		private readonly IUnitOfWork _unitOfWork;

		public QuestionDataService(IQuestionDataRepository questionDataRepository, IUnitOfWork unitOfWork)
		{
			_questionDataRepository = questionDataRepository;
			_unitOfWork = unitOfWork;
		}

		public async Task<PaginationResult<List<QuestionData>>> GetQuestionDatasWithPaginateAsync(int currentPage, int pageSize)
		{
			return await _questionDataRepository.GetQuestionDataWithPaginateAsync(currentPage, pageSize);
		}

		public async Task<QuestionData> GetQuestionDataByIdAsync(int id)
		{
			return await _questionDataRepository.GetByIdAsync(id);
		}

		public async Task<List<QuestionData>> GetQuestionDatasAsync()
		{
			return await _questionDataRepository.GetAllAsync();
		}

		public async Task CreateAsync(QuestionData questionData)
		{
			try
			{
				await _questionDataRepository.CreateAsync(questionData);
				await _unitOfWork.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				throw;
			}
		}

		public async Task UpdateAsync(QuestionData questionData)
		{
			try
			{
				_questionDataRepository.UpdateAsync(questionData);
				await _unitOfWork.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				throw;
			}
		}

		public async Task DeleteAsync(int id)
		{
			try
			{
				var questionData = await _questionDataRepository.GetByIdAsync(id);
				if (questionData != null)
				{
					_questionDataRepository.RemoveAsync(questionData);
					await _unitOfWork.SaveChangesAsync();
				}
			}
			catch (Exception ex)
			{
				throw;
			}
		}
	}
}
