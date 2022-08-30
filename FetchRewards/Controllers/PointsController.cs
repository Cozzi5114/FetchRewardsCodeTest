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
    public class PointsController : ControllerBase
    {
        private readonly TransactionRecordContext _context;

        public PointsController(TransactionRecordContext context)
        {
            _context = context;
        }


        /// <summary>
        /// Calculates and returns the point balance of all transaction records.
        /// </summary>
        /// <returns>The balance of points for each payer as JSON</returns>
        // GET: api/GetBalance
        [HttpGet]
        [Route("GetPointBalance")]
        public async Task<ActionResult<IEnumerable<TransactionRecord>>> GetPointBalance()
        {
            if (_context.TransactionRecords == null)
            {
                return NotFound();
            }

            List<TransactionBalanceResult> results = CalculatePointBalance();

            return new JsonResult(results);
        }


        /// <summary>
        /// Attempts to spend points and returns the new transactions created as JSON
        /// </summary>
        /// <param name="points">The amount of points to attempt to spend</param>
        /// <returns></returns>
        // POST: api/SpendPoints
        [HttpPost]
        [Route("SpendPoints")]
        public async Task<ActionResult<TransactionRecord>> PostSpendPoints(int points)
        {
            if (_context.TransactionRecords == null)
            {
                return Problem("Entity set 'TransactionRecordContext.TransactionRecords'  is null.");
            }

            List<SpendResult> results = SpendPoints(points);

            await _context.SaveChangesAsync();


            return new JsonResult(results);

        }


        /// <summary>
        /// attempts to spend points with the oldest transactions getting used first.
        /// </summary>
        /// <param name="amount">The amount of points to attempt to spend</param>
        /// <returns>a list of type SpendResult, which is an object used to hold the changes made in the transaction record </returns>
        private List<SpendResult> SpendPoints(int amount)
        {

            //select transaction records and order them by time.
            List<TransactionRecord> sortedTransactions = _context.TransactionRecords.Select(x => x).
                OrderBy(x => x.Timestamp).ToList();

            if (sortedTransactions.Count <= 0)
            {
                //if there are no results, throw error
            }

            bool notEnoughPointsAvailable = (sortedTransactions.Sum(X => X.Points)) <= amount;
            if (notEnoughPointsAvailable)
            {
                //if not enough points are available to subtract, throw error
                throw new Exception();
            }


            //a list to save our work on while we're iterating through the other list.
            List<TransactionRecord> subtractedTransactions = sortedTransactions;

            //account for the point deductions in theTransaction Record before we try to spend our points
            foreach (TransactionRecord tr in sortedTransactions)
            {
                //if we run into a point deduction
                 if (tr.Points < 0)
                {
                    //subtract points from the items in our working list 
                    subtractedTransactions = SubtractPointsByTransactionRecord(tr, subtractedTransactions);
                }
            }


            //create a new list to save our completed deductions
            List<TransactionRecord> transactionsCompleted = new List<TransactionRecord>();
            int newAmount = amount;
            int counter = 0;
            while (newAmount > 0)
            {
                TransactionRecord validTransaction = subtractedTransactions[counter];

                //save the amount before subtraction
                int oldAmount = newAmount;

                //subtract points from the item and save the new amount
                newAmount = subtractedTransactions.First(tr => tr.ID == validTransaction.ID).SubtractPoints(newAmount);

                //calculate the point difference 
                int pointsDeducted = oldAmount - newAmount;

                if (pointsDeducted != 0)
                {
                    //if points were subtracted, we create a new transaction and save it to the completedTransactions list
                    TransactionRecord newTransaction = new TransactionRecord(validTransaction.Payer, -pointsDeducted, DateTime.UtcNow);
                    transactionsCompleted.Add(newTransaction);
                }

                counter++;
            }


            //clear the changes we've made to the objects. Entity Framework keeps track of changes to these these objects and commits them to the database
            //even when we put them in a new list. Because we're using the Point property to keep track of things, we need to discard these changes,
            //that way we dont change the point values of the actual TransactionRecords. 
            _context.ChangeTracker.Clear();

            foreach (TransactionRecord tr in transactionsCompleted)
            {
                //save the newly created transactions to the TransactionRecord
                _context.TransactionRecords.Add(tr);
            }

            //create a list of type SpendResult to return the newly created transactions.
            List<SpendResult> results = new List<SpendResult>();
            foreach (TransactionRecord tr in transactionsCompleted)
            {
                results.Add(new SpendResult(tr));
            }

            return results;
        }


        /// <summary>
        /// calculates the balance of points and groups them by payer
        /// </summary>
        /// <returns></returns>
        private List<TransactionBalanceResult> CalculatePointBalance()
        {
            //a list of transactions, ordered by payer, and a results list to hold the calculated balances.
            List<TransactionRecord> orderedTransactions = _context.TransactionRecords.Select(x => x).OrderBy(x => x.Payer).ToList();
            List<TransactionBalanceResult> results = new List<TransactionBalanceResult>();

            //a temporary object to work with while we add up our balances
            TransactionBalanceResult currentResult = new TransactionBalanceResult(orderedTransactions[0].Payer);
            foreach (TransactionRecord tr in orderedTransactions)
            {
                //if payer doesnt match, we're at the end of the group, so save the old one and start over with a fresh result object. 
                if (tr.Payer != currentResult.Payer)
                {
                    //save current result and start new count
                    results.Add(currentResult);
                    currentResult = new TransactionBalanceResult(tr.Payer);
                }
                    currentResult.addPoints(tr.Points);
            }

            //add the final calculated Balance to the results
            results.Add(currentResult);

            return results;
        }

        private List<TransactionBalanceResult> CalculatePointBalanceWithLinq()
        {
            //here's how it would work in Linq?
            //So this method isnt called, but I feel like just throwing one line of Linq here was a cop out for a code test,
            //even if its probably more optimal. So the above method is how I wouldve done it without Linq, and is the one used by the webservice.

            //this basically says group records by payer, and then for each of those groups
            //create a new transactionBalanceResult object whose 'Payer' value is taken from the first result in the group,
            //and whose 'Points' value is the sum of all 'Points' values in the group
            List<TransactionBalanceResult> results = _context.TransactionRecords.GroupBy(x => x.Payer).
                Select(g => new TransactionBalanceResult(g.First().Payer, g.Sum(x => x.Points))).ToList();

            return results;
        }

        private List<TransactionRecord> SubtractPointsByTransactionRecord(TransactionRecord transaction, List<TransactionRecord> transactions)
        {
            //select transaction records with the correct payer, which are older than the transaction passed in,
            //which also have a positive point amount, then order them by time.
            List<TransactionRecord> validTransactionsFromThisPayer = _context.TransactionRecords.Select(x => x).
                Where(x => x.Payer == transaction.Payer).
                Where(x => x.Timestamp < transaction.Timestamp).
                Where(x => x.Points > 0).
                OrderBy(x => x.Timestamp).ToList();

            if (validTransactionsFromThisPayer.Count <= 0)
            {
                //if there are no results, throw error
                return transactions;
            }

            if ((validTransactionsFromThisPayer.Sum(X => X.Points)) < transaction.Points)
            {
                //if not enough points are available to subtract, throw error
                return transactions;
            }


            int counter = 0;
            //using absolute value for readability purposes. Could leave it negative and just add the negative value on the SubTractPoints function.
            int amountToSubtract = Math.Abs(transaction.Points);
            while (amountToSubtract > 0)
            {
                //while we still ahve points to subtract, get the id of transaction to be subtracted from.
                int id = validTransactionsFromThisPayer[counter].ID;

                //Find the object with the corresponding ID in the transactions list. subtract points from the original item. Increment and iterate.
                amountToSubtract = transactions.First(tr => tr.ID == id).SubtractPoints(amountToSubtract);
                counter++;
            }
            return transactions;
        }

        /// <summary>
        /// An object to hold resulting transactions of a spend operation.
        /// </summary>
        public class SpendResult
        {
            public string Payer { get; set; }
            public int Points { get; set; }

            public SpendResult(TransactionRecord tr)
            {
                Payer = tr.Payer;
                Points = tr.Points;
            }

        }

        /// <summary>
        /// an object to keep track of a balance calcualtion
        /// </summary>
        public class TransactionBalanceResult
        {
            public string Payer { get; set; }
            public int Points { get; set; }

            public TransactionBalanceResult(string payer)
            {
                Payer = payer;
                Points = 0;
            }

            //This constructor is only here for calculating balances with Linq. It is currently unused.
            public TransactionBalanceResult(string payer, int points)
            {
                Payer = payer;
                Points = points;
            }

            //Adds points. Didnt really have to make this but it just feels better to me architecture-wise. 
            internal void addPoints(int pointsToAdd)
            {
                    Points += pointsToAdd;
            }
        }

    }
}
