namespace Fetch.Models
{
    public class TransactionRecord
    {
        public int ID { get; set; }
        int OwnerID;

        public string Payer { get; set; }
        int Points;
        public int PointsRemaining { get; set; }
        public DateTime Timestamp { get; set; }

        public TransactionRecord(int ownerID, int transactionID, string payer, int points, int pointsRemaining, DateTime timestamp)
        {
            OwnerID = ownerID;
            ID = transactionID;
            Payer = payer;
            Points = points;
            Timestamp = timestamp;
            PointsRemaining = pointsRemaining;

            if (points <= 0)
            {
                PointsRemaining = 0;
            }
        }

        public TransactionRecord(string payer, int points, DateTime timestamp)
        {
            //OwnerID = ownerID;
            //ID = transactionID;
            Payer = payer;
            Points = points;
            Timestamp = timestamp;
            PointsRemaining = points;

            if (points <= 0)
            {
                PointsRemaining = 0;
            }
        }

        public TransactionRecord()
        {
            //if (Payer == null)
            //{
            //    Payer = "";
            //}
        }

            public int SubtractPoints(int amount)
        {
            int newAmount = amount;
            if (PointsRemaining <= 0)
            {
                return amount;
            }


            if ((PointsRemaining - amount) <= 0)
            {
                //spend all available points for this transaction record
                newAmount = amount - PointsRemaining;
                PointsRemaining = 0;

                //return extra points to rollover to the next newest transaction record
                return newAmount;
            }
            else
            {
                PointsRemaining = PointsRemaining - newAmount;

                //spend was completed successfully, return 0
                return 0;
            }


        }
    }
}
