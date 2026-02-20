using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoplazzaAddonApp.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Merchants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Shop = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    StoreName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    StoreEmail = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    AccessToken = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Scopes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    TokenCreatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TokenExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    LastLoginAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UninstalledAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ScriptTagId = table.Column<string>(type: "TEXT", nullable: true),
                    Metadata = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Merchants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Configurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MerchantId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    DefaultTaxable = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultRequiresShipping = table.Column<bool>(type: "INTEGER", nullable: false),
                    WidgetSettings = table.Column<string>(type: "TEXT", nullable: true),
                    AnalyticsSettings = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    CustomSettings = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Configurations_Merchants_MerchantId",
                        column: x => x.MerchantId,
                        principalTable: "Merchants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FunctionConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MerchantId = table.Column<int>(type: "INTEGER", nullable: false),
                    FunctionId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    FunctionName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    FunctionType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    ActivatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ConfigurationJson = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FunctionConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FunctionConfigurations_Merchants_MerchantId",
                        column: x => x.MerchantId,
                        principalTable: "Merchants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductAddOns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MerchantId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductId = table.Column<string>(type: "TEXT", nullable: false),
                    ProductTitle = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ProductHandle = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddOnProductId = table.Column<string>(type: "TEXT", nullable: true),
                    AddOnVariantId = table.Column<string>(type: "TEXT", nullable: true),
                    AddOnTitle = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    AddOnDescription = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    AddOnPriceCents = table.Column<int>(type: "INTEGER", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    DisplayText = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    AddOnSku = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RequiresShipping = table.Column<bool>(type: "INTEGER", nullable: false),
                    WeightGrams = table.Column<int>(type: "INTEGER", nullable: false),
                    IsTaxable = table.Column<bool>(type: "INTEGER", nullable: false),
                    ImageUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    ConfigurationJson = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductAddOns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductAddOns_Merchants_MerchantId",
                        column: x => x.MerchantId,
                        principalTable: "Merchants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Configurations_MerchantId",
                table: "Configurations",
                column: "MerchantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FunctionConfigurations_MerchantId",
                table: "FunctionConfigurations",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "IX_Merchants_Shop",
                table: "Merchants",
                column: "Shop",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductAddOns_MerchantId_ProductId",
                table: "ProductAddOns",
                columns: new[] { "MerchantId", "ProductId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Configurations");

            migrationBuilder.DropTable(
                name: "FunctionConfigurations");

            migrationBuilder.DropTable(
                name: "ProductAddOns");

            migrationBuilder.DropTable(
                name: "Merchants");
        }
    }
}
