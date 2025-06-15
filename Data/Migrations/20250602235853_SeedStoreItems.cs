using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ClimbUpAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedStoreItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "StoreItems",
                columns: new[] { "StoreItemId", "Category", "Description", "EffectDetails", "IconUrl", "IsConsumable", "MaxQuantityPerUser", "Name", "PriceSS" },
                values: new object[,]
                {
                    { 1, "Kozmetik", "Profilin için özel bir keşifçi simgesi.", "{\"effect\": \"profile_icon_unlock\", \"icon_id\": \"alpha_explorer\"}", "https://res.cloudinary.com/dp7utrng4/image/upload/v1700000001/store_icons/discovery_badge_alpha.png", false, null, "Keşif Simgesi Alfa", 25 },
                    { 2, "Kozmetik", "Uygulama arayüzün için ferahlatıcı bir dağ manzarası teması.", "{\"effect\": \"theme_unlock\", \"theme_id\": \"mountain_peak\"}", "https://res.cloudinary.com/dp7utrng4/image/upload/v1700000002/store_icons/theme_mountain_peak.png", false, null, "Zirve Manzarası Teması", 45 },
                    { 3, "İşlevsel", "Bir sonraki tamamladığın görev için ekstra +25 Steps kazandırır.", "{\"type\": \"todo_steps_bonus\", \"amount\": 25}", "https://res.cloudinary.com/dp7utrng4/image/upload/v1700000003/store_icons/item_compass.png", true, 5, "Pusula", 10 },
                    { 4, "İşlevsel", "Bir sonraki odak seansından kazanacağın temel Steps miktarını %15 artırır.", "{\"type\": \"session_steps_boost_percentage\", \"amount\": 15}", "https://res.cloudinary.com/dp7utrng4/image/upload/v1700000004/store_icons/item_energy_bar.png", true, 3, "Enerji Barı", 20 },
                    { 5, "Seri Koruyucular", "Bir günlük serini kaybetmeni önler. Ayda bir kullanılabilir (bu kural servis katmanında uygulanmalı).", "{\"type\": \"streak_shield\", \"duration_days\": 1}", "https://res.cloudinary.com/dp7utrng4/image/upload/v1700000005/store_icons/item_streak_shield.png", true, 1, "Günlük İzin", 120 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "StoreItems",
                keyColumn: "StoreItemId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "StoreItems",
                keyColumn: "StoreItemId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "StoreItems",
                keyColumn: "StoreItemId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "StoreItems",
                keyColumn: "StoreItemId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "StoreItems",
                keyColumn: "StoreItemId",
                keyValue: 5);
        }
    }
}
