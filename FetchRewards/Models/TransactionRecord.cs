using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FetchRewards.Models
{
    public class TransactionRecord
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public string Payer { get; set; }
        public int Points { get; set; }
        public DateTime Timestamp { get; set; }

        public TransactionRecord() 
        { 
        }

        public TransactionRecord(string payer, int points, DateTime timestamp)
        {
            Payer = payer;
            Points = points;
            Timestamp = timestamp;
        }

        public int SubtractPoints(int amount)
        {
            //if no points to subtract, return.
            if (Points <= 0)
            {
                return amount;
            }
            
            int newAmount = amount;

            //if subtracting the amount would result in a negative or 0 Points value
            if ((Points - amount) <= 0)
            {
                //subtract all apossible points from the amount
                newAmount = amount - Points;

                //we've subtracted all the points. Set points to 0.
                Points = 0;

                //return extra points to rollover to the next newest transaction record
                return newAmount;
            }
            else
            {
                Points = Points - newAmount;

                //subtraction was fully completed, return 0
                return 0;
            }
        }
    }
}
