-- Sync log table for bidirectional synchronization
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Sync_Log]') AND type = N'U')
BEGIN
    CREATE TABLE [dbo].[tbl_Sync_Log]
    (
        [sync_log_id]       BIGINT IDENTITY(1,1)    NOT NULL PRIMARY KEY,
        [sync_batch_id]     UNIQUEIDENTIFIER        NOT NULL,
        [table_name]        NVARCHAR(128)           NOT NULL,
        [record_id]         BIGINT                  NULL,
        [local_pk_value]    NVARCHAR(128)           NULL,
        [remote_pk_value]   NVARCHAR(128)           NULL,
        [direction]         NVARCHAR(32)            NOT NULL,   -- e.g. LocalToRemote, RemoteToLocal, Bidirectional
        [status]            NVARCHAR(32)            NOT NULL,   -- e.g. Pending, InProgress, Completed, Failed, Conflicted
        [conflict_strategy] NVARCHAR(32)            NULL,       -- LastWriteWins, LocalPriority, RemotePriority, Manual
        [conflict_reason]   NVARCHAR(256)           NULL,
        [local_modified_date]  DATETIME2(3)         NULL,
        [remote_modified_date] DATETIME2(3)         NULL,
        [resolved_side]        NVARCHAR(16)         NULL,       -- Local / Remote
        [error_message]        NVARCHAR(1024)       NULL,
        [created_date]         DATETIME2(3)         NOT NULL CONSTRAINT DF_tbl_Sync_Log_created_date DEFAULT (SYSUTCDATETIME()),
        [created_by]           NVARCHAR(100)        NULL,
        [notes]                NVARCHAR(MAX)        NULL
    );

    CREATE NONCLUSTERED INDEX [IX_tbl_Sync_Log_Table_Record]
        ON [dbo].[tbl_Sync_Log] ([table_name] ASC, [record_id] ASC, [created_date] DESC);

    CREATE NONCLUSTERED INDEX [IX_tbl_Sync_Log_Batch]
        ON [dbo].[tbl_Sync_Log] ([sync_batch_id] ASC, [created_date] DESC);

    CREATE NONCLUSTERED INDEX [IX_tbl_Sync_Log_Status]
        ON [dbo].[tbl_Sync_Log] ([status] ASC, [created_date] DESC);
END
GO

-- Helper: create modified_date index if it does not exist
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE name = N'usp_CreateModifiedDateIndexIfMissing' AND type = 'P')
BEGIN
    EXEC ('
    CREATE PROCEDURE [dbo].[usp_CreateModifiedDateIndexIfMissing]
        @TableName sysname,
        @IndexName sysname
    AS
    BEGIN
        SET NOCOUNT ON;

        DECLARE @sql NVARCHAR(MAX);

        -- Only proceed if the table has a [modified_date] column
        IF EXISTS (
            SELECT 1
            FROM sys.columns c
            INNER JOIN sys.objects o ON c.object_id = o.object_id
            WHERE o.name = @TableName
              AND c.name = ''modified_date''
        )
        BEGIN
            IF NOT EXISTS (
                SELECT 1
                FROM sys.indexes i
                JOIN sys.objects o ON i.object_id = o.object_id
                WHERE o.name = @TableName
                  AND i.name = @IndexName
            )
            BEGIN
                SET @sql = N''CREATE NONCLUSTERED INDEX '' + QUOTENAME(@IndexName) + N'' ON '' + QUOTENAME(''dbo'') + N''.'' + QUOTENAME(@TableName) + N''([modified_date] DESC);'';
                EXEC sp_executesql @sql;
            END
        END
    END');
END
GO

-- Ensure modified_date indexes on core business tables
EXEC [dbo].[usp_CreateModifiedDateIndexIfMissing] @TableName = N''tbl_Users'',           @IndexName = N''IX_tbl_Users_ModifiedDate'';
EXEC [dbo].[usp_CreateModifiedDateIndexIfMissing] @TableName = N''tbl_Customers'',       @IndexName = N''IX_tbl_Customers_ModifiedDate'';
EXEC [dbo].[usp_CreateModifiedDateIndexIfMissing] @TableName = N''tbl_Invoices'',        @IndexName = N''IX_tbl_Invoices_ModifiedDate'';
EXEC [dbo].[usp_CreateModifiedDateIndexIfMissing] @TableName = N''tbl_Sales_Pipeline'',  @IndexName = N''IX_tbl_Sales_Pipeline_ModifiedDate'';
EXEC [dbo].[usp_CreateModifiedDateIndexIfMissing] @TableName = N''tbl_Interactions'',    @IndexName = N''IX_tbl_Interactions_ModifiedDate'';
EXEC [dbo].[usp_CreateModifiedDateIndexIfMissing] @TableName = N''tbl_Transactions'',    @IndexName = N''IX_tbl_Transactions_ModifiedDate'';
EXEC [dbo].[usp_CreateModifiedDateIndexIfMissing] @TableName = N''tbl_Campaigns'',       @IndexName = N''IX_tbl_Campaigns_ModifiedDate'';
EXEC [dbo].[usp_CreateModifiedDateIndexIfMissing] @TableName = N''tbl_Documents'',       @IndexName = N''IX_tbl_Documents_ModifiedDate'';
GO
