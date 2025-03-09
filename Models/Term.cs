using SQLite;

namespace C971.Models
{
    public class Term
    {
        [PrimaryKey, AutoIncrement]
        public int TermId { get; set; }
        public string? Title { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}