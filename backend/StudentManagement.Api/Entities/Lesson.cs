using System;
using System.Collections.Generic;

namespace StudentManagement.Api.Entities
{
    public class Lesson
    {
        public int Id { get; set; }
        public int ChapterId { get; set; }
        public CourseChapter Chapter { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int OrderIndex { get; set; }
        public string VideoUrl { get; set; }
        public bool IsPublished { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<LessonResource> Resources { get; set; } = new List<LessonResource>();
        public ICollection<LessonProgress> Progresses { get; set; } = new List<LessonProgress>();
    }
}
