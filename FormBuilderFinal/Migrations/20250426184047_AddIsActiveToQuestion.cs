using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormBuilderFinal.Migrations
{
    /// <inheritdoc />
    public partial class AddIsActiveToQuestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Questions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Questions");
        }
    }
}
