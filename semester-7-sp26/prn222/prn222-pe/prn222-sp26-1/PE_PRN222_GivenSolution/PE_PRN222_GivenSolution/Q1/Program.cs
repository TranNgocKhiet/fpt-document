using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Server5000
{
    public class Product
    {
        public String productid { get; set; } = "";
        public String productName { get; set; } = "";
        public String category { get; set; } = "";
        public double price { get; set; }
    }
    public class Program5000
    {
        private static List<Product> products = new List<Product>();
        // Defines a method to initialize the product data
        public static void InitializeData()
        {
            products.AddRange(new[]
            {
                new Product { productid = "P001", productName = "Laptop", category = "Electronics", price = 12000 },
                new Product { productid = "P002", productName = "T-Shirt", category = "Clothing", price = 25 },
                new Product { productid = "P003", productName = "Headphones", category = "Electronics", price = 150 },
                new Product { productid = "P004", productName = "Jeans", category = "Clothing", price = 60 },
                new Product { productid = "P005", productName = "Rice (5kg)", category = "Food", price = 15 },
            });
        }

        public static async Task Main(string[] args)
        {
            InitializeData();
            // Code for TCP server goes here

            // 1. Setup and Start Server
            // 1a. Start and listen for incoming connections on IP address 127.0.0.1 and port 5000
            TcpListener server = new TcpListener(IPAddress.Parse("127.0.0.1"), 5000);
            server.Start();
            // 1b. Display console message when the server starts
            Console.WriteLine("Product Server is running on 127.0.0.1:5000");

            // 2. Accept Multiple Client Connections
            while (true)
            {
                try
                {
                    // 2a. Support multiple concurring client connections using asynchronous programming
                    TcpClient client = await server.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClient(client));
                    // 2b. Display console message when a client connects
                    Console.WriteLine("Client connected from " + client.Client.RemoteEndPoint);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error accepting client: {ex.Message}");
                }
            }
        }

        public static async Task HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            try
            {
                // Read category from client
                byte[] buffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string category = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                // Check if the category is valid (exists in the product list or is "ALL")
                bool isValidCategory = string.Equals(category, "ALL") || products.Any(p => string.Equals(p.category, category));
                
                if (!isValidCategory)
                {
                    // 5. Handle Not Found Category
                    Console.WriteLine($"Query: {category}; Not Found");
                    var responseMessage = new { category = category, status = "not found", message = "No products found in this category" };
                    string jsonResponse = JsonSerializer.Serialize(responseMessage);
                    byte[] responseBytes = Encoding.UTF8.GetBytes(jsonResponse);
                    await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                    await stream.FlushAsync();
                }
                else if (string.Equals(category, "ALL"))
                {
                    // 4. "ALL" Command
                    Console.WriteLine($"Query: {category}; Found {products.Count} products");
                    string jsonResponse = JsonSerializer.Serialize(products);
                    byte[] responseBytes = Encoding.UTF8.GetBytes(jsonResponse);
                    await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                    await stream.FlushAsync();
                }
                else
                {
                    // 3. Handle Category Query
                    List<Product> filteredProducts;
                    filteredProducts = products.Where(p => string.Equals(p.category, category)).OrderBy(p => p.productid).ToList();
                    Console.WriteLine($"Query: {category}; Found {filteredProducts.Count} products"); ;
                    string jsonResponse = JsonSerializer.Serialize(filteredProducts);
                    byte[] responseBytes = Encoding.UTF8.GetBytes(jsonResponse);
                    await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                    await stream.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client: {ex.Message}");
            }
            finally
            {
                // 6. Handle Client Disconnections
                Console.WriteLine("Client from " + client.Client.RemoteEndPoint + " disconnected");
                client.Close();
            }
        }
    }
}