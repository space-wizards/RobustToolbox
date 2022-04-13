using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Robust.Benchmarks.Exporters;

#nullable disable

namespace Robust.Benchmarks.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BenchmarkRuns",
                columns: table => new
                {
                    Id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    GitHash = table.Column<string>(type: "text", nullable: false),
                    RunDate = table.Column<DateTime>(type: "Date", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Reports = table.Column<BenchmarkRunReport[]>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenchmarkRuns", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BenchmarkRuns");
        }
    }
}
