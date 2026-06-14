namespace SmartRoomFinder.Models.DTOs
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int Count { get; set; }
        public T? Data { get; set; }

        public static ApiResponse<T> Ok(T data, int count = 0, string message = "Success")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Count = count,
                Data = data
            };
        }

        public static ApiResponse<T> Fail(string message)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Count = 0,
                Data = default
            };
        }
    }
}
