using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SmartRoomFinder.Models;
using SmartRoomFinder.Data;
using SmartRoomFinder.Models.DTOs;
using SmartRoomFinder.Services.Interfaces;
using SmartRoomFinder.Helpers;
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

                if (parsedIntent.HasFilters)
                {
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
                                HaversineHelper.CalculateDistance(landmark.Latitude, landmark.Longitude, r.Latitude, r.Longitude) <= 5.0
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
                    response.ReplyText = !string.IsNullOrEmpty(parsedIntent.ReplyMessage) 
                        ? parsedIntent.ReplyMessage 
                        : $"Dạ, mình đã tìm thấy {response.SuggestedRooms.Count} phòng phù hợp. Bạn tham khảo danh sách bên dưới nhé!";
                        
                    if (response.SuggestedRooms.Count == 0)
                    {
                        response.ReplyText = "Tiếc quá, hiện tại mình chưa tìm thấy phòng nào khớp với toàn bộ yêu cầu của bạn. Bạn thử thay đổi tiêu chí xem sao nhé!";
                    }
                }
                else
                {
                    response.SuggestedRooms = new List<RecommendedRoomDto>();
                    response.ReplyText = !string.IsNullOrEmpty(parsedIntent.ReplyMessage) 
                        ? parsedIntent.ReplyMessage 
                        : "Dạ, bạn cần mình giúp tìm phòng ở khu vực nào ạ?";
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
You are a friendly AI assistant for a room rental platform in Vietnam.
You must analyze the user's message, reply conversationally in Vietnamese, and extract room search filters if the user is looking for a room.
Return JSON ONLY.

JSON Schema:
{
  ""ReplyMessage"": ""Your natural, friendly response to the user in Vietnamese."",
  ""HasFilters"": true or false (true if the user is searching for a room, false if it's just a greeting or general chat),
  ""Location"": ""string or null (e.g., 'Quận 7', 'Bình Thạnh')"",
  ""Landmark"": ""string or null (e.g., 'HUTECH', 'ĐH Bách Khoa')"",
  ""MinPrice"": ""number or null (in VND, e.g., 2000000)"",
  ""MaxPrice"": ""number or null (in VND, e.g., 4000000)"",
  ""RoomType"": ""string or null (exact match: 'Studio', 'Apartment', 'House', 'Villa')"",
  ""Amenities"": [""array of strings"", ""e.g."", ""máy lạnh"", ""ban công""]
}

Example 1 (Room Search):
User: 'Tìm studio gần HUTECH dưới 4 củ có máy lạnh'
-> { ""ReplyMessage"": ""Dạ, mình đã tìm thấy một số phòng Studio gần HUTECH giá dưới 4 triệu có máy lạnh. Bạn tham khảo danh sách bên dưới nhé!"", ""HasFilters"": true, ""Landmark"": ""HUTECH"", ""MaxPrice"": 4000000, ""RoomType"": ""Studio"", ""Amenities"": [""máy lạnh""] }

Example 2 (General Chat):
User: 'Bạn là ai?'
-> { ""ReplyMessage"": ""Chào bạn, mình là Trợ lý AI của SmartRoomFinder. Mình có thể giúp bạn tìm phòng trọ, căn hộ theo yêu cầu nhanh chóng. Bạn cần tìm phòng ở khu vực nào ạ?"", ""HasFilters"": false, ""Location"": null, ""Landmark"": null, ""MinPrice"": null, ""MaxPrice"": null, ""RoomType"": null, ""Amenities"": [] }
";

            var reqBody = new
            {
                model = "llama-3.3-70b-versatile",
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
    }
}
