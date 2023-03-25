using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Robust.Benchmarks.Migrations
{
    public partial class DB : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Reports",
                table: "BenchmarkRuns",
                newName: "Statistics");

            migrationBuilder.AddColumn<string>(
                name: "ParameterMapping",
                table: "BenchmarkRuns",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ParameterMapping",
                table: "BenchmarkRuns");

            migrationBuilder.RenameColumn(
                name: "Statistics",
                table: "BenchmarkRuns",
                newName: "Reports");
        }
    }
}
