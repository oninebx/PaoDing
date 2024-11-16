INSERT INTO dbo.Customers (FirstName, LastName, Email)
VALUES 
('Tom', 'Smith', 'tom.smith@example.com'),
('Jerry', 'Port', 'jerry.port@example.com');
Delete from Customers where FirstName IN ('Tom', 'Jerry');