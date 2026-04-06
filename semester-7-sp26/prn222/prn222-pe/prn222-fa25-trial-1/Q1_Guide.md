# Q1 Guide — TCP Client for Library Borrow History

## What You're Building

A .NET 8 console app that acts as a **TCP client**. It connects to a pre-built server running at `127.0.0.1:3000`, sends a Reader ID, and displays the borrow history returned as JSON.

You do NOT touch the server. You only write the client.

---

## How the Server Works (from reading its source code)

Understanding the server is key to writing the client correctly.

### Protocol
- The server uses raw **TCP sockets** (not HTTP, not SignalR).
- For each client connection, the server:
  1. Reads bytes from the `NetworkStream` (expects a UTF-8 string of the Reader ID, e.g. `"101"`)
  2. Parses it to an `int`
  3. Serializes a `List<BorrowRecord>` to JSON using `System.Text.Json`
  4. Writes the JSON bytes back to the stream
  5. Closes the connection

### What the Server Returns

The server **always** returns a JSON array (`List<BorrowRecord>`). There are three scenarios:

| Scenario | Server Behavior | JSON Response |
|---|---|---|
| Reader ID not found | Returns empty list | `[]` |
| Reader exists, no borrows | Returns empty list | `[]` |
| Reader exists, has borrows | Returns populated list | `[{...}, {...}]` |

### The BorrowRecord shape

```json
{
  "BookID": "B1001",
  "Title": "The Great Gatsby",
  "Author": "F. Scott Fitzgerald",
  "BorrowDate": "2024-01-15T00:00:00",
  "ReturnDate": "2024-02-15T00:00:00",   // or null
  "Status": "Returned"                     // "Returned", "Borrowed", or "Overdue"
}
```

### Critical Observation — "Reader not found" vs "No borrow records"

The server returns `[]` (empty list) for **both** "reader not found" AND "reader exists but no borrows". However, the problem statement says you must distinguish between these two cases with different messages.

Looking at the sample data:
- Reader 103 (Bob Johnson) has 1 borrow record (B1001, Status: "Borrowed")
- Readers 101 and 102 have multiple records
- Any ID not in {101, 102, 103} is "not found"

**But wait** — the expected output says Reader 103 should show "No borrow records found". This contradicts the server data (103 has a record). This likely means the exam expects you to treat an empty response as "reader not found" for IDs that don't match known readers, and the sample data may differ at exam time.

**Practical approach**: Since the server returns `[]` for both cases and there's no way to distinguish them from the client side alone, you have two options:
1. Treat empty list as "Reader not found" (matches the problem's example for ID 999)
2. Check the list count: if empty → "Reader not found", if populated → display records

Looking at the expected output more carefully:
- ID 999 → empty list → "Reader with ID 999 does not exist."
- ID 103 → empty list → "No borrow records found for Reader ID 103."

Since the server can't distinguish these, the **exam likely expects** you to just handle it simply. The given solution's Q1/Program.cs is currently a stub (`Hello, World!`), so you need to figure out the intended logic. The most reasonable interpretation: **if the response is an empty array, print "Reader not found"**. The "no borrow records" case may just be a theoretical scenario described in the problem.

> **Tip**: If the exam provides a modified server that includes a flag like `"readerFound": true/false` in the response, adjust accordingly. But based on the given server code, empty list = reader not found is the safest bet.

---

## Step-by-Step Implementation Plan

### Step 1: Create the BorrowRecord Model Class

Define a class matching the server's JSON structure:

- `BookID` (string)
- `Title` (string)
- `Author` (string)
- `BorrowDate` (DateTime)
- `ReturnDate` (DateTime?) — nullable
- `Status` (string)

### Step 2: Main Loop — Read User Input

```
while true:
    print "Enter Reader ID (or press Enter to exit): "
    read input
    if input is empty → print goodbye message, break
    if input is not a valid int OR int <= 0 → print error, continue
    otherwise → proceed to connect
```

Key points:
- Use `Console.ReadLine()` — it returns `null` or `""` on Enter
- Use `int.TryParse()` for validation
- Check the parsed value is `> 0`

### Step 3: TCP Connection

Use `TcpClient` to connect to `127.0.0.1:3000`.

```
try:
    create TcpClient
    connect to 127.0.0.1:3000
    get NetworkStream
    send Reader ID as UTF-8 bytes
    read response bytes
    convert to string
catch:
    print "Library server is not running. Please try again later."
    continue loop
```

Key classes:
- `System.Net.Sockets.TcpClient`
- `System.Text.Encoding.UTF8`
- `NetworkStream` for reading/writing

**Important**: The server closes the connection after responding, so you need to read until the stream ends. Use a loop or `ReadToEnd` via a `StreamReader`.

### Step 4: Deserialize the JSON Response

Use `System.Text.Json.JsonSerializer.Deserialize<List<BorrowRecord>>(jsonString)`.

Make sure your property names match exactly (PascalCase, same as the server's C# properties). `System.Text.Json` is case-sensitive by default, but since both client and server are C# using the same serializer, PascalCase will match.

### Step 5: Display the Results

Three cases based on the deserialized list:

1. **List is null or empty** → `"Reader with ID {id} does not exist."`
2. **List has items** → Print the formatted borrow history

For the formatted output, match this exact format:
```
=== Borrow History for Reader ID: {id} 
Book ID: {BookID}
Title: {Title}
Author: {Author}
Borrow Date: {BorrowDate:yyyy-MM-dd}
Return Date: {ReturnDate:yyyy-MM-dd} or "Not returned yet"
Status: {Status}
---
```

Key formatting details:
- Dates formatted as `yyyy-MM-dd`
- If `ReturnDate` is `null` → print `"Not returned yet"`
- Each book separated by `---`

---

## Namespaces You'll Need

```csharp
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
```

No NuGet packages needed — everything is in the base .NET 8 SDK.

---

## Common Pitfalls

1. **Not reading the full response** — The server might send data in chunks. Use a `StreamReader` with `ReadToEndAsync()` or loop `NetworkStream.ReadAsync()` until 0 bytes are returned.

2. **JSON case sensitivity** — `System.Text.Json` is case-sensitive by default. Your property names must match the server's exactly (PascalCase). Alternatively, pass `JsonSerializerOptions { PropertyNameCaseInsensitive = true }`.

3. **Forgetting to close/dispose the TcpClient** — Use a `using` statement.

4. **Date formatting** — Use `.ToString("yyyy-MM-dd")` for dates. Don't let the default `DateTime.ToString()` format leak through.

5. **The "=== Borrow History" header** — Note there's a space after `101` but no `===` at the end in the expected output. Match it exactly.

---

## Program Structure Overview

```
Program.cs
├── class BorrowRecord          // Model matching server JSON
└── static Main()
    └── while (true)
        ├── Read input
        ├── Validate (empty → exit, non-int/<=0 → error)
        ├── Try TCP connect to 127.0.0.1:3000
        │   ├── Send reader ID
        │   └── Read JSON response
        ├── Deserialize to List<BorrowRecord>
        └── Display results (not found / empty / borrow list)
```

Everything fits in a single `Program.cs` file. Keep it simple.
