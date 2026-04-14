using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BakeryPOS.Migrations
{
    /// <inheritdoc />
    public partial class HashedPasswordsYDBPath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$75IpD3/Q0pbp6VhE39JrcupgqjR/pkDW/mS7G1Azp1ydJbEc1on0G");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "PasswordHash",
                value: "$2a$11$UMdwxpbiWwE.fOazIWs4weZeVNIOY1XCP6kgUSkS8ytUSgJjyiZpK");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "admin");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "PasswordHash",
                value: "cajero");
        }
    }
}
