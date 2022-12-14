using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoList.Migrations.Migrations
{
    public partial class V1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Role",
                columns: table => new
                {
                    Name = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    IsEmailVerified = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHashBase64 = table.Column<string>(type: "varchar(max)", unicode: false, nullable: false),
                    SaltBase64 = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    SignupDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "GETDATE()"),
                    RoleName = table.Column<string>(type: "varchar(20)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                    table.ForeignKey(
                        name: "FK_User_Role_RoleName",
                        column: x => x.RoleName,
                        principalTable: "Role",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TodoItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    IsComplete = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TodoItem_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Role",
                columns: new[] { "Name", "Priority" },
                values: new object[] { "Admin", 0 });

            migrationBuilder.InsertData(
                table: "Role",
                columns: new[] { "Name", "Priority" },
                values: new object[] { "User", 1 });

            migrationBuilder.CreateIndex(
                name: "IX_TodoItem_UserId",
                table: "TodoItem",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_User_Email",
                table: "User",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_RoleName",
                table: "User",
                column: "RoleName");

            //IDistributedCache for sql server
            string cacheSql = MigrationUtility.ReadSql(typeof(V1), "Create dbo.RefreshTokenCache.sql");
            migrationBuilder.Sql(cacheSql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TodoItem");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "Role");
        }
    }
}
