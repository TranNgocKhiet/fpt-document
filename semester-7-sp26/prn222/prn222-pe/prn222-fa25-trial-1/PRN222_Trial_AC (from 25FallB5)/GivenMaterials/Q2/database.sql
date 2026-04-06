-- Tạo cơ sở dữ liệu
CREATE DATABASE PE_PRN_25FallB5_23;
GO

USE PE_PRN_25FallB5_23;
GO

-- Tạo bảng Genres
CREATE TABLE Genres (
    GenreId INT PRIMARY KEY IDENTITY(1,1),
    GenreName NVARCHAR(50) NOT NULL
);

-- Tạo bảng Books
CREATE TABLE Books (
    BookId INT PRIMARY KEY IDENTITY(1,1),
    Title NVARCHAR(255) NOT NULL,
    PublicationYear INT NOT NULL,
    GenreId INT NOT NULL,
    FOREIGN KEY (GenreId) REFERENCES Genres(GenreId)
);

-- Tạo bảng Authors
CREATE TABLE Authors (
    AuthorId INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(255) NOT NULL,
    BirthYear INT
);

-- Tạo bảng BookAuthors
CREATE TABLE BookAuthors (
    BookId INT NOT NULL,
    AuthorId INT NOT NULL,
    PRIMARY KEY (BookId, AuthorId),
    FOREIGN KEY (BookId) REFERENCES Books(BookId),
    FOREIGN KEY (AuthorId) REFERENCES Authors(AuthorId)
);

-- Tạo bảng BookCopies
CREATE TABLE BookCopies (
    CopyId INT PRIMARY KEY IDENTITY(1,1),
    BookId INT NOT NULL,
    Status NVARCHAR(20) NOT NULL,
    FOREIGN KEY (BookId) REFERENCES Books(BookId)
);

-- Tạo bảng Borrowers
CREATE TABLE Borrowers (
    BorrowerId INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(255) NOT NULL,
    Email NVARCHAR(255)
);

-- Tạo bảng BorrowHistory
CREATE TABLE BorrowHistory (
    BorrowId INT PRIMARY KEY IDENTITY(1,1),
    CopyId INT NOT NULL,
    BorrowerId INT NOT NULL,
    BorrowDate DATE NOT NULL,
    ReturnDate DATE,
    FOREIGN KEY (CopyId) REFERENCES BookCopies(CopyId),
    FOREIGN KEY (BorrowerId) REFERENCES Borrowers(BorrowerId)
);

-- Chèn dữ liệu demo --

-- Genres (5 bản ghi)
INSERT INTO Genres (GenreName) VALUES
('Literature'),
('Science'),
('Mystery'),
('Self-Help'),
('History');

-- Authors (5 bản ghi)
INSERT INTO Authors (Name, BirthYear) VALUES
('Nguyen Nhat Anh', 1955),
('Stephen Hawking', 1942),
('Agatha Christie', 1890),
('Dale Carnegie', 1888),
('Yuval Noah Harari', 1976);

-- Books (5 bản ghi)
INSERT INTO Books (Title, PublicationYear, GenreId) VALUES
('Blue Eyes', 1990, 1),
('Promise of an Icon', 1995, 1),
('Advice from a Physicist', 2018, 2),
('A New Conscience', 2015, 5),
('The Mysterious Woman', 1930, 3);

-- BookAuthors (liên kết sách và tác giả, có sách với 2-3 tác giả)
INSERT INTO BookAuthors (BookId, AuthorId) VALUES
(1, 1), -- Blue Eyes - Nguyen Nhat Anh
(1, 4), -- Blue Eyes - Dale Carnegie (2 tác giả)
(2, 1), -- Promise of an Icon - Nguyen Nhat Anh
(2, 3), -- Promise of an Icon - Agatha Christie (2 tác giả)
(3, 2), -- Advice from a Physicist - Stephen Hawking
(4, 5), -- A New Conscience - Yuval Noah Harari
(5, 3); -- The Mysterious Woman - Agatha Christie

-- BookCopies (mỗi sách có ít nhất 2 bản sao)
INSERT INTO BookCopies (BookId, Status) VALUES
(1, 'Available'), (1, 'Available'), -- Blue Eyes: 2 bản sao
(2, 'Borrowed'), (2, 'Available'), -- Promise of an Icon: 2 bản sao
(3, 'Available'), (3, 'Borrowed'), -- Advice from a Physicist: 2 bản sao
(4, 'Available'), (4, 'Available'), -- A New Conscience: 2 bản sao
(5, 'Borrowed'), (5, 'Available'); -- The Mysterious Woman: 2 bản sao

-- Borrowers (5 bản ghi)
INSERT INTO Borrowers (Name, Email) VALUES
('Tran Van A', 'a@example.com'),
('Le Thi B', 'b@example.com'),
('Nguyen Van C', 'c@example.com'),
('Pham Thi D', 'd@example.com'),
('Hoang Van E', 'e@example.com');

-- BorrowHistory (lịch sử mượn không mâu thuẫn thời gian)
-- CopyId 1 (BookId 1)
INSERT INTO BorrowHistory (CopyId, BorrowerId, BorrowDate, ReturnDate) VALUES
(1, 1, '2023-01-01', '2023-01-15'), -- Copy 1 mượn lần 1
(1, 2, '2023-02-01', '2023-02-15'); -- Copy 1 mượn lần 2

-- CopyId 2 (BookId 1)
INSERT INTO BorrowHistory (CopyId, BorrowerId, BorrowDate, ReturnDate) VALUES
(2, 3, '2023-01-10', '2023-01-20');

-- CopyId 3 (BookId 2)
INSERT INTO BorrowHistory (CopyId, BorrowerId, BorrowDate, ReturnDate) VALUES
(3, 4, '2023-03-01', NULL); -- Copy 3 đang mượn

-- CopyId 4 (BookId 2)
INSERT INTO BorrowHistory (CopyId, BorrowerId, BorrowDate, ReturnDate) VALUES
(4, 5, '2023-02-15', '2023-02-25');

-- CopyId 5 (BookId 3)
INSERT INTO BorrowHistory (CopyId, BorrowerId, BorrowDate, ReturnDate) VALUES
(5, 1, '2023-04-01', '2023-04-15');

-- CopyId 6 (BookId 3)
INSERT INTO BorrowHistory (CopyId, BorrowerId, BorrowDate, ReturnDate) VALUES
(6, 2, '2023-05-01', NULL); -- Copy 6 đang mượn

-- CopyId 7 (BookId 4)
INSERT INTO BorrowHistory (CopyId, BorrowerId, BorrowDate, ReturnDate) VALUES
(7, 3, '2023-06-01', '2023-06-15');

-- CopyId 8 (BookId 4)
INSERT INTO BorrowHistory (CopyId, BorrowerId, BorrowDate, ReturnDate) VALUES
(8, 4, '2023-07-01', '2023-07-15');

-- CopyId 9 (BookId 5)
INSERT INTO BorrowHistory (CopyId, BorrowerId, BorrowDate, ReturnDate) VALUES
(9, 5, '2023-08-01', NULL); -- Copy 9 đang mượn

-- CopyId 10 (BookId 5)
INSERT INTO BorrowHistory (CopyId, BorrowerId, BorrowDate, ReturnDate) VALUES
(10, 1, '2023-09-01', '2023-09-15');
GO