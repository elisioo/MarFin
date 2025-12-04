CREATE TABLE tbl_Sync_Log (
    sync_log_id INT IDENTITY(1,1) PRIMARY KEY,
    table_name NVARCHAR(128) NOT NULL,
    last_sync_date DATETIME2 NOT NULL,
    sync_direction NVARCHAR(20) NOT NULL, -- 'LocalToRemote', 'RemoteToLocal', 'Bidirectional'
    records_synced INT DEFAULT 0,
    sync_status NVARCHAR(20) NOT NULL, -- 'Success', 'Failed', 'InProgress'
    error_message NVARCHAR(MAX) NULL,
    created_date DATETIME2 DEFAULT GETDATE()
)