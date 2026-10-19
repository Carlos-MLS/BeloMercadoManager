using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeloMercado.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConsolidacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConsolidacoesDiarias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LojaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    DataInicio = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DataFim = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Tentativas = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    UltimaMensagemErro = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Origem = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsolidacoesDiarias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsolidacoesDiarias_Lojas_LojaId",
                        column: x => x.LojaId,
                        principalTable: "Lojas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KPIsDiarios",
                columns: table => new
                {
                    LojaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalVendas = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    NumTransacoes = table.Column<int>(type: "int", nullable: false),
                    TicketMedio = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    TotalIva = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    TotalDescontos = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    VendasAnuladas = table.Column<int>(type: "int", nullable: false),
                    Devolucoes = table.Column<int>(type: "int", nullable: false),
                    UltimaAtualizacao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KPIsDiarios", x => new { x.LojaId, x.Data });
                    table.ForeignKey(
                        name: "FK_KPIsDiarios_Lojas_LojaId",
                        column: x => x.LojaId,
                        principalTable: "Lojas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RankingProdutosDiarios",
                columns: table => new
                {
                    LojaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    ProdutoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Posicao = table.Column<int>(type: "int", nullable: false),
                    QuantidadeVendida = table.Column<int>(type: "int", nullable: false),
                    Receita = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RankingProdutosDiarios", x => new { x.LojaId, x.Data, x.ProdutoId });
                    table.ForeignKey(
                        name: "FK_RankingProdutosDiarios_Lojas_LojaId",
                        column: x => x.LojaId,
                        principalTable: "Lojas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RankingProdutosDiarios_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsolidacoesDiarias_Estado",
                table: "ConsolidacoesDiarias",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_ConsolidacoesDiarias_LojaId_Data",
                table: "ConsolidacoesDiarias",
                columns: new[] { "LojaId", "Data" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KPIsDiarios_LojaId_Data",
                table: "KPIsDiarios",
                columns: new[] { "LojaId", "Data" });

            migrationBuilder.CreateIndex(
                name: "IX_RankingProdutosDiarios_LojaId_Data_Posicao",
                table: "RankingProdutosDiarios",
                columns: new[] { "LojaId", "Data", "Posicao" });

            migrationBuilder.CreateIndex(
                name: "IX_RankingProdutosDiarios_ProdutoId",
                table: "RankingProdutosDiarios",
                column: "ProdutoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsolidacoesDiarias");

            migrationBuilder.DropTable(
                name: "KPIsDiarios");

            migrationBuilder.DropTable(
                name: "RankingProdutosDiarios");
        }
    }
}
