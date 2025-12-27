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

namespace SEOBoostAI.Service.Services.Payments
{
	public class FeatureInformationService : IFeatureInformationService
	{
		private readonly IFeatureInformationRepository _featureInformationRepository;
		private readonly IFeatureRepository _featureRepository;
		private readonly IUnitOfWork _unitOfWork;

		public FeatureInformationService(
			IFeatureInformationRepository featureInformationRepository,
			IFeatureRepository featureRepository,
			IUnitOfWork unitOfWork)
		{
			_featureInformationRepository = featureInformationRepository;
			_featureRepository = featureRepository;
			_unitOfWork = unitOfWork;
		}

		public async Task<List<FeatureInformationDto>> GetListByFeatureIdAsync(int featureId)
		{
			var entities = await _featureInformationRepository.GetByFeatureIdAsync(featureId);
			return entities.Select(e => new FeatureInformationDto
			{
				InformationID = e.InformationID,
				FeatureID = e.FeatureID,
				InformationFeature = e.InformationFeature,
				CreatedAt = e.CreatedAt
			}).ToList();
		}

		public async Task<FeatureInformationDto> GetByIdAsync(int id)
		{
			var entity = await _featureInformationRepository.GetByIdAsync(id);
			if (entity == null) return null;

			return new FeatureInformationDto
			{
				InformationID = entity.InformationID,
				FeatureID = entity.FeatureID,
				InformationFeature = entity.InformationFeature,
				CreatedAt = entity.CreatedAt
			};
		}

		public async Task<FeatureInformationDto> CreateAsync(CreateFeatureInfoRequest request)
		{
			// Check Feature tồn tại
			var feature = await _featureRepository.GetByIdAsync(request.FeatureID);
			if (feature == null) throw new Exception("Gói tính năng không tồn tại.");

			var newInfo = new FeatureInformation
			{
				FeatureID = request.FeatureID,
				InformationFeature = request.InformationFeature,
				CreatedAt = DateTime.UtcNow.AddHours(7)
			};

			await _featureInformationRepository.CreateAsync(newInfo);
			await _unitOfWork.SaveChangesAsync();

			return new FeatureInformationDto
			{
				InformationID = newInfo.InformationID,
				FeatureID = newInfo.FeatureID,
				InformationFeature = newInfo.InformationFeature,
				CreatedAt = newInfo.CreatedAt
			};
		}

		public async Task UpdateAsync(int id, UpdateFeatureInfoRequest request)
		{
			var entity = await _featureInformationRepository.GetByIdAsync(id);
			if (entity == null) throw new Exception("Không tìm thấy thông tin.");

			entity.InformationFeature = request.InformationFeature;
			entity.UpdatedAt = DateTime.UtcNow.AddHours(7);

			await _featureInformationRepository.UpdateAsync(entity);
			await _unitOfWork.SaveChangesAsync();
		}

		public async Task DeleteAsync(int id)
		{
			var entity = await _featureInformationRepository.GetByIdAsync(id);
			if (entity == null) throw new Exception("Không tìm thấy thông tin.");

			await _featureInformationRepository.RemoveAsync(entity);
			await _unitOfWork.SaveChangesAsync();
		}

		public Task<PaginationResult<List<FeatureInformation>>> GetFeatureInformationsWithPaginateAsync(int currentPage, int pageSize)
		{
			return _featureInformationRepository.GetFeatureInformationsWithPaginateAsync(currentPage, pageSize);
		}
	}
}
