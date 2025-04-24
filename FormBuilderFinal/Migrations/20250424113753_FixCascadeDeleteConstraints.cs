using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormBuilderFinal.Migrations
{
    /// <inheritdoc />
    public partial class FixCascadeDeleteConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TemplateAccesses_AspNetUsers_UserId",
                table: "TemplateAccesses");

            migrationBuilder.DropForeignKey(
                name: "FK_TemplateAccesses_Templates_TemplateId",
                table: "TemplateAccesses");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Questions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AddForeignKey(
                name: "FK_TemplateAccesses_AspNetUsers_UserId",
                table: "TemplateAccesses",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TemplateAccesses_Templates_TemplateId",
                table: "TemplateAccesses",
                column: "TemplateId",
                principalTable: "Templates",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TemplateAccesses_AspNetUsers_UserId",
                table: "TemplateAccesses");

            migrationBuilder.DropForeignKey(
                name: "FK_TemplateAccesses_Templates_TemplateId",
                table: "TemplateAccesses");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Questions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TemplateAccesses_AspNetUsers_UserId",
                table: "TemplateAccesses",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TemplateAccesses_Templates_TemplateId",
                table: "TemplateAccesses",
                column: "TemplateId",
                principalTable: "Templates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
