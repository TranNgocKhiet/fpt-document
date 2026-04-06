# Q2 Guide — ASP.NET Core MVC Book List with Author Filter

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

## Final Project Structure

```
Q2/
├── Controllers/
│   └── BookController.cs
├── Data/
│   └── AppDbContext.cs
├── Models/
│   ├── Book.cs
│   ├── Author.cs
│   └── BookAuthor.cs
├── Views/
│   └── Book/
│       ├── Index.cshtml       ← /Book
│       └── Details.cshtml     ← /Book/{id}
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

### Step 4: Create the DbContext

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

### Step 5: Register Services in Program.cs

```csharp
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionStr = builder.Configuration.GetConnectionString("MyCnn");
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(connectionStr));
builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute(name: "default", pattern: "{controller=Book}/{action=Index}/{id?}");

app.Run();
```

### Step 6: Create the BookController

```csharp
// Controllers/BookController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class BookController : Controller
{
    private readonly AppDbContext _db;
    public BookController(AppDbContext db) => _db = db;

    // GET /Book  — no [Route] needed, convention routing handles this
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
```

### Step 7: Create the List View (Index.cshtml)

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

> The filter button uses `<input type="submit">` not `<button>` — the ID requirement says `<input>` tag with `id="bt_filter"`.

### Step 8: Create the Detail View (Details.cshtml)

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

1. **Wrong database name in connection string** — The SQL script creates `PE_PRN_25FallB5_23`, make sure your connection string matches.

2. **Filter button must be `<input>` not `<button>`** — The ID requirement explicitly says `<input>` tag with `id="bt_filter"`.

3. **"All authors" option must have `id="op_0"` and `value=""`** — An empty value string lets the controller treat it as "no filter".

4. **Author names in `<div>` tags, not plain text** — Each author in the book list must be wrapped in `<div id="div_{bookId}_{authorId}">`.

5. **Missing `ThenInclude`** — Chain `.Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)`. Without `ThenInclude`, `ba.Author` will be null at runtime.

6. **`/Book/1` returns 404** — Keep `[Route("Book/{id}")]` on `Details` but do NOT put `[Route("Book")]` on `Index`. Convention routing handles `/Book` → `Index` automatically. Having both `[Route]` attributes causes an ambiguous match conflict.

7. **Detail page span IDs** — The IDs are `span_bookId`, `span_title`, `span_publicationYear` — they do NOT include the book's ID value.

8. **Missing relationship config in OnModelCreating** — You need both `HasKey` for the composite PK and `HasOne`/`WithMany` for both sides of the junction table, otherwise EF can't resolve the navigation properties.
