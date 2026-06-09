using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolMvc.Migrations
{
    /// <inheritdoc />
    public partial class AddTargetUsernameToEncryptionHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "IF COL_LENGTH('EncryptionHistories', 'TargetUsername') IS NULL " +
                "BEGIN ALTER TABLE EncryptionHistories ADD TargetUsername nvarchar(max) NOT NULL CONSTRAINT DF_EncryptionHistories_TargetUsername DEFAULT('everyone') END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "IF COL_LENGTH('EncryptionHistories', 'TargetUsername') IS NOT NULL " +
                "BEGIN ALTER TABLE EncryptionHistories DROP CONSTRAINT IF EXISTS DF_EncryptionHistories_TargetUsername; ALTER TABLE EncryptionHistories DROP COLUMN TargetUsername END");
        }
    }
}
