using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolMvc.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultFieldsToUserEncryptionMethods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultPassword",
                table: "UserEncryptionMethods",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultTargetUsername",
                table: "UserEncryptionMethods",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultPassword",
                table: "UserEncryptionMethods");

            migrationBuilder.DropColumn(
                name: "DefaultTargetUsername",
                table: "UserEncryptionMethods");
        }
    }
}
