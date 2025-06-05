using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TNK.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedWorkerServices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceWorker",
                schema: "dbo",
                columns: table => new
                {
                    ServicesId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkersId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceWorker", x => new { x.ServicesId, x.WorkersId });
                    table.ForeignKey(
                        name: "FK_ServiceWorker_Services_ServicesId",
                        column: x => x.ServicesId,
                        principalSchema: "dbo",
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceWorker_Workers_WorkersId",
                        column: x => x.WorkersId,
                        principalSchema: "dbo",
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceWorker_WorkersId",
                schema: "dbo",
                table: "ServiceWorker",
                column: "WorkersId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceWorker",
                schema: "dbo");
        }
    }
}
