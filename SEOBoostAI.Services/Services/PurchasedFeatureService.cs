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
	public class PurchasedFeatureService : IPurchasedFeatureService
	{
		private readonly IPurchasedFeatureRepository _purchasedFeatureRepository;
		private readonly IUnitOfWork _unitOfWork;

		public PurchasedFeatureService(IPurchasedFeatureRepository purchasedFeatureRepository, IUnitOfWork unitOfWork)
		{
			_purchasedFeatureRepository = purchasedFeatureRepository;
			_unitOfWork = unitOfWork;
		}

		public async Task<PaginationResult<List<PurchasedFeature>>> GetPurchasedFeaturesWithPaginateAsync(int currentPage, int pageSize)
		{
			return await _purchasedFeatureRepository.GetPurchasedFeaturesWithPaginateAsync(currentPage, pageSize);
		}

		public async Task<PurchasedFeature> GetPurchasedFeatureByIdAsync(int id)
		{
			return await _purchasedFeatureRepository.GetByIdAsync(id);
		}

		public async Task<List<PurchasedFeature>> GetPurchasedFeaturesAsync()
		{
			return await _purchasedFeatureRepository.GetAllAsync();
		}

		public async Task CreateAsync(PurchasedFeature purchasedFeature)
		{
			try
			{
				await _purchasedFeatureRepository.CreateAsync(purchasedFeature);
				await _unitOfWork.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				throw;
			}
		}

		public async Task UpdateAsync(PurchasedFeature purchasedFeature)
		{
			try
			{
				_purchasedFeatureRepository.UpdateAsync(purchasedFeature);
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
				var purchasedFeature = await _purchasedFeatureRepository.GetByIdAsync(id);
				await _purchasedFeatureRepository.RemoveAsync(purchasedFeature);
				await _unitOfWork.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				throw;
			}
		}
	}
}
