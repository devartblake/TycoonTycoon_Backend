using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synaptix.Backend.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionTaxonomy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "canonical_category",
                table: "questions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "display_category",
                table: "questions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "subject",
                table: "questions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "topic",
                table: "questions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "subtopic",
                table: "questions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "grade_band",
                table: "questions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "age_group",
                table: "questions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "audience",
                table: "questions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_dataset",
                table: "questions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_question_id",
                table: "questions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "question_type",
                table: "questions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "multiple_choice");

            migrationBuilder.AddColumn<string>(
                name: "media_type",
                table: "questions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "text");

            migrationBuilder.AddColumn<string>(
                name: "taxonomy_tags_json",
                table: "questions",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "canonical_category",
                table: "learning_modules",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "subject",
                table: "learning_modules",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "topic",
                table: "learning_modules",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "grade_band",
                table: "learning_modules",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "age_group",
                table: "learning_modules",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "audience",
                table: "learning_modules",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE questions
                SET canonical_category = lower(regexp_replace(regexp_replace(category, '[^a-zA-Z0-9]+', '_', 'g'), '^_|_$', '', 'g')),
                    display_category = category,
                    question_type = 'multiple_choice',
                    media_type = CASE WHEN media_key IS NULL OR btrim(media_key) = '' THEN 'text' ELSE 'image' END,
                    taxonomy_tags_json = '[]'
                WHERE canonical_category = '';

                UPDATE learning_modules
                SET canonical_category = lower(regexp_replace(regexp_replace(category, '[^a-zA-Z0-9]+', '_', 'g'), '^_|_$', '', 'g'))
                WHERE canonical_category = '';
                """);

            migrationBuilder.CreateIndex(
                name: "ix_questions_canonical_category",
                table: "questions",
                column: "canonical_category");

            migrationBuilder.CreateIndex(
                name: "ix_questions_subject",
                table: "questions",
                column: "subject");

            migrationBuilder.CreateIndex(
                name: "ix_questions_grade_band",
                table: "questions",
                column: "grade_band");

            migrationBuilder.CreateIndex(
                name: "ix_questions_age_group",
                table: "questions",
                column: "age_group");

            migrationBuilder.CreateIndex(
                name: "ix_questions_source_dataset",
                table: "questions",
                column: "source_dataset");

            migrationBuilder.CreateIndex(
                name: "ix_questions_source_dataset_source_question_id",
                table: "questions",
                columns: new[] { "source_dataset", "source_question_id" },
                unique: true,
                filter: "source_dataset IS NOT NULL AND source_question_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_questions_status_canonical_category_difficulty",
                table: "questions",
                columns: new[] { "status", "canonical_category", "difficulty" });

            migrationBuilder.CreateIndex(
                name: "ix_learning_modules_canonical_category",
                table: "learning_modules",
                column: "canonical_category");

            migrationBuilder.CreateIndex(
                name: "ix_learning_modules_subject",
                table: "learning_modules",
                column: "subject");

            migrationBuilder.CreateIndex(
                name: "ix_learning_modules_grade_band",
                table: "learning_modules",
                column: "grade_band");

            migrationBuilder.CreateIndex(
                name: "ix_learning_modules_age_group",
                table: "learning_modules",
                column: "age_group");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "ix_questions_canonical_category", table: "questions");
            migrationBuilder.DropIndex(name: "ix_questions_subject", table: "questions");
            migrationBuilder.DropIndex(name: "ix_questions_grade_band", table: "questions");
            migrationBuilder.DropIndex(name: "ix_questions_age_group", table: "questions");
            migrationBuilder.DropIndex(name: "ix_questions_source_dataset", table: "questions");
            migrationBuilder.DropIndex(name: "ix_questions_source_dataset_source_question_id", table: "questions");
            migrationBuilder.DropIndex(name: "ix_questions_status_canonical_category_difficulty", table: "questions");
            migrationBuilder.DropIndex(name: "ix_learning_modules_canonical_category", table: "learning_modules");
            migrationBuilder.DropIndex(name: "ix_learning_modules_subject", table: "learning_modules");
            migrationBuilder.DropIndex(name: "ix_learning_modules_grade_band", table: "learning_modules");
            migrationBuilder.DropIndex(name: "ix_learning_modules_age_group", table: "learning_modules");

            migrationBuilder.DropColumn(name: "canonical_category", table: "questions");
            migrationBuilder.DropColumn(name: "display_category", table: "questions");
            migrationBuilder.DropColumn(name: "subject", table: "questions");
            migrationBuilder.DropColumn(name: "topic", table: "questions");
            migrationBuilder.DropColumn(name: "subtopic", table: "questions");
            migrationBuilder.DropColumn(name: "grade_band", table: "questions");
            migrationBuilder.DropColumn(name: "age_group", table: "questions");
            migrationBuilder.DropColumn(name: "audience", table: "questions");
            migrationBuilder.DropColumn(name: "source_dataset", table: "questions");
            migrationBuilder.DropColumn(name: "source_question_id", table: "questions");
            migrationBuilder.DropColumn(name: "question_type", table: "questions");
            migrationBuilder.DropColumn(name: "media_type", table: "questions");
            migrationBuilder.DropColumn(name: "taxonomy_tags_json", table: "questions");
            migrationBuilder.DropColumn(name: "canonical_category", table: "learning_modules");
            migrationBuilder.DropColumn(name: "subject", table: "learning_modules");
            migrationBuilder.DropColumn(name: "topic", table: "learning_modules");
            migrationBuilder.DropColumn(name: "grade_band", table: "learning_modules");
            migrationBuilder.DropColumn(name: "age_group", table: "learning_modules");
            migrationBuilder.DropColumn(name: "audience", table: "learning_modules");
        }
    }
}
