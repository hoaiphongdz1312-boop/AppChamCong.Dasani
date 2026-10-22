using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NguyenHoaiPhong_1681.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Shifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    LateToleranceMinutes = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shifts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    RadiusMeters = table.Column<double>(type: "float", nullable: false),
                    WifiBssid = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    KioskKey = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    QrSecret = table.Column<byte[]>(type: "varbinary(32)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkLocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ShiftAssignmentId = table.Column<int>(type: "int", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    DistanceMeters = table.Column<double>(type: "float", nullable: false),
                    IsMockLocation = table.Column<bool>(type: "bit", nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    WifiBssid = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Method = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    FaceScore = table.Column<double>(type: "float", nullable: true),
                    LivenessPassed = table.Column<bool>(type: "bit", nullable: true),
                    QrWindow = table.Column<long>(type: "bigint", nullable: true),
                    TrustScore = table.Column<int>(type: "int", nullable: false),
                    IsSuspicious = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    EditedById = table.Column<int>(type: "int", nullable: true),
                    EditReason = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ManagerId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PhoneVerified = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    AvatarUrl = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Role = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EmailVerified = table.Column<bool>(type: "bit", nullable: false),
                    GoogleSubjectId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    BoundDeviceId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    FailedOtpCount = table.Column<int>(type: "int", nullable: false),
                    LockoutUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FaceChallenges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Token = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Actions = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Used = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaceChallenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FaceChallenges_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FaceEmbeddings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Vector = table.Column<byte[]>(type: "varbinary(2048)", nullable: false),
                    SampleCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaceEmbeddings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FaceEmbeddings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    FromDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ToDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ReviewerId = table.Column<int>(type: "int", nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveRequests_Users_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LocationShares",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    Accuracy = table.Column<double>(type: "float", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsSharing = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationShares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocationShares_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OtpCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    CodeHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    SendCount = table.Column<int>(type: "int", nullable: false),
                    LastSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Used = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtpCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OtpCodes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OtpSendLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Destination = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    Error = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtpSendLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OtpSendLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeviceId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShiftAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ShiftId = table.Column<int>(type: "int", nullable: false),
                    WorkLocationId = table.Column<int>(type: "int", nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShiftAssignments_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShiftAssignments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShiftAssignments_WorkLocations_WorkLocationId",
                        column: x => x.WorkLocationId,
                        principalTable: "WorkLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_EditedById",
                table: "AttendanceRecords",
                column: "EditedById");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_ShiftAssignmentId",
                table: "AttendanceRecords",
                column: "ShiftAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_UserId_Timestamp",
                table: "AttendanceRecords",
                columns: new[] { "UserId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_Departments_ManagerId",
                table: "Departments",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_FaceChallenges_Token",
                table: "FaceChallenges",
                column: "Token");

            migrationBuilder.CreateIndex(
                name: "IX_FaceChallenges_UserId",
                table: "FaceChallenges",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FaceEmbeddings_UserId",
                table: "FaceEmbeddings",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_ReviewerId",
                table: "LeaveRequests",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_UserId",
                table: "LeaveRequests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationShares_UserId_Timestamp",
                table: "LocationShares",
                columns: new[] { "UserId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_OtpCodes_SessionId",
                table: "OtpCodes",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_OtpCodes_UserId",
                table: "OtpCodes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OtpSendLogs_UserId",
                table: "OtpSendLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftAssignments_ShiftId",
                table: "ShiftAssignments",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftAssignments_UserId_WorkDate_ShiftId",
                table: "ShiftAssignments",
                columns: new[] { "UserId", "WorkDate", "ShiftId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShiftAssignments_WorkLocationId",
                table: "ShiftAssignments",
                column: "WorkLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_DepartmentId",
                table: "Users",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_GoogleSubjectId",
                table: "Users",
                column: "GoogleSubjectId",
                unique: true,
                filter: "[GoogleSubjectId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_ShiftAssignments_ShiftAssignmentId",
                table: "AttendanceRecords",
                column: "ShiftAssignmentId",
                principalTable: "ShiftAssignments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_Users_EditedById",
                table: "AttendanceRecords",
                column: "EditedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_Users_UserId",
                table: "AttendanceRecords",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Users_ManagerId",
                table: "Departments",
                column: "ManagerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Users_ManagerId",
                table: "Departments");

            migrationBuilder.DropTable(
                name: "AttendanceRecords");

            migrationBuilder.DropTable(
                name: "FaceChallenges");

            migrationBuilder.DropTable(
                name: "FaceEmbeddings");

            migrationBuilder.DropTable(
                name: "LeaveRequests");

            migrationBuilder.DropTable(
                name: "LocationShares");

            migrationBuilder.DropTable(
                name: "OtpCodes");

            migrationBuilder.DropTable(
                name: "OtpSendLogs");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "ShiftAssignments");

            migrationBuilder.DropTable(
                name: "Shifts");

            migrationBuilder.DropTable(
                name: "WorkLocations");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Departments");
        }
    }
}
