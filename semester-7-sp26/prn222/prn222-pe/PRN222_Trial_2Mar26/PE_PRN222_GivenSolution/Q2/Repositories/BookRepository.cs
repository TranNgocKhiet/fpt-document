using Q2.Data;
using Q2.Models;
using Microsoft.EntityFrameworkCore;

namespace Q2.Repositories
{
    public class BookRepository : IBookRepository
    {
        private readonly AppDbContext _db;
        public BookRepository(AppDbContext db) => _db = db;

        public async Task<List<Book>> GetBooksAsync(int? authorId)
        {
            var query = _db.Books
                .Include(b => b.BookAuthors)
                .ThenInclude(ba => ba.Author)
                .AsQueryable();

            if (authorId.HasValue && authorId.Value > 0)
                query = query.Where(b => b.BookAuthors.Any(ba => ba.AuthorId == authorId.Value));

            return await query.OrderBy(b => b.BookId).ToListAsync();
        }

        public async Task<Book?> GetBookByIdAsync(int id)
        {
            return await _db.Books
                .Include(b => b.BookAuthors)
                .ThenInclude(ba => ba.Author)
                .FirstOrDefaultAsync(b => b.BookId == id);
        }

        public async Task<List<Author>> GetAllAuthorsAsync()
        {
            return await _db.Authors.OrderBy(a => a.Name).ToListAsync();
        }
    }
}
