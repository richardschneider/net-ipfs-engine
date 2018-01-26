using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Ipfs.Engine.Migrations
{
    /// <inheritdoc />
    public partial class Blocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BlockInfos",
                columns: table => new
                {
                    Cid = table.Column<string>(nullable: false),
                    Pinned = table.Column<bool>(nullable: false),
                    DataSize = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlockInfos", x => x.Cid);
                });

            migrationBuilder.CreateTable(
                name: "BlockValues",
                columns: table => new
                {
                    Cid = table.Column<string>(nullable: false),
                    Data = table.Column<byte[]>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlockValues", x => x.Cid);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlockInfos");

            migrationBuilder.DropTable(
                name: "BlockValues");
        }
    }
}
