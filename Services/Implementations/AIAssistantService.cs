using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SmartRoomFinder.Models;
using SmartRoomFinder.Models.DTOs;
using SmartRoomFinder.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartRoomFinder.Services.Implementations
{
    public class AIAssistantService : IAIAssistantService
    {
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly string _openAiApiKey;

        public AIAssistantService(AppDbContext context, HttpClient httpClient, IConfiguration configuration)
        {
            _context = context;
            _httpClient = httpClient;
            _openAiApiKey = configuration["OpenAI_ApiKey"] ?? "";
        }

        public async Task<AIChatResponseDto> ProcessUserMessageAsync(AIChatRequestDto request)
        {
            var response = new AIChatResponseDto();

            if (string.IsNullOrEmpty(_openAiApiKey))
            {
                response.IsError = true;
                response.ReplyText = "Hệ thống AI đang được bảo trì hoặc chưa cấu hình API Key. Bạn vui lòng sử dụng bộ lọc thủ công nhé!";
                return response;
            }

            try
            {
                // 1. Call OpenAI to extract JSON
                var parsedIntent = await ExtractIntentWithOpenAI(request.Message);
                response.ExtractedFilters = parsedIntent;
                string extractedJsonStr = JsonSerializer.Serialize(parsedIntent);

                // 2. Build Query
                var query = _context.Rooms.Where(r => r.IsActive && !r.IsDraft).AsQueryable();

                // 2.1 Price
                if (parsedIntent.MinPrice.HasValue) query = query.Where(r => r.Price >= parsedIntent.MinPrice.Value);
                if (parsedIntent.MaxPrice.HasValue) query = query.Where(r => r.Price <= parsedIntent.MaxPrice.Value);

                // 2.2 Room Type
                if (!string.IsNullOrEmpty(parsedIntent.RoomType))
                {
                    if (Enum.TryParse<RoomType>(parsedIntent.RoomType, true, out var rType))
                    {
                        query = query.Where(r => r.Type == rType);
                    }
                }

                // 2.3 Location (Text based)
                if (!string.IsNullOrEmpty(parsedIntent.Location))
                {
                    query = query.Where(r => r.Location.Contains(parsedIntent.Location) || r.Address.Contains(parsedIntent.Location));
                }

                var candidateRooms = await query.ToListAsync();

                // 2.4 Landmark (Radius Search)
                if (!string.IsNullOrEmpty(parsedIntent.Landmark))
                {
                    var landmark = await _context.Landmarks.FirstOrDefaultAsync(l => l.Name.Contains(parsedIntent.Landmark));
                    if (landmark != null)
                    {
                        // Lọc bán kính 5km
                        candidateRooms = candidateRooms.Where(r => 
                            CalculateHaversineDistance(landmark.Latitude, landmark.Longitude, r.Latitude, r.Longitude) <= 5.0
                        ).ToList();
                    }
                }

                // 2.5 Amenities
                if (parsedIntent.Amenities != null && parsedIntent.Amenities.Any())
                {
                    candidateRooms = candidateRooms.Where(r => 
                        parsedIntent.Amenities.All(a => r.Amenities.Any(ra => ra.Contains(a, StringComparison.OrdinalIgnoreCase)))
                    ).ToList();
                }

                // Map to DTO
                response.SuggestedRooms = candidateRooms.Take(5).Select(r => new RecommendedRoomDto
                {
                    Id = r.Id,
                    Title = r.Title,
                    Price = r.Price,
                    Area = r.Area,
                    Address = r.Address,
                    MainImageUrl = r.MainImageUrl,
                    SimilarityScore = 100, // Matching filter
                    MatchReason = "Khớp với yêu cầu tìm kiếm"
                }).ToList();

                // 3. Generate Reply
                response.ReplyText = $"Dạ, dựa vào yêu cầu của bạn, mình đã tìm thấy {response.SuggestedRooms.Count} phòng phù hợp. Bạn tham khảo danh sách bên dưới nhé!";
                if (response.SuggestedRooms.Count == 0)
                {
                    response.ReplyText = "Tiếc quá, hiện tại mình chưa tìm thấy phòng nào khớp với toàn bộ yêu cầu của bạn. Bạn thử thay đổi tiêu chí xem sao nhé!";
                }

                // 4. Save Log
                var log = new AIChatLogModel
                {
                    SessionId = request.SessionId,
                    UserMessage = request.Message,
                    BotReply = response.ReplyText,
                    ExtractedJson = extractedJsonStr,
                    CreatedAt = DateTime.UtcNow
                };
                _context.AIChatLogs.Add(log);
                await _context.SaveChangesAsync();

                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AIAssistant Error]: {ex.Message}");
                response.IsError = true;
                response.ReplyText = "Hệ thống AI đang quá tải, bạn vui lòng sử dụng bộ lọc thủ công hoặc thử lại sau ít phút nhé!";
                return response;
            }
        }

        private async Task<ParsedIntentDto> ExtractIntentWithOpenAI(string message)
        {
            var prompt = @"
You are an AI assistant for a room rental platform in Vietnam.
Extract filters from the user's message.
Return JSON ONLY.

JSON Schema:
{
  ""Location"": ""string or null (e.g., 'Quận 7', 'Bình Thạnh')"",
  ""Landmark"": ""string or null (e.g., 'HUTECH', 'ĐH Bách Khoa')"",
  ""MinPrice"": ""number or null (in VND, e.g., 2000000)"",
  ""MaxPrice"": ""number or null (in VND, e.g., 4000000)"",
  ""RoomType"": ""string or null (exact match: 'Studio', 'Apartment', 'House', 'Villa')"",
  ""Amenities"": [""array of strings"", ""e.g."", ""máy lạnh"", ""ban công""]
}

Example: 'Tìm studio gần HUTECH dưới 4 củ có máy lạnh'
-> { ""Landmark"": ""HUTECH"", ""MaxPrice"": 4000000, ""RoomType"": ""Studio"", ""Amenities"": [""máy lạnh""] }
";

            var reqBody = new
            {
                model = "mixtral-8x7b-32768",
                messages = new[]
                {
                    new { role = "system", content = prompt },
                    new { role = "user", content = message }
                },
                response_format = new { type = "json_object" },
                temperature = 0
            };

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openAiApiKey);
            requestMessage.Content = new StringContent(JsonSerializer.Serialize(reqBody), Encoding.UTF8, "application/json");

            var res = await _httpClient.SendAsync(requestMessage);
            res.EnsureSuccessStatusCode();

            var jsonResponse = await res.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonResponse);
            var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

            if (content == null) return new ParsedIntentDto();

            return JsonSerializer.Deserialize<ParsedIntentDto>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ParsedIntentDto();
        }

        private double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // km
            var dLat = Math.PI * (lat2 - lat1) / 180.0;
            var dLon = Math.PI * (lon2 - lon1) / 180.0;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(Math.PI * lat1 / 180.0) * Math.Cos(Math.PI * lat2 / 180.0) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }
    }
}
