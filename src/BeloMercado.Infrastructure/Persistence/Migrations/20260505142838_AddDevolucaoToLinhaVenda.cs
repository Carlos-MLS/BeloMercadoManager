using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeloMercado.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDevolucaoToLinhaVenda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "QuantidadeDevolvida",
                table: "LinhasVenda",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "CK_LinhasVenda_QtdDevolvida_Limite",
                table: "LinhasVenda",
                sql: "[QuantidadeDevolvida] <= [Quantidade]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LinhasVenda_QtdDevolvida_NaoNeg",
                table: "LinhasVenda",
                sql: "[QuantidadeDevolvida] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_LinhasVenda_QtdDevolvida_Limite",
                table: "LinhasVenda");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LinhasVenda_QtdDevolvida_NaoNeg",
                table: "LinhasVenda");

            migrationBuilder.DropColumn(
                name: "QuantidadeDevolvida",
                table: "LinhasVenda");
        }
    }
}
