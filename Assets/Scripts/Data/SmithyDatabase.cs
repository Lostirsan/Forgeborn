using System.Collections.Generic;
using UnityEngine;

namespace ForgeGame.Data
{
    /// <summary>
    /// Central registry mapping stable string ids to their reference assets. Runtime
    /// systems and save data only ever store ids; this database resolves them back to
    /// assets. Lookups are built once, on demand, and cached.
    /// </summary>
    [CreateAssetMenu(menuName = "Forge Game/Smithy Database", fileName = "SmithyDatabase")]
    public class SmithyDatabase : ScriptableObject
    {
        [SerializeField] private List<MaterialData> materials = new List<MaterialData>();
        [SerializeField] private List<OreData> ores = new List<OreData>();
        [SerializeField] private List<ItemData> items = new List<ItemData>();
        [SerializeField] private List<WeaponBlueprintData> blueprints = new List<WeaponBlueprintData>();
        [SerializeField] private List<WeaponComponentData> components = new List<WeaponComponentData>();
        [SerializeField] private List<DefectData> defects = new List<DefectData>();

        private Dictionary<string, MaterialData> _materials;
        private Dictionary<string, OreData> _ores;
        private Dictionary<string, ItemData> _items;
        private Dictionary<string, WeaponBlueprintData> _blueprints;
        private Dictionary<string, WeaponComponentData> _components;
        private Dictionary<string, DefectData> _defects;

        public IReadOnlyList<MaterialData> Materials => materials;
        public IReadOnlyList<OreData> Ores => ores;
        public IReadOnlyList<ItemData> Items => items;
        public IReadOnlyList<WeaponBlueprintData> Blueprints => blueprints;
        public IReadOnlyList<WeaponComponentData> Components => components;
        public IReadOnlyList<DefectData> Defects => defects;

        private void OnEnable() => _materials = null; // force rebuild after domain reload

        public MaterialData GetMaterial(string id) => Resolve(ref _materials, materials, m => m.Id, id);
        public OreData GetOre(string id) => Resolve(ref _ores, ores, o => o.Id, id);
        public ItemData GetItem(string id) => Resolve(ref _items, items, i => i.Id, id);
        public WeaponBlueprintData GetBlueprint(string id) => Resolve(ref _blueprints, blueprints, b => b.Id, id);
        public WeaponComponentData GetComponent(string id) => Resolve(ref _components, components, c => c.Id, id);
        public DefectData GetDefect(string id) => Resolve(ref _defects, defects, d => d.Id, id);

        public WeaponBlueprintData FirstBlueprint => blueprints.Count > 0 ? blueprints[0] : null;

        private static T Resolve<T>(ref Dictionary<string, T> cache, List<T> source,
            System.Func<T, string> idOf, string id) where T : Object
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (cache == null)
            {
                cache = new Dictionary<string, T>();
                foreach (var entry in source)
                {
                    if (entry == null) continue;
                    string key = idOf(entry);
                    if (!string.IsNullOrEmpty(key) && !cache.ContainsKey(key))
                        cache.Add(key, entry);
                }
            }
            return cache.TryGetValue(id, out var value) ? value : null;
        }

        /// <summary>Generator helper to populate the database with created assets.</summary>
        public void Configure(List<MaterialData> mats, List<OreData> oreList, List<ItemData> itemList,
            List<WeaponBlueprintData> bps, List<WeaponComponentData> comps, List<DefectData> defs)
        {
            materials = mats; ores = oreList; items = itemList;
            blueprints = bps; components = comps; defects = defs;
            _materials = null; _ores = null; _items = null;
            _blueprints = null; _components = null; _defects = null;
        }
    }
}
