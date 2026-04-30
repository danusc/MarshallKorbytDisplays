using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarshallDisplayRegistry.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Actor = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ObjectType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ObjectId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DisplayDevices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ComputerName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FriendlyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Building = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Room = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MacAddress = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    AgentVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LastSeenUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CurrentUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    DesiredUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    ChromeRunning = table.Column<bool>(type: "bit", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisplayDevices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UrlProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UrlProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeviceCredentials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayDeviceId = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUsedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceCredentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceCredentials_DisplayDevices_DisplayDeviceId",
                        column: x => x.DisplayDeviceId,
                        principalTable: "DisplayDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DisplayCheckIns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayDeviceId = table.Column<int>(type: "int", nullable: false),
                    CheckInUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ComputerName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CurrentUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    DesiredUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    ChromeRunning = table.Column<bool>(type: "bit", nullable: false),
                    AgentVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisplayCheckIns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisplayCheckIns_DisplayDevices_DisplayDeviceId",
                        column: x => x.DisplayDeviceId,
                        principalTable: "DisplayDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DisplayCommands",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayDeviceId = table.Column<int>(type: "int", nullable: false),
                    CommandType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PickedUpUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResultMessage = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisplayCommands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisplayCommands_DisplayDevices_DisplayDeviceId",
                        column: x => x.DisplayDeviceId,
                        principalTable: "DisplayDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DisplayAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayDeviceId = table.Column<int>(type: "int", nullable: false),
                    UrlProfileId = table.Column<int>(type: "int", nullable: false),
                    AssignedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisplayAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisplayAssignments_DisplayDevices_DisplayDeviceId",
                        column: x => x.DisplayDeviceId,
                        principalTable: "DisplayDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DisplayAssignments_UrlProfiles_UrlProfileId",
                        column: x => x.UrlProfileId,
                        principalTable: "UrlProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceCredentials_DisplayDeviceId",
                table: "DeviceCredentials",
                column: "DisplayDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceCredentials_TokenHash",
                table: "DeviceCredentials",
                column: "TokenHash");

            migrationBuilder.CreateIndex(
                name: "IX_DisplayAssignments_DisplayDeviceId",
                table: "DisplayAssignments",
                column: "DisplayDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_DisplayAssignments_UrlProfileId",
                table: "DisplayAssignments",
                column: "UrlProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_DisplayCheckIns_DisplayDeviceId",
                table: "DisplayCheckIns",
                column: "DisplayDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_DisplayCommands_DisplayDeviceId",
                table: "DisplayCommands",
                column: "DisplayDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_DisplayDevices_ComputerName",
                table: "DisplayDevices",
                column: "ComputerName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UrlProfiles_Name",
                table: "UrlProfiles",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "DeviceCredentials");

            migrationBuilder.DropTable(
                name: "DisplayAssignments");

            migrationBuilder.DropTable(
                name: "DisplayCheckIns");

            migrationBuilder.DropTable(
                name: "DisplayCommands");

            migrationBuilder.DropTable(
                name: "UrlProfiles");

            migrationBuilder.DropTable(
                name: "DisplayDevices");
        }
    }
}
