namespace Q2.Models
{
    public class Book
    {
        public int BookId { get; set; }
        public string Title { get; set; } = "";
        public int PublicationYear { get; set; }
        public int GenreId { get; set; }
        public ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
    }
}
