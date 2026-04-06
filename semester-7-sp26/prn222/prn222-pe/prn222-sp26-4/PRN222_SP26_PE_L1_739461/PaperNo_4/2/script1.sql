CREATE DATABASE PRN222_26SprB1_1;
GO

USE PRN222_26SprB1_1;
GO

CREATE TABLE Customer
(
    CustomerId   INT IDENTITY(1,1) PRIMARY KEY,
    CustomerName NVARCHAR(100) NOT NULL,
    Email        NVARCHAR(100) NOT NULL,
    City         NVARCHAR(50)  NOT NULL
);
GO

CREATE TABLE Orders
(
    OrderId      INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId   INT NOT NULL,
    OrderDate    DATETIME NOT NULL,
    TotalAmount  DECIMAL(10,2) NOT NULL,
    Status       NVARCHAR(20) NOT NULL,
    CONSTRAINT FK_Orders_Customer
        FOREIGN KEY (CustomerId) REFERENCES Customer(CustomerId)
);
GO

INSERT INTO Customer (CustomerName, Email, City)
VALUES
(N'Nguyen Van A', N'a@gmail.com', N'Ha Noi'),
(N'Tran Thi B',   N'b@gmail.com', N'Da Nang'),
(N'Le Van C',     N'c@gmail.com', N'Ha Noi'),
(N'Pham Thi D',   N'd@gmail.com', N'Hai Phong');
GO

INSERT INTO Orders (CustomerId, OrderDate, TotalAmount, Status)
VALUES
(1, '2026-03-01 09:15:00', 150.50, N'Pending'),
(1, '2026-03-02 14:20:00', 220.00, N'Shipped'),
(2, '2026-03-03 08:00:00', 500.75, N'Delivered'),
(3, '2026-03-04 16:45:00',  99.99, N'Pending'),
(3, '2026-03-05 10:30:00', 300.00, N'Delivered');
GO
