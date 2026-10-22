using System;
using System.ComponentModel.DataAnnotations;

namespace StudentManagement.Api.DTOs
{
    public class CourseDto
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int TeacherId { get; set; }
        public string TeacherName { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsEnrolled { get; set; }
        public int EnrollmentCount { get; set; }
    }

    public class CreateCourseRequest
    {
        [Required(ErrorMessage = "Mã khóa học không được để trống")]
        [MaxLength(50, ErrorMessage = "Mã khóa học không được quá 50 ký tự")]
        public string Code { get; set; }

        [Required(ErrorMessage = "Tên khóa học không được để trống")]
        [MaxLength(250, ErrorMessage = "Tên khóa học không được quá 250 ký tự")]
        public string Title { get; set; }

        public string Description { get; set; }

        public int? TeacherId { get; set; }
    }

    public class UpdateCourseRequest
    {
        [Required(ErrorMessage = "Tên khóa học không được để trống")]
        [MaxLength(250, ErrorMessage = "Tên khóa học không được quá 250 ký tự")]
        public string Title { get; set; }

        public string Description { get; set; }

        [Required(ErrorMessage = "Trạng thái không được để trống")]
        public string Status { get; set; }
    }
}
