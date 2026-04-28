using Backend.Common;
using Backend.DTOs.Question;
using Backend.Exceptions;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers
{
    [Route("api/questions")]
    [ApiController]
    [Authorize(Policy = nameof(Roles.Teacher))]
    public class QuestionController : BaseController
    {
        private readonly IQuestionService _questionService;

        public QuestionController(IQuestionService questionService)
        {
            _questionService = questionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetQuestionsAsync([FromQuery] QuestionListQueryDto query)
        {
            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                return Unauthorized(new { message = "Invalid token." });
            }

            var result = await _questionService.GetQuestionsAsync(query, userId);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetQuestionByIdAsync(int id)
        {
            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                return Unauthorized(new { message = "Invalid token." });
            }

            var result = await _questionService.GetQuestionByIdAsync(id, userId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateQuestionsAsync([FromBody] List<QuestionDto> request)
        {
            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                return Unauthorized(new { message = "Invalid token." });
            }

            var result = await _questionService.CreateQuestionsAsync(userId, request);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateQuestionAsync(int id, [FromBody] QuestionDto request)
        {
            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                return Unauthorized(new { message = "Invalid token." });
            }

            var result = await _questionService.UpdateQuestionAsync(id, userId, request);
            return Ok(result);
        }

        [HttpPatch("status")]
        public async Task<IActionResult> UpdateQuestionStatusAsync([FromBody] QuestionStatusUpdateDto request)
        {
            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                return Unauthorized(new { message = "Invalid token." });
            }

            if (request == null || !(request.QuestionIds?.Any() ?? false) || string.IsNullOrWhiteSpace(request.Status))
            {
                return BadRequest(new { message = "Question IDs and Status are required." });
            }

            var count = await _questionService.UpdateQuestionStatusAsync(request.QuestionIds, userId, request.Status);
            return Ok(new { message = $"Đã cập nhật trạng thái cho {count} câu hỏi thành công.", count });
        }

        [HttpGet("metadata")]
        public async Task<IActionResult> GetMetadataAsync()
        {
            var result = await _questionService.GetQuestionMetadataAsync();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteQuestionAsync(int id)
        {
            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                return Unauthorized(new { message = "Invalid token." });
            }

            await _questionService.DeleteQuestionAsync(id, userId);
            return Ok(new { message = "Đã xóa câu hỏi thành công." });
        }


    }
}
