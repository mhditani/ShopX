using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopX_API.Models;
using ShopX_API.Models.DTO;
using ShopX_API.Services;

namespace ShopX_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext db;

        public OrdersController(ApplicationDbContext db)
        {
            this.db = db;
        }








        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateOrder(OrderDto orderDto)
        {
            // Check if the payment method is valid or not
            if (!OrderHelper.PaymentMethods.ContainsKey(orderDto.PaymentMethod))
            {
                ModelState.AddModelError("Payment Method", "Please select a valid payment method");
                return BadRequest(ModelState);
            }

             int userId = JwtReader.GetUserId(User);
            var user = await db.Users.FindAsync(userId);

            if (user == null)
            {
                ModelState.AddModelError("Order", "Unable to create the order");
                return BadRequest(ModelState);
            }

            var productDictionary = OrderHelper.GetProductDictionary(orderDto.ProductIdentifiers);

            // Create a new order
            var order = new Order();
            order.UserId = userId;
            order.CreatedAt = DateTime.Now;
            order.ShippingFee = OrderHelper.ShippingFee;
            order.DeliveryAddress = orderDto.DeliveryAddress;
            order.PaymentMethod = orderDto.PaymentMethod;
            order.PaymentStatus = OrderHelper.PaymentStatus[0]; // Pending
            order.OrderStatus = OrderHelper.OrderStatus[0]; // Created


            foreach (var pair in productDictionary)
            {
                int productId = pair.Key;
                var product = await db.Products.FindAsync(productId);
                if (product == null)
                {
                    ModelState.AddModelError("Product", "Product with id " + productId + " is not available");
                    return BadRequest(ModelState);
                }
                var orderItem = new OrderItem();
                orderItem.ProductId = productId;
                orderItem.Quantity = pair.Value;
                orderItem.UnitPrice = product.Price;

                order.OrderItems.Add(orderItem);
            }

            if (order.OrderItems.Count < 1)
            {
                ModelState.AddModelError("Order", "Unable to create the order");
                return BadRequest(ModelState);
            }


            // Save the order in the database
            await db.Orders.AddAsync(order);
            await db.SaveChangesAsync();

            // get rid of the object cycle
            foreach (var item in order.OrderItems)
            {
                item.Order = null;
            }

            // hide the user password
            order.User.Password = "";

            return Ok(order);
        }




















        [Authorize]
        [HttpGet]
        public IActionResult GetOrders(int? page)
        {
            int userId = JwtReader.GetUserId(User);

            string role =  db.Users.Find(userId)?.Role ?? "";  // JwtReader.GetUserRole(User);  

            IQueryable<Order> query =  db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product);

            if (role != "admin")
            {
                query = query.Where(o => o.UserId == userId);
            }

            query = query.OrderByDescending(o => o.Id);



            // implement pagination function
            if (page == null || page < 1)
            {
                page = 1;
            }

            int pageSize = 5;
            int totalPages = 0;

            decimal count = query.Count();
            totalPages = (int)Math.Ceiling(count / pageSize);

            query = query.Skip((int)(page - 1) * pageSize).Take(pageSize);

            // Read the orders
            var orders = query.ToList();

            foreach (var order in orders)
            {
                // get rid of the object cycle
                foreach (var item in order.OrderItems)
                {
                    item.Order = null;
                }

                order.User.Password = "";
            }

            var response = new
            {
                Orders = orders,
                TotalPages = totalPages,
                PageSize = pageSize,
                Page = page,
            };

            return Ok(response);
        }


















        [Authorize]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetOrder([FromRoute]int id)
        {
            int userId = JwtReader.GetUserId(User);
            string role = db.Users.Find(userId)?.Role ?? "";

            Order? order = null;
            
            // read the order based on the role
            if (role == "admin")
            {
                order = await db.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(x => x.Id == id);
            }
            else
            {
                order = await db.Orders
                 .Include(o => o.OrderItems)
                 .ThenInclude(oi => oi.Product)
                 .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
            }

            if (order == null)
            {
                return NotFound();
            }

            // get rid of the object cycle
            foreach (var item in order.OrderItems)
            {
                item.Order = null;
            }

            // hide the User password
            order.User.Password = "";

            return Ok(order);

        }






















        [Authorize(Roles = "admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateOrder([FromRoute] int id, string? paymentStatus, string? orderStatus)
        {
            if (paymentStatus == null && orderStatus == null)
            {
                ModelState.AddModelError("Update Order", "There is nothing to update");
                return BadRequest(ModelState);
            }

            if (paymentStatus != null && !OrderHelper.PaymentStatus.Contains(paymentStatus))
            {
                ModelState.AddModelError("Payment Status", "The payment status is not valid");
                return BadRequest(ModelState);
            }

            if (orderStatus != null && !OrderHelper.PaymentStatus.Contains(orderStatus))
            {
                ModelState.AddModelError("Order Status", "The order status is not valid");
                return BadRequest(ModelState);
            }

            var order = await db.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            if (paymentStatus != null)
            {
                order.PaymentStatus = paymentStatus;
            }

            if (orderStatus != null)
            {
                order.OrderStatus = orderStatus;
            }

            await db.SaveChangesAsync();


            return Ok(order);
        }


















        [Authorize(Roles = "admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteOrder([FromRoute]int id)
        {
            var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            db.Orders.Remove(order);

            await db.SaveChangesAsync();

            return Ok();
        }
    }
}
