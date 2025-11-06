using Microsoft.AspNetCore.Mvc;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Service.Services.Interfaces;

namespace SEOBoostAI.API.Controllers
{
	[Route("api/transactions")]
	[ApiController]
	public class TransactionsController : ControllerBase
	{
		private readonly ITransactionService _transactionService;

		public TransactionsController(ITransactionService transactionService)
		{
			_transactionService = transactionService;
		}

		// GET: api/<TransactionsController>
		[HttpGet]
		public async Task<IEnumerable<Transaction>> Get()
		{
			return await _transactionService.GetTransactionsAsync();
		}

		[HttpGet("{currentPage}/{pageSize}")]
		public async Task<PaginationResult<List<Transaction>>> Get(int currentPage, int pageSize)
		{
			return await _transactionService.GetTransactionsWithPaginateAsync(currentPage, pageSize);
		}

		// GET api/<TransactionsController>/5
		[HttpGet("{id}")]
		public async Task<Transaction> Get(int id)
		{
			return await _transactionService.GetTransactionByIdAsync(id);
		}

		// POST api/<TransactionsController>
		[HttpPost]
		public async Task<int> Post([FromBody] Transaction transaction)
		{
			throw new NotImplementedException();
		}

		// PUT api/<TransactionsController>/
		[HttpPut]
		public async Task<int> Put([FromBody] Transaction transaction)
		{
			throw new NotImplementedException();
		}

		// DELETE api/<TransactionsController>/5
		[HttpDelete("{id}")]
		public async Task<bool> Delete(int id)
		{
			throw new NotImplementedException();
		}
	}
}
