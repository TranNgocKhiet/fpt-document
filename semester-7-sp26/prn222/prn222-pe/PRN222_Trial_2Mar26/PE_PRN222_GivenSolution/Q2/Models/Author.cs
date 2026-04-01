namespace Q2.Models
{
    public class Author
    {
        public int AuthorId { get; set; }
        public string Name { get; set; } = "";
        public int? BirthYear { get; set; }
        public ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
    }
}
