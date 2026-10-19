using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeloMercado.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Stocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LojaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProdutoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantidade = table.Column<int>(type: "int", nullable: false),
                    QtdMinima = table.Column<int>(type: "int", nullable: false),
                    QtdMaxima = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stocks", x => x.Id);
                    table.CheckConstraint("CK_Stocks_Bounds", "[QtdMaxima] >= [QtdMinima]");
                    table.CheckConstraint("CK_Stocks_Quantidade_NaoNegativa", "[Quantidade] >= 0");
                    table.ForeignKey(
                        name: "FK_Stocks_Lojas_LojaId",
                        column: x => x.LojaId,
                        principalTable: "Lojas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Stocks_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_LojaId_ProdutoId",
                table: "Stocks",
                columns: new[] { "LojaId", "ProdutoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_LojaId_Quantidade",
                table: "Stocks",
                columns: new[] { "LojaId", "Quantidade" });

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_ProdutoId",
                table: "Stocks",
                column: "ProdutoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Stocks");
        }
    }
}
