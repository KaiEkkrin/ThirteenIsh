using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThirteenIsh.Database.Migrations
{
    /// <inheritdoc />
    public partial class Add_AdventurerName_To_Messages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Adventurers_AdventureId_UserId_IsDefault",
                table: "Adventurers");

            migrationBuilder.AddColumn<string>(
                name: "AdventurerName",
                table: "Messages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResetAdventurerMessage_AdventurerName",
                table: "Messages",
                type: "text",
                nullable: true);

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
                name: "AdventurerName",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ResetAdventurerMessage_AdventurerName",
                table: "Messages");

            migrationBuilder.CreateIndex(
                name: "IX_Adventurers_AdventureId_UserId_IsDefault",
                table: "Adventurers",
                columns: new[] { "AdventureId", "UserId", "IsDefault" },
                unique: true,
                filter: "[IsDefault] = 1");
        }
    }
}
