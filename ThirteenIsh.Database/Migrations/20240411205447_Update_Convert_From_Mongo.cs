using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThirteenIsh.Database.Migrations
{
    /// <inheritdoc />
    public partial class Update_Convert_From_Mongo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Encounters_GuildId",
                table: "Encounters");

            migrationBuilder.DropIndex(
                name: "IX_Combatants_EncounterId",
                table: "Combatants");

            migrationBuilder.DropColumn(
                name: "TurnIndex",
                table: "Encounters");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "Combatants");

            migrationBuilder.AddColumn<string>(
                name: "TurnAlias",
                table: "Encounters",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Encounters_GuildId_ChannelId",
                table: "Encounters",
                columns: new[] { "GuildId", "ChannelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Combatants_EncounterId_Alias",
                table: "Combatants",
                columns: new[] { "EncounterId", "Alias" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Adventurers_AdventureId_UserId",
                table: "Adventurers",
                columns: new[] { "AdventureId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Encounters_GuildId_ChannelId",
                table: "Encounters");

            migrationBuilder.DropIndex(
                name: "IX_Combatants_EncounterId_Alias",
                table: "Combatants");

            migrationBuilder.DropIndex(
                name: "IX_Adventurers_AdventureId_UserId",
                table: "Adventurers");

            migrationBuilder.DropColumn(
                name: "TurnAlias",
                table: "Encounters");

            migrationBuilder.AddColumn<int>(
                name: "TurnIndex",
                table: "Encounters",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "Combatants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Encounters_GuildId",
                table: "Encounters",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_Combatants_EncounterId",
                table: "Combatants",
                column: "EncounterId");
        }
    }
}
