using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThirteenIsh.Database.Migrations
{
    /// <inheritdoc />
    public partial class Add_ListCharactersMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "After",
                table: "Messages",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeleteCustomCounterMessage_CharacterType",
                table: "Messages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeleteCustomCounterMessage_Name",
                table: "Messages",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "After",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "DeleteCustomCounterMessage_CharacterType",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "DeleteCustomCounterMessage_Name",
                table: "Messages");
        }
    }
}
