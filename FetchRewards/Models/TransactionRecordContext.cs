using Microsoft.EntityFrameworkCore;

namespace FetchRewards.Models
{
    public class TransactionRecordContext : DbContext
    {

        public TransactionRecordContext(DbContextOptions<TransactionRecordContext> options)
            : base(options)
        {
        }

        public DbSet<TransactionRecord> TransactionRecords { get; set; } = null!;

    }
}
