using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeloMercado.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUtilizadorNomeAndPasswordTemp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Nome",
                table: "Utilizadores",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "PasswordTemporaria",
                table: "Utilizadores",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Utilizadores",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "Nome", "PasswordTemporaria" },
                values: new object[] { "Administrador BeloMercado", false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Nome",
                table: "Utilizadores");

            migrationBuilder.DropColumn(
                name: "PasswordTemporaria",
                table: "Utilizadores");
        }
    }
}
