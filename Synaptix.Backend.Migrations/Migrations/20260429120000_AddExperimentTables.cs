using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synaptix.Backend.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddExperimentTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "experiments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "draft"),
                    allocation_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 100m),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experiments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "experiment_variants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    experiment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    weight = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false, defaultValue: 50m),
                    is_control = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    config_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experiment_variants", x => x.id);
                    table.ForeignKey(
                        name: "FK_experiment_variants_experiments_experiment_id",
                        column: x => x.experiment_id,
                        principalTable: "experiments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "experiment_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    experiment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    experiment_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    variant_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    first_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    impression_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    outcome_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    outcome_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experiment_assignments", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_experiments_key",
                table: "experiments",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_experiments_status",
                table: "experiments",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_experiment_variants_experiment_id_key",
                table: "experiment_variants",
                columns: new[] { "experiment_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_experiment_assignments_player_id_experiment_id",
                table: "experiment_assignments",
                columns: new[] { "player_id", "experiment_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_experiment_assignments_experiment_id_variant_key",
                table: "experiment_assignments",
                columns: new[] { "experiment_id", "variant_key" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "experiment_assignments");
            migrationBuilder.DropTable(name: "experiment_variants");
            migrationBuilder.DropTable(name: "experiments");
        }
    }
}
