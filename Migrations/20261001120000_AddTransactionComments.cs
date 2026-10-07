using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EPEAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "epe_transactioncomment",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    transactionid = table.Column<long>(type: "bigint", nullable: false),
                    userid = table.Column<int>(type: "int", nullable: false),
                    employeeid = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    message = table.Column<string>(type: "text", unicode: false, nullable: false),
                    type = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Comment"),
                    parentcommentid = table.Column<int>(type: "int", nullable: true),
                    isresolved = table.Column<bool>(type: "bit", nullable: false),
                    createdat = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    updatedat = table.Column<DateTime>(type: "datetime", nullable: true),
                    metadata = table.Column<string>(type: "text", unicode: false, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_epe_transactioncomment", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_epe_transactioncomment_transactionid",
                table: "epe_transactioncomment",
                column: "transactionid");

            migrationBuilder.CreateIndex(
                name: "IX_epe_transactioncomment_parentcommentid",
                table: "epe_transactioncomment",
                column: "parentcommentid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "epe_transactioncomment");
        }
    }
}