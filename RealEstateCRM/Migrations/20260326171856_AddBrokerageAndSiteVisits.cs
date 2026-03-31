using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RealEstateCRM.Migrations
{
    /// <inheritdoc />
    public partial class AddBrokerageAndSiteVisits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Brokerages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LeadId = table.Column<int>(type: "INTEGER", nullable: true),
                    PropertyId = table.Column<int>(type: "INTEGER", nullable: true),
                    DealValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CommissionPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GstPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    GstAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalPayable = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SubBrokerName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    SubBrokerSplitPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    SubBrokerAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PaymentStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brokerages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Brokerages_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Brokerages_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SiteVisits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ContactId = table.Column<int>(type: "INTEGER", nullable: false),
                    PropertyId = table.Column<int>(type: "INTEGER", nullable: false),
                    ScheduledDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    PickupLocation = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Feedback = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteVisits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SiteVisits_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SiteVisits_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Brokerages_LeadId",
                table: "Brokerages",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_Brokerages_PaymentStatus",
                table: "Brokerages",
                column: "PaymentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Brokerages_PropertyId",
                table: "Brokerages",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteVisits_ContactId",
                table: "SiteVisits",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteVisits_PropertyId",
                table: "SiteVisits",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteVisits_ScheduledDate",
                table: "SiteVisits",
                column: "ScheduledDate");

            migrationBuilder.CreateIndex(
                name: "IX_SiteVisits_Status",
                table: "SiteVisits",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Brokerages");

            migrationBuilder.DropTable(
                name: "SiteVisits");
        }
    }
}
