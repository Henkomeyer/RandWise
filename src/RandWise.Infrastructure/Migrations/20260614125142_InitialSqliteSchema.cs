using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RandWise.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialSqliteSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    IdentityUserId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    PreferredCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false, defaultValue: "ZAR"),
                    TimeZone = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, defaultValue: "Africa/Johannesburg"),
                    PreferredLanguage = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false, defaultValue: "en-ZA"),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DeletedUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    EntityId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    MetadataJson = table.Column<string>(type: "TEXT", nullable: true),
                    IpAddressHash = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BudgetCategories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    CategoryType = table.Column<int>(type: "INTEGER", nullable: false),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsSystem = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BudgetCategories_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BudgetPeriods",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ExpectedIncomeCents = table.Column<long>(type: "INTEGER", nullable: false),
                    ActualIncomeCents = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L),
                    OpeningBalanceCents = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BudgetPeriods_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancialProfiles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    DefaultMonthlyIncomeCents = table.Column<long>(type: "INTEGER", nullable: false),
                    PaydayDay = table.Column<int>(type: "INTEGER", nullable: true),
                    BudgetCycleType = table.Column<int>(type: "INTEGER", nullable: false),
                    StartingBalanceCents = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L),
                    SafetyBufferCents = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L),
                    SavingsCommitmentCents = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L),
                    NotificationMode = table.Column<int>(type: "INTEGER", nullable: false),
                    FirstDayOfWeek = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialProfiles_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IncomingMessages",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    WhatsAppMessageId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    PlatformContactId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    MessageType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    RawTextEncrypted = table.Column<string>(type: "TEXT", nullable: true),
                    PayloadHash = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    ProcessingStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    FailureReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ReceivedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProcessedUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncomingMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncomingMessages_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Channel = table.Column<int>(type: "INTEGER", nullable: false),
                    NotificationType = table.Column<int>(type: "INTEGER", nullable: false),
                    MessageEncrypted = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ScheduledUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SentUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FailureReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    AttemptCount = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WhatsAppContacts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    PhoneNumberHash = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    EncryptedPhoneNumber = table.Column<string>(type: "TEXT", nullable: false),
                    PlatformContactId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    IsVerified = table.Column<bool>(type: "INTEGER", nullable: false),
                    VerifiedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhatsAppContacts_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecurringTransactions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    CategoryId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 280, nullable: false),
                    Merchant = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    AmountCents = table.Column<long>(type: "INTEGER", nullable: false),
                    TransactionType = table.Column<int>(type: "INTEGER", nullable: false),
                    Frequency = table.Column<int>(type: "INTEGER", nullable: false),
                    DayOfMonth = table.Column<int>(type: "INTEGER", nullable: true),
                    DayOfWeek = table.Column<int>(type: "INTEGER", nullable: true),
                    NextOccurrenceDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    AutoCreate = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecurringTransactions_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecurringTransactions_BudgetCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "BudgetCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserCategoryRules",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    MatchType = table.Column<int>(type: "INTEGER", nullable: false),
                    MatchValue = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    NormalizedMatchValue = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    CategoryId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCategoryRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCategoryRules_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserCategoryRules_BudgetCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "BudgetCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CategoryBudgets",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    BudgetPeriodId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    CategoryId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    AllocatedAmountCents = table.Column<long>(type: "INTEGER", nullable: false),
                    RolloverAmountCents = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L),
                    WarningThresholdPercent = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 80),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryBudgets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategoryBudgets_BudgetCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "BudgetCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CategoryBudgets_BudgetPeriods_BudgetPeriodId",
                        column: x => x.BudgetPeriodId,
                        principalTable: "BudgetPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MessageInterpretations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    IncomingMessageId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Intent = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    AmountCents = table.Column<long>(type: "INTEGER", nullable: true),
                    TransactionType = table.Column<int>(type: "INTEGER", nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 280, nullable: true),
                    Merchant = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    TransactionDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    SuggestedCategoryId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    ConfidenceBasisPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    ParserVersion = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    RawStructuredResult = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageInterpretations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageInterpretations_BudgetCategories_SuggestedCategoryId",
                        column: x => x.SuggestedCategoryId,
                        principalTable: "BudgetCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MessageInterpretations_IncomingMessages_IncomingMessageId",
                        column: x => x.IncomingMessageId,
                        principalTable: "IncomingMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    CategoryId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    IncomingMessageId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    RecurringTransactionId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    AmountCents = table.Column<long>(type: "INTEGER", nullable: false),
                    TransactionType = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 280, nullable: false),
                    Merchant = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    TransactionDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ConfidenceBasisPoints = table.Column<int>(type: "INTEGER", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DeletedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transactions_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transactions_BudgetCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "BudgetCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transactions_IncomingMessages_IncomingMessageId",
                        column: x => x.IncomingMessageId,
                        principalTable: "IncomingMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transactions_RecurringTransactions_RecurringTransactionId",
                        column: x => x.RecurringTransactionId,
                        principalTable: "RecurringTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_IdentityUserId",
                table: "AppUsers",
                column: "IdentityUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId_CreatedUtc",
                table: "AuditLogs",
                columns: new[] { "UserId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetCategories_UserId_Slug",
                table: "BudgetCategories",
                columns: new[] { "UserId", "Slug" });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetPeriods_UserId_StartDate_EndDate",
                table: "BudgetPeriods",
                columns: new[] { "UserId", "StartDate", "EndDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CategoryBudgets_BudgetPeriodId_CategoryId",
                table: "CategoryBudgets",
                columns: new[] { "BudgetPeriodId", "CategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CategoryBudgets_CategoryId",
                table: "CategoryBudgets",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialProfiles_UserId",
                table: "FinancialProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IncomingMessages_UserId",
                table: "IncomingMessages",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_IncomingMessages_WhatsAppMessageId",
                table: "IncomingMessages",
                column: "WhatsAppMessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MessageInterpretations_IncomingMessageId",
                table: "MessageInterpretations",
                column: "IncomingMessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MessageInterpretations_SuggestedCategoryId",
                table: "MessageInterpretations",
                column: "SuggestedCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_ScheduledUtc",
                table: "Notifications",
                columns: new[] { "UserId", "ScheduledUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTransactions_CategoryId",
                table: "RecurringTransactions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTransactions_UserId_NextOccurrenceDate",
                table: "RecurringTransactions",
                columns: new[] { "UserId", "NextOccurrenceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_CategoryId",
                table: "Transactions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_IncomingMessageId",
                table: "Transactions",
                column: "IncomingMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_RecurringTransactionId",
                table: "Transactions",
                column: "RecurringTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_UserId_CategoryId_TransactionDate",
                table: "Transactions",
                columns: new[] { "UserId", "CategoryId", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_UserId_TransactionDate",
                table: "Transactions",
                columns: new[] { "UserId", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_UserCategoryRules_CategoryId",
                table: "UserCategoryRules",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCategoryRules_UserId_MatchType_NormalizedMatchValue",
                table: "UserCategoryRules",
                columns: new[] { "UserId", "MatchType", "NormalizedMatchValue" });

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppContacts_PhoneNumberHash",
                table: "WhatsAppContacts",
                column: "PhoneNumberHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppContacts_UserId",
                table: "WhatsAppContacts",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "CategoryBudgets");

            migrationBuilder.DropTable(
                name: "FinancialProfiles");

            migrationBuilder.DropTable(
                name: "MessageInterpretations");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "UserCategoryRules");

            migrationBuilder.DropTable(
                name: "WhatsAppContacts");

            migrationBuilder.DropTable(
                name: "BudgetPeriods");

            migrationBuilder.DropTable(
                name: "IncomingMessages");

            migrationBuilder.DropTable(
                name: "RecurringTransactions");

            migrationBuilder.DropTable(
                name: "BudgetCategories");

            migrationBuilder.DropTable(
                name: "AppUsers");
        }
    }
}
