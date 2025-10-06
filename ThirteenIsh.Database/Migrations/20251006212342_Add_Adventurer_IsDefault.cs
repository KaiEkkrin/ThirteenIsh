using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThirteenIsh.Database.Migrations
{
    /// <inheritdoc />
    public partial class Add_Adventurer_IsDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Adventurers_AdventureId_UserId",
                table: "Adventurers");

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "Adventurers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Set all existing adventurers to IsDefault = true
            migrationBuilder.Sql("UPDATE \"Adventurers\" SET \"IsDefault\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_Adventurers_AdventureId_UserId_IsDefault",
                table: "Adventurers",
                columns: new[] { "AdventureId", "UserId", "IsDefault" },
                unique: true,
                filter: "\"IsDefault\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Adventurers_AdventureId_UserId_IsDefault",
                table: "Adventurers");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "Adventurers");

            migrationBuilder.CreateIndex(
                name: "IX_Adventurers_AdventureId_UserId",
                table: "Adventurers",
                columns: new[] { "AdventureId", "UserId" },
                unique: true);
        }
    }
}
