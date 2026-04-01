# Q2 Guide — ASP.NET Core MVC Book List with Author Filter (3-Layer Architecture)

## What You're Building

An ASP.NET Core MVC app that:
1. Lists books from a SQL Server database at `/Book`
2. Filters books by author via a dropdown
3. Shows book detail with its authors at `/Book/{BookId}`

The project already has EF Core SqlServer packages installed and a connection string in `appsettings.json`.

---

## Database Schema (from database.sql)

```
Books       (BookId, Title, PublicationYear, GenreId)
Authors     (AuthorId, Name, BirthYear)
BookAuthors (BookId, AuthorId)   ← junction table
```

You only need these three tables for this question.

---

## 3-Layer Architecture Overview

```
Presentation Layer   →  Controllers/ + Views/
Business Logic Layer →  Services/  (IBookService, BookService)
Data Access Layer    →  Repositories/ (IBookRepository, BookRepository) + Data/AppDbContext.cs
```

The controller calls the service. The service calls the repository. The repository talks to EF Core.
Models live in `Models/` and are shared across all layers.

---

## Final Project Structure

```
Q2/
├── Controllers/
│   └── BookController.cs          ← Presentation
├── Services/
│   ├── IBookService.cs            ← BLL interface
│   └── BookService.cs             ← BLL implementation
├── Repositories/
│   ├── IBookRepository.cs         ← DAL interface
│   └── BookRepository.cs          ← DAL implementation
├── Data/
│   └── AppDbContext.cs            ← EF Core DbContext
├── Models/
│   ├── Book.cs
│   ├── Author.cs
│   └── BookAuthor.cs
├── Views/
│   └── Book/
│       ├── Index.cshtml           ← /Book
│       └── Details.cshtml         ← /Book/{id}
├── appsettings.json
└── Program.cs
```

---

## Step-by-Step Implementation

### Step 1: Set Up the Database

Run `database.sql` in SSMS. It creates `PE_PRN_25FallB5_23` with demo data.

### Step 2: Update appsettings.json

```json
"ConnectionStrings": {
  "MyCnn": "server=localhost;database=PE_PRN_25FallB5_23;Integrated Security=SSPI;TrustServerCertificate=true"
}
```

> The exam requires the connection string to be in `appsettings.json` — never hardcode it.


### Step 3: Create Model Classes

```csharp
// Models/Book.cs
public class Book
{
    public int BookId { get; set; }
    public string Title { get; set; } = "";
    public int PublicationYear { get; set; }
    public int GenreId { get; set; }
    public ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
}

// Models/Author.cs
public class Author
{
    public int AuthorId { get; set; }
    public string Name { get; set; } = "";
    public int? BirthYear { get; set; }
    public ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
}

// Models/BookAuthor.cs
public class BookAuthor
{
    public int BookId { get; set; }
    public Book Book { get; set; } = null!;
    public int AuthorId { get; set; }
    public Author Author { get; set; } = null!;
}
```

### Step 4: Data Access Layer — DbContext

```csharp
// Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Book> Books { get; set; }
    public DbSet<Author> Authors { get; set; }
    public DbSet<BookAuthor> BookAuthors { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BookAuthor>()
            .HasKey(ba => new { ba.BookId, ba.AuthorId });

        modelBuilder.Entity<BookAuthor>()
            .HasOne(ba => ba.Book)
            .WithMany(b => b.BookAuthors)
            .HasForeignKey(ba => ba.BookId);

        modelBuilder.Entity<BookAuthor>()
            .HasOne(ba => ba.Author)
            .WithMany(a => a.BookAuthors)
            .HasForeignKey(ba => ba.AuthorId);
    }
}
```

### Step 5: Data Access Layer — Repository

```csharp
// Repositories/IBookRepository.cs
public interface IBookRepository
{
    Task<List<Book>> GetBooksAsync(int? authorId);
    Task<Book?> GetBookByIdAsync(int id);
    Task<List<Author>> GetAllAuthorsAsync();
}
```

```csharp
// Repositories/BookRepository.cs
using Microsoft.EntityFrameworkCore;

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
```


### Step 6: Business Logic Layer — Service

```csharp
// Services/IBookService.cs
public interface IBookService
{
    Task<List<Book>> GetBooksAsync(int? authorId);
    Task<Book?> GetBookByIdAsync(int id);
    Task<List<Author>> GetAllAuthorsAsync();
}
```

```csharp
// Services/BookService.cs
public class BookService : IBookService
{
    private readonly IBookRepository _repo;
    public BookService(IBookRepository repo) => _repo = repo;

    public Task<List<Book>> GetBooksAsync(int? authorId) => _repo.GetBooksAsync(authorId);
    public Task<Book?> GetBookByIdAsync(int id) => _repo.GetBookByIdAsync(id);
    public Task<List<Author>> GetAllAuthorsAsync() => _repo.GetAllAuthorsAsync();
}
```

> In a real app the service layer would contain business rules (validation, transformations, etc.). Here it's a thin pass-through, but the separation still satisfies the 3-layer requirement.

### Step 7: Presentation Layer — Controller

```csharp
// Controllers/BookController.cs
using Microsoft.AspNetCore.Mvc;

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
```

### Step 8: Register Everything in Program.cs

```csharp
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionStr = builder.Configuration.GetConnectionString("MyCnn");
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(connectionStr));

// Register DAL and BLL
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<IBookService, BookService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute(name: "default", pattern: "{controller=Book}/{action=Index}/{id?}");

app.Run();
```


### Step 9: Presentation Layer — Views

**Views/Book/Index.cshtml** (matches `list.html` exactly):

```html
@model IEnumerable<Book>
@{
    var authors = ViewBag.Authors as List<Author>;
    var selectedId = (int?)ViewBag.SelectedAuthorId;
}

<h2>List of books</h2>

<form method="get" action="/Book">
    <label for="authorId">Filter by author:</label>
    <select name="authorId" id="sl_authors">
        <option id="op_0" value="">All authors</option>
        @foreach (var a in authors!)
        {
            <option id="op_@a.AuthorId" value="@a.AuthorId"
                @(selectedId == a.AuthorId ? "selected" : "")>@a.Name</option>
        }
    </select>
    <input id="bt_filter" type="submit" value="Filter" />
</form>

<table class="table">
    <thead>
        <tr>
            <th>BookId</th><th>Title</th><th>Publication Year</th><th>Authors</th><th>Action</th>
        </tr>
    </thead>
    <tbody>
        @foreach (var book in Model)
        {
            <tr>
                <td id="td_bookId_@book.BookId">@book.BookId</td>
                <td id="td_title_@book.BookId">@book.Title</td>
                <td id="td_publicationYear_@book.BookId">@book.PublicationYear</td>
                <td id="td_authors_@book.BookId">
                    @foreach (var ba in book.BookAuthors)
                    {
                        <div id="div_@(book.BookId)_@(ba.AuthorId)">@ba.Author.Name</div>
                    }
                </td>
                <td id="td_action_@book.BookId">
                    <a id="a_@book.BookId" href="/Book/@book.BookId">View Details</a>
                </td>
            </tr>
        }
    </tbody>
</table>
```

**Views/Book/Details.cshtml** (matches `detail.html` exactly):

```html
@model Book

<h2>Book's detail</h2>

<p><strong>BookId:</strong> <span id="span_bookId">@Model.BookId</span></p>
<p><strong>Title:</strong> <span id="span_title">@Model.Title</span></p>
<p><strong>Publication year:</strong> <span id="span_publicationYear">@Model.PublicationYear</span></p>

<h3>Authors:</h3>
<table class="table">
    <thead>
        <tr>
            <th>AuthorId</th><th>AuthorName</th><th>BirthYear</th>
        </tr>
    </thead>
    <tbody>
        @foreach (var ba in Model.BookAuthors)
        {
            <tr>
                <td id="td_authorId_@ba.AuthorId">@ba.AuthorId</td>
                <td id="td_authorName_@ba.AuthorId">@ba.Author.Name</td>
                <td id="td_birthYear_@ba.AuthorId">@ba.Author.BirthYear</td>
            </tr>
        }
    </tbody>
</table>
```

---

## ID Requirements Cheat Sheet

| Element | Tag | ID Pattern | Example |
|---|---|---|---|
| Book table cells | `<td>` | `td_{columnName}_{bookId}` | `td_bookId_1`, `td_title_1` |
| Author name in book row | `<div>` | `div_{bookId}_{authorId}` | `div_1_1`, `div_1_4` |
| View Details link | `<a>` | `a_{bookId}` | `a_1` |
| Author dropdown | `<select>` | `sl_authors` | — |
| "All authors" option | `<option>` | `op_0` | — |
| Author options | `<option>` | `op_{authorId}` | `op_1`, `op_2` |
| Filter button | `<input>` | `bt_filter` | — |
| Book detail fields | `<span>` | `span_bookId`, `span_title`, `span_publicationYear` | — |
| Author table cells | `<td>` | `td_{columnName}_{authorId}` | `td_authorId_1`, `td_authorName_1`, `td_birthYear_1` |

Column names in IDs use camelCase: `bookId`, `title`, `publicationYear`, `authors`, `action`, `authorId`, `authorName`, `birthYear`.

---

## Common Pitfalls

1. **Forgetting to register services** — Both `IBookRepository`/`BookRepository` and `IBookService`/`BookService` must be registered with `AddScoped` in `Program.cs`. Missing either causes a DI runtime error.

2. **Wrong database name in connection string** — The SQL script creates `PE_PRN_25FallB5_23`, make sure your connection string matches.

3. **Filter button must be `<input>` not `<button>`** — The ID requirement explicitly says `<input>` tag with `id="bt_filter"`.

4. **"All authors" option must have `id="op_0"` and `value=""`** — An empty value string lets the controller treat it as "no filter".

5. **Author names in `<div>` tags, not plain text** — Each author in the book list must be wrapped in `<div id="div_{bookId}_{authorId}">`.

6. **Missing `ThenInclude`** — In the repository, chain `.Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)`. Without `ThenInclude`, `ba.Author` will be null at runtime.

7. **Detail page span IDs** — The IDs are `span_bookId`, `span_title`, `span_publicationYear` — they do NOT include the book's ID value.

8. **URL routing** — The default MVC route maps `/Book` → `BookController.Index()` and `/Book/1` → `BookController.Details(1)` automatically.
