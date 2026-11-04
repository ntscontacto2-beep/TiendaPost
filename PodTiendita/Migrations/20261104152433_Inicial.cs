using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PodTiendita.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Productos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    Categoria = table.Column<int>(type: "INTEGER", nullable: false),
                    Precio = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    Stock = table.Column<int>(type: "INTEGER", nullable: false),
                    StockMinimo = table.Column<int>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Productos", x => x.Id);
                    table.CheckConstraint("CK_Producto_Precio", "Precio >= 0");
                    table.CheckConstraint("CK_Producto_Stock", "Stock >= 0");
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NombreUsuario = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    Rol = table.Column<int>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Movimientos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FechaHora = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductoId = table.Column<int>(type: "INTEGER", nullable: true),
                    ProductoNombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    StockAnterior = table.Column<int>(type: "INTEGER", nullable: true),
                    StockNuevo = table.Column<int>(type: "INTEGER", nullable: true),
                    Detalle = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Movimientos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Movimientos_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Ventas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    MetodoPago = table.Column<int>(type: "INTEGER", nullable: false),
                    Total = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ventas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ventas_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DetallesVenta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VentaId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Cantidad = table.Column<int>(type: "INTEGER", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesVenta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetallesVenta_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DetallesVenta_Ventas_VentaId",
                        column: x => x.VentaId,
                        principalTable: "Ventas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DetallesVenta_ProductoId",
                table: "DetallesVenta",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesVenta_VentaId",
                table: "DetallesVenta",
                column: "VentaId");

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_FechaHora",
                table: "Movimientos",
                column: "FechaHora");

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_UsuarioId",
                table: "Movimientos",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_NombreUsuario",
                table: "Usuarios",
                column: "NombreUsuario",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_UsuarioId",
                table: "Ventas",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetallesVenta");

            migrationBuilder.DropTable(
                name: "Movimientos");

            migrationBuilder.DropTable(
                name: "Productos");

            migrationBuilder.DropTable(
                name: "Ventas");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
