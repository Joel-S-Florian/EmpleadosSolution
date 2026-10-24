using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Empleados.Data.Migrations
{
    /// <inheritdoc />
    public partial class EliminarPuestoTextoEmpleado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Antes de borrar el texto: los empleados que solo tienen el texto pasan a tener PuestoId.
            // Si el puesto no existe en su departamento, se crea.
            migrationBuilder.Sql("""
                INSERT INTO Puestos (Nombre, DepartamentoId)
                SELECT DISTINCT LTRIM(RTRIM(e.Puesto)), e.DepartamentoId
                FROM Empleados e
                WHERE e.PuestoId IS NULL AND LTRIM(RTRIM(e.Puesto)) <> ''
                  AND NOT EXISTS (SELECT 1 FROM Puestos p
                                  WHERE p.DepartamentoId = e.DepartamentoId AND p.Nombre = LTRIM(RTRIM(e.Puesto)));

                UPDATE e SET e.PuestoId = p.Id
                FROM Empleados e
                JOIN Puestos p ON p.DepartamentoId = e.DepartamentoId AND p.Nombre = LTRIM(RTRIM(e.Puesto))
                WHERE e.PuestoId IS NULL;
                """);

            migrationBuilder.DropColumn(
                name: "Puesto",
                table: "Empleados");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Puesto",
                table: "Empleados",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE e SET e.Puesto = p.Nombre
                FROM Empleados e
                JOIN Puestos p ON p.Id = e.PuestoId;
                """);
        }
    }
}
