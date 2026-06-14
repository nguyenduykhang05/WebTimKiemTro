using Microsoft.AspNetCore.Mvc;
using SmartRoomFinder.Models.DTOs;
using SmartRoomFinder.Services.Interfaces;
using System.Threading.Tasks;

namespace SmartRoomFinder.Controllers.Api
{
    [Route("api/ai-assistant")]
    [ApiController]
    public class AIAssistantApiController : ControllerBase
    {
        private readonly IAIAssistantService _aiService;

        public AIAssistantApiController(IAIAssistantService aiService)
        {
            _aiService = aiService;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] AIChatRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { success = false, message = "Tin nhắn không được để trống" });
            }

            // Ensure SessionId exists
            if (string.IsNullOrEmpty(request.SessionId))
            {
                request.SessionId = HttpContext.Session?.Id ?? System.Guid.NewGuid().ToString();
            }

            var response = await _aiService.ProcessUserMessageAsync(request);

            return Ok(new
            {
                success = !response.IsError,
                data = response
            });
        }
    }
}
