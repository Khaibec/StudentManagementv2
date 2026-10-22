using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StudentManagement.Api.DTOs
{
    public class AssignmentDto
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime DueDate { get; set; }
        public decimal MaxScore { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsOverdue { get; set; }

        // Student's own submission summary
        public bool HasSubmitted { get; set; }
        public string SubmissionStatus { get; set; } // NOT_SUBMITTED, SUBMITTED_ON_TIME, SUBMITTED_LATE
        public decimal? Score { get; set; }
        public bool IsGraded { get; set; }

        // Teacher's view
        public int SubmissionCount { get; set; }
        public int GradedCount { get; set; }
    }

    public class AssignmentDetailDto
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public string CourseTitle { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime DueDate { get; set; }
        public decimal MaxScore { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsOverdue { get; set; }

        // Current student's submission (if student)
        public SubmissionDto MySubmission { get; set; }
    }

    public class CreateAssignmentRequest
    {
        [Required(ErrorMessage = "Tiêu đề bài tập không được để trống")]
        [MaxLength(250, ErrorMessage = "Tiêu đề không được vượt quá 250 ký tự")]
        public string Title { get; set; }

        public string Description { get; set; }

        [Required(ErrorMessage = "Hạn nộp không được để trống")]
        public DateTime DueDate { get; set; }

        [Range(1, 1000, ErrorMessage = "Điểm tối đa phải từ 1 đến 1000")]
        public decimal MaxScore { get; set; } = 100m;

        public bool IsActive { get; set; } = true;
    }

    public class UpdateAssignmentRequest
    {
        [Required(ErrorMessage = "Tiêu đề bài tập không được để trống")]
        [MaxLength(250, ErrorMessage = "Tiêu đề không được vượt quá 250 ký tự")]
        public string Title { get; set; }

        public string Description { get; set; }

        [Required(ErrorMessage = "Hạn nộp không được để trống")]
        public DateTime DueDate { get; set; }

        [Range(1, 1000, ErrorMessage = "Điểm tối đa phải từ 1 đến 1000")]
        public decimal MaxScore { get; set; } = 100m;

        public bool IsActive { get; set; } = true;
    }

    public class SubmissionDto
    {
        public int Id { get; set; }
        public int AssignmentId { get; set; }
        public string AssignmentTitle { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string StudentUsername { get; set; }
        public string TextContent { get; set; }
        public string FileName { get; set; }
        public bool HasFile => !string.IsNullOrEmpty(FileName);
        public DateTime SubmittedAt { get; set; }
        public string Status { get; set; } // SUBMITTED_ON_TIME, SUBMITTED_LATE
        public decimal? Score { get; set; }
        public string Feedback { get; set; }
        public DateTime? GradedAt { get; set; }
        public string GradedByName { get; set; }
        public bool IsGraded => Score.HasValue;
    }

    public class GradeSubmissionRequest
    {
        [Required(ErrorMessage = "Điểm số không được để trống")]
        public decimal Score { get; set; }

        public string Feedback { get; set; }
    }
}
