/*
  Script de preparación para SQL Server.
  Ejecutar en SQL Server Management Studio (SSMS).
  Si no se tienen permisos para CREATE DATABASE, crear FeedbackDB manualmente
  y ejecutar desde USE FeedbackDB en adelante.
*/
IF DB_ID(N'FeedbackDB') IS NULL
BEGIN
    CREATE DATABASE FeedbackDB;
END;
GO

USE FeedbackDB;
GO

IF OBJECT_ID(N'dbo.Reviews', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Reviews
    (
        Id INT NOT NULL CONSTRAINT PK_Reviews PRIMARY KEY,
        CustomerName NVARCHAR(150) NOT NULL,
        Rating INT NOT NULL,
        Comment NVARCHAR(2000) NULL,
        CreatedAt DATETIME2(0) NOT NULL
            CONSTRAINT DF_Reviews_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_Reviews_Rating CHECK (Rating BETWEEN 1 AND 5)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Reviews WHERE Id = 1)
BEGIN
    INSERT INTO dbo.Reviews (Id, CustomerName, Rating, Comment, CreatedAt)
    VALUES
    (1, N'Cliente de prueba', 5, N'Servicio satisfactorio', SYSUTCDATETIME()),
    (2, N'Cliente de prueba 2', 3, N'Experiencia regular', SYSUTCDATETIME()),
    (3, N'Cliente de prueba 3', 4, N'Buena atención', SYSUTCDATETIME());
END;
GO

SELECT Id, CustomerName, Rating, Comment, CreatedAt
FROM dbo.Reviews
ORDER BY Id;
GO
