using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StudentManagement.Api.DTOs
{
    // Chapter DTOs
    public class ChapterDto
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int OrderIndex { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<LessonSummaryDto> Lessons { get; set; } = new List<LessonSummaryDto>();
    }

    public class CreateChapterRequest
    {
        [Required(ErrorMessage = "Tiêu đề chương không được để trống")]
        [MaxLength(250, ErrorMessage = "Tiêu đề không được vượt quá 250 ký tự")]
        public string Title { get; set; }

        public string Description { get; set; }

        public int OrderIndex { get; set; } = 0;
    }

    public class UpdateChapterRequest
    {
        [Required(ErrorMessage = "Tiêu đề chương không được để trống")]
        [MaxLength(250, ErrorMessage = "Tiêu đề không được vượt quá 250 ký tự")]
        public string Title { get; set; }

        public string Description { get; set; }

        public int OrderIndex { get; set; } = 0;
    }

    // Lesson DTOs
    public class LessonSummaryDto
    {
        public int Id { get; set; }
        public int ChapterId { get; set; }
        public string Title { get; set; }
        public int OrderIndex { get; set; }
        public bool IsPublished { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int ResourceCount { get; set; }
    }

    public class LessonDetailDto
    {
        public int Id { get; set; }
        public int ChapterId { get; set; }
        public string ChapterTitle { get; set; }
        public int CourseId { get; set; }
        public string CourseTitle { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int OrderIndex { get; set; }
        public string VideoUrl { get; set; }
        public bool IsPublished { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
        public List<ResourceDto> Resources { get; set; } = new List<ResourceDto>();
    }

    public class CreateLessonRequest
    {
        [Required(ErrorMessage = "Tiêu đề bài học không được để trống")]
        [MaxLength(250, ErrorMessage = "Tiêu đề không được vượt quá 250 ký tự")]
        public string Title { get; set; }

        public string Description { get; set; }

        public int OrderIndex { get; set; } = 0;

        public string VideoUrl { get; set; }

        public bool IsPublished { get; set; } = true;
    }

    public class UpdateLessonRequest
    {
        [Required(ErrorMessage = "Tiêu đề bài học không được để trống")]
        [MaxLength(250, ErrorMessage = "Tiêu đề không được vượt quá 250 ký tự")]
        public string Title { get; set; }

        public string Description { get; set; }

        public int OrderIndex { get; set; } = 0;

        public string VideoUrl { get; set; }

        public bool IsPublished { get; set; } = true;
    }

    // Resource DTOs
    public class ResourceDto
    {
        public int Id { get; set; }
        public int LessonId { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public long FileSize { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // Progress DTOs
    public class LessonProgressDto
    {
        public int LessonId { get; set; }
        public int StudentId { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
