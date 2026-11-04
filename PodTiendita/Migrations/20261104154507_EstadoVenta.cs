using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PodTiendita.Migrations
{
    /// <inheritdoc />
    public partial class EstadoVenta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Estado",
                table: "Ventas",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_Fecha",
                table: "Ventas",
                column: "Fecha");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Ventas_Fecha",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "Ventas");
        }
    }
}
