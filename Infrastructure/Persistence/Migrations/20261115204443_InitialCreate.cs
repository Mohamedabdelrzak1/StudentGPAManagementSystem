using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CreditHours = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.Id);
                    table.CheckConstraint("CK_Courses_CreditHours", "[CreditHours] BETWEEN 1 AND 3");
                });

            migrationBuilder.CreateTable(
                name: "GradingScales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    FromGrade = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ToGrade = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Points = table.Column<decimal>(type: "decimal(3,1)", nullable: false),
                    Grade = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradingScales", x => x.Id);
                    table.CheckConstraint("CK_GradingScales_Points", "[Points] BETWEEN 0 AND 4");
                    table.CheckConstraint("CK_GradingScales_Range", "[FromGrade] >= 0 AND [ToGrade] > [FromGrade] AND [ToGrade] <= 101");
                });

            migrationBuilder.CreateTable(
                name: "Semesters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Semesters", x => x.Id);
                    table.CheckConstraint("CK_Semesters_Dates", "[EndDate] > [StartDate]");
                });

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Enrollments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    SemesterId = table.Column<int>(type: "int", nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    CreditHours = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    GradingScaleId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrollments", x => x.Id);
                    table.CheckConstraint("CK_Enrollments_CreditHours", "[CreditHours] BETWEEN 1 AND 3");
                    table.CheckConstraint("CK_Enrollments_ScoreOrStatus", "([Status] = 1 AND [Score] IS NOT NULL AND [GradingScaleId] IS NOT NULL) OR ([Status] <> 1 AND [Score] IS NULL AND [GradingScaleId] IS NULL)");
                    table.CheckConstraint("CK_Enrollments_ScoreRange", "[Score] IS NULL OR ([Score] >= 0 AND [Score] <= 100)");
                    table.CheckConstraint("CK_Enrollments_Status", "[Status] IN (1,2,3,4)");
                    table.ForeignKey(
                        name: "FK_Enrollments_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enrollments_GradingScales_GradingScaleId",
                        column: x => x.GradingScaleId,
                        principalTable: "GradingScales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enrollments_Semesters_SemesterId",
                        column: x => x.SemesterId,
                        principalTable: "Semesters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enrollments_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "GradingScales",
                columns: new[] { "Id", "FromGrade", "Grade", "Points", "ToGrade" },
                values: new object[,]
                {
                    { 1, 96m, "A+", 4.0m, 101m },
                    { 2, 92m, "A", 3.7m, 96m },
                    { 3, 88m, "A-", 3.4m, 92m },
                    { 4, 84m, "B+", 3.2m, 88m },
                    { 5, 80m, "B", 3.0m, 84m },
                    { 6, 76m, "B-", 2.8m, 80m },
                    { 7, 72m, "C+", 2.6m, 76m },
                    { 8, 68m, "C", 2.4m, 72m },
                    { 9, 64m, "C-", 2.2m, 68m },
                    { 10, 60m, "D+", 2.0m, 64m },
                    { 11, 55m, "D", 1.5m, 60m },
                    { 12, 50m, "D-", 1.0m, 55m },
                    { 13, 0m, "F", 0.0m, 50m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Courses_Code",
                table: "Courses",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_CourseId",
                table: "Enrollments",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_GradingScaleId",
                table: "Enrollments",
                column: "GradingScaleId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_SemesterId",
                table: "Enrollments",
                column: "SemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_Student_Semester_Course",
                table: "Enrollments",
                columns: new[] { "StudentId", "SemesterId", "CourseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_StudentId_SemesterId",
                table: "Enrollments",
                columns: new[] { "StudentId", "SemesterId" });

            migrationBuilder.CreateIndex(
                name: "IX_GradingScales_FromGrade",
                table: "GradingScales",
                column: "FromGrade",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GradingScales_Grade",
                table: "GradingScales",
                column: "Grade",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Semesters_Name",
                table: "Semesters",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_Code",
                table: "Students",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Enrollments");

            migrationBuilder.DropTable(
                name: "Courses");

            migrationBuilder.DropTable(
                name: "GradingScales");

            migrationBuilder.DropTable(
                name: "Semesters");

            migrationBuilder.DropTable(
                name: "Students");
        }
    }
}
