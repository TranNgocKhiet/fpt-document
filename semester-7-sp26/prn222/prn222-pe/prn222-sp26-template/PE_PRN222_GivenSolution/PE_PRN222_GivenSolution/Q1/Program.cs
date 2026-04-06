using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Server5000
{
    public class ObjectName
    {
        public String objectId { get; set; } = "";
        public String objectName { get; set; } = "";
        public String queryString { get; set; } = "";
        public double number { get; set; }
    }

    public class Program5000
    {
        public static List<ObjectName> objetcList = new List<ObjectName>();

        public static void InitializeData()
        {
            objetcList.AddRange(new[] 
            {
                new ObjectName { objectId = "", objectName = "", queryString = "", number = 0 },
                new ObjectName { objectId = "", objectName = "", queryString = "", number = 0 },
                new ObjectName { objectId = "", objectName = "", queryString = "", number = 0 },
                new ObjectName { objectId = "", objectName = "", queryString = "", number = 0 },
                new ObjectName { objectId = "", objectName = "", queryString = "", number = 0 }
            });
        }

        public static async Task Main(string[] args)
        {
            InitializeData();
            
            int serverPort = 5000; // Use correct port (5000/3000/?)
            TcpListener server = new TcpListener(IPAddress.Parse("127.0.0.1"), serverPort);
            server.Start();
            Console.WriteLine("{ObjectName} Server is running on 127.0.0.1:{port}");

            while (true)
            {
                try
                {
                    TcpClient client = await server.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClient(client));
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
                byte[] buffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string queryString = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

                bool isValidQueryString = string.Equals(queryString, "ALL") || objetcList.Any(p => string.Equals(p.queryString, queryString));

                // Write response back to client code here
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client: {ex.Message}");
            }
            finally
            {
                client.Close();
                Console.WriteLine("Client from " + client.Client.RemoteEndPoint + " disconnected");
            }
        }
    }
}