using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Q2.Data;

namespace Q2.Controllers
{
    public class BookController : Controller
    {
        private readonly AppDbContext _db;
        public BookController(AppDbContext db) => _db = db;

        // GET /Book
       
        public async Task<IActionResult> Index(int? authorId)
        {
            ViewBag.Authors = await _db.Authors.OrderBy(a => a.Name).ToListAsync();
            ViewBag.SelectedAuthorId = authorId;

            var query = _db.Books
                .Include(b => b.BookAuthors)
                .ThenInclude(ba => ba.Author)
                .AsQueryable();

            if (authorId.HasValue && authorId.Value > 0)
                query = query.Where(b => b.BookAuthors.Any(ba => ba.AuthorId == authorId.Value));

            var books = await query.OrderBy(b => b.BookId).ToListAsync();
            return View(books);
        }

        // GET /Book/{id}
        [Route("Book/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var book = await _db.Books
                .Include(b => b.BookAuthors)
                .ThenInclude(ba => ba.Author)
                .FirstOrDefaultAsync(b => b.BookId == id);

            if (book == null) return NotFound();
            return View(book);
        }
    }
}
