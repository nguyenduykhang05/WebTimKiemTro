using System.ComponentModel.DataAnnotations;

namespace SmartRoomFinder.Models
{
    public class LandmarkModel
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string Category { get; set; } = "University";
        
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        
        public bool IsActive { get; set; } = true;
    }
}
