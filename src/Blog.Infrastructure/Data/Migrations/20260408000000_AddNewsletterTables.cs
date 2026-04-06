using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blog.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Adds Newsletter, NewsletterSubscriber, NewsletterSendLog, and OutboxMessage tables
    /// per detailed design 14 (Newsletter).
    /// </summary>
    public partial class AddNewsletterTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Newsletter table
            migrationBuilder.CreateTable(
                name: "Newsletters",
                columns: table => new
                {
                    NewsletterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BodyHtml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)0),
                    DateSent = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Newsletters", x => x.NewsletterId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Newsletter_Status",
                table: "Newsletters",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Newsletter_CreatedAt",
                table: "Newsletters",
                column: "CreatedAt");

            // Filtered unique index on Slug (non-NULL values only — allows multiple drafts with NULL slug)
            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX IX_Newsletter_Slug ON Newsletters (Slug) WHERE Slug IS NOT NULL");

            // NewsletterSubscriber table
            migrationBuilder.CreateTable(
                name: "NewsletterSubscribers",
                columns: table => new
                {
                    SubscriberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ConfirmationTokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    TokenExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Confirmed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ResubscribedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsletterSubscribers", x => x.SubscriberId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterSubscriber_Email",
                table: "NewsletterSubscribers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterSubscriber_IsActive",
                table: "NewsletterSubscribers",
                column: "IsActive");

            // Filtered index on ConfirmationTokenHash (non-NULL only)
            migrationBuilder.Sql(
                "CREATE INDEX IX_NewsletterSubscriber_ConfirmationTokenHash ON NewsletterSubscribers (ConfirmationTokenHash) WHERE ConfirmationTokenHash IS NOT NULL");

            // NewsletterSendLog table
            migrationBuilder.CreateTable(
                name: "NewsletterSendLogs",
                columns: table => new
                {
                    NewsletterSendLogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NewsletterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubscriberId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecipientIdempotencyKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsletterSendLogs", x => x.NewsletterSendLogId);
                    table.ForeignKey(
                        name: "FK_NewsletterSendLogs_Newsletters_NewsletterId",
                        column: x => x.NewsletterId,
                        principalTable: "Newsletters",
                        principalColumn: "NewsletterId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NewsletterSendLogs_NewsletterSubscribers_SubscriberId",
                        column: x => x.SubscriberId,
                        principalTable: "NewsletterSubscribers",
                        principalColumn: "SubscriberId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "UQ_NewsletterSendLog_Newsletter_Recipient",
                table: "NewsletterSendLogs",
                columns: new[] { "NewsletterId", "RecipientIdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterSendLogs_SubscriberId",
                table: "NewsletterSendLogs",
                column: "SubscriberId");

            // OutboxMessage table
            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    OutboxMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    NextRetryAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.OutboxMessageId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_Status_NextRetryAt",
                table: "OutboxMessages",
                columns: new[] { "Status", "NextRetryAt" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "NewsletterSendLogs");
            migrationBuilder.DropTable(name: "OutboxMessages");
            migrationBuilder.DropTable(name: "NewsletterSubscribers");
            migrationBuilder.DropTable(name: "Newsletters");
        }
    }
}
