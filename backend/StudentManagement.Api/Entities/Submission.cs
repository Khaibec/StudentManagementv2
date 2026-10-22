using System;

namespace StudentManagement.Api.Entities
{
    public class Submission
    {
        public int Id { get; set; }
        public int AssignmentId { get; set; }
        public Assignment Assignment { get; set; }
        public int StudentId { get; set; }
        public User Student { get; set; }
        public string TextContent { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } // SUBMITTED_ON_TIME, SUBMITTED_LATE
        public decimal? Score { get; set; }
        public string Feedback { get; set; }
        public DateTime? GradedAt { get; set; }
        public int? GradedById { get; set; }
        public User GradedBy { get; set; }
    }
}
