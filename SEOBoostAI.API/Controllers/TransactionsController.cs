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
		public async Task<IActionResult> Post([FromBody] Transaction transaction)
		{
			await _transactionService.CreateAsync(transaction);
			return Ok(transaction);
        }

		// PUT api/<TransactionsController>/
		[HttpPut]
		public async Task<IActionResult> Put([FromBody] Transaction transaction)
		{
			await _transactionService.UpdateAsync(transaction);
			return Ok(transaction);
        }

		// DELETE api/<TransactionsController>/5
		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(int id)
		{
			await _transactionService.DeleteAsync(id);
			return Ok();
        }
	}
}
