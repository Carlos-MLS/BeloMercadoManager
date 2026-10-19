using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeloMercado.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEncomendas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Encomendas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FornecedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LojaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CriadoPorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataSubmissao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DataRececao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Total = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Encomendas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Encomendas_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Encomendas_Lojas_LojaId",
                        column: x => x.LojaId,
                        principalTable: "Lojas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LinhasEncomenda",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EncomendaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProdutoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProdutoDescricao = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ProdutoEan13 = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: false),
                    Quantidade = table.Column<int>(type: "int", nullable: false),
                    PrecoCustoUnitario = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    DescontoPercentagem = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    QuantidadeRecebida = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LinhasEncomenda", x => x.Id);
                    table.CheckConstraint("CK_LinhasEncomenda_QtdRecebida_NaoNeg", "[QuantidadeRecebida] >= 0");
                    table.CheckConstraint("CK_LinhasEncomenda_Quantidade_Pos", "[Quantidade] > 0");
                    table.ForeignKey(
                        name: "FK_LinhasEncomenda_Encomendas_EncomendaId",
                        column: x => x.EncomendaId,
                        principalTable: "Encomendas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LinhasEncomenda_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Encomendas_FornecedorId_Estado",
                table: "Encomendas",
                columns: new[] { "FornecedorId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Encomendas_LojaId_DataCriacao",
                table: "Encomendas",
                columns: new[] { "LojaId", "DataCriacao" });

            migrationBuilder.CreateIndex(
                name: "IX_LinhasEncomenda_EncomendaId",
                table: "LinhasEncomenda",
                column: "EncomendaId");

            migrationBuilder.CreateIndex(
                name: "IX_LinhasEncomenda_ProdutoId",
                table: "LinhasEncomenda",
                column: "ProdutoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LinhasEncomenda");

            migrationBuilder.DropTable(
                name: "Encomendas");
        }
    }
}
