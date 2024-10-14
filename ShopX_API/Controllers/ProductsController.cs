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
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext db;
        private readonly IWebHostEnvironment env;
        private readonly List<string> ListCategories = new List<string>()
        {
            "Phones", "Computers", "Accessories", "Printers", "Cameras", "Other"
        };

        public ProductsController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            this.db = db;
            this.env = env;
        }


        [HttpGet("categories")]
        public IActionResult GetCategories()
        {
            return Ok(ListCategories);
        }








        [HttpGet]
        public async Task<IActionResult> GetProducts(string? search, string? category, int? minPrice, int? maxPrice, string? sort, string? order, int? page)
        {
            IQueryable<Product> query =  db.Products; 

            // search function
            if (search != null)
            {
                query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));
            }

            if (category != null)
            {
                query = query.Where(p => p.Category == category);
            }

            if (minPrice != null)
            {
                query = query.Where(p => p.Price >= minPrice);
            }

            if (maxPrice != null)
            {
                query = query.Where(p => p.Price <= maxPrice);
            }

            // sort function
            if (sort == null) sort = "id";
            if (order != null || order != "asc") order = "desc";

            if (sort.ToLower() == "name")
            {
                if (order == "asc")
                {
                    query = query.OrderBy(p => p.Name);
                }
                else
                {
                    query = query.OrderByDescending(p => p.Name);
                }
            }
            else if (sort.ToLower() == "brand")
            {
                if (order == "asc")
                {
                    query = query.OrderBy(p => p.Brand);
                }
                else
                {
                    query = query.OrderByDescending(p => p.Brand);
                }
            }
            else if (sort.ToLower() == "category")
            {
                if (order == "asc")
                {
                    query = query.OrderBy(p => p.Category);
                }
                else
                {
                    query = query.OrderByDescending(p => p.Category);
                }
            }
            else if (sort.ToLower() == "price")
            {
                if (order == "asc")
                {
                    query = query.OrderBy(p => p.Price);
                }
                else
                {
                    query = query.OrderByDescending(p => p.Price);
                }
            }
            else if (sort.ToLower() == "date")
            {
                if (order == "asc")
                {
                    query = query.OrderBy(p => p.CreatedAt);
                }
                else
                {
                    query = query.OrderByDescending(p => p.CreatedAt);
                }
            }
            else 
            {
                if (order == "asc")
                {
                    query = query.OrderBy(p => p.Id);
                }
                else
                {
                    query = query.OrderByDescending(p => p.Id);
                }
            }

            // Pagination function
            if (page == null || page < 1) page = 1;

            int pageSize = 5;
            int totalPages = 0;

            decimal count = query.Count();
            totalPages = (int)Math.Ceiling(count / pageSize);

            query = query.Skip((int) (page - 1) * pageSize).Take(pageSize);
            

            var products = await query.ToListAsync();

            var response = new
            {
                Product = products,
                TotalPages = totalPages,
                PageSize = pageSize,
                Page = page
            };

            return Ok(response);
        }






        [HttpGet, Route("{id:int}")]
        public async Task<IActionResult> GetProduct([FromRoute] int id)
        {
            var product = await db.Products.FirstOrDefaultAsync(x => x.Id == id);
            if (product == null)
            {
                return NotFound(); 
            }
            return Ok(product);
        }










        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromForm]ProductDto productDto)
        {
            if (!ListCategories.Contains(productDto.Category))
            {
                ModelState.AddModelError("Category", "Please select a valid category");
                return BadRequest(ModelState);
            }


            if (productDto.ImageFile == null)
            {
                ModelState.AddModelError("ImageFile", "The Image File is required");
                return BadRequest(ModelState); 
            }

            // save the image to the server
            string imageFileName = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            imageFileName += Path.GetExtension(productDto.ImageFile.FileName);

            string imagesFolder = env.WebRootPath + "/images/products/";

            using (var stream = System.IO.File.Create(imagesFolder + imageFileName))
            {
                productDto.ImageFile.CopyTo(stream);
            }

            // save products in database 
            var product = new Product
            {
                Name = productDto.Name,
                Brand = productDto.Brand,
                Category = productDto.Category,
                Price = productDto.Price,
                Description = productDto.Description ?? "",
                ImageFileName = imageFileName,
                CreatedAt = DateTime.Now
            };

            await db.Products.AddAsync(product);
            await db.SaveChangesAsync();

            return Ok(product);
        }


















        [Authorize(Roles = "admin")]
        [HttpPut, Route("{id:int}")]
        public async Task<IActionResult> UpdateProduct([FromRoute] int id, [FromForm]ProductDto productDto)
        {
            if (!ListCategories.Contains(productDto.Category))
            {
                ModelState.AddModelError("Category", "Please select a valid category");
                return BadRequest(ModelState);
            } 

            var product = await db.Products.FirstOrDefaultAsync(x => x.Id == id);   
            if (product == null)
            {
                return NotFound();  
            }

            string imageFileName = product.ImageFileName;
            if (productDto.ImageFile != null)
            {
                // save the new image
                imageFileName = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                imageFileName += Path.GetExtension(productDto.ImageFile.FileName);

                string imagesFolder = env.WebRootPath + "/images/products/";
                using (var stream = System.IO.File.Create(imagesFolder + imageFileName))
                {
                    productDto.ImageFile.CopyTo(stream);
                }

                // delete old image
                System.IO.File.Delete(imagesFolder + product.ImageFileName);

            }

            // update the product in the database
            product.Name = productDto.Name;
            product.Brand = productDto.Brand;
            product.Category = productDto.Category;
            product.Price = productDto.Price;
            product.Description = productDto.Description ?? "";
            product.ImageFileName = imageFileName;

            await db.SaveChangesAsync();

            return Ok(product);
        }












        [Authorize(Roles = "admin")]
        [HttpDelete, Route("{id:int}")]
        public async Task<IActionResult> DeleteProduct([FromRoute]int id)
        {
            var product = await db.Products.FirstOrDefaultAsync(x => x.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            // delete the image
            string imagesFolder = env.WebRootPath + "/images/products/";
            System.IO.File.Delete(imagesFolder + product.ImageFileName);

            // delete the product from the database
            db.Products.Remove(product);
            await db.SaveChangesAsync();

            return Ok();
        }
    }
}
