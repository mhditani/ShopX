using Microsoft.EntityFrameworkCore;

namespace ShopX_API.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        public int ProductId { get; set; }

        public int Quantity { get; set; }

        [Precision(16, 2)]
        public decimal UnitPrice { get; set; }



        public Order Order { get; set; } = null!;

        public Product Product { get; set; } = null!;
    }
}
