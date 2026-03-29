# Modern Web Chat Application

A real-time chat application built with ASP.NET Core Razor Pages and SignalR, featuring modern UI design and comprehensive chat functionality.

## Features

### Core Chat Features
- **Real-time messaging** using SignalR
- **User connection management** with join/leave notifications
- **Typing indicators** showing when users are typing
- **File sharing** with support for images and documents
- **Emoji picker** with popular emojis
- **Dark/Light theme toggle**
- **Responsive design** for desktop and mobile

### Technical Features
- **ASP.NET Core 8.0** web application
- **SignalR Hub** for real-time communication
- **Razor Pages** for server-side rendering
- **Modern CSS** with animations and transitions
- **JavaScript ES6+** for client-side functionality
- **File upload/download** with progress indicators

## Getting Started

### Prerequisites
- .NET 8.0 SDK
- Modern web browser

### Running the Application

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd ChatApp
   ```

2. **Build the application**
   ```bash
   dotnet build
   ```

3. **Run the application**
   ```bash
   dotnet run
   ```

4. **Open your browser**
   Navigate to `http://localhost:5000`

### Usage

1. **Enter your username** in the input field
2. **Click Connect** to join the chat room
3. **Start chatting** - type messages and press Enter
4. **Share files** using the 📎 button
5. **Add emojis** using the 😊 button
6. **Toggle theme** using the 🌙/☀️ button

## Architecture

### Project Structure
```
ChatApp/
├── Hubs/
│   └── ChatHub.cs          # SignalR hub for real-time communication
├── Pages/
│   ├── Index.cshtml        # Main chat page
│   ├── Index.cshtml.cs     # Page model
│   └── Shared/             # Shared layouts
├── wwwroot/
│   ├── css/
│   │   └── chat.css        # Styling
│   └── js/
│       └── chat.js         # Client-side logic
└── Program.cs              # Application startup
```

### Key Components

#### ChatHub (SignalR Hub)
- Manages user connections and groups
- Handles message broadcasting
- Manages typing indicators
- Handles file sharing

#### Client-Side JavaScript
- SignalR connection management
- Real-time message handling
- File upload/download
- UI interactions and animations
- Typing indicator logic

#### Responsive CSS
- Modern design with smooth animations
- Dark/light theme support
- Mobile-friendly responsive layout
- Custom scrollbars and hover effects

## Features in Detail

### Real-time Messaging
- Instant message delivery using SignalR
- Message timestamps
- Sender identification
- System notifications for user join/leave

### Typing Indicators
- Shows when users are typing
- Automatically hides after 2 seconds of inactivity
- Handles multiple users typing simultaneously

### File Sharing
- Support for all file types
- Image preview for image files
- Click to download files
- File size display

### Theme Support
- Light theme (default)
- Dark theme toggle
- Persistent theme preference
- Smooth theme transitions

### Mobile Responsive
- Optimized for mobile devices
- Touch-friendly interface
- Responsive emoji picker
- Adaptive message bubbles

## Browser Support
- Chrome (recommended)
- Firefox
- Safari
- Edge

## Development

### Adding New Features
1. **Server-side**: Extend `ChatHub.cs` for new SignalR methods
2. **Client-side**: Update `chat.js` for new functionality
3. **UI**: Modify `chat.css` and `Index.cshtml` for interface changes

### Customization
- **Colors**: Update CSS custom properties
- **Emojis**: Modify the emoji grid in `Index.cshtml`
- **Layout**: Adjust responsive breakpoints in CSS

## Deployment

### Local Development
```bash
dotnet run --environment Development
```

### Production
```bash
dotnet publish -c Release
```

## License
This project is open source and available under the MIT License.