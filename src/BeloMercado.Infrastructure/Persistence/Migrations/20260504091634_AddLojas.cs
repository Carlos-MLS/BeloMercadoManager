using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeloMercado.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLojas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Lojas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Designacao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Morada = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CodigoPostal = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Contacto = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Horario = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    StockMinPorDefeito = table.Column<int>(type: "int", nullable: false),
                    StockMaxPorDefeito = table.Column<int>(type: "int", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lojas", x => x.Id);
                    table.CheckConstraint("CK_Lojas_StockBounds", "[StockMaxPorDefeito] >= [StockMinPorDefeito]");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Utilizadores_LojaId",
                table: "Utilizadores",
                column: "LojaId");

            migrationBuilder.CreateIndex(
                name: "IX_Lojas_Designacao",
                table: "Lojas",
                column: "Designacao",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Utilizadores_Lojas_LojaId",
                table: "Utilizadores",
                column: "LojaId",
                principalTable: "Lojas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Utilizadores_Lojas_LojaId",
                table: "Utilizadores");

            migrationBuilder.DropTable(
                name: "Lojas");

            migrationBuilder.DropIndex(
                name: "IX_Utilizadores_LojaId",
                table: "Utilizadores");
        }
    }
}
