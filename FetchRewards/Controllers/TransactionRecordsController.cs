using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FetchRewards.Models;

namespace FetchRewards.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionRecordsController : ControllerBase
    {
        private readonly TransactionRecordContext _context;

        public TransactionRecordsController(TransactionRecordContext context)
        {
            _context = context;
        }

        // GET: api/TransactionRecords
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TransactionRecord>>> GetAllTransactionRecords()
        {
          if (_context.TransactionRecords == null)
          {
              return NotFound();
          }
            return await _context.TransactionRecords.ToListAsync();
        }

        // GET: api/TransactionRecords/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TransactionRecord>> GetTransactionRecord(int id)
        {
            if (_context.TransactionRecords == null)
            {
                return NotFound();
            }
            var transactionRecord = await _context.TransactionRecords.FindAsync(id);

            if (transactionRecord == null)
            {
                return NotFound();
            }

            return transactionRecord;
        }

        //This whole page is just scaffolding for CRUD operations that Entity Framework will generate automatically, all I did
        //was cull the unused methods and added this bit here which makes sure the balance cant go negative when a TransactionRecord is added.
        // POST: api/TransactionRecords
        [HttpPost]
        [ActionName("SubmitTransactionRecord")]
        [Route("SubmitTransactionRecord")]
        public async Task<ActionResult<TransactionRecord>> SubmitTransactionRecord(TransactionRecord transactionRecord)
        {
            if (_context.TransactionRecords == null)
            {
                return Problem("Entity set 'TransactionRecordContext.TransactionRecords'  is null.");
            }

            //calculate the balance of points for this payer
            int balanceForPayer = _context.TransactionRecords.Select(x => x).
                Where(x => x.Payer == transactionRecord.Payer).
                Sum(x => x.Points);

            //check if adding the transaction's amount will cause a negative point balance.
            bool transactionIsSafe = (balanceForPayer + transactionRecord.Points) > 0;

            //if the addition of this transactionRecord does not cause a negative point balance for a given payer, then it is safe to add.
            if (transactionIsSafe)
            {
                _context.TransactionRecords.Add(transactionRecord);
                await _context.SaveChangesAsync();
            }

            return CreatedAtAction("GetTransactionRecord", new { id = transactionRecord.ID }, transactionRecord);
        }
    }
}
