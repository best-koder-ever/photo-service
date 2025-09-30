using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PhotoService.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgreSQLSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "photos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    original_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    stored_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    file_extension = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    mime_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    width = table.Column<int>(type: "integer", nullable: false),
                    height = table.Column<int>(type: "integer", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    moderation_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "AUTO_APPROVED"),
                    moderation_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    quality_score = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    metadata = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    tags = table.Column<string[]>(type: "text[]", nullable: true),
                    content_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_photos", x => x.id);
                    table.CheckConstraint("ck_photos_dimensions_positive", "width > 0 AND height > 0");
                    table.CheckConstraint("ck_photos_display_order_positive", "display_order > 0");
                    table.CheckConstraint("ck_photos_file_size_positive", "file_size_bytes > 0");
                    table.CheckConstraint("ck_photos_quality_score_range", "quality_score >= 0 AND quality_score <= 100");
                });

            migrationBuilder.CreateTable(
                name: "photo_moderation_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    photo_id = table.Column<int>(type: "integer", nullable: false),
                    previous_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    new_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    moderator_id = table.Column<int>(type: "integer", nullable: true),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_photo_moderation_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_photo_moderation_logs_photo_id",
                        column: x => x.photo_id,
                        principalTable: "photos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "photo_processing_jobs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    photo_id = table.Column<int>(type: "integer", nullable: false),
                    job_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "PENDING"),
                    parameters = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    result = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    error_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_photo_processing_jobs", x => x.id);
                    table.ForeignKey(
                        name: "fk_photo_processing_jobs_photo_id",
                        column: x => x.photo_id,
                        principalTable: "photos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_photo_moderation_logs_created_at",
                table: "photo_moderation_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_photo_moderation_logs_moderator_id",
                table: "photo_moderation_logs",
                column: "moderator_id");

            migrationBuilder.CreateIndex(
                name: "ix_photo_moderation_logs_photo_id",
                table: "photo_moderation_logs",
                column: "photo_id");

            migrationBuilder.CreateIndex(
                name: "ix_photo_processing_jobs_created_at",
                table: "photo_processing_jobs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_photo_processing_jobs_photo_id",
                table: "photo_processing_jobs",
                column: "photo_id");

            migrationBuilder.CreateIndex(
                name: "ix_photo_processing_jobs_status",
                table: "photo_processing_jobs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_photos_content_hash",
                table: "photos",
                column: "content_hash");

            migrationBuilder.CreateIndex(
                name: "ix_photos_metadata_gin",
                table: "photos",
                column: "metadata")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ix_photos_moderation_status",
                table: "photos",
                column: "moderation_status");

            migrationBuilder.CreateIndex(
                name: "ix_photos_tags_gin",
                table: "photos",
                column: "tags")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ix_photos_user_active_display_order",
                table: "photos",
                columns: new[] { "user_id", "is_deleted", "display_order" });

            migrationBuilder.CreateIndex(
                name: "ix_photos_user_id",
                table: "photos",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_photos_user_primary_active",
                table: "photos",
                columns: new[] { "user_id", "is_primary", "is_deleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "photo_moderation_logs");

            migrationBuilder.DropTable(
                name: "photo_processing_jobs");

            migrationBuilder.DropTable(
                name: "photos");
        }
    }
}
