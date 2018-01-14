using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Ipfs.Engine.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Configs",
                columns: table => new
                {
                    Name = table.Column<string>(nullable: false),
                    Value = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configs", x => x.Name);
                });
            var defaultConfig = @"{
  ""Addresses"": {
    ""API"": ""/ip4/127.0.0.1/tcp/5001"",
    ""Gateway"": ""/ip4/127.0.0.1/tcp/8080"",
    ""Swarm"": [
      ""/ip4/0.0.0.0/tcp/4001"",
      ""/ip6/::/tcp/4001""
    ]
  },
}";
            migrationBuilder.Sql($"INSERT INTO Configs(Name,Value) Values('ipfs','{defaultConfig}')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Configs");
        }
    }
}
