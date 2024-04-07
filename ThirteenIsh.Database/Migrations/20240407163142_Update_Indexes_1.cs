using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThirteenIsh.Database.Migrations
{
    /// <inheritdoc />
    public partial class Update_Indexes_1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Characters_UserId_Name",
                table: "Characters");

            migrationBuilder.DropIndex(
                name: "IX_Characters_UserId_NameUpper",
                table: "Characters");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_Timestamp",
                table: "Messages",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_UserId_CharacterType_Name",
                table: "Characters",
                columns: new[] { "UserId", "CharacterType", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Characters_UserId_CharacterType_NameUpper",
                table: "Characters",
                columns: new[] { "UserId", "CharacterType", "NameUpper" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Messages_Timestamp",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Characters_UserId_CharacterType_Name",
                table: "Characters");

            migrationBuilder.DropIndex(
                name: "IX_Characters_UserId_CharacterType_NameUpper",
                table: "Characters");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_UserId_Name",
                table: "Characters",
                columns: new[] { "UserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Characters_UserId_NameUpper",
                table: "Characters",
                columns: new[] { "UserId", "NameUpper" },
                unique: true);
        }
    }
}
