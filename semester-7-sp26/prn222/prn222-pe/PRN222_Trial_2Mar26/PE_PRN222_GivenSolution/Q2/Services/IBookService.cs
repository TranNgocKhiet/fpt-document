using Q2.Models;

namespace Q2.Services
{
    public interface IBookService
    {
        Task<List<Book>> GetBooksAsync(int? authorId);
        Task<Book?> GetBookByIdAsync(int id);
        Task<List<Author>> GetAllAuthorsAsync();
    }
}
