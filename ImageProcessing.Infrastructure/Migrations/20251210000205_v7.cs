using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImageProcessing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CameraId",
                table: "timelapse",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "timelapse",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "FromUtc",
                table: "timelapse",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "timelapse",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ToUtc",
                table: "timelapse",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CameraId",
                table: "timelapse");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "timelapse");

            migrationBuilder.DropColumn(
                name: "FromUtc",
                table: "timelapse");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "timelapse");

            migrationBuilder.DropColumn(
                name: "ToUtc",
                table: "timelapse");
        }
    }
}
