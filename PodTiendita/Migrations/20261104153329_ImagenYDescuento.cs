using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PodTiendita.Migrations
{
    /// <inheritdoc />
    public partial class ImagenYDescuento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DescuentoPorcentaje",
                table: "Ventas",
                type: "TEXT",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ImagenRuta",
                table: "Productos",
                type: "TEXT",
                maxLength: 260,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DescuentoPorcentaje",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "ImagenRuta",
                table: "Productos");
        }
    }
}
