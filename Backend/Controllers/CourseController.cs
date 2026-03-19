using Backend.DTOs.Course;
using Backend.DTOs.ExamBlueprint;
using Backend.Models;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CourseController : ControllerBase
    {
        private readonly ICourseService _service;
        private readonly IChapterService _chapterService;
        private readonly ILogger<CourseController> _logger;

        public CourseController(ICourseService service, IChapterService chapterService, ILogger<CourseController> logger)
        {
            _service = service;
            _chapterService = chapterService;
            _logger = logger;
        }


        [HttpGet("my")]
        [Authorize(Roles = "Teacher,Student,Giáo viên,Học sinh")]
        public async Task<IActionResult> GetMyClasses()
        {
            // Keep same behaviour as before; now it will run without auth
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(idClaim) || !int.TryParse(idClaim, out var userId))
            {
                // If no user id available, return all classes for quick verification or return BadRequest.
                return BadRequest("No user id claim; for debug you can return all or set a default id.");
            }

            var result = await _service.GetCoursesForUserAsync(userId);
            return Ok(result);
        }

        // New: return exams for a class that are visible now
        [HttpGet("{id}/exams")]
        [Authorize(Roles = "Teacher,Student,Giáo viên,Học sinh")]
        public async Task<IActionResult> GetExamsForClass(int id)
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
            bool isTeacher = User.IsInRole("Teacher") || User.IsInRole("Giáo viên");
            _logger.LogInformation("GetExamsForClass: id={id}, userId={userId}, isTeacher={isTeacher}", id, idClaim, isTeacher);
            var exams = await _service.GetExamsByClassAsync(id, isTeacher);
            return Ok(exams);
        }

        [HttpGet("{id}/chapters")]
        [Authorize(Roles = "Teacher,Student,Giáo viên,Học sinh")]
        public async Task<IActionResult> GetChaptersForClass(int id)
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
            var course = await _service.GetByIdAsync(id);

            if (course == null)
                return NotFound();

            return Ok(course.Chapters);
        }
        [HttpPost("{id}/leave")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> LeaveCourse(int id)
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(idClaim) || !int.TryParse(idClaim, out var userId))
            {
                return Unauthorized();
            }
            try
            {
                await _service.LeaveCourseAsync(id, userId);
                return Ok(new { message = "Rời lớp thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("join")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> JoinCourse([FromBody] JoinCourseRequestDTO request)
        {
            if (string.IsNullOrWhiteSpace(request?.InvitationCode))
            {
                return BadRequest("Mã mời không thể trống.");
            }

            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(idClaim) || !int.TryParse(idClaim, out var userId))
            {
                return Unauthorized();
            }

            try
            {
                await _service.JoinCourseAsync(userId, request.InvitationCode);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Teacher,Giáo viên")]
        public async Task<IActionResult> CreateCourse([FromBody] CreateCourseRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(idClaim) || !int.TryParse(idClaim, out var userId))
            {
                return Unauthorized();
            }

            try
            {
                var result = await _service.CreateCourseAsync(userId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}/students")]
        [Authorize(Roles = "Teacher,Student,Giáo viên,Học sinh")]
        public async Task<IActionResult> GetStudentsInClass(int id)
        {
            var students = await _service.GetStudentsInClassAsync(id);
            return Ok(students);
        }

        [HttpGet("{id}/settings")]
        [Authorize(Roles = "Teacher,Giáo viên")]
        public async Task<IActionResult> GetClassSettings(int id)
        {
            var course = await _service.GetByIdAsync(id);
            if (course == null) return NotFound();
            return Ok(course);
        }

        [HttpPut("{id}/settings")]
        [Authorize(Roles = "Teacher,Giáo viên")]
        public async Task<IActionResult> UpdateClassSettings(int id, [FromBody] UpdateCourseSettingsRequestDTO request)
        {
            if (string.IsNullOrWhiteSpace(request.ClassName))
                return BadRequest("Tên lớp không được để trống.");

            var success = await _service.UpdateClassSettingsAsync(id, request.ClassName, request.InvitationCodeStatus);
            if (!success) return NotFound("Không tìm thấy lớp học.");

            return Ok();
        }

        [HttpGet("subjects")]
        [Authorize(Roles = "Teacher,Giáo viên")]
        public async Task<IActionResult> GetSubjects()
        {
            var subjects = await _service.GetSubjectsAsync();
            return Ok(subjects);
        }
    }
}