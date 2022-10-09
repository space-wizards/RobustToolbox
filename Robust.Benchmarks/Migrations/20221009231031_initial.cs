using System;
using BenchmarkDotNet.Mathematics;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Robust.Benchmarks.Migrations
{
    public partial class initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BenchmarkRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GitHash = table.Column<string>(type: "text", nullable: false),
                    RunDate = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ParameterMapping = table.Column<string>(type: "text", nullable: false),
                    Statistics = table.Column<Statistics>(type: "jsonb", nullable: false)
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
