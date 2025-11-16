using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImageProcessing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Compute",
                table: "edge_event");

            migrationBuilder.DropColumn(
                name: "Image",
                table: "edge_event");

            migrationBuilder.RenameColumn(
                name: "TimestampUtc",
                table: "edge_event",
                newName: "CaptureTimestampUtc");

            migrationBuilder.RenameColumn(
                name: "People",
                table: "edge_event",
                newName: "ComputeModel");

            migrationBuilder.AddColumn<double>(
                name: "ComputeInferenceMs",
                table: "edge_event",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImageHeight",
                table: "edge_event",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImageWidth",
                table: "edge_event",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ComputeInferenceMs",
                table: "edge_event");

            migrationBuilder.DropColumn(
                name: "ImageHeight",
                table: "edge_event");

            migrationBuilder.DropColumn(
                name: "ImageWidth",
                table: "edge_event");

            migrationBuilder.RenameColumn(
                name: "ComputeModel",
                table: "edge_event",
                newName: "People");

            migrationBuilder.RenameColumn(
                name: "CaptureTimestampUtc",
                table: "edge_event",
                newName: "TimestampUtc");

            migrationBuilder.AddColumn<string>(
                name: "Compute",
                table: "edge_event",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Image",
                table: "edge_event",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
