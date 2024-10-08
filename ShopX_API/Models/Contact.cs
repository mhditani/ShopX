using System.ComponentModel.DataAnnotations;

namespace ShopX_API.Models
{
    public class Contact
    {
        public int Id { get; set; }
        [MaxLength(100)]
        public string FirstName { get; set; } = "";
        [MaxLength(100)]

        public string LastName { get; set; } = "";
        [MaxLength(100), EmailAddress]

        public string Email { get; set; } = "";
        [MaxLength(100)]

        public string Phone { get; set; } = "";
        [MaxLength(100)]

        public string Subject { get; set; } = "";

        public string Message { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.Now; 
    }
}
