using System;
using System.Collections.Generic;

namespace StudentManagement.Api.Entities
{
    public class Course
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int TeacherId { get; set; }
        public User Teacher { get; set; }
        public string Status { get; set; } = "Active"; // Active, Inactive, Archived
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public ICollection<CourseChapter> Chapters { get; set; } = new List<CourseChapter>();
        public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    }
}
