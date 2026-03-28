using Backend.DTOs.Course;
using Backend.Models;
using Backend.Services.Interfaces;
using Backend.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CourseController : ControllerBase
    {
        private readonly ICourseService _service;

        public CourseController(ICourseService service)
        {
            _service = service;
        }

        [HttpGet("my")]
        [Authorize(Roles = "Teacher,Student")]
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
        [Authorize(Roles = "Teacher,Student")]
        public async Task<IActionResult> GetExamsForClass(int id)
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(idClaim) || !int.TryParse(idClaim, out var userId))
                return Unauthorized();

            var myCourses = await _service.GetCoursesForUserAsync(userId);
            if (!myCourses.Any(c => c.ClassId == id && c.Role != "Pending"))
                return Forbid();

            var exams = await _service.GetExamsByClassAsync(id);
            return Ok(exams);
        }

        // New: return chapters belonging to the class's subject
        // Route: GET api/course/{id}/chapters
        [HttpGet("{id}/chapters")]
        [Authorize]
        public async Task<IActionResult> GetChaptersForClass(int id)
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(idClaim) || !int.TryParse(idClaim, out var userId))
                return Unauthorized();

            var myCourses = await _service.GetCoursesForUserAsync(userId);
            if (!myCourses.Any(c => c.ClassId == id && c.Role != "Pending"))
                return Forbid();

            var course = await _service.GetByIdAsync(id);
            if (course == null) return NotFound();

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
        [Authorize(Roles = "Teacher")]
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
                // Typically you might use an ApiException filter or specific exceptions,
                // but for now catching generic exceptions matched in the Service layer is fine.
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}/students")]
        [Authorize(Roles = "Teacher,Student")]
        public async Task<IActionResult> GetStudentsInClass(int id)
        {
            var students = await _service.GetStudentsInClassAsync(id);
            return Ok(students);
        }

        [HttpGet("{id}/settings")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> GetClassSettings(int id)
        {
            var course = await _service.GetByIdAsync(id);
            if (course == null) return NotFound();
            return Ok(course);
        }

        [HttpPut("{id}/settings")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> UpdateClassSettings(int id, [FromBody] UpdateCourseSettingsRequestDTO request)
        {
            try
            {
                await _service.UpdateClassSettingsAsync(id, request.ClassName, request.InvitationCodeStatus);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{id}/invite")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> InviteStudent(int id, [FromBody] InviteStudentRequestDTO request)
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(idClaim) || !int.TryParse(idClaim, out var teacherId))
                return Unauthorized();

            try
            {
                var token = await _service.InviteStudentByEmailAsync(teacherId, id, request.Email);
                return Ok(new { message = "Đã gửi thư mời.", token }); // Sending token back for debugging/frontend copy just in case
            }
            catch (AutoApprovePendingException ex)
            {
                return Ok(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("accept-invite")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> AcceptInvite([FromBody] AcceptInviteRequestDTO request)
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(idClaim) || !int.TryParse(idClaim, out var studentId))
                return Unauthorized();

            try
            {
                await _service.AcceptInvitationAsync(studentId, request.Token);
                return Ok(new { message = "Tham gia lớp học thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}/students/pending")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> GetPendingStudents(int id)
        {
            var students = await _service.GetPendingStudentsAsync(id);
            return Ok(students);
        }

        [HttpPost("{id}/students/{studentId}/approve")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> ApproveStudent(int id, int studentId)
        {
            try {
                await _service.ApproveStudentAsync(id, studentId);
                return Ok();
            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}/students/{studentId}/reject")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> RejectStudent(int id, int studentId)
        {
            try {
                await _service.RejectStudentAsync(id, studentId);
                return Ok();
            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("subjects")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> GetSubjects()
        {
            var subjects = await _service.GetSubjectsAsync();
            return Ok(subjects);
        }
    }
}