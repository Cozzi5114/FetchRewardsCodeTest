using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Fetch.Models
{
    public class User : DbContext
    {
        int ID;
        List<TransactionRecord> Transactions;

        public User(int id, List<TransactionRecord> transactions)
        {
            ID = id;
            Transactions = transactions;
        }

        public TransactionRecord CreateTransaction(int transactionID, string payer, int amount, DateTime dateTime)
        {
            if (amount > 0)
            {
                //if this is a point addition, simply add the new transaction.
                Transactions.Add(new TransactionRecord(ID, transactionID, payer, amount, amount, dateTime));
            }
            else
            {
                //
                DeductPoints(payer, Math.Abs(amount));

                //save a record of this transaction
                Transactions.Add(new TransactionRecord(ID, transactionID, payer, amount, 0, dateTime));
            }
            
            //return the last transaction in case it's needed by the spend() function.
            return Transactions.Last();
        }

        public void DeductPoints(string payer, int amount)
        {
            //select transaction records with the correct payer which still have points remaining to subtract, then order them by time.
            List<TransactionRecord> validTransactionsFromThisPayer = Transactions.Select(x => x).
                Where(x => x.Payer == payer).
                Where(x => x.PointsRemaining > 0).
                OrderBy(x => x.Timestamp).ToList();

            if (validTransactionsFromThisPayer.Count <= 0)
            {
                //if there are no results, throw error
            }

            if ((validTransactionsFromThisPayer.Sum(X => X.PointsRemaining)) < amount)
            {
                //if not enough points are available to subtract, throw error
            }


            //while we still have points to subtract 
            int counter = 0;
            while (amount > 0)
            {
                //get the matching index of the oldest transaction from the master transaction list.
                int indexInOriginalList = Transactions.FindIndex(x => x.ID == validTransactionsFromThisPayer[counter].ID);

                if (indexInOriginalList < 0)
                {
                    //means a matching index wasnt found, we just made this list, this shouldn't ever happen, throw error.
                }

                //subtract points from the original item in the master list which will also commit the changes to the item in the master list
                amount = Transactions[indexInOriginalList].SubtractPoints(amount);

                counter++;
            }
        }



        public List<TransactionRecord> Spend(int amount)
        {

            //select transaction records with the correct payer which still have points remaining to subtract, then order them by time.
            List<TransactionRecord> validTransactions = Transactions.Select(x => x).
                Where(x => x.PointsRemaining > 0).
                OrderBy(x => x.Timestamp).ToList();

            //if there are no results, throw error
            if (validTransactions.Count <= 0)
            {
                //throw error
            }

            bool EnoughPointsAvailable = (validTransactions.Sum(X => X.PointsRemaining)) >= amount;
            //if not enough points are available to subtract, throw error
            if (!EnoughPointsAvailable)
            {
                //throw error
            }


            //while we still have points to subtract 
            List<TransactionRecord> transactionsCompleted = new List<TransactionRecord>();
            int counter = 0;
            int newAmount = amount;
            int highestID = Transactions.OrderByDescending(x => x.ID).First().ID;
            while (newAmount > 0)
            {
                //get the matching index from the master transaction list.
                int indexInOriginalList = Transactions.FindIndex(x => x.ID == validTransactions[counter].ID);

                if (indexInOriginalList < 0)
                {
                    //means a matching index wasnt found, this shouldn't ever happen, but throw error.
                }
                //save the previous amount
                int oldAmount = newAmount;
                
                //subtract points from the original item, which will also commit the changes to the item in the list
                newAmount = Transactions[indexInOriginalList].SubtractPoints(newAmount);

                //calculate the point difference
                int pointsDeducted = oldAmount - newAmount;

                highestID++;

                //create a new transaction, and save it in the completed transactions list.
                transactionsCompleted.Add(new TransactionRecord(ID, highestID, Transactions[indexInOriginalList].Payer, -pointsDeducted, 0, DateTime.UtcNow));

                counter++;
            }


            foreach (TransactionRecord tr in transactionsCompleted)
            {
                Transactions.Add(tr);
            }

            return transactionsCompleted;
        }

        public string GetPointBalance()
        {
            List<TransactionRecord> orderedTransactions = Transactions.Select(x => x).OrderBy(x => x.Payer).ToList();
            string output = "";
            string currentPayer = orderedTransactions.First().Payer;
            int currentAmount = 0;
            foreach (TransactionRecord tr in orderedTransactions)
            {
                if (tr.Payer != currentPayer)
                {
                    //start new count and save current result
                    output += "\""+currentPayer.ToUpper()+"\": "+currentAmount+",";
                    currentAmount = 0;
                    currentPayer = tr.Payer;
                }
                currentAmount = currentAmount + tr.PointsRemaining;
                string s = System.Text.Json.JsonSerializer.Serialize<TransactionRecord>(tr);
                s += "this is for test pls delete me";
            }

            output += "\"" + currentPayer.ToUpper() + "\": " + currentAmount;
            return output;

        }
    }
}
