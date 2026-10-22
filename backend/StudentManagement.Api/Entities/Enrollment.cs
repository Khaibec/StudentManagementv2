using System;

namespace StudentManagement.Api.Entities
{
    public class Enrollment
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public Course Course { get; set; }
        public int StudentId { get; set; }
        public User Student { get; set; }
        public string Status { get; set; } = "Enrolled"; // Enrolled, Completed, Dropped
        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    }
}
