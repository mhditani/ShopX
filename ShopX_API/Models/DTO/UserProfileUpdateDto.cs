using System.ComponentModel.DataAnnotations;

namespace ShopX_API.Models.DTO
{
    public class UserProfileUpdateDto
    {
        [Required, MaxLength(100)]

        public string FirstName { get; set; } = "";

        [Required, MaxLength(100)]


        public string LastName { get; set; } = "";

        [Required, MaxLength(100), EmailAddress]

        public string Email { get; set; } = "";

        [MaxLength(100)]


        public string? Phone { get; set; }
        [Required, MaxLength(100)]


        public string Address { get; set; } = "";
    }
}
