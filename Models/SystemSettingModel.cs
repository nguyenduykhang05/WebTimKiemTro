using System.ComponentModel.DataAnnotations;

namespace SmartRoomFinder.Models
{
    public class SystemSettingModel
    {
        [Key]
        public string Key { get; set; } = string.Empty;
        
        public string Value { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
    }
}
