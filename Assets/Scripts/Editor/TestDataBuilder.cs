using System.Collections.Generic;
using ForgeGame.Data;
using UnityEditor;
using UnityEngine;

namespace ForgeGame.EditorTools
{
    /// <summary>
    /// Creates (or refreshes) all test ScriptableObjects — materials, ores, items,
    /// blueprint, components, defects — and the database that indexes them. Values
    /// are game-tuned, not physically real. Safe to re-run: existing assets are
    /// updated in place so scene references stay intact.
    /// </summary>
    public static class TestDataBuilder
    {
        private const string Dir = "Assets/Data/Smithy";
        private const string DbPath = Dir + "/SmithyDatabase.asset";

        [MenuItem("Tools/Forge Game/Build Test Data")]
        public static void BuildMenu()
        {
            BuildAll();
            EditorUtility.DisplayDialog("Forge Game", "Тестовые данные созданы в " + Dir, "OK");
        }

        public static SmithyDatabase BuildAll()
        {
            EnsureFolder("Assets/Data");
            EnsureFolder(Dir);

            // ---- Materials ----
            var iron = Material(SmithyIds.Iron, "Железо", MaterialCategory.Metal, ItemRarity.Common,
                new Color(0.62f, 0.62f, 0.66f),
                new Vector4(900, 1100, 1350, 1550), new Vector4(650, 800, 1100, 1250),
                7.8f, 0.5f, 0.7f, 0.6f, 0.3f, 1f, 1f, 5,
                new[] { (QuenchMedium.Water, 0.9f), (QuenchMedium.Oil, 0.8f) }, null,
                "Начальный металл. Широкие допуски, прощает ошибки.");

            var steel = Material(SmithyIds.Steel, "Сталь", MaterialCategory.Metal, ItemRarity.Uncommon,
                new Color(0.55f, 0.58f, 0.66f),
                new Vector4(1150, 1300, 1450, 1600), new Vector4(800, 900, 1050, 1200),
                7.85f, 0.75f, 0.6f, 0.5f, 0.5f, 0.9f, 1.1f, 14,
                new[] { (QuenchMedium.Water, 0.5f), (QuenchMedium.Oil, 0.9f) }, null,
                "Прочнее железа, но капризнее в закалке. Любит масло.");

            var silver = Material(SmithyIds.Silver, "Серебро", MaterialCategory.Metal, ItemRarity.Rare,
                new Color(0.83f, 0.84f, 0.88f),
                new Vector4(750, 900, 980, 1100), new Vector4(450, 550, 680, 820),
                10.5f, 0.35f, 0.45f, 0.7f, 0.8f, 1.3f, 1.2f, 40,
                new[] { (QuenchMedium.Water, 0.6f), (QuenchMedium.Oil, 0.7f) },
                new[] { "anti_supernatural" },
                "Мягкое и дорогое. Чувствительно к перегреву, бьёт нечисть.");

            var bronze = Material(SmithyIds.Bronze, "Бронза", MaterialCategory.Metal, ItemRarity.Common,
                new Color(0.72f, 0.48f, 0.24f),
                new Vector4(700, 900, 1050, 1200), new Vector4(500, 650, 850, 1000),
                8.7f, 0.45f, 0.6f, 0.55f, 0.7f, 1f, 1f, 8,
                new[] { (QuenchMedium.Water, 0.7f), (QuenchMedium.Oil, 0.7f) }, null,
                "Литейный сплав. Плавится и заливается в форму меча.");

            // ---- Ores ----
            var ironOre = Ore(SmithyIds.IronOre, "Железная руда", iron, 1.0f, 0.2f, 0.3f, 3);
            var steelOre = Ore(SmithyIds.SteelOre, "Стальная руда", steel, 0.9f, 0.3f, 0.5f, 8);
            var silverOre = Ore(SmithyIds.SilverOre, "Серебряная руда", silver, 0.7f, 0.25f, 0.5f, 20);

            // ---- Items (inventory-facing) ----
            var itemIronOre = Item(SmithyIds.IronOre, "Железная руда", ItemType.Ore, 3, new Color(0.5f, 0.35f, 0.3f), "Руда для выплавки железа.");
            var itemSteelOre = Item(SmithyIds.SteelOre, "Стальная руда", ItemType.Ore, 8, new Color(0.45f, 0.45f, 0.5f), "Руда для выплавки стали.");
            var itemSilverOre = Item(SmithyIds.SilverOre, "Серебряная руда", ItemType.Ore, 20, new Color(0.7f, 0.7f, 0.75f), "Руда для выплавки серебра.");
            var itemCoal = Item(SmithyIds.Coal, "Уголь", ItemType.Fuel, 2, new Color(0.15f, 0.15f, 0.15f), "Топливо для печи и горна.");
            var itemFlux = Item(SmithyIds.Flux, "Флюс", ItemType.Flux, 4, new Color(0.8f, 0.75f, 0.5f), "Снижает примеси при плавке.");
            var itemBronze = Item(SmithyIds.Bronze, "Бронза", ItemType.Ingot, 8, new Color(0.72f, 0.48f, 0.24f), "Слиток бронзы для переплавки в форму меча.");
            // Dungeon raw resources.
            var itemCopper = Item(SmithyIds.Copper, "Медь", ItemType.Ore, 4, new Color(0.85f, 0.45f, 0.25f), "Медь, добытая в подземелье. Сплавляется с оловом в бронзу.");
            var itemTin = Item(SmithyIds.Tin, "Олово", ItemType.Ore, 4, new Color(0.78f, 0.80f, 0.84f), "Олово, добытое в подземелье. Сплавляется с медью в бронзу.");

            // ---- Blueprints ----
            FoundryArtGenerator.EnsureAll(); // make sure weapon icons exist before wiring previews
            var sword = Blueprint(SmithyIds.SwordBlueprint, "Меч", 3f, 5,
                new List<float> { 0.25f, 0.55f, 0.75f, 0.9f, 0.5f }, 20f, 1f, 100f, 5f);
            var bronzeSword = Blueprint(SmithyIds.BronzeSword, "Бронзовый меч", 3f, 14,
                new List<float>(), 22f, 1f, 110f, 6f, FoundryArtGenerator.WpSword);
            // New bronze mold recipes (the player picks one before choosing ore).
            var bronzeSickle = Blueprint(SmithyIds.BronzeSickle, "Бронзовый серп", 3f, 14,
                new List<float>(), 16f, 1.3f, 90f, 4f, FoundryArtGenerator.WpSickle);
            var bronzeHammer = Blueprint(SmithyIds.BronzeHammer, "Бронзовый молот", 3f, 14,
                new List<float>(), 34f, 0.7f, 130f, 12f, FoundryArtGenerator.WpHammer);
            var bronzeScythe = Blueprint(SmithyIds.BronzeScythe, "Бронзовая коса", 3f, 14,
                new List<float>(), 26f, 0.9f, 100f, 6f, FoundryArtGenerator.WpScythe);
            var bronzeKnife = Blueprint(SmithyIds.BronzeKnife, "Бронзовый нож", 3f, 14,
                new List<float>(), 12f, 1.6f, 70f, 3f, FoundryArtGenerator.WpKnife);
            var bronzePitchfork = Blueprint(SmithyIds.BronzePitchfork, "Бронзовые вилы", 3f, 14,
                new List<float>(), 20f, 1f, 95f, 8f, FoundryArtGenerator.WpPitchfork);

            // ---- Components ----
            var guard = Component(SmithyIds.BasicGuard, "Простая гарда", ComponentSlot.Guard, 0.15f, 0.05f, 0f, 3);
            var handle = Component(SmithyIds.BasicHandle, "Простая рукоять", ComponentSlot.Handle, 0.12f, 0.08f, 0.02f, 2);
            var pommel = Component(SmithyIds.BasicPommel, "Простое навершие", ComponentSlot.Pommel, 0.18f, 0.12f, -0.01f, 3);

            // ---- Defects ----
            var defects = new List<DefectData>
            {
                Defect(DefectIds.PorousIngot, "Пористый слиток", "Пузыри и примеси ослабляют металл.", DefectSeverity.Minor, false, true, 1f, 1f, 0.9f, 1f, 0f),
                Defect(DefectIds.OverheatedMetal, "Перегретый металл", "Структура выгорела от жара.", DefectSeverity.Moderate, false, false, 0.95f, 1f, 0.85f, 1f, 0f),
                Defect(DefectIds.HairlineCrack, "Микротрещина", "Скрытая трещина резко снижает прочность.", DefectSeverity.Major, false, false, 1f, 1f, 0.7f, 1f, 0f),
                Defect(DefectIds.UnevenEdge, "Неровная кромка", "Быстрее тупится, риск зазубрин.", DefectSeverity.Minor, false, true, 0.95f, 1f, 0.95f, 1f, 0f),
                Defect(DefectIds.HeavyTip, "Тяжёлое остриё", "Больше урона, но хуже баланс и скорость.", DefectSeverity.Moderate, false, false, 1.1f, 0.9f, 1f, 1f, -0.1f),
                Defect(DefectIds.WeakTang, "Слабый хвостовик", "Ненадёжное крепление рукояти.", DefectSeverity.Moderate, false, true, 1f, 1f, 0.8f, 1f, 0f),
                Defect(DefectIds.BrittleStructure, "Хрупкая структура", "Твёрдый, но ломкий клинок.", DefectSeverity.Major, false, false, 1.05f, 1f, 0.7f, 1f, 0f),
                Defect(DefectIds.SoftMetal, "Мягкий металл", "Недокалён — гнётся и слабо режет.", DefectSeverity.Moderate, false, true, 0.85f, 1f, 1.05f, 0.9f, 0f),
                Defect(DefectIds.BentBlade, "Кривой клинок", "Асимметрия ухудшает баланс.", DefectSeverity.Moderate, false, true, 0.9f, 1f, 1f, 1f, -0.15f),
            };

            // ---- Database ----
            var db = AssetDatabase.LoadAssetAtPath<SmithyDatabase>(DbPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<SmithyDatabase>();
                AssetDatabase.CreateAsset(db, DbPath);
            }
            db.Configure(
                new List<MaterialData> { bronze, iron, steel, silver },
                new List<OreData> { ironOre, steelOre, silverOre },
                new List<ItemData> { itemBronze, itemCopper, itemTin, itemIronOre, itemSteelOre, itemSilverOre, itemCoal, itemFlux },
                new List<WeaponBlueprintData> { bronzeSword, sword, bronzeSickle, bronzeHammer, bronzeScythe, bronzeKnife, bronzePitchfork },
                new List<WeaponComponentData> { guard, handle, pommel },
                defects);
            EditorUtility.SetDirty(db);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return db;
        }

        // ---- Asset helpers (create or update in place) ----

        private static T GetOrCreate<T>(string id) where T : ScriptableObject
        {
            string path = $"{Dir}/{typeof(T).Name}_{id}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }

        private static MaterialData Material(string id, string name, MaterialCategory cat, ItemRarity rar, Color color,
            Vector4 melting, Vector4 forging, float dens, float hard, float tough, float flex, float corr,
            float heatMul, float coolMul, int value, (QuenchMedium, float)[] quench, string[] traits, string desc)
        {
            var m = GetOrCreate<MaterialData>(id);
            var q = new List<QuenchAffinity>();
            if (quench != null)
                foreach (var (medium, comp) in quench)
                    q.Add(new QuenchAffinity { medium = medium, compatibility = comp });
            m.Configure(id, name, cat, rar, color, melting, forging, dens, hard, tough, flex, corr,
                heatMul, coolMul, value, q, traits != null ? new List<string>(traits) : new List<string>(), desc);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static OreData Ore(string id, string name, MaterialData mat, float yield, float imp, float diff, int value)
        {
            var o = GetOrCreate<OreData>(id);
            o.Configure(id, name, mat, yield, imp, diff, value);
            EditorUtility.SetDirty(o);
            return o;
        }

        private static ItemData Item(string id, string name, ItemType type, int value, Color tint, string desc)
        {
            var it = GetOrCreate<ItemData>(id);
            it.Configure(id, name, type, true, 999, value, tint, desc);
            EditorUtility.SetDirty(it);
            return it;
        }

        private static WeaponBlueprintData Blueprint(string id, string name, float mass, int zones,
            List<float> shape, float dmg, float speed, float durability, float pen, string previewPath = null)
        {
            var b = GetOrCreate<WeaponBlueprintData>(id);
            b.Configure(id, name, mass, zones, shape, dmg, speed, durability, pen, new List<string>());
            if (!string.IsNullOrEmpty(previewPath))
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(previewPath);
                if (sprite != null) b.SetPreview(sprite);
            }
            EditorUtility.SetDirty(b);
            return b;
        }

        private static WeaponComponentData Component(string id, string name, ComponentSlot slot,
            float weight, float balance, float speed, int value)
        {
            var c = GetOrCreate<WeaponComponentData>(id);
            c.Configure(id, name, slot, weight, balance, speed, value);
            EditorUtility.SetDirty(c);
            return c;
        }

        private static DefectData Defect(string id, string name, string desc, DefectSeverity sev,
            bool hidden, bool repairable, float dmg, float speed, float durability, float pen, float balance)
        {
            var d = GetOrCreate<DefectData>(id);
            d.Configure(id, name, desc, sev, hidden, repairable, dmg, speed, durability, pen, balance);
            EditorUtility.SetDirty(d);
            return d;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
