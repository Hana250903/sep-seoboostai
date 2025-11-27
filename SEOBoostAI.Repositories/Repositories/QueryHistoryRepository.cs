using Microsoft.EntityFrameworkCore;
using SEOBoostAI.Repository.GenericRepository;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.Repositories
{


    public class QueryHistoryRepository : GenericRepository<QueryHistory>, IQueryHistoryRepository
    {
        public QueryHistoryRepository(SEP_SEOBoostAIContext context) : base(context) { }

        public async Task<PaginationResult<List<QueryHistory>>> GetQueryHistorisWithPaginateAsync(int userId, int currentPage, int pageSize)
        {
            // THÊM OrderByDescending Ở ĐÂY:
            var query = _context.Set<QueryHistory>()
                                .Where(q => q.MemberId == userId)
                                .OrderByDescending(q => q.CreatedAt) // <-- Sắp xếp mới nhất lên đầu
                                .AsQueryable();

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var queryHistories = await query // (Tôi sửa lại chính tả Historis -> Histories cho đẹp nhé)
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new PaginationResult<List<QueryHistory>>
            {
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = currentPage,
                PageSize = pageSize,
                Items = queryHistories
            };
            return result;
        }

    }


}
