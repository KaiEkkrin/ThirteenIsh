using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThirteenIsh.Database.Migrations
{
    /// <inheritdoc />
    public partial class Add_DeleteCustomCounter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CcName",
                table: "Messages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeleteCharacterMessage_CharacterType",
                table: "Messages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeleteCharacterMessage_Name",
                table: "Messages",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CcName",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "DeleteCharacterMessage_CharacterType",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "DeleteCharacterMessage_Name",
                table: "Messages");
        }
    }
}
