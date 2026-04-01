using Q2.Models;
using Q2.Repositories;

namespace Q2.Services
{
    public class BookService : IBookService
    {
        private readonly IBookRepository _repo;
        public BookService(IBookRepository repo) => _repo = repo;

        public Task<List<Book>> GetBooksAsync(int? authorId) => _repo.GetBooksAsync(authorId);
        public Task<Book?> GetBookByIdAsync(int id) => _repo.GetBookByIdAsync(id);
        public Task<List<Author>> GetAllAuthorsAsync() => _repo.GetAllAuthorsAsync();
    }
}
