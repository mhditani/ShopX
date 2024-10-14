using System.ComponentModel.DataAnnotations;

namespace ShopX_API.Models.DTO
{
    public class OrderDto
    {
        [Required]
        public string ProductIdentifiers { get; set; } = "";
        [Required, MinLength(30), MaxLength(100)]
        public string DeliveryAddress { get; set; } = "";
        [Required]
        public string PaymentMethod { get; set; } = "";
    }
}
