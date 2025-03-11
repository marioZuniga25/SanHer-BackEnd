using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SanHer.Migrations
{
    /// <inheritdoc />
    public partial class UpdateHorario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Asunto",
                table: "Cita",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Asunto",
                table: "Cita");
        }
    }
}
