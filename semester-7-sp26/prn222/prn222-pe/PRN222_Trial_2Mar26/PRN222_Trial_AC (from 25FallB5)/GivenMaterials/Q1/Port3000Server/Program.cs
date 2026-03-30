using System.Net.Sockets;
using System.Net;
using System.Text.Json;
using System.Text;

namespace Server3000
{
    public class BorrowRecord
    {
        public string BookID { get; set; }="";
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public DateTime BorrowDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public string Status { get; set; } = "";
    }

    public class Reader
    {
        public int ReaderID { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public DateTime RegistrationDate { get; set; }
    }

    public class Book
    {
        public string BookID { get; set; } = "";
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public string ISBN { get; set; } = "";
        public int PublishYear { get; set; }
        public string Category { get; set; } = "";
    }

    public class Program3000
    {
        private static List<Reader> readers = new List<Reader>();
        private static List<Book> books = new List<Book>();
        private static List<(int RecordID, int ReaderID, string BookID, DateTime BorrowDate, DateTime? ReturnDate, string Status)> borrowRecords = new List<(int, int, string, DateTime, DateTime?, string)>();

        public static async Task Main(string[] args)
        {
            InitializeSampleData();

            TcpListener server = new TcpListener(IPAddress.Parse("127.0.0.1"), 3000);
            server.Start();

            Console.WriteLine("Library Server is running on 127.0.0.1:3000");
            Console.WriteLine("Waiting for client connections...");

            while (true)
            {
                try
                {
                    TcpClient client = await server.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClient(client));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error accepting client: {ex.Message}");
                }
            }
        }

        private static async Task HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();

            try
            {
                // Read Reader ID from client
                byte[] buffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string readerIdStr = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                Console.WriteLine($"Received Reader ID: {readerIdStr}");

                if (int.TryParse(readerIdStr, out int readerId))
                {
                    var borrowHistory = GetBorrowHistory(readerId);
                    string jsonResponse = JsonSerializer.Serialize(borrowHistory);

                    byte[] response = Encoding.UTF8.GetBytes(jsonResponse);
                    await stream.WriteAsync(response, 0, response.Length);

                    Console.WriteLine($"Sent {borrowHistory.Count} records for Reader ID: {readerId}");
                }
                else
                {
                    // Send empty list for invalid Reader ID
                    string emptyResponse = JsonSerializer.Serialize(new List<BorrowRecord>());
                    byte[] response = Encoding.UTF8.GetBytes(emptyResponse);
                    await stream.WriteAsync(response, 0, response.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client: {ex.Message}");
            }
            finally
            {
                client.Close();
            }
        }

        private static List<BorrowRecord> GetBorrowHistory(int readerId)
        {
            var result = new List<BorrowRecord>();

            // Check if reader exists
            bool readerExists = readers.Any(r => r.ReaderID == readerId);
            if (!readerExists)
            {
                return result; // Return empty list if reader doesn't exist
            }

            foreach (var record in borrowRecords.Where(r => r.ReaderID == readerId))
            {
                var book = books.FirstOrDefault(b => b.BookID == record.BookID);
                if (book != null)
                {
                    result.Add(new BorrowRecord
                    {
                        BookID = book.BookID,
                        Title = book.Title,
                        Author = book.Author,
                        BorrowDate = record.BorrowDate,
                        ReturnDate = record.ReturnDate,
                        Status = record.Status
                    });
                }
            }

            return result;
        }

        private static void InitializeSampleData()
        {
            // Sample Readers
            readers.AddRange(new[]
            {
                new Reader { ReaderID = 101, FullName = "John Smith", Email = "john@email.com", PhoneNumber = "123-456-7890", RegistrationDate = DateTime.Now.AddMonths(-6) },
                new Reader { ReaderID = 102, FullName = "Jane Doe", Email = "jane@email.com", PhoneNumber = "098-765-4321", RegistrationDate = DateTime.Now.AddMonths(-3) },
                new Reader { ReaderID = 103, FullName = "Bob Johnson", Email = "bob@email.com", PhoneNumber = "555-123-4567", RegistrationDate = DateTime.Now.AddMonths(-1) }
            });

            // Sample Books
            books.AddRange(new[]
            {
                new Book { BookID = "B1001", Title = "The Great Gatsby", Author = "F. Scott Fitzgerald", ISBN = "978-0-7432-7356-5", PublishYear = 1925, Category = "Fiction" },
                new Book { BookID = "B1002", Title = "To Kill a Mockingbird", Author = "Harper Lee", ISBN = "978-0-06-112008-4", PublishYear = 1960, Category = "Fiction" },
                new Book { BookID = "B1003", Title = "1984", Author = "George Orwell", ISBN = "978-0-452-28423-4", PublishYear = 1949, Category = "Dystopian" },
                new Book { BookID = "B1004", Title = "Pride and Prejudice", Author = "Jane Austen", ISBN = "978-0-14-143951-8", PublishYear = 1813, Category = "Romance" },
                new Book { BookID = "B1005", Title = "The Catcher in the Rye", Author = "J.D. Salinger", ISBN = "978-0-316-76948-0", PublishYear = 1951, Category = "Fiction" }
            });

            // Sample Borrow Records
            borrowRecords.AddRange(new[]
            {
                (1, 101, "B1001", DateTime.Parse("2024-01-15"), (DateTime?)DateTime.Parse("2024-02-15"), "Returned"),
                (2, 101, "B1002", DateTime.Parse("2024-02-20"), null, "Borrowed"),

                (3, 101, "B1003", DateTime.Parse("2024-01-10"), DateTime.Parse("2024-02-25"), "Overdue"),
                (4, 102, "B1004", DateTime.Parse("2024-03-01"), DateTime.Parse("2024-03-15"), "Returned"),
                (5, 102, "B1005", DateTime.Parse("2024-03-20"), null, "Borrowed"),
                (6, 103, "B1001", DateTime.Parse("2024-04-01"), null, "Borrowed")
            });

            Console.WriteLine("Sample data initialized successfully!");
            Console.WriteLine($"Readers: {readers.Count}, Books: {books.Count}, Borrow Records: {borrowRecords.Count}");
        }
    }
}
