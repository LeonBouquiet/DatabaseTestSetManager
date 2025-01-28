CREATE TABLE [dbo].[Product] (
    [Id]   INT            IDENTITY (1, 1) NOT NULL,
    [Code] NVARCHAR (32)  NOT NULL,
    [Name] NVARCHAR (100) NULL,
    CONSTRAINT [PK_Product] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [UQ_ProductCode]
    ON [dbo].[Product]([Code] ASC);

