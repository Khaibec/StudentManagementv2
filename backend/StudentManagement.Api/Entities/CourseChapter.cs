using System;
using System.Collections.Generic;

namespace StudentManagement.Api.Entities
{
    public class CourseChapter
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public Course Course { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int OrderIndex { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    }
}
