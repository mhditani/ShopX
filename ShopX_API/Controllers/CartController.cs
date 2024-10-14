using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopX_API.Models.DTO;
using ShopX_API.Services;

namespace ShopX_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ApplicationDbContext db;

        public CartController(ApplicationDbContext db)
        {
            this.db = db;
        }





        [HttpGet]
        public async Task<IActionResult> GetCart(string productIdntifiers)
        {
            var cartDto = new CartDto();
            cartDto.CartItems = new List<CartItemDto>();
            cartDto.SubTotal = 0;
            cartDto.ShippingFee = OrderHelper.ShippingFee;
            cartDto.TotalPrice = 0;

            var productDictionary =  OrderHelper.GetProductDictionary(productIdntifiers);

            foreach (var pair in productDictionary) 
            {
                int productId = pair.Key;

                var product = await db.Products.FindAsync(productId);

                if (product == null)
                {
                    continue;
                }

                var cartItemDto = new CartItemDto();
                cartItemDto.Product = product;
                cartItemDto.Quantity = pair.Value;

                cartDto.CartItems.Add(cartItemDto);

                cartDto.SubTotal += product.Price * pair.Value;
                cartDto.TotalPrice = cartDto.SubTotal + cartDto.ShippingFee;
            }


            return Ok(cartDto);
        }



















        [HttpGet("PaymentMethods")]
        public IActionResult GetPaymentMethods()
        {
            return Ok(OrderHelper.PaymentMethods);
        }
    }
}
