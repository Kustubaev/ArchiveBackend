using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchiveWeb.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "applicants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Surname = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Patronymic = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    EducationLevel = table.Column<int>(type: "integer", nullable: false),
                    StudyForm = table.Column<int>(type: "integer", nullable: false),
                    IsOriginalSubmitted = table.Column<bool>(type: "boolean", nullable: false),
                    IsBudgetFinancing = table.Column<bool>(type: "boolean", nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_applicants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "archive_configurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BoxCount = table.Column<int>(type: "integer", nullable: false),
                    BoxCapacity = table.Column<int>(type: "integer", nullable: false),
                    AdaptiveRedistributionThreshold = table.Column<int>(type: "integer", nullable: false),
                    AdaptiveWeightNew = table.Column<double>(type: "double precision", nullable: false),
                    AdaptiveWeightOld = table.Column<double>(type: "double precision", nullable: false),
                    PercentReservedFiles = table.Column<int>(type: "integer", nullable: false),
                    PercentDeletedFiles = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_archive_configurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "boxes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    ExpectedCount = table.Column<int>(type: "integer", nullable: true),
                    ActualCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_boxes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "letters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<char>(type: "character(1)", maxLength: 1, nullable: false),
                    ExpectedCount = table.Column<int>(type: "integer", nullable: true),
                    StartBox = table.Column<int>(type: "integer", nullable: true),
                    EndBox = table.Column<int>(type: "integer", nullable: true),
                    StartPosition = table.Column<int>(type: "integer", nullable: true),
                    EndPosition = table.Column<int>(type: "integer", nullable: true),
                    ActualCount = table.Column<int>(type: "integer", nullable: false),
                    UsedCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_letters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "file_archives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileNumberForArchive = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    FullName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FirstLetterSurname = table.Column<char>(type: "character(1)", maxLength: 1, nullable: false),
                    FileNumberForLetter = table.Column<int>(type: "integer", nullable: false),
                    PositionInBox = table.Column<int>(type: "integer", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApplicantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BoxId = table.Column<Guid>(type: "uuid", nullable: true),
                    LetterId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_file_archives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_file_archives_applicants_ApplicantId",
                        column: x => x.ApplicantId,
                        principalTable: "applicants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_file_archives_boxes_BoxId",
                        column: x => x.BoxId,
                        principalTable: "boxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_file_archives_letters_LetterId",
                        column: x => x.LetterId,
                        principalTable: "letters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "archive_history",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    OldBoxNumber = table.Column<int>(type: "integer", nullable: true),
                    OldPosition = table.Column<int>(type: "integer", nullable: true),
                    NewBoxNumber = table.Column<int>(type: "integer", nullable: true),
                    NewPosition = table.Column<int>(type: "integer", nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FileArchiveId = table.Column<Guid>(type: "uuid", nullable: false),
                    NewLetterId = table.Column<Guid>(type: "uuid", nullable: true),
                    OldLetterId = table.Column<Guid>(type: "uuid", nullable: true),
                    NewBoxId = table.Column<Guid>(type: "uuid", nullable: true),
                    OldBoxId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_archive_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_archive_history_boxes_NewBoxId",
                        column: x => x.NewBoxId,
                        principalTable: "boxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_archive_history_file_archives_FileArchiveId",
                        column: x => x.FileArchiveId,
                        principalTable: "file_archives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_archive_history_letters_NewLetterId",
                        column: x => x.NewLetterId,
                        principalTable: "letters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "idx_applicant_email",
                table: "applicants",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "idx_applicant_phone",
                table: "applicants",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "idx_archive_history_created_at",
                table: "archive_history",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "idx_archive_history_file_id",
                table: "archive_history",
                column: "FileArchiveId");

            migrationBuilder.CreateIndex(
                name: "IX_archive_history_NewBoxId",
                table: "archive_history",
                column: "NewBoxId");

            migrationBuilder.CreateIndex(
                name: "IX_archive_history_NewLetterId",
                table: "archive_history",
                column: "NewLetterId");

            migrationBuilder.CreateIndex(
                name: "uk_box_number",
                table: "boxes",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_file_archives_box_id",
                table: "file_archives",
                column: "BoxId");

            migrationBuilder.CreateIndex(
                name: "idx_file_archives_fullName",
                table: "file_archives",
                column: "FullName");

            migrationBuilder.CreateIndex(
                name: "idx_file_archives_is_deleted",
                table: "file_archives",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "idx_file_archives_letter_id",
                table: "file_archives",
                column: "LetterId");

            migrationBuilder.CreateIndex(
                name: "idx_file_archives_letterValue",
                table: "file_archives",
                column: "FirstLetterSurname");

            migrationBuilder.CreateIndex(
                name: "uk_applicant",
                table: "file_archives",
                column: "ApplicantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uk_letter_value",
                table: "letters",
                column: "Value",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "archive_configurations");

            migrationBuilder.DropTable(
                name: "archive_history");

            migrationBuilder.DropTable(
                name: "file_archives");

            migrationBuilder.DropTable(
                name: "applicants");

            migrationBuilder.DropTable(
                name: "boxes");

            migrationBuilder.DropTable(
                name: "letters");
        }
    }
}
