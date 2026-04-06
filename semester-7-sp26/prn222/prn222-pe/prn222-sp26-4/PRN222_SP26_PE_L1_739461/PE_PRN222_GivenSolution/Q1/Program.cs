using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Q1
{
    public class Employee
    {
        public string employeeId { get; set; } = "";
        public string fullName { get; set; } = "";
        public string department { get; set; } = "";
        public decimal salary { get; set; }
    }

    public class NotFoundResponse
    {
        public string department { get; set; } = "";
        public string status { get; set; } = "not_found";
        public string message { get; set; } = "No employees found in this department";
    }

    public class Q1
    {
        private static List<Employee> employees = new List<Employee>();

        public static async Task Main(string[] args)
        {
            InitializeData();

            TcpListener server = new TcpListener(IPAddress.Parse("127.0.0.1"), 5000);
            server.Start();

            Console.WriteLine("Employee Server is running on 127.0.0.1:5000");
            Console.WriteLine("Waiting for client connections...");

            while (true)
            {
                try
                {
                    TcpClient client = await server.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClient(client));

                    if (client.Client.RemoteEndPoint is IPEndPoint remoteEndPoint)
                    {
                        Console.WriteLine($"Client connected from {remoteEndPoint.Address}:{remoteEndPoint.Port}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error accepting client: {ex.Message}");
                }
            }
        }

        private static async Task HandleClient(TcpClient client)
        {
            IPEndPoint? remoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
            string clientInfo = remoteEndPoint is null ? "unknown" : $"{remoteEndPoint.Address}:{remoteEndPoint.Port}";

            try
            {
                using NetworkStream stream = client.GetStream();
                using StreamReader reader = new StreamReader(stream, new UTF8Encoding(false), leaveOpen: true);
                using StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };

                string? input = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(input))
                {
                    Console.WriteLine($"Client {clientInfo} disconnected without sending command");
                    return;
                }

                Console.WriteLine($"Received from {clientInfo}: {input}");

                string json;
                if (input.Equals("ALL", StringComparison.Ordinal))
                {
                    List<Employee> allEmployees = employees
                        .OrderBy(e => e.employeeId, StringComparer.Ordinal)
                        .ToList();

                    json = JsonSerializer.Serialize(allEmployees);
                    Console.WriteLine($"Query: ALL => {allEmployees.Count} employees");
                }
                else
                {
                    List<Employee> employeesByDepartment = employees
                        .Where(e => e.department.Equals(input, StringComparison.Ordinal))
                        .OrderBy(e => e.employeeId, StringComparer.Ordinal)
                        .ToList();

                    if (employeesByDepartment.Count == 0)
                    {
                        NotFoundResponse response = new NotFoundResponse { department = input };
                        json = JsonSerializer.Serialize(response);
                        Console.WriteLine($"Query: {input} => Not Found");
                    }
                    else
                    {
                        json = JsonSerializer.Serialize(employeesByDepartment);
                        Console.WriteLine($"Query: {input} => {employeesByDepartment.Count} employees");
                    }
                }

                await writer.WriteLineAsync(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client {clientInfo}: {ex.Message}");
            }
            finally
            {
                Console.WriteLine($"Client {clientInfo} disconnected");
                client.Close();
            }
        }

        private static void InitializeData()
        {
            employees = new List<Employee>
            {
                new Employee { employeeId = "E001", fullName = "Nguyen Van An", department = "Engineering", salary = 5000 },
                new Employee { employeeId = "E002", fullName = "Tran Thi Bich", department = "Marketing", salary = 4500 },
                new Employee { employeeId = "E003", fullName = "Le Van Cuong", department = "Finance", salary = 4800 },
                new Employee { employeeId = "E004", fullName = "Pham Thi Dung", department = "Engineering", salary = 5200 },
                new Employee { employeeId = "E005", fullName = "Hoang Minh Duc", department = "HR", salary = 4300 }
            };
        }

    }
}