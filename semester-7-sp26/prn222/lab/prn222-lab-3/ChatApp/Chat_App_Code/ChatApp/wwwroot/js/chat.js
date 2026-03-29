// SignalR connection
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chathub")
    .build();

// DOM elements
const usernameInput = document.getElementById('username');
const connectBtn = document.getElementById('connect-btn');
const messageInput = document.getElementById('message-input');
const sendBtn = document.getElementById('send-btn');
const chatMessages = document.getElementById('chat-messages');
const fileBtn = document.getElementById('file-btn');
const localFileBtn = document.getElementById('local-file-btn');
const emojiBtn = document.getElementById('emoji-btn');
const fileInput = document.getElementById('file-input');
const emojiPicker = document.getElementById('emoji-picker');
const themeToggle = document.getElementById('theme-toggle');
const typingIndicator = document.getElementById('typing-indicator');
const typingText = document.getElementById('typing-text');

// Local file dialog elements
const localFileDialog = document.getElementById('local-file-dialog');
const localFilePath = document.getElementById('local-file-path');
const localFileShareBtn = document.getElementById('local-file-share-btn');
const localFileCancelBtn = document.getElementById('local-file-cancel-btn');

// Image modal elements
const imageModal = document.getElementById('image-modal');
const modalImage = document.getElementById('modal-image');
const modalCloseBtn = document.querySelector('.image-modal-close');
const modalDownloadBtn = document.getElementById('modal-download-btn');
const modalBackdrop = document.querySelector('.image-modal-backdrop');

// State
let currentUsername = '';
let isConnected = false;
let isDarkMode = false;
let typingTimer;
let isTyping = false;
let typingUsers = new Set();

// File chunk receiving
const fileChunks = new Map(); // Store file chunks by fileId
const senderFileData = new Map(); // Store sender's file data for preview

// Initialize
document.addEventListener('DOMContentLoaded', function() {
    setupEventListeners();
    loadTheme();
    setupDragAndDrop();
});

function setupEventListeners() {
    // Connection
    connectBtn.addEventListener('click', connect);
    usernameInput.addEventListener('keypress', (e) => {
        if (e.key === 'Enter') connect();
    });

    // Messaging
    sendBtn.addEventListener('click', sendMessage);
    messageInput.addEventListener('keypress', (e) => {
        if (e.key === 'Enter') {
            sendMessage();
        } else {
            handleTyping();
        }
    });

    messageInput.addEventListener('input', handleTyping);
    messageInput.addEventListener('blur', stopTyping);

    // File handling
    fileBtn.addEventListener('click', () => fileInput.click());
    localFileBtn.addEventListener('click', showLocalFileDialog);
    fileInput.addEventListener('change', handleFileSelect);
    
    // Local file dialog
    localFileShareBtn.addEventListener('click', shareLocalFile);
    localFileCancelBtn.addEventListener('click', hideLocalFileDialog);
    document.querySelector('.dialog-backdrop').addEventListener('click', hideLocalFileDialog);

    // Emoji picker
    emojiBtn.addEventListener('click', toggleEmojiPicker);
    document.addEventListener('click', (e) => {
        if (!emojiPicker.contains(e.target) && e.target !== emojiBtn) {
            emojiPicker.style.display = 'none';
        }
    });

    // Emoji buttons
    document.querySelectorAll('.emoji-btn').forEach(btn => {
        btn.addEventListener('click', (e) => {
            messageInput.value += e.target.textContent;
            messageInput.focus();
            emojiPicker.style.display = 'none';
        });
    });

    // Theme toggle
    themeToggle.addEventListener('click', toggleTheme);
    
    // Image modal event listeners
    modalCloseBtn.addEventListener('click', closeImageModal);
    modalBackdrop.addEventListener('click', closeImageModal);
    modalDownloadBtn.addEventListener('click', downloadModalImage);
    
    // Close modal with Escape key
    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape' && imageModal.style.display !== 'none') {
            closeImageModal();
        }
    });
}

// SignalR event handlers
connection.start().then(function () {
    console.log('SignalR connected');
}).catch(function (err) {
    console.error('SignalR connection error:', err);
});

connection.on("ReceiveMessage", function (username, message, time) {
    addMessage(username, message, time, username === currentUsername);
});

connection.on("FileUploaded", async function (username, fileName, fileType, fileSize, fileId, time) {
    if (username === currentUsername) return; // Skip own uploads
    
    console.log('File uploaded by:', username, fileName);
    
    // Add receiving message
    addFileReceivingMessage(username, fileName, fileType, fileId, time, fileSize);
    
    try {
        // Download file as blob (binary, much faster than base64)
        const response = await fetch(`/api/file/download/${fileId}`);
        if (!response.ok) {
            throw new Error('Download failed');
        }
        
        const blob = await response.blob();
        console.log('File downloaded as blob:', fileName, blob.size);
        
        // Convert to data URL only for display
        const dataUrl = await new Promise((resolve) => {
            const reader = new FileReader();
            reader.onload = (e) => resolve(e.target.result);
            reader.readAsDataURL(blob);
        });
        
        // Display the file
        addCompleteFileMessage(username, fileName, dataUrl, fileType, fileId, time);
        
    } catch (err) {
        console.error('Download error:', err);
        markFileError(fileId);
        addSystemMessage(`Failed to download file: ${fileName}`);
    }
});

connection.on("FileTransferError", function (errorMessage) {
    alert(`File transfer error: ${errorMessage}`);
});

connection.on("JoinConfirmed", function (username) {
    addSystemMessage('Connected successfully!');
});

connection.on("UserJoined", function (username) {
    addSystemMessage(`${username} joined the chat`);
});

connection.on("UserLeft", function (username) {
    addSystemMessage(`${username} left the chat`);
    typingUsers.delete(username);
    updateTypingIndicator();
});

connection.on("UserTyping", function (username) {
    console.log('UserTyping event received:', username);
    typingUsers.add(username);
    updateTypingIndicator();
});

connection.on("UserStoppedTyping", function (username) {
    console.log('UserStoppedTyping event received:', username);
    typingUsers.delete(username);
    updateTypingIndicator();
});

// Connection functions
async function connect() {
    const username = usernameInput.value.trim();
    if (!username) {
        alert('Please enter your name!');
        return;
    }

    try {
        currentUsername = username;
        await connection.invoke("JoinChat", username);
        
        // Update UI
        connectBtn.disabled = true;
        usernameInput.disabled = true;
        connectBtn.style.background = '#9CA3AF';
        
        // Enable chat controls
        messageInput.disabled = false;
        sendBtn.disabled = false;
        fileBtn.disabled = false;
        localFileBtn.disabled = false;
        emojiBtn.disabled = false;
        
        isConnected = true;
        messageInput.focus();
        
        // Don't add system message here - it will come from JoinConfirmed event
    } catch (err) {
        console.error('Connection error:', err);
        alert('Connection failed. Please try again.');
    }
}

// Message functions
async function sendMessage() {
    const message = messageInput.value.trim();
    if (!message || !isConnected) return;

    try {
        await connection.invoke("SendMessage", currentUsername, message);
        messageInput.value = '';
        stopTyping();
    } catch (err) {
        console.error('Send message error:', err);
    }
}

function addMessage(username, message, time, isOwn) {
    const messageDiv = document.createElement('div');
    messageDiv.className = `message ${isOwn ? 'own' : ''}`;
    
    messageDiv.innerHTML = `
        <div class="message-content">
            <div class="sender-name">${isOwn ? 'You' : username}</div>
            <div class="message-bubble">
                ${escapeHtml(message)}
            </div>
            <div class="message-time">${time}</div>
        </div>
    `;
    
    chatMessages.appendChild(messageDiv);
    scrollToBottom();
}

function addSystemMessage(message) {
    const messageDiv = document.createElement('div');
    messageDiv.className = 'message system';
    
    messageDiv.innerHTML = `
        <div class="message-content">
            <div class="message-bubble">
                ${escapeHtml(message)}
            </div>
            <div class="message-time">${new Date().toLocaleTimeString('en-US', {hour12: false, hour: '2-digit', minute: '2-digit'})}</div>
        </div>
    `;
    
    chatMessages.appendChild(messageDiv);
    scrollToBottom();
}

// File handling
function handleFileSelect(event) {
    const file = event.target.files[0];
    if (!file || !isConnected) return;

    processFile(file);
    
    // Clear file input
    event.target.value = '';
}

function addFileUploadMessage(fileName, fileSize, isImage, isVideo, fileId) {
    const messageDiv = document.createElement('div');
    messageDiv.className = 'message own';
    messageDiv.setAttribute('data-file-id', fileId);
    
    // Format file size
    const formattedSize = formatFileSize(fileSize);
    let fileIcon = '📁';
    if (isImage) fileIcon = '🖼️';
    if (isVideo) fileIcon = '🎥';
    
    messageDiv.innerHTML = `
        <div class="message-content">
            <div class="sender-name">You</div>
            <div class="message-bubble">
                <div class="file-message">
                    <div class="file-icon">${fileIcon}</div>
                    <div class="file-info">
                        <div class="file-name">📤 Uploading: ${fileName}</div>
                        <div class="file-size">${formattedSize}</div>
                    </div>
                </div>
                <div class="progress-bar">
                    <div class="progress-fill" style="width: 0%"></div>
                </div>
            </div>
            <div class="message-time">${new Date().toLocaleTimeString('en-US', {hour12: false, hour: '2-digit', minute: '2-digit'})}</div>
        </div>
    `;
    
    chatMessages.appendChild(messageDiv);
    scrollToBottom();
}

function updateFileProgress(fileId, progress) {
    const messageDiv = document.querySelector(`[data-file-id="${fileId}"]`);
    if (messageDiv) {
        const progressFill = messageDiv.querySelector('.progress-fill');
        const fileName = messageDiv.querySelector('.file-name');
        
        if (progressFill) {
            progressFill.style.width = `${progress}%`;
        }
        
        if (progress >= 100 && fileName) {
            fileName.textContent = fileName.textContent.replace('📤 Uploading:', '✅ Sent:');
        }
    }
}

function updateSenderFileCompleted(fileId, fileName, fileType) {
    console.log('updateSenderFileCompleted called:', { fileId, fileName, fileType });
    const messageDiv = document.querySelector(`[data-file-id="${fileId}"]`);
    if (!messageDiv) {
        console.error('Message div not found for fileId:', fileId);
        return;
    }
    
    const isImage = fileType.startsWith('image/');
    const isVideo = fileType.startsWith('video/');
    
    // Check if we have the file data stored
    const storedFile = senderFileData.get(fileId);
    console.log('Stored file check:', storedFile ? 'Found' : 'Not found', { isImage, isVideo });
    
    if (storedFile && isImage) {
        console.log('Showing image preview for sender');
        // For images with stored data, show image preview without bubble
        messageDiv.innerHTML = `
            <div class="message-content">
                <div class="sender-name">You</div>
                <div class="image-message" onclick="openImageModal('${storedFile.data}', '${fileName}')">
                    <img src="${storedFile.data}" alt="${fileName}" loading="lazy" style="max-width: 300px; max-height: 300px; object-fit: contain;" />
                </div>
                <div class="message-time">${new Date().toLocaleTimeString('en-US', {hour12: false, hour: '2-digit', minute: '2-digit'})}</div>
            </div>
        `;
        // Clean up stored data
        senderFileData.delete(fileId);
    } else if (storedFile && isVideo) {
        // For videos with stored data, show video player without bubble
        messageDiv.innerHTML = `
            <div class="message-content">
                <div class="sender-name">You</div>
                <div class="video-message" onclick="downloadFile('${storedFile.data}', '${fileName}')">
                    <video controls style="max-width: 300px; max-height: 200px;">
                        <source src="${storedFile.data}" type="${fileType}">
                        Your browser does not support the video tag.
                    </video>
                </div>
                <div class="message-time">${new Date().toLocaleTimeString('en-US', {hour12: false, hour: '2-digit', minute: '2-digit'})}</div>
            </div>
        `;
        // Clean up stored data
        senderFileData.delete(fileId);
    } else if (isImage || isVideo) {
        // For large images/videos without stored data, show completion message
        messageDiv.innerHTML = `
            <div class="message-content">
                <div class="sender-name">You</div>
                <div class="message-bubble">
                    <div class="file-message">
                        <div class="file-icon">${isImage ? '🖼️' : '🎥'}</div>
                        <div class="file-info">
                            <div class="file-name">✅ Sent: ${fileName}</div>
                            <div class="file-size">${isImage ? 'Image' : 'Video'} uploaded successfully</div>
                        </div>
                    </div>
                </div>
                <div class="message-time">${new Date().toLocaleTimeString('en-US', {hour12: false, hour: '2-digit', minute: '2-digit'})}</div>
            </div>
        `;
    } else {
        // For other files, show completion
        messageDiv.innerHTML = `
            <div class="message-content">
                <div class="sender-name">You</div>
                <div class="message-bubble">
                    <div class="file-message">
                        <div class="file-icon">📁</div>
                        <div class="file-info">
                            <div class="file-name">✅ Sent: ${fileName}</div>
                            <div class="file-size">File uploaded successfully</div>
                        </div>
                    </div>
                </div>
                <div class="message-time">${new Date().toLocaleTimeString('en-US', {hour12: false, hour: '2-digit', minute: '2-digit'})}</div>
            </div>
        `;
    }
}

function generateFileId() {
    return Date.now().toString(36) + Math.random().toString(36).substr(2);
}

function addFileReceivingMessage(username, fileName, fileType, fileId, time, totalFileSize) {
    const messageDiv = document.createElement('div');
    messageDiv.className = 'message';
    messageDiv.setAttribute('data-file-id', fileId);
    
    const isImage = fileType.startsWith('image/');
    const isVideo = fileType.startsWith('video/');
    const formattedSize = formatFileSize(totalFileSize || 0);
    let fileIcon = '📁';
    if (isImage) fileIcon = '🖼️';
    if (isVideo) fileIcon = '🎥';
    
    messageDiv.innerHTML = `
        <div class="message-content">
            <div class="sender-name">${username}</div>
            <div class="message-bubble">
                <div class="file-message">
                    <div class="file-icon">${fileIcon}</div>
                    <div class="file-info">
                        <div class="file-name">📥 Receiving: ${fileName}</div>
                        <div class="file-size">${formattedSize}</div>
                    </div>
                </div>
                <div class="progress-bar">
                    <div class="progress-fill" style="width: 0%"></div>
                </div>
            </div>
            <div class="message-time">${time}</div>
        </div>
    `;
    
    chatMessages.appendChild(messageDiv);
    scrollToBottom();
}

function updateFileReceiveProgress(fileId, progress) {
    const messageDiv = document.querySelector(`[data-file-id="${fileId}"]`);
    if (messageDiv) {
        const progressFill = messageDiv.querySelector('.progress-fill');
        if (progressFill) {
            progressFill.style.width = `${progress}%`;
        }
    }
}

function addCompleteFileMessage(username, fileName, fileData, fileType, fileId, time) {
    const messageDiv = document.querySelector(`[data-file-id="${fileId}"]`);
    if (!messageDiv) return;
    
    const isImage = fileType.startsWith('image/');
    const isVideo = fileType.startsWith('video/');
    
    if (isImage) {
        // For images, show without message bubble
        messageDiv.innerHTML = `
            <div class="message-content">
                <div class="sender-name">${username}</div>
                <div class="image-message" onclick="openImageModal('${fileData}', '${fileName}')">
                    <img src="${fileData}" alt="${fileName}" loading="lazy" style="max-width: 300px; max-height: 300px; object-fit: contain;" />
                </div>
                <div class="message-time">${time}</div>
            </div>
        `;
    } else if (isVideo) {
        // For videos, show without message bubble
        messageDiv.innerHTML = `
            <div class="message-content">
                <div class="sender-name">${username}</div>
                <div class="video-message" onclick="downloadFile('${fileData}', '${fileName}')">
                    <video controls style="max-width: 300px; max-height: 200px;">
                        <source src="${fileData}" type="${fileType}">
                        Your browser does not support the video tag.
                    </video>
                    <div class="video-overlay">
                        <div class="video-info">
                            <div class="file-name">${fileName}</div>
                            <div class="download-hint">Click to download</div>
                        </div>
                    </div>
                </div>
                <div class="message-time">${time}</div>
            </div>
        `;
    } else {
        // For other files, keep the message bubble
        const fileSize = Math.round((fileData.length * 0.75)); // Approximate size in bytes
        const formattedSize = formatFileSize(fileSize);
        messageDiv.innerHTML = `
            <div class="message-content">
                <div class="sender-name">${username}</div>
                <div class="message-bubble">
                    <div class="file-message" onclick="downloadFile('${fileData}', '${fileName}')">
                        <div class="file-icon">📁</div>
                        <div class="file-info">
                            <div class="file-name">${fileName}</div>
                            <div class="file-size">${formattedSize} - Click to download</div>
                        </div>
                    </div>
                </div>
                <div class="message-time">${time}</div>
            </div>
        `;
    }
}

function downloadFile(fileData, fileName) {
    const link = document.createElement('a');
    link.href = fileData;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

// Typing indicator functions
function handleTyping() {
    if (!isConnected) return;
    
    if (!isTyping) {
        isTyping = true;
        connection.invoke("SendTyping", currentUsername).catch(err => {
            console.error('SendTyping error:', err);
        });
    }
    
    clearTimeout(typingTimer);
    typingTimer = setTimeout(stopTyping, 2000);
}

function stopTyping() {
    if (isTyping) {
        isTyping = false;
        connection.invoke("StopTyping", currentUsername).catch(err => {
            console.error('StopTyping error:', err);
        });
    }
    clearTimeout(typingTimer);
}

function updateTypingIndicator() {
    console.log('updateTypingIndicator called, typingUsers:', Array.from(typingUsers));
    if (typingUsers.size > 0) {
        const users = Array.from(typingUsers);
        let text;
        if (users.length === 1) {
            text = `${users[0]} is typing...`;
        } else if (users.length === 2) {
            text = `${users[0]} and ${users[1]} are typing...`;
        } else {
            text = `${users.length} people are typing...`;
        }
        typingText.textContent = text;
        typingIndicator.style.display = 'block';
        console.log('Typing indicator shown:', text);
    } else {
        typingIndicator.style.display = 'none';
        console.log('Typing indicator hidden');
    }
}

// UI functions
function toggleEmojiPicker() {
    emojiPicker.style.display = emojiPicker.style.display === 'none' ? 'block' : 'none';
}

function toggleTheme() {
    isDarkMode = !isDarkMode;
    document.body.classList.toggle('dark', isDarkMode);
    themeToggle.textContent = isDarkMode ? '☀️' : '🌙';
    localStorage.setItem('darkMode', isDarkMode);
}

function loadTheme() {
    const savedTheme = localStorage.getItem('darkMode');
    if (savedTheme === 'true') {
        isDarkMode = true;
        document.body.classList.add('dark');
        themeToggle.textContent = '☀️';
    }
}

function scrollToBottom() {
    chatMessages.scrollTop = chatMessages.scrollHeight;
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Drag and drop functionality
function setupDragAndDrop() {
    const chatContainer = document.querySelector('.chat-container');
    
    ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
        chatContainer.addEventListener(eventName, preventDefaults, false);
    });
    
    function preventDefaults(e) {
        e.preventDefault();
        e.stopPropagation();
    }
    
    ['dragenter', 'dragover'].forEach(eventName => {
        chatContainer.addEventListener(eventName, highlight, false);
    });
    
    ['dragleave', 'drop'].forEach(eventName => {
        chatContainer.addEventListener(eventName, unhighlight, false);
    });
    
    function highlight(e) {
        chatMessages.classList.add('drag-over');
    }
    
    function unhighlight(e) {
        chatMessages.classList.remove('drag-over');
    }
    
    chatContainer.addEventListener('drop', handleDrop, false);
    
    function handleDrop(e) {
        const dt = e.dataTransfer;
        const files = dt.files;
        
        if (files.length > 0 && isConnected) {
            const file = files[0]; // Handle first file only
            processFile(file);
        }
    }
}

function processFile(file) {
    // Check file size (2GB limit)
    const maxSize = 2 * 1024 * 1024 * 1024; // 2GB
    if (file.size > maxSize) {
        alert('File size must be less than 2GB');
        return;
    }
    
    // Show upload progress immediately
    const fileId = generateFileId();
    const isImage = file.type.startsWith('image/');
    const isVideo = file.type.startsWith('video/');
    
    // Add upload message
    addFileUploadMessage(file.name, file.size, isImage, isVideo, fileId);
    
    // Categorize file size for different handling
    const isVeryLargeFile = file.size > 1024 * 1024 * 1024; // >1GB
    const isLargeFile = file.size > 100 * 1024 * 1024; // >100MB
    
    let warningMessage = '';
    if (isVeryLargeFile) {
        const sizeGB = (file.size / (1024 * 1024 * 1024)).toFixed(2);
        warningMessage = `This is a very large file (${sizeGB}GB). Upload will take significant time and memory. The file will be sent in small chunks to prevent server overload. Continue?`;
    } else if (isLargeFile) {
        const sizeMB = Math.round(file.size / (1024 * 1024));
        warningMessage = `This is a large file (${sizeMB}MB). Upload may take several minutes. Continue?`;
    }
    
    if (warningMessage) {
        const confirmUpload = confirm(warningMessage);
        if (!confirmUpload) {
            // Remove the upload message
            const messageDiv = document.querySelector(`[data-file-id="${fileId}"]`);
            if (messageDiv) messageDiv.remove();
            return;
        }
    }
    
    // Start file transfer
    startFileTransfer(file, fileId, isLargeFile, isVeryLargeFile);
}

async function startFileTransfer(file, fileId, isLargeFile, isVeryLargeFile) {
    try {
        console.log(`Starting file transfer: ${file.name}, Size: ${file.size} bytes`);
        
        // Check if file has a path property (indicates it's from server's local filesystem)
        // This works when user drags a file from their local filesystem on the server machine
        const isLocalFile = file.path && file.path.length > 0;
        
        if (isLocalFile) {
            // File is on server's local filesystem - share it directly without uploading
            console.log('File is local on server, sharing path:', file.path);
            
            try {
                const response = await fetch('/api/file/share-local', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        fileId: fileId,
                        fileName: file.name,
                        fileType: file.type,
                        filePath: file.path,
                        username: currentUsername
                    })
                });
                
                if (!response.ok) {
                    throw new Error('Failed to share local file');
                }
                
                const result = await response.json();
                console.log('Local file shared successfully:', result);
                
                // Mark as complete immediately (no upload needed)
                updateFileProgress(fileId, 100);
                
                // Notify other users via SignalR
                await connection.invoke("NotifyFileUploaded", currentUsername, file.name, file.type, file.size, fileId);
                
                // Update sender's UI
                const isImage = file.type.startsWith('image/');
                const isVideo = file.type.startsWith('video/');
                
                if ((isImage || isVideo) && file.size < 50 * 1024 * 1024) {
                    // For small images/videos, read and show preview
                    const reader = new FileReader();
                    reader.onload = (e) => {
                        senderFileData.set(fileId, {
                            data: e.target.result,
                            fileName: file.name,
                            fileType: file.type
                        });
                        updateSenderFileCompleted(fileId, file.name, file.type);
                    };
                    reader.readAsDataURL(file);
                } else {
                    updateSenderFileCompleted(fileId, file.name, file.type);
                }
                
                return; // Done - no upload needed
            } catch (err) {
                console.error('Failed to share local file, falling back to upload:', err);
                // Fall through to regular upload
            }
        }
        
        // Regular HTTP upload for non-local files
        const formData = new FormData();
        formData.append('file', file);
        formData.append('username', currentUsername);
        formData.append('fileId', fileId);
        
        // Upload with progress tracking
        const xhr = new XMLHttpRequest();
        
        // Track upload progress
        xhr.upload.addEventListener('progress', (e) => {
            if (e.lengthComputable) {
                const progress = (e.loaded / e.total) * 100;
                updateFileProgress(fileId, progress);
                
                // Log progress every 10%
                if (Math.floor(progress) % 10 === 0) {
                    console.log(`Upload progress: ${progress.toFixed(1)}%`);
                }
            }
        });
        
        // Handle completion
        xhr.addEventListener('load', async () => {
            if (xhr.status === 200) {
                const response = JSON.parse(xhr.responseText);
                console.log('File uploaded successfully:', response);
                
                // Notify other users via SignalR
                await connection.invoke("NotifyFileUploaded", currentUsername, file.name, file.type, file.size, fileId);
                
                // Update sender's UI
                const isImage = file.type.startsWith('image/');
                const isVideo = file.type.startsWith('video/');
                
                if ((isImage || isVideo) && file.size < 50 * 1024 * 1024) {
                    // For small images/videos, read and show preview
                    const reader = new FileReader();
                    reader.onload = (e) => {
                        senderFileData.set(fileId, {
                            data: e.target.result,
                            fileName: file.name,
                            fileType: file.type
                        });
                        updateSenderFileCompleted(fileId, file.name, file.type);
                    };
                    reader.readAsDataURL(file);
                } else {
                    updateSenderFileCompleted(fileId, file.name, file.type);
                }
            } else {
                console.error('Upload failed:', xhr.statusText);
                alert('File upload failed. Please try again.');
                markFileError(fileId);
            }
        });
        
        // Handle errors
        xhr.addEventListener('error', () => {
            console.error('Upload error');
            alert('File upload failed. Please check your connection.');
            markFileError(fileId);
        });
        
        // Send request
        xhr.open('POST', '/api/file/upload');
        xhr.send(formData);
        
    } catch (err) {
        console.error('File transfer start error:', err);
        alert('Failed to start file transfer. Please try again.');
        markFileError(fileId);
    }
}

// Send file data in parallel chunks (for small files with preview)
async function sendFileInParallelChunks(fileData, file, fileId, chunkSize, delayBetweenChunks, parallelChunks) {
    try {
        const totalChunks = Math.ceil(fileData.length / chunkSize);
        console.log(`Sending file in parallel chunks: ${file.name}, Chunks: ${totalChunks}, ChunkSize: ${chunkSize} bytes, Parallel: ${parallelChunks}`);
        
        let chunkIndex = 0;
        
        while (chunkIndex < totalChunks) {
            // Prepare batch of chunks to send in parallel
            const promises = [];
            const batchSize = Math.min(parallelChunks, totalChunks - chunkIndex);
            
            for (let i = 0; i < batchSize; i++) {
                const currentIndex = chunkIndex + i;
                const start = currentIndex * chunkSize;
                const end = Math.min(start + chunkSize, fileData.length);
                const chunk = fileData.slice(start, end);
                
                // Send chunk (don't await yet)
                promises.push(
                    connection.invoke("SendFileChunk", currentUsername, file.name, chunk, file.type, currentIndex, totalChunks, fileId)
                        .then(() => {
                            // Update progress
                            const progress = ((currentIndex + 1) / totalChunks) * 100;
                            updateFileProgress(fileId, progress);
                            
                            // Log progress every 10%
                            if (currentIndex % Math.floor(totalChunks / 10) === 0 || currentIndex === 0) {
                                console.log(`Upload progress: ${progress.toFixed(1)}%`);
                            }
                        })
                );
            }
            
            // Wait for all chunks in this batch to complete
            await Promise.all(promises);
            
            chunkIndex += batchSize;
            
            // Small delay between batches
            if (chunkIndex < totalChunks && delayBetweenChunks > 0) {
                await new Promise(resolve => setTimeout(resolve, delayBetweenChunks));
            }
        }
        
        console.log(`File transfer completed: ${file.name}`);
    } catch (err) {
        console.error('Send file chunks error:', err);
        alert('Failed to send file. Connection may have been lost.');
        markFileError(fileId);
        senderFileData.delete(fileId);
    }
}

// Stream large files in parallel chunks without loading entire file into memory
async function streamFileInParallelChunks(file, fileId, chunkSize, delayBetweenChunks, parallelChunks) {
    try {
        // Calculate total chunks based on raw file size
        const totalChunks = Math.ceil(file.size / chunkSize);
        console.log(`Streaming file in parallel: ${file.name}, Size: ${file.size} bytes, Chunks: ${totalChunks}, Parallel: ${parallelChunks}`);
        
        let chunkIndex = 0;
        
        while (chunkIndex < totalChunks) {
            // Prepare batch of chunks to send in parallel
            const promises = [];
            const batchSize = Math.min(parallelChunks, totalChunks - chunkIndex);
            
            for (let i = 0; i < batchSize; i++) {
                const currentIndex = chunkIndex + i;
                const offset = currentIndex * chunkSize;
                
                if (offset < file.size) {
                    // Read and send chunk
                    promises.push(
                        (async () => {
                            // Read one chunk at a time
                            const blob = file.slice(offset, offset + chunkSize);
                            
                            // Convert chunk to base64
                            const base64Chunk = await new Promise((resolve, reject) => {
                                const reader = new FileReader();
                                reader.onload = (e) => resolve(e.target.result);
                                reader.onerror = (e) => reject(e);
                                reader.readAsDataURL(blob);
                            });
                            
                            // Send chunk
                            await connection.invoke("SendFileChunk", currentUsername, file.name, base64Chunk, file.type, currentIndex, totalChunks, fileId);
                            
                            // Update progress
                            const progress = ((currentIndex + 1) / totalChunks) * 100;
                            updateFileProgress(fileId, progress);
                            
                            // Log progress
                            if (currentIndex % Math.floor(totalChunks / 10) === 0 || currentIndex === 0) {
                                console.log(`Upload progress: ${progress.toFixed(1)}% (${currentIndex + 1}/${totalChunks} chunks)`);
                            }
                        })()
                    );
                }
            }
            
            // Wait for all chunks in this batch to complete
            await Promise.all(promises);
            
            chunkIndex += batchSize;
            
            // Small delay between batches
            if (chunkIndex < totalChunks && delayBetweenChunks > 0) {
                await new Promise(resolve => setTimeout(resolve, delayBetweenChunks));
            }
        }
        
        console.log(`File streaming completed: ${file.name}`);
    } catch (err) {
        console.error('Stream file error:', err);
        alert('Failed to stream file. The connection may have been lost.');
        markFileError(fileId);
    }
}

function markFileError(fileId) {
    const messageDiv = document.querySelector(`[data-file-id="${fileId}"]`);
    if (messageDiv) {
        const fileMessage = messageDiv.querySelector('.file-message');
        const fileName = messageDiv.querySelector('.file-name');
        if (fileMessage) fileMessage.classList.add('error');
        if (fileName) fileName.textContent = fileName.textContent.replace('📤 Uploading:', '❌ Failed:');
    }
}

// Image modal functions
let currentModalImageData = '';
let currentModalFileName = '';

function openImageModal(imageData, fileName) {
    currentModalImageData = imageData;
    currentModalFileName = fileName;
    
    modalImage.src = imageData;
    modalImage.alt = fileName;
    imageModal.style.display = 'flex';
    
    // Prevent body scrolling when modal is open
    document.body.style.overflow = 'hidden';
}

function closeImageModal() {
    imageModal.style.display = 'none';
    currentModalImageData = '';
    currentModalFileName = '';
    
    // Restore body scrolling
    document.body.style.overflow = 'auto';
}

function downloadModalImage() {
    if (currentModalImageData && currentModalFileName) {
        downloadFile(currentModalImageData, currentModalFileName);
    }
}
// Utility function to format file size
function formatFileSize(bytes) {
    if (bytes === 0) return '0 Bytes';
    
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}


// Local file dialog functions
function showLocalFileDialog() {
    localFileDialog.style.display = 'flex';
    localFilePath.value = '';
    localFilePath.focus();
}

function hideLocalFileDialog() {
    localFileDialog.style.display = 'none';
}

async function shareLocalFile() {
    const filePath = localFilePath.value.trim();
    
    if (!filePath) {
        alert('Please enter a file path');
        return;
    }
    
    hideLocalFileDialog();
    
    // Extract filename from path
    const fileName = filePath.split(/[/\\]/).pop();
    const fileId = generateFileId();
    
    // Guess file type from extension
    const ext = fileName.split('.').pop().toLowerCase();
    const mimeTypes = {
        'jpg': 'image/jpeg', 'jpeg': 'image/jpeg', 'png': 'image/png', 'gif': 'image/gif',
        'mp4': 'video/mp4', 'avi': 'video/avi', 'mov': 'video/quicktime',
        'pdf': 'application/pdf', 'doc': 'application/msword', 'docx': 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
        'zip': 'application/zip', 'rar': 'application/x-rar-compressed',
        'txt': 'text/plain', 'csv': 'text/csv'
    };
    const fileType = mimeTypes[ext] || 'application/octet-stream';
    
    // Add upload message
    addFileUploadMessage(fileName, 0, fileType.startsWith('image/'), fileType.startsWith('video/'), fileId);
    
    try {
        const response = await fetch('/api/file/share-local', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                fileId: fileId,
                fileName: fileName,
                fileType: fileType,
                filePath: filePath,
                username: currentUsername
            })
        });
        
        if (!response.ok) {
            const error = await response.text();
            throw new Error(error);
        }
        
        const result = await response.json();
        console.log('Local file shared successfully:', result);
        
        // Mark as complete immediately
        updateFileProgress(fileId, 100);
        
        // Notify other users via SignalR
        await connection.invoke("NotifyFileUploaded", currentUsername, fileName, fileType, result.fileSize, fileId);
        
        // Update sender's UI
        updateSenderFileCompleted(fileId, fileName, fileType);
        
    } catch (err) {
        console.error('Failed to share local file:', err);
        alert(`Failed to share local file: ${err.message}\n\nMake sure the file exists on the server and the path is correct.`);
        markFileError(fileId);
    }
}
