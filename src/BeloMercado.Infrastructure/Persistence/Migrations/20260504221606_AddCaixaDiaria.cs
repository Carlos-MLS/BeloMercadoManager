using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeloMercado.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCaixaDiaria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CaixasDiarias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LojaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    OperadorAberturaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataHoraAbertura = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OperadorFechoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DataHoraFecho = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FundoInicial = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    ValorEsperado = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    ValorContado = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    Diferenca = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    Justificacao = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ConsolidacaoTriggered = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaixasDiarias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaixasDiarias_Lojas_LojaId",
                        column: x => x.LojaId,
                        principalTable: "Lojas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CaixasDiarias_LojaId_Data",
                table: "CaixasDiarias",
                columns: new[] { "LojaId", "Data" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaixasDiarias");
        }
    }
}
