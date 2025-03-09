using SQLite;

namespace C971.Models
{
    public class Course
    {
        [PrimaryKey, AutoIncrement]
        public int CourseId { get; set; }
        public int TermId { get; set; }
        public string? Title { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Status { get; set; }
        public string? InstructorName { get; set; }
        public string? InstructorPhone { get; set; }
        public string? InstructorEmail { get; set; }
        public string? Notes { get; set; }
        public bool EnableNotifications { get; set; }
    }
}