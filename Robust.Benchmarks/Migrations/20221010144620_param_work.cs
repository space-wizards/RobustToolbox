using Microsoft.EntityFrameworkCore.Migrations;
using Robust.Benchmarks.Exporters;

#nullable disable

namespace Robust.Benchmarks.Migrations
{
    public partial class ParamWork : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ParameterMapping",
                table: "BenchmarkRuns",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<BenchmarkRunParameter[]>(
                name: "ParameterMappingJson",
                table: "BenchmarkRuns",
                type: "jsonb",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ParameterMappingJson",
                table: "BenchmarkRuns");

            migrationBuilder.AlterColumn<string>(
                name: "ParameterMapping",
                table: "BenchmarkRuns",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
