using Q2.Models;

namespace Q2.Repositories
{
    public interface IBookRepository
    {
        Task<List<Book>> GetBooksAsync(int? authorId);
        Task<Book?> GetBookByIdAsync(int id);
        Task<List<Author>> GetAllAuthorsAsync();
    }
}
