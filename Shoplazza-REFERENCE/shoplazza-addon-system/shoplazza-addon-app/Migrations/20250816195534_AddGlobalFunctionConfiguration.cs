using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoplazzaAddonApp.Migrations
{
    /// <inheritdoc />
    public partial class AddGlobalFunctionConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GlobalFunctionConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FunctionId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    FunctionName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    FunctionNamespace = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FunctionType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ConfigurationJson = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Version = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalFunctionConfigurations", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GlobalFunctionConfigurations");
        }
    }
}
