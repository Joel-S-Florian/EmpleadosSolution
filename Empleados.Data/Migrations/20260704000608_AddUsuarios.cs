using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Empleados.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUsuarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Apellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Correo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContrasenaHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Rol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Usuarios",
                columns: new[] { "Id", "Apellido", "ContrasenaHash", "Correo", "Nombre", "Rol" },
                values: new object[,]
                {
                    { 1, "Sistema", "$2a$11$Nr576Sz4aJ2ITbPq5yCvX.8Tr7gLMVLagPZAeLp.DJMlA.631XDPS", "admin@empresa.com", "Admin", "Admin" },
                    { 2, "Pérez", "$2a$11$CpJN6rHAo/2iKhJu3Eo9vOGJ2VlAYebSPRRqgradN26FvO/YFxYw6", "rrhh@empresa.com", "Juan", "RRHH" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Correo",
                table: "Usuarios",
                column: "Correo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
