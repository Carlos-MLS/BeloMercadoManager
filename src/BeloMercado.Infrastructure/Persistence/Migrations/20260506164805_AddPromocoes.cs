using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeloMercado.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPromocoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Promocoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProdutoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LojaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Tipo = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    DataInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    DataFim = table.Column<DateOnly>(type: "date", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Promocoes", x => x.Id);
                    table.CheckConstraint("CK_Promocoes_Datas", "[DataFim] >= [DataInicio]");
                    table.CheckConstraint("CK_Promocoes_Valor_Positivo", "[Valor] > 0");
                    table.ForeignKey(
                        name: "FK_Promocoes_Lojas_LojaId",
                        column: x => x.LojaId,
                        principalTable: "Lojas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Promocoes_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LinhaVendaPromocao",
                columns: table => new
                {
                    LinhaVendaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PromocaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ValorDescontoAplicado = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LinhaVendaPromocao", x => new { x.LinhaVendaId, x.PromocaoId });
                    table.ForeignKey(
                        name: "FK_LinhaVendaPromocao_LinhasVenda_LinhaVendaId",
                        column: x => x.LinhaVendaId,
                        principalTable: "LinhasVenda",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LinhaVendaPromocao_Promocoes_PromocaoId",
                        column: x => x.PromocaoId,
                        principalTable: "Promocoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LinhaVendaPromocao_PromocaoId",
                table: "LinhaVendaPromocao",
                column: "PromocaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Promocoes_LojaId",
                table: "Promocoes",
                column: "LojaId");

            migrationBuilder.CreateIndex(
                name: "IX_Promocoes_ProdutoId_LojaId_Ativo",
                table: "Promocoes",
                columns: new[] { "ProdutoId", "LojaId", "Ativo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LinhaVendaPromocao");

            migrationBuilder.DropTable(
                name: "Promocoes");
        }
    }
}
