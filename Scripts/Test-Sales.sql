USE [dev-sales];
IF OBJECT_ID('dbo.OrderDetails', 'U') IS NOT NULL DROP TABLE dbo.OrderDetails;
IF OBJECT_ID('dbo.Orders', 'U') IS NOT NULL DROP TABLE dbo.Orders;
IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID('dbo.Customers', 'U') IS NOT NULL DROP TABLE dbo.Customers;

CREATE TABLE dbo.Customers (
    CustomerID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    Email NVARCHAR(100) UNIQUE NOT NULL,
    RegistrationDate DATE NOT NULL DEFAULT GETDATE()
);

CREATE TABLE dbo.Products (
    ProductID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ProductName NVARCHAR(100) NOT NULL,
    Category NVARCHAR(50),
    Price DECIMAL(10, 2) NOT NULL CHECK (Price >= 0),
    StockQuantity INT NOT NULL CHECK (StockQuantity >= 0)
);

CREATE TABLE dbo.Orders (
    OrderID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CustomerID UNIQUEIDENTIFIER NOT NULL,
    OrderDate DATE NOT NULL DEFAULT GETDATE(),
    TotalAmount DECIMAL(10, 2) NOT NULL CHECK (TotalAmount >= 0),
    Status NVARCHAR(20) CHECK (Status IN ('Pending', 'Completed', 'Shipped')),
    FOREIGN KEY (CustomerID) REFERENCES dbo.Customers(CustomerID)
);

CREATE TABLE dbo.OrderDetails (
    OrderDetailID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    OrderID UNIQUEIDENTIFIER NOT NULL,
    ProductID UNIQUEIDENTIFIER NOT NULL,
    Quantity INT NOT NULL CHECK (Quantity > 0),
    UnitPrice DECIMAL(10, 2) NOT NULL CHECK (UnitPrice >= 0),
    FOREIGN KEY (OrderID) REFERENCES dbo.Orders(OrderID),
    FOREIGN KEY (ProductID) REFERENCES dbo.Products(ProductID)
);

INSERT INTO dbo.Customers (FirstName, LastName, Email)
VALUES 
('John', 'Doe', 'john.doe@example.com'),
('Jane', 'Smith', 'jane.smith@example.com'),
('Alice', 'Johnson', 'alice.johnson@example.com');

INSERT INTO dbo.Products (ProductName, Category, Price, StockQuantity)
VALUES 
('Laptop', 'Electronics', 799.99, 50),
('Headphones', 'Electronics', 19.99, 150),
('Coffee Maker', 'Appliances', 29.99, 75);

INSERT INTO dbo.Orders (CustomerID, TotalAmount, Status)
VALUES 
((SELECT CustomerID FROM dbo.Customers WHERE FirstName = 'John' AND LastName = 'Doe'), 150.75, 'Completed'),
((SELECT CustomerID FROM dbo.Customers WHERE FirstName = 'Jane' AND LastName = 'Smith'), 300.00, 'Pending'),
((SELECT CustomerID FROM dbo.Customers WHERE FirstName = 'Alice' AND LastName = 'Johnson'), 450.25, 'Completed');

INSERT INTO dbo.OrderDetails (OrderID, ProductID, Quantity, UnitPrice)
VALUES 
((SELECT OrderID FROM dbo.Orders WHERE TotalAmount = 150.75), (SELECT ProductID FROM dbo.Products WHERE ProductName = 'Laptop'), 1, 799.99),
((SELECT OrderID FROM dbo.Orders WHERE TotalAmount = 300.00), (SELECT ProductID FROM dbo.Products WHERE ProductName = 'Headphones'), 2, 19.99),
((SELECT OrderID FROM dbo.Orders WHERE TotalAmount = 450.25), (SELECT ProductID FROM dbo.Products WHERE ProductName = 'Coffee Maker'), 1, 29.99);

SELECT * FROM dbo.Customers;
SELECT * FROM dbo.Products;
SELECT * FROM dbo.Orders;
SELECT * FROM dbo.OrderDetails;
