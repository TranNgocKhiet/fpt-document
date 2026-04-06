using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.IO;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace Q1
{
    public class BorrowRecord
    {
        public string BookID { get; set; } = "";
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public DateTime? BorrowDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public string Status { get; set; } = "";
    }

    public static class Program
    {

        public static async Task HandleResponse(TcpClient client, int ReaderID)
        {
            NetworkStream stream ;

            stream = client.GetStream();

            string jsonString = null;
            List<BorrowRecord> borrowRecords = null;
            try
            {
                byte[] buffer = new byte[1024];
                int bytes = await stream.ReadAsync(buffer, 0, buffer.Length);
                jsonString = Encoding.UTF8.GetString(buffer, 0, bytes);
                

                borrowRecords = JsonSerializer.Deserialize<List<BorrowRecord>>(jsonString);

                if (borrowRecords.Count > 0)
                {
                    Console.WriteLine($"=== Borrow History for Reader ID: {ReaderID}");
                    int RecordsCount = 0;
                    foreach (var record in borrowRecords)
                    {
                        Console.WriteLine($"Book ID: {record.BookID}");
                        Console.WriteLine($"Title: {record.Title}");
                        Console.WriteLine($"Author: {record.Author}");
                        String recordBorrowDate = record.BorrowDate.Value.ToString("yyyy-MM-dd");
                        Console.WriteLine($"Borrow Date: {recordBorrowDate}");
                        String recordReturnDate = null;
                        if (record.ReturnDate != null)
                        {
                            recordReturnDate = record.ReturnDate.Value.ToString("yyyy-MM-dd");
                        }
                        else
                        {
                            recordReturnDate = "Not returned yet";
                        }
                        Console.WriteLine($"Return Date: {recordReturnDate}");
                        Console.WriteLine($"Status: {record.Status}");
                        RecordsCount++;
                        if (RecordsCount != borrowRecords.Count)
                        {
                            Console.WriteLine("---");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"No borrow records found for Reader ID {ReaderID}.");
                }
            }
            catch
            {
                Console.WriteLine("Library server is not running. Please try again later.");
            }
        }

        public static async Task Main(string[] args)
        {
            TcpClient client;
            NetworkStream stream;
            string serverIP = "127.0.0.1";
            int serverPort = 3000;
            string input = null;
            int ReaderID;
            while (true)
            {
                Console.Write("Enter Reader ID (or press Enter to exit): ");
                input = Console.ReadLine();
                if (string.IsNullOrEmpty(input))
                {
                    Console.WriteLine("Goodbye! Library client is shutting down.");
                    break;
                }
                if (!int.TryParse(input, out ReaderID))
                {
                    Console.WriteLine("Invalid input! Please enter a valid Reader ID (positive integer).");
                    continue;
                
                } 
                else if (ReaderID < 1)
                {
                    Console.WriteLine("Invalid input! Please enter a valid Reader ID (positive integer).");
                    continue;
                }

                try
                {
                    client = new TcpClient(serverIP, serverPort);
                    stream = client.GetStream();

                    byte[] readerIDBuffer = Encoding.UTF8.GetBytes(ReaderID.ToString());
                    stream.Write(readerIDBuffer);

                    string jsonString = null;
                    List<BorrowRecord> borrowRecords = null;

                    await HandleResponse(client, ReaderID);

                    continue;
                }
                catch
                {
                    Console.WriteLine("Library server is not running. Please try again later.");
                    continue;
                }
            }
        }
    }
}
