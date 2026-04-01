using Microsoft.AspNetCore.Mvc;
using Q2.Services;

namespace Q2.Controllers
{
    public class BookController : Controller
    {
        private readonly IBookService _service;
        public BookController(IBookService service) => _service = service;

        // GET /Book
        public async Task<IActionResult> Index(int? authorId)
        {
            ViewBag.Authors = await _service.GetAllAuthorsAsync();
            ViewBag.SelectedAuthorId = authorId;
            var books = await _service.GetBooksAsync(authorId);
            return View(books);
        }

        // GET /Book/{id}
        [Route("Book/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var book = await _service.GetBookByIdAsync(id);
            if (book == null) return NotFound();
            return View(book);
        }
    }
}
