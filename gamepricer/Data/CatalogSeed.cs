using gamepricer.Entities;

namespace gamepricer.Data
{
    public static class CatalogSeed
    {
        /// <summary>
        /// İlk kurulumdan sonra da çalışır: eksik slug'lı oyunları ekler (fiyat yok; canlı veri ITAD ile).
        /// </summary>
        public static void EnsureExtendedCatalog(AppDbContext db)
        {
            var rpg = db.Categories.FirstOrDefault(c => c.Name == "RPG");
            var action = db.Categories.FirstOrDefault(c => c.Name == "Action");
            if (rpg == null || action == null)
                return;

            var extra = new (string Name, string Slug, string Dev, string Pub, string Desc)[]
            {
                ("Baldur's Gate 3", "baldurs-gate-3", "Larian Studios", "Larian Studios", "Party tabanlı CRPG."),
                ("The Witcher 3: Wild Hunt", "the-witcher-3-wild-hunt", "CD Projekt Red", "CD Projekt", "Açık dünya aksiyon RPG."),
                ("Hogwarts Legacy", "hogwarts-legacy", "Avalanche Software", "Warner Bros.", "Büyücülük dünyasında macera."),
                ("Red Dead Redemption 2", "red-dead-redemption-2", "Rockstar Games", "Rockstar Games", "Western temalı açık dünya."),
                ("Hades", "hades", "Supergiant Games", "Supergiant Games", "Roguelike aksiyon."),
                ("Stardew Valley", "stardew-valley", "ConcernedApe", "ConcernedApe", "Çiftlik ve yaşam simülasyonu."),
                ("Grand Theft Auto V", "grand-theft-auto-v", "Rockstar North", "Rockstar Games", "Açık dünya aksiyon."),
                ("Portal 2", "portal-2", "Valve", "Valve", "Bulmaca platform oyunu.")
            };

            var addedGames = new List<Game>();
            foreach (var (name, slug, dev, pub, desc) in extra)
            {
                if (db.Games.Any(g => g.Slug == slug))
                    continue;

                var g = new Game
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Slug = slug,
                    Description = desc,
                    Developer = dev,
                    Publisher = pub,
                    CoverImageUrl = null,
                    CreatedAt = DateTime.UtcNow
                };
                db.Games.Add(g);
                addedGames.Add(g);
                db.GameCategories.AddRange(
                    new GameCategory { GameId = g.Id, CategoryId = rpg.Id },
                    new GameCategory { GameId = g.Id, CategoryId = action.Id }
                );
            }

            if (addedGames.Count == 0)
                return;

            db.SaveChanges();
        }
    }
}
