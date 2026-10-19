using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeloMercado.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFornecedores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Fornecedores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Nif = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false),
                    Morada = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Contacto = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Ramo = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CondicoesPagamento = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fornecedores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProdutoFornecedor",
                columns: table => new
                {
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProdutoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrecoCusto = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    DescontoPercentagem = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    PrazoEntregaDias = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProdutoFornecedor", x => new { x.FornecedorId, x.ProdutoId });
                    table.CheckConstraint("CK_ProdutoFornecedor_Desconto_0_100", "[DescontoPercentagem] BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_ProdutoFornecedor_Prazo_NaoNeg", "[PrazoEntregaDias] >= 0");
                    table.CheckConstraint("CK_ProdutoFornecedor_Preco_NaoNeg", "[PrecoCusto] >= 0");
                    table.ForeignKey(
                        name: "FK_ProdutoFornecedor_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProdutoFornecedor_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Fornecedores_Nif",
                table: "Fornecedores",
                column: "Nif",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProdutoFornecedor_ProdutoId",
                table: "ProdutoFornecedor",
                column: "ProdutoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProdutoFornecedor");

            migrationBuilder.DropTable(
                name: "Fornecedores");
        }
    }
}
