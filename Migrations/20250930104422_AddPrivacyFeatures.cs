using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoService.Migrations
{
    /// <inheritdoc />
    public partial class AddPrivacyFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "BlurIntensity",
                table: "photos",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "BlurredFileName",
                table: "photos",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModeratedAt",
                table: "photos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<JsonDocument>(
                name: "ModerationResults",
                table: "photos",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrivacyLevel",
                table: "photos",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "RequiresMatch",
                table: "photos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "SafetyScore",
                table: "photos",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlurIntensity",
                table: "photos");

            migrationBuilder.DropColumn(
                name: "BlurredFileName",
                table: "photos");

            migrationBuilder.DropColumn(
                name: "ModeratedAt",
                table: "photos");

            migrationBuilder.DropColumn(
                name: "ModerationResults",
                table: "photos");

            migrationBuilder.DropColumn(
                name: "PrivacyLevel",
                table: "photos");

            migrationBuilder.DropColumn(
                name: "RequiresMatch",
                table: "photos");

            migrationBuilder.DropColumn(
                name: "SafetyScore",
                table: "photos");
        }
    }
}
