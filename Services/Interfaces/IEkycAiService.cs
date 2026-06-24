using System.Threading.Tasks;

namespace SmartRoomFinder.Services.Interfaces
{
    public interface IEkycAiService
    {
        /// <summary>
        /// Gửi ảnh CCCD mặt trước lên Python AI Service để OCR bóc tách thông tin.
        /// Trả về JSON chứa: identity_card_number, full_name, date_of_birth.
        /// </summary>
        Task<EkycOcrResult> ExtractCccdInfoAsync(string frontImageUrl);

        /// <summary>
        /// Gửi ảnh CCCD và ảnh Selfie lên Python AI Service để so khớp khuôn mặt.
        /// Trả về: match_score (0.0-1.0), is_match (bool).
        /// </summary>
        Task<EkycFaceMatchResult> CompareFacesAsync(string cccdImageUrl, string selfieImageUrl);
    }

    public class EkycOcrResult
    {
        public bool Success { get; set; }
        public string IdentityCardNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string DateOfBirth { get; set; } = string.Empty;
        public string RawJson { get; set; } = "{}";
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class EkycFaceMatchResult
    {
        public bool Success { get; set; }
        public double MatchScore { get; set; }
        public bool IsMatch { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
