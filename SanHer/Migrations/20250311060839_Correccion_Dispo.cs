using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SanHer.Migrations
{
    /// <inheritdoc />
    public partial class Correccion_Dispo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "telefono",
                table: "Cita",
                newName: "Telefono");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Telefono",
                table: "Cita",
                newName: "telefono");
        }
    }
}
