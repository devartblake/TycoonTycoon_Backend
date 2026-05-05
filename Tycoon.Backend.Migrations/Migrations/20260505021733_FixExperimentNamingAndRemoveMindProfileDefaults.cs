using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tycoon.Backend.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class FixExperimentNamingAndRemoveMindProfileDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_experiment_variants_experiments_experiment_id",
                table: "experiment_variants");

            migrationBuilder.DropPrimaryKey(
                name: "PK_experiments",
                table: "experiments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_experiment_variants",
                table: "experiment_variants");

            migrationBuilder.DropPrimaryKey(
                name: "PK_experiment_assignments",
                table: "experiment_assignments");

            migrationBuilder.RenameIndex(
                name: "IX_experiments_status",
                table: "experiments",
                newName: "ix_experiments_status");

            migrationBuilder.RenameIndex(
                name: "IX_experiments_key",
                table: "experiments",
                newName: "ix_experiments_key");

            migrationBuilder.RenameIndex(
                name: "IX_experiment_variants_experiment_id_key",
                table: "experiment_variants",
                newName: "ix_experiment_variants_experiment_id_key");

            migrationBuilder.RenameIndex(
                name: "IX_experiment_assignments_player_id_experiment_id",
                table: "experiment_assignments",
                newName: "ix_experiment_assignments_player_id_experiment_id");

            migrationBuilder.RenameIndex(
                name: "IX_experiment_assignments_experiment_id_variant_key",
                table: "experiment_assignments",
                newName: "ix_experiment_assignments_experiment_id_variant_key");

            migrationBuilder.AlterColumn<decimal>(
                name: "store_affinity_score",
                table: "player_mind_profiles",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldDefaultValue: 0.50m);

            migrationBuilder.AlterColumn<string>(
                name: "social_preference",
                table: "player_mind_profiles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldDefaultValue: "solo");

            migrationBuilder.AlterColumn<bool>(
                name: "sidecar_scoring_enabled",
                table: "player_mind_profiles",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "sidecar_scores_json",
                table: "player_mind_profiles",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldDefaultValue: "{}");

            migrationBuilder.AlterColumn<decimal>(
                name: "risk_tolerance",
                table: "player_mind_profiles",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldDefaultValue: 0.50m);

            migrationBuilder.AlterColumn<decimal>(
                name: "reward_sensitivity_score",
                table: "player_mind_profiles",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldDefaultValue: 0.50m);

            migrationBuilder.AlterColumn<string>(
                name: "preferred_pace",
                table: "player_mind_profiles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldDefaultValue: "balanced");

            migrationBuilder.AlterColumn<string>(
                name: "preference_json",
                table: "player_mind_profiles",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldDefaultValue: "{}");

            migrationBuilder.AlterColumn<bool>(
                name: "personalization_enabled",
                table: "player_mind_profiles",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "notification_fatigue_score",
                table: "player_mind_profiles",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldDefaultValue: 0.00m);

            migrationBuilder.AlterColumn<string>(
                name: "learning_style",
                table: "player_mind_profiles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldDefaultValue: "mixed");

            migrationBuilder.AlterColumn<string>(
                name: "guardrail_json",
                table: "player_mind_profiles",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldDefaultValue: "{}");

            migrationBuilder.AlterColumn<decimal>(
                name: "frustration_risk_score",
                table: "player_mind_profiles",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldDefaultValue: 0.00m);

            migrationBuilder.AlterColumn<decimal>(
                name: "confidence_level",
                table: "player_mind_profiles",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldDefaultValue: 0.50m);

            migrationBuilder.AlterColumn<string>(
                name: "competitive_preference",
                table: "player_mind_profiles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldDefaultValue: "balanced");

            migrationBuilder.AlterColumn<decimal>(
                name: "churn_risk_score",
                table: "player_mind_profiles",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldDefaultValue: 0.00m);

            migrationBuilder.AlterColumn<string>(
                name: "category_weaknesses_json",
                table: "player_mind_profiles",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldDefaultValue: "{}");

            migrationBuilder.AlterColumn<string>(
                name: "category_strengths_json",
                table: "player_mind_profiles",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldDefaultValue: "{}");

            migrationBuilder.AlterColumn<string>(
                name: "archetype",
                table: "player_mind_profiles",
                type: "character varying(96)",
                maxLength: 96,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(96)",
                oldMaxLength: 96,
                oldDefaultValue: "new_player");

            migrationBuilder.AlterColumn<string>(
                name: "metadata_json",
                table: "player_behavior_events",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldDefaultValue: "{}");

            migrationBuilder.AlterColumn<string>(
                name: "rule_json",
                table: "personalization_rules",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldDefaultValue: "{}");

            migrationBuilder.AlterColumn<bool>(
                name: "is_enabled",
                table: "personalization_rules",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "personalization_rules",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldDefaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "source",
                table: "personalization_recommendations",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldDefaultValue: "backend");

            migrationBuilder.AlterColumn<decimal>(
                name: "score",
                table: "personalization_recommendations",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldDefaultValue: 0.50m);

            migrationBuilder.AlterColumn<int>(
                name: "priority",
                table: "personalization_recommendations",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "payload_json",
                table: "personalization_recommendations",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldDefaultValue: "{}");

            migrationBuilder.AlterColumn<string>(
                name: "guardrail_json",
                table: "personalization_recommendations",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldDefaultValue: "{}");

            migrationBuilder.AddPrimaryKey(
                name: "pk_experiments",
                table: "experiments",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_experiment_variants",
                table: "experiment_variants",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_experiment_assignments",
                table: "experiment_assignments",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_experiment_variants_experiments_experiment_id",
                table: "experiment_variants",
                column: "experiment_id",
                principalTable: "experiments",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_experiment_variants_experiments_experiment_id",
                table: "experiment_variants");

            migrationBuilder.DropPrimaryKey(
                name: "pk_experiments",
                table: "experiments");

            migrationBuilder.DropPrimaryKey(
                name: "pk_experiment_variants",
                table: "experiment_variants");

            migrationBuilder.DropPrimaryKey(
                name: "pk_experiment_assignments",
                table: "experiment_assignments");

            migrationBuilder.RenameIndex(
                name: "ix_experiments_status",
                table: "experiments",
                newName: "IX_experiments_status");

            migrationBuilder.RenameIndex(
                name: "ix_experiments_key",
                table: "experiments",
                newName: "IX_experiments_key");

            migrationBuilder.RenameIndex(
                name: "ix_experiment_variants_experiment_id_key",
                table: "experiment_variants",
                newName: "IX_experiment_variants_experiment_id_key");

            migrationBuilder.RenameIndex(
                name: "ix_experiment_assignments_player_id_experiment_id",
                table: "experiment_assignments",
                newName: "IX_experiment_assignments_player_id_experiment_id");

            migrationBuilder.RenameIndex(
                name: "ix_experiment_assignments_experiment_id_variant_key",
                table: "experiment_assignments",
                newName: "IX_experiment_assignments_experiment_id_variant_key");

            migrationBuilder.AlterColumn<decimal>(
                name: "store_affinity_score",
                table: "player_mind_profiles",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0.50m,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "social_preference",
                table: "player_mind_profiles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "solo",
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<bool>(
                name: "sidecar_scoring_enabled",
                table: "player_mind_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "sidecar_scores_json",
                table: "player_mind_profiles",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}",
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<decimal>(
                name: "risk_tolerance",
                table: "player_mind_profiles",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0.50m,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "reward_sensitivity_score",
                table: "player_mind_profiles",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0.50m,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "preferred_pace",
                table: "player_mind_profiles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "balanced",
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "preference_json",
                table: "player_mind_profiles",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}",
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<bool>(
                name: "personalization_enabled",
                table: "player_mind_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<decimal>(
                name: "notification_fatigue_score",
                table: "player_mind_profiles",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0.00m,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "learning_style",
                table: "player_mind_profiles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "mixed",
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "guardrail_json",
                table: "player_mind_profiles",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}",
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<decimal>(
                name: "frustration_risk_score",
                table: "player_mind_profiles",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0.00m,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "confidence_level",
                table: "player_mind_profiles",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0.50m,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "competitive_preference",
                table: "player_mind_profiles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "balanced",
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<decimal>(
                name: "churn_risk_score",
                table: "player_mind_profiles",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0.00m,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "category_weaknesses_json",
                table: "player_mind_profiles",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}",
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "category_strengths_json",
                table: "player_mind_profiles",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}",
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "archetype",
                table: "player_mind_profiles",
                type: "character varying(96)",
                maxLength: 96,
                nullable: false,
                defaultValue: "new_player",
                oldClrType: typeof(string),
                oldType: "character varying(96)",
                oldMaxLength: 96);

            migrationBuilder.AlterColumn<string>(
                name: "metadata_json",
                table: "player_behavior_events",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}",
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "rule_json",
                table: "personalization_rules",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}",
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<bool>(
                name: "is_enabled",
                table: "personalization_rules",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "personalization_rules",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);

            migrationBuilder.AlterColumn<string>(
                name: "source",
                table: "personalization_recommendations",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "backend",
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<decimal>(
                name: "score",
                table: "personalization_recommendations",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0.50m,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<int>(
                name: "priority",
                table: "personalization_recommendations",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "payload_json",
                table: "personalization_recommendations",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}",
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "guardrail_json",
                table: "personalization_recommendations",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}",
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AddPrimaryKey(
                name: "PK_experiments",
                table: "experiments",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_experiment_variants",
                table: "experiment_variants",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_experiment_assignments",
                table: "experiment_assignments",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_experiment_variants_experiments_experiment_id",
                table: "experiment_variants",
                column: "experiment_id",
                principalTable: "experiments",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
