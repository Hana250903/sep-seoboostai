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
	public class FeatureService : IFeatureService
	{
		private readonly IFeatureRepository _featureRepository;
		private readonly IUnitOfWork _unitOfWork;

		public FeatureService(IFeatureRepository featureRepository, IUnitOfWork unitOfWork)
		{
			_featureRepository = featureRepository;
			_unitOfWork = unitOfWork;
		}

		public async Task<PaginationResult<List<Feature>>> GetFeaturesWithPaginateAsync(int currentPage, int pageSize)
		{
			return await _featureRepository.GetFeaturesWithPaginateAsync(currentPage, pageSize);
		}

		public async Task<Feature> GetFeatureByIdAsync(int id)
		{
			return await _featureRepository.GetByIdAsync(id);
		}

		public async Task<List<Feature>> GetFeaturesAsync()
		{
			return await _featureRepository.GetAllAsync();
		}

		public async Task CreateAsync(Feature feature)
		{
			try
			{
				await _featureRepository.CreateAsync(feature);
				await _unitOfWork.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				throw;
			}
		}

		public async Task UpdateAsync(Feature feature)
		{
			try
			{
				_featureRepository.UpdateAsync(feature);
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
				var feature = await _featureRepository.GetByIdAsync(id);
				await _featureRepository.RemoveAsync(feature);
				await _unitOfWork.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				throw;
			}
		}

		public async Task<List<FeatureDto>> GetAllFeaturesAsync()
		{
			var features = await _featureRepository.GetAllFeaturesAsync();

			return features.Select(f => new FeatureDto
			{
				FeatureID = f.FeatureID,
				Name = f.Name,
				Price = f.Price,
				Description = f.Description,
				// Chuyển danh sách InformationFeature thành List<string>
				Benefits = f.FeatureInformations
							.Select(info => info.InformationFeature)
							.ToList()
			}).ToList();
		}

		public async Task UpdateFeatureAsync(int id, UpdateFeatureRequest request)
		{
			// Tìm Feature trong DB
			var feature = await _featureRepository.GetByIdAsync(id);

			if (feature == null)
			{
				throw new Exception($"Không tìm thấy gói dịch vụ có ID = {id}");
			}

			//Cập nhật dữ liệu mới
			feature.Price = request.Price;
			feature.Description = request.Description;

			//Gọi Repository update & Lưu
			_featureRepository.UpdateAsync(feature);
			await _unitOfWork.SaveChangesAsync();
		}
	}
}
