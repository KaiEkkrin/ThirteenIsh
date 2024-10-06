using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThirteenIsh.Database.Migrations
{
    /// <inheritdoc />
    public partial class Add_ListCharactersMessage_PageSize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PageSize",
                table: "Messages",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PageSize",
                table: "Messages");
        }
    }
}
