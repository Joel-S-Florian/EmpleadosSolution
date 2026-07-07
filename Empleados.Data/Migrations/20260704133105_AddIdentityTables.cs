using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Empleados.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DeleteData(
                table: "Empleados",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Empleados",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Empleados",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Empleados",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Empleados",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Empleados",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Empleados",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Empleados",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Empleados",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Empleados",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Departamentos",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Departamentos",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Departamentos",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Apellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ContrasenaHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Correo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Rol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
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
                table: "Usuarios",
                columns: new[] { "Id", "Apellido", "ContrasenaHash", "Correo", "Nombre", "Rol" },
                values: new object[,]
                {
                    { 1, "Sistema", "$2a$11$Nr576Sz4aJ2ITbPq5yCvX.8Tr7gLMVLagPZAeLp.DJMlA.631XDPS", "admin@empresa.com", "Admin", "Admin" },
                    { 2, "Pérez", "$2a$11$CpJN6rHAo/2iKhJu3Eo9vOGJ2VlAYebSPRRqgradN26FvO/YFxYw6", "rrhh@empresa.com", "Juan", "RRHH" }
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
                name: "IX_Usuarios_Correo",
                table: "Usuarios",
                column: "Correo",
                unique: true);
        }
    }
}
