using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThirteenIsh.Database.Migrations
{
    /// <inheritdoc />
    public partial class Add_Fixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Fixes",
                table: "Adventurers",
                type: "jsonb",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Fixes",
                table: "Adventurers");
        }
    }
}
