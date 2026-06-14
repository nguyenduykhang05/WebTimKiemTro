using SmartRoomFinder.Models.DTOs;
using System.Threading.Tasks;

namespace SmartRoomFinder.Services.Interfaces
{
    public interface IAIAssistantService
    {
        Task<AIChatResponseDto> ProcessUserMessageAsync(AIChatRequestDto request);
    }
}
