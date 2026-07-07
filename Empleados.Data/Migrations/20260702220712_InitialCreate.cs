using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Empleados.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Departamentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departamentos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Empleados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Apellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaNacimiento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Direccion = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CorreoElectronico = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Puesto = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Salario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FechaContratacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaTerminacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FotoRuta = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DepartamentoId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empleados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Empleados_Departamentos_DepartamentoId",
                        column: x => x.DepartamentoId,
                        principalTable: "Departamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Departamentos",
                columns: new[] { "Id", "Descripcion", "Nombre" },
                values: new object[,]
                {
                    { 1, "Departamento de sistemas y desarrollo", "Tecnología" },
                    { 2, "Gestión de talento y personal", "Recursos Humanos" },
                    { 3, "Contabilidad y finanzas corporativas", "Finanzas" }
                });

            migrationBuilder.InsertData(
                table: "Empleados",
                columns: new[] { "Id", "Apellido", "CorreoElectronico", "DepartamentoId", "Direccion", "Estado", "FechaContratacion", "FechaNacimiento", "FechaTerminacion", "FotoRuta", "Nombre", "Puesto", "Salario", "Telefono" },
                values: new object[,]
                {
                    { 1, "García", "ana.garcia@empresa.com", 1, "Calle Principal 123, Madrid", 0, new DateTime(2018, 3, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1990, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Ana", "Desarrolladora Senior", 55000m, "600111222" },
                    { 2, "López", "carlos.lopez@empresa.com", 2, "Avenida Central 45, Barcelona", 0, new DateTime(2019, 6, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1985, 8, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Carlos", "Analista de RRHH", 42000m, "600333444" },
                    { 3, "Martínez", "maria.martinez@empresa.com", 3, "Calle Secundaria 78, Valencia", 0, new DateTime(2020, 2, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1992, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "María", "Contadora", 48000m, "600555666" },
                    { 4, "Gómez", "juan.gomez@empresa.com", 1, "Calle Luna 12, Sevilla", 0, new DateTime(2021, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1988, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Juan", "Desarrollador", 38000m, "600777888" },
                    { 5, "Rodríguez", "laura.rodriguez@empresa.com", 1, "Avenida del Sol 34, Bilbao", 0, new DateTime(2022, 1, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1995, 3, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Laura", "Analista de Datos", 40000m, "600999000" },
                    { 6, "Fernández", "javier.fernandez@empresa.com", 1, "Calle Pino 56, Málaga", 0, new DateTime(2017, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1987, 9, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Javier", "Scrum Master", 52000m, "600444555" },
                    { 7, "Díaz", "sofia.diaz@empresa.com", 1, "Avenida del Mar 78, Alicante", 0, new DateTime(2020, 5, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1993, 12, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Sofía", "Analista de Datos", 41000m, "600666777" },
                    { 8, "Torres", "pablo.torres@empresa.com", 2, "Calle Real 90, Zaragoza", 0, new DateTime(2015, 8, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1986, 2, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Pablo", "Jefe de RRHH", 58000m, "600888999" },
                    { 9, "Ramírez", "elena.ramirez@empresa.com", 3, "Calle Olmo 23, Murcia", 0, new DateTime(2021, 3, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1991, 6, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Elena", "Contable", 43000m, "600111333" },
                    { 10, "Moreno", "david.moreno@empresa.com", 1, "Avenida Libertad 67, Pamplona", 0, new DateTime(2023, 1, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1989, 10, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "David", "Desarrollador Junior", 35000m, "600555777" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_CorreoElectronico",
                table: "Empleados",
                column: "CorreoElectronico",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_DepartamentoId",
                table: "Empleados",
                column: "DepartamentoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Empleados");

            migrationBuilder.DropTable(
                name: "Departamentos");
        }
    }
}
