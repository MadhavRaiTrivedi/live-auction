using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiveAuction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateTable(
                name: "auctions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    starting_price_in_paise = table.Column<long>(type: "bigint", nullable: false),
                    reserve_price_in_paise = table.Column<long>(type: "bigint", nullable: true),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    current_price_in_paise = table.Column<long>(type: "bigint", nullable: true),
                    leading_bidder_id = table.Column<Guid>(type: "uuid", nullable: true),
                    leader_max_in_paise = table.Column<long>(type: "bigint", nullable: true),
                    bid_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auctions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "bids",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    auction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    bidder_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount_in_paise = table.Column<long>(type: "bigint", nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    placed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bids", x => x.id);
                    table.ForeignKey(
                        name: "fk_bids_auctions_auction_id",
                        column: x => x.auction_id,
                        principalTable: "auctions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_auctions_live_ends_at",
                table: "auctions",
                column: "ends_at",
                filter: "status = 'Live'");

            migrationBuilder.CreateIndex(
                name: "ix_auctions_scheduled_starts_at",
                table: "auctions",
                column: "starts_at",
                filter: "status = 'Scheduled'");

            migrationBuilder.CreateIndex(
                name: "ix_auctions_seller_id",
                table: "auctions",
                column: "seller_id");

            migrationBuilder.CreateIndex(
                name: "ix_auctions_title_trigram",
                table: "auctions",
                column: "title")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_bids_auction_id_sequence",
                table: "bids",
                columns: new[] { "auction_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bids_bidder_id_auction_id",
                table: "bids",
                columns: new[] { "bidder_id", "auction_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bids");

            migrationBuilder.DropTable(
                name: "auctions");
        }
    }
}
