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
    public class ContactsController : ControllerBase
    {
        private readonly ApplicationDbContext db;
        private readonly EmailSender emailSender;

        public ContactsController(ApplicationDbContext db, EmailSender emailSender)
        {
            this.db = db;
            this.emailSender = emailSender;
        }









        [HttpGet("subjects")]
        public async Task<IActionResult> GetSubjects()
        {
            var listSubjects = await db.Subjects.ToListAsync();
            return Ok(listSubjects);
        }







        [HttpGet]
        public async Task<IActionResult> GetContacts(int? page)
        {
            if (page == null || page < 1)
            {
                page = 1;
            }
            int pageSize = 5;
            int totalPages = 0;

            decimal count = db.Contacts.Count();

            totalPages = (int) Math.Ceiling(count / pageSize);

            var contacts = await db.Contacts
                .Include(c => c.Subject)
                .OrderByDescending(c => c.Id)
                .Skip((int) (page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var response = new
            {
                Contacts = contacts,
                TotalPages = totalPages,
                PageSize = pageSize,
                Page = page
            };

            return Ok(response);  
        }



        [HttpGet, Route("{id:int}")]
        public async Task<IActionResult> GetContact([FromRoute]int id)
        {
            var contact = await db.Contacts.Include(c => c.Subject).FirstOrDefaultAsync(x => x.Id == id);
            if (contact == null)
            {
                return NotFound();
            }
            return Ok(contact);
        }










        [HttpPost]
        public async Task<IActionResult> CreatContact([FromBody]ContactDto contactDto)
        {
            var subject = await db.Subjects.FindAsync(contactDto.SubjectId);

            if (subject == null)
            {
                ModelState.AddModelError("Subject", "Please select a valid subject");
                return BadRequest(ModelState);
            }

            var contact = new Contact
            {
                FirstName = contactDto.FirstName,
                LastName = contactDto.LastName,
                Email = contactDto.Email,
                Phone = contactDto.Phone ?? "", 
                Subject = subject,
                Message = contactDto.Message,
                CreatedAt = DateTime.Now,

            };

            await db.Contacts.AddAsync(contact);
            await db.SaveChangesAsync();

            // send confirmation email
            string emailSubject = "Email Confirmation";
            string username = contactDto.FirstName + " " + contactDto.LastName;
            string emailMessage = "Dear " + username + "\n" + " We recieved your message. Thank you for contacting us.\n" + " Our team will contact you very soon.\n" + " Best Regards\n\n" + " Your Message:\n" + contactDto.Message;

            
            emailSender.SendEmail(emailSubject, contact.Email, username, emailMessage).Wait();

            return Ok(contact);
        }














        [HttpPut, Route("{id:int}")]
        public async Task<IActionResult> UpdateContact([FromRoute] int id,[FromBody]ContactDto contactDto)
        {
            var subject = await db.Subjects.FindAsync(contactDto.SubjectId);

            if (subject == null)
            {
                ModelState.AddModelError("Subject", "Please select a valid subject");
                return BadRequest(ModelState);
            }

            var contact = await db.Contacts.FirstOrDefaultAsync(x => x.Id ==id);    
            if (contact == null)
            {
                return NotFound();
            }
            contact.FirstName = contactDto.FirstName;
            contact.LastName = contactDto.LastName;
            contact.Email = contactDto.Email;
            contact.Phone = contactDto.Phone ?? "";
            contact.Subject = subject;
            contact.Message = contactDto.Message;

           await db.SaveChangesAsync();
            return Ok(contact);

        }










        [HttpDelete, Route("{id:int}")]
        public async Task<IActionResult> DeleteContact([FromRoute] int id)
        {
            try
            {
                var contact = new Contact() { Id = id, Subject = new Subject()};
                db.Contacts.Remove(contact);
                await db.SaveChangesAsync();
            }
            catch (Exception)
            {

                return NotFound();
            }
            return Ok();
        }

    }
}
