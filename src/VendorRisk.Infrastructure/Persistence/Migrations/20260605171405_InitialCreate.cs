using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace VendorRisk.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "risk_categories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_risk_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vendors",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    financial_health = table.Column<int>(type: "integer", nullable: false),
                    sla_uptime = table.Column<int>(type: "integer", nullable: false),
                    major_incidents = table.Column<int>(type: "integer", nullable: false),
                    security_certs = table.Column<List<string>>(type: "text[]", nullable: false),
                    contract_valid = table.Column<bool>(type: "boolean", nullable: true),
                    privacy_policy_valid = table.Column<bool>(type: "boolean", nullable: true),
                    pentest_report_valid = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vendors", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "risk_factors",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    category_id = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_risk_factors", x => x.id);
                    table.ForeignKey(
                        name: "fk_risk_factors_risk_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "risk_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "risk_factor_edges",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    parent_factor_id = table.Column<int>(type: "integer", nullable: false),
                    child_factor_id = table.Column<int>(type: "integer", nullable: false),
                    weight = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_risk_factor_edges", x => x.id);
                    table.ForeignKey(
                        name: "fk_risk_factor_edges_risk_factors_child_factor_id",
                        column: x => x.child_factor_id,
                        principalTable: "risk_factors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_risk_factor_edges_risk_factors_parent_factor_id",
                        column: x => x.parent_factor_id,
                        principalTable: "risk_factors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_risk_categories_code",
                table: "risk_categories",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_risk_factor_edges_child_factor_id",
                table: "risk_factor_edges",
                column: "child_factor_id");

            migrationBuilder.CreateIndex(
                name: "ix_risk_factor_edges_parent_factor_id_child_factor_id",
                table: "risk_factor_edges",
                columns: new[] { "parent_factor_id", "child_factor_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_risk_factors_category_id",
                table: "risk_factors",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_risk_factors_code",
                table: "risk_factors",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "risk_factor_edges");

            migrationBuilder.DropTable(
                name: "vendors");

            migrationBuilder.DropTable(
                name: "risk_factors");

            migrationBuilder.DropTable(
                name: "risk_categories");
        }
    }
}
