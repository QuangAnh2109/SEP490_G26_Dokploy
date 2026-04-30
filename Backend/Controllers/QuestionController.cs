using Backend.Common;
using Backend.DTOs.Question;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [Route("api/questions")]
    [ApiController]
    public class QuestionController(IQuestionService questionService) : ControllerBase
    {
        private readonly IQuestionService _questionService = questionService;

        [HttpGet]
        [Authorize(Roles = RoleIds.Teacher)]
        public async Task<IActionResult> GetQuestionsAsync([FromQuery] QuestionListQueryDto query)
        {
            var result = await _questionService.GetQuestionsAsync(query);
            return result.ToActionResult(this);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = RoleIds.Teacher)]
        public async Task<IActionResult> GetQuestionByIdAsync(int id)
        {
            var result = await _questionService.GetQuestionByIdAsync(id);
            return result.ToActionResult(this);
        }

        [HttpPost]
        [Authorize(Roles = RoleIds.Teacher)]
        public async Task<IActionResult> CreateQuestionsAsync([FromBody] List<QuestionDto> request)
        {
            var result = await _questionService.CreateQuestionsAsync(request);
            return result.ToActionResult(this);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = RoleIds.Teacher)]
        public async Task<IActionResult> UpdateQuestionAsync(int id, [FromBody] QuestionDto request)
        {
            var result = await _questionService.UpdateQuestionAsync(id, request);
            return result.ToActionResult(this);
        }

        [HttpPatch("status")]
        [Authorize(Roles = RoleIds.Teacher)]
        public async Task<IActionResult> UpdateQuestionStatusAsync([FromBody] QuestionStatusUpdateDto request)
        {
            var result = await _questionService.UpdateQuestionStatusAsync(request.QuestionIds!, request.Status!);
            return result.ToActionResult(this);
        }

        [HttpGet("metadata")]
        [Authorize(Roles = RoleIds.Teacher)]
        public async Task<IActionResult> GetMetadataAsync()
        {
            var result = await _questionService.GetQuestionMetadataAsync();
            return result.ToActionResult(this);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = RoleIds.Teacher)]
        public async Task<IActionResult> DeleteQuestionAsync(int id)
        {
            var result = await _questionService.DeleteQuestionAsync(id);
            return result.ToActionResult(this);
        }
    }
}
