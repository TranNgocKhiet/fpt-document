using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Server5000
{
    public class Employee
    {
        public String employeeId { get; set; } = "";
        public String fullName { get; set; } = "";
        public String department { get; set; } = "";
        public double salary { get; set; }
    }

    public class Program5000
    {
        public static List<Employee> employees = new List<Employee>();

        public static void InitializeData()
        {
            employees.AddRange(new[] 
            {
                new Employee { employeeId = "E001", fullName = "Nguyen Van An", department = "Engineering", salary = 5000 },
                new Employee { employeeId = "E002", fullName = "Tran Thi Bich", department = "Marketing", salary = 4500 },
                new Employee { employeeId = "E003", fullName = "Le Van Cuong", department = "Finance", salary = 4800 },
                new Employee { employeeId = "E004", fullName = "Pham Thi Dung", department = "Engineering", salary = 5200 },
                new Employee { employeeId = "E005", fullName = "Hoang Minh Duc", department = "HR", salary = 4300 }
            });
        }

        public static async Task Main(string[] args)
        {
            InitializeData();
            
            TcpListener server = new TcpListener(IPAddress.Parse("127.0.0.1"), 5000);
            server.Start();
            Console.WriteLine("Employee Server is running on 127.0.0.1:5000");

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
                string department = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

                bool isValidDepartment = string.Equals(department, "ALL") || employees.Any(p => string.Equals(p.department, department));

                if (!isValidDepartment)
                {
                    Console.WriteLine($"Query: {department}; Not Found");
                    var responseMessage = new { department = department, status = "not found", message = "No employees found in this department" };
                    string jsonResponse = JsonSerializer.Serialize(responseMessage);
                    byte[] responseBytes = Encoding.UTF8.GetBytes(jsonResponse);
                    await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                    await stream.FlushAsync();
                }
                else if (string.Equals(department, "ALL"))
                {
                    Console.WriteLine($"Query: {department}; Found {employees.Count} employees");
                    string jsonResponse = JsonSerializer.Serialize(employees);
                    byte[] responseBytes = Encoding.UTF8.GetBytes(jsonResponse);
                    await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                    await stream.FlushAsync();
                }
                else
                {
                    List<Employee> filteredEmployees;
                    filteredEmployees = employees.Where(p => string.Equals(p.department, department)).OrderBy(p => p.employeeId).ToList();
                    Console.WriteLine($"Query: {department}; Found {filteredEmployees.Count} employees"); ;
                    string jsonResponse = JsonSerializer.Serialize(filteredEmployees);
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
                client.Close();
                Console.WriteLine("Client from " + client.Client.RemoteEndPoint + " disconnected");
            }
        }
    }
}