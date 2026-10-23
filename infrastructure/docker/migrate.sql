IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [DisplayName] nvarchar(120) NOT NULL,
        [Active] bit NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE TABLE [Categories] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Active] bit NOT NULL,
        CONSTRAINT [PK_Categories] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE TABLE [Queues] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Active] bit NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Queues] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE TABLE [Responses] (
        [Id] uniqueidentifier NOT NULL,
        [Title] nvarchar(120) NOT NULL,
        [Body] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Responses] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE TABLE [SlaPolicies] (
        [Priority] int NOT NULL,
        [FirstResponseMinutes] int NOT NULL,
        [ResolutionMinutes] int NOT NULL,
        [PauseWhileWaiting] bit NOT NULL,
        [CalendarJson] nvarchar(max) NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_SlaPolicies] PRIMARY KEY ([Priority])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE TABLE [Articles] (
        [Id] uniqueidentifier NOT NULL,
        [Title] nvarchar(180) NOT NULL,
        [Slug] nvarchar(180) NOT NULL,
        [Summary] nvarchar(400) NOT NULL,
        [Body] nvarchar(max) NOT NULL,
        [Category] nvarchar(100) NOT NULL,
        [AuthorId] nvarchar(450) NOT NULL,
        [Published] bit NOT NULL,
        [Featured] bit NOT NULL,
        [UpdatedAt] datetimeoffset NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Articles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Articles_AspNetUsers_AuthorId] FOREIGN KEY ([AuthorId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE TABLE [Memberships] (
        [QueueId] uniqueidentifier NOT NULL,
        [AgentId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_Memberships] PRIMARY KEY ([QueueId], [AgentId]),
        CONSTRAINT [FK_Memberships_AspNetUsers_AgentId] FOREIGN KEY ([AgentId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Memberships_Queues_QueueId] FOREIGN KEY ([QueueId]) REFERENCES [Queues] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE TABLE [Tickets] (
        [Id] uniqueidentifier NOT NULL,
        [Number] bigint NOT NULL IDENTITY(1001, 1),
        [SubmissionId] uniqueidentifier NOT NULL,
        [Subject] nvarchar(180) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [CustomerId] nvarchar(450) NOT NULL,
        [AssignedAgentId] nvarchar(450) NULL,
        [QueueId] uniqueidentifier NOT NULL,
        [CategoryId] uniqueidentifier NOT NULL,
        [Priority] int NOT NULL,
        [Status] int NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [UpdatedAt] datetimeoffset NOT NULL,
        [FirstResponseAt] datetimeoffset NULL,
        [ResolvedAt] datetimeoffset NULL,
        [ClosedAt] datetimeoffset NULL,
        [FirstResponseDue] datetimeoffset NOT NULL,
        [ResolutionDue] datetimeoffset NOT NULL,
        [FirstResponseMinutes] int NOT NULL,
        [ResolutionMinutes] int NOT NULL,
        [CalendarSnapshot] nvarchar(max) NOT NULL,
        [PauseWhileWaiting] bit NOT NULL,
        [ResolutionCycle] int NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Tickets] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Tickets_AspNetUsers_AssignedAgentId] FOREIGN KEY ([AssignedAgentId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Tickets_AspNetUsers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Tickets_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Tickets_Queues_QueueId] FOREIGN KEY ([QueueId]) REFERENCES [Queues] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE TABLE [Feedback] (
        [TicketId] uniqueidentifier NOT NULL,
        [Rating] int NOT NULL,
        [Comment] nvarchar(1000) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_Feedback] PRIMARY KEY ([TicketId]),
        CONSTRAINT [CK_Feedback_Rating] CHECK ([Rating] BETWEEN 1 AND 5),
        CONSTRAINT [FK_Feedback_Tickets_TicketId] FOREIGN KEY ([TicketId]) REFERENCES [Tickets] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE TABLE [Notifications] (
        [Id] uniqueidentifier NOT NULL,
        [RecipientId] nvarchar(450) NOT NULL,
        [TicketId] uniqueidentifier NOT NULL,
        [Title] nvarchar(180) NOT NULL,
        [EventKey] nvarchar(180) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [ReadAt] datetimeoffset NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Notifications_AspNetUsers_RecipientId] FOREIGN KEY ([RecipientId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Notifications_Tickets_TicketId] FOREIGN KEY ([TicketId]) REFERENCES [Tickets] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE TABLE [SlaPause] (
        [Id] uniqueidentifier NOT NULL,
        [TicketId] uniqueidentifier NOT NULL,
        [StartedAt] datetimeoffset NOT NULL,
        [EndedAt] datetimeoffset NULL,
        CONSTRAINT [PK_SlaPause] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SlaPause_Tickets_TicketId] FOREIGN KEY ([TicketId]) REFERENCES [Tickets] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE TABLE [TicketActivity] (
        [Id] uniqueidentifier NOT NULL,
        [TicketId] uniqueidentifier NOT NULL,
        [ActorId] nvarchar(max) NOT NULL,
        [Description] nvarchar(300) NOT NULL,
        [Internal] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_TicketActivity] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TicketActivity_Tickets_TicketId] FOREIGN KEY ([TicketId]) REFERENCES [Tickets] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE TABLE [TicketAttachment] (
        [Id] uniqueidentifier NOT NULL,
        [TicketId] uniqueidentifier NOT NULL,
        [FileName] nvarchar(180) NOT NULL,
        [StorageKey] nvarchar(64) NOT NULL,
        [ContentType] nvarchar(100) NOT NULL,
        [Length] bigint NOT NULL,
        [Internal] bit NOT NULL,
        [UploadedBy] nvarchar(max) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_TicketAttachment] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TicketAttachment_Tickets_TicketId] FOREIGN KEY ([TicketId]) REFERENCES [Tickets] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE TABLE [TicketMessage] (
        [Id] uniqueidentifier NOT NULL,
        [TicketId] uniqueidentifier NOT NULL,
        [SubmissionId] uniqueidentifier NOT NULL,
        [AuthorId] nvarchar(450) NOT NULL,
        [AuthorName] nvarchar(120) NOT NULL,
        [Visibility] int NOT NULL,
        [Body] nvarchar(max) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_TicketMessage] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TicketMessage_AspNetUsers_AuthorId] FOREIGN KEY ([AuthorId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TicketMessage_Tickets_TicketId] FOREIGN KEY ([TicketId]) REFERENCES [Tickets] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE INDEX [IX_Articles_AuthorId] ON [Articles] ([AuthorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Articles_Slug] ON [Articles] ([Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Categories_Name] ON [Categories] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE INDEX [IX_Memberships_AgentId] ON [Memberships] ([AgentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE INDEX [IX_Notifications_RecipientId_CreatedAt] ON [Notifications] ([RecipientId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Notifications_RecipientId_EventKey] ON [Notifications] ([RecipientId], [EventKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE INDEX [IX_Notifications_TicketId] ON [Notifications] ([TicketId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Queues_Name] ON [Queues] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE INDEX [IX_SlaPause_TicketId] ON [SlaPause] ([TicketId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE INDEX [IX_TicketActivity_TicketId_CreatedAt] ON [TicketActivity] ([TicketId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE INDEX [IX_TicketAttachment_TicketId] ON [TicketAttachment] ([TicketId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE INDEX [IX_TicketMessage_AuthorId] ON [TicketMessage] ([AuthorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TicketMessage_TicketId_AuthorId_SubmissionId] ON [TicketMessage] ([TicketId], [AuthorId], [SubmissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE INDEX [IX_Tickets_AssignedAgentId] ON [Tickets] ([AssignedAgentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE INDEX [IX_Tickets_CategoryId] ON [Tickets] ([CategoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Tickets_CustomerId_SubmissionId] ON [Tickets] ([CustomerId], [SubmissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE INDEX [IX_Tickets_FirstResponseDue] ON [Tickets] ([FirstResponseDue]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Tickets_Number] ON [Tickets] ([Number]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE INDEX [IX_Tickets_QueueId_Status_UpdatedAt] ON [Tickets] ([QueueId], [Status], [UpdatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    CREATE INDEX [IX_Tickets_Status_ResolutionDue] ON [Tickets] ([Status], [ResolutionDue]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022130920_InitialHelpdesk'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261022130920_InitialHelpdesk', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022134220_VersionedAdministration'
)
BEGIN
    ALTER TABLE [Responses] ADD [Active] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022134220_VersionedAdministration'
)
BEGIN
    ALTER TABLE [Responses] ADD [RowVersion] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022134220_VersionedAdministration'
)
BEGIN
    ALTER TABLE [Categories] ADD [RowVersion] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022134220_VersionedAdministration'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261022134220_VersionedAdministration', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022134614_SlaAlertIdentity'
)
BEGIN
    ALTER TABLE [TicketActivity] ADD [EventKey] nvarchar(180) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022134614_SlaAlertIdentity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_TicketActivity_TicketId_EventKey] ON [TicketActivity] ([TicketId], [EventKey]) WHERE [EventKey] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022134614_SlaAlertIdentity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261022134614_SlaAlertIdentity', N'10.0.13');
END;

COMMIT;
GO
