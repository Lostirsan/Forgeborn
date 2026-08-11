using System;
using System.Collections.Generic;
using ForgeGame.Data;
using ForgeGame.Items;
using UnityEngine;

namespace ForgeGame.Inventory
{
    /// <summary>
    /// Runtime facade over <see cref="InventoryModel"/>. It enforces stacking rules
    /// using item definitions, exposes typed queries, and raises a change event the
    /// UI subscribes to. Save data comes straight from <see cref="Export"/>.
    /// </summary>
    public class InventoryService : MonoBehaviour
    {
        [SerializeField] private SmithyDatabase database;

        private InventoryModel _model = new InventoryModel();

        public event Action InventoryChanged;

        public int Money => _model.money;
        public IReadOnlyList<InventoryItemInstance> Stacks => _model.stacks;
        public IReadOnlyList<IngotInstance> Ingots => _model.ingots;
        public IReadOnlyList<WeaponInstance> Weapons => _model.weapons;

        public void SetDatabase(SmithyDatabase db) => database = db;

        // ---- Stackable goods ----

        public void AddItem(string itemId, int count)
        {
            if (string.IsNullOrEmpty(itemId) || count <= 0) return;

            var data = database != null ? database.GetItem(itemId) : null;
            bool stackable = data == null || data.Stackable;

            if (stackable)
            {
                var stack = _model.stacks.Find(s => s.itemId == itemId);
                if (stack == null)
                    _model.stacks.Add(new InventoryItemInstance(itemId, count));
                else
                    stack.count += count;
            }
            else
            {
                for (int i = 0; i < count; i++)
                    _model.stacks.Add(new InventoryItemInstance(itemId, 1));
            }
            InventoryChanged?.Invoke();
        }

        public bool RemoveItem(string itemId, int count)
        {
            if (string.IsNullOrEmpty(itemId) || count <= 0) return false;
            if (GetCount(itemId) < count) return false;

            int remaining = count;
            for (int i = _model.stacks.Count - 1; i >= 0 && remaining > 0; i--)
            {
                if (_model.stacks[i].itemId != itemId) continue;
                int take = Mathf.Min(remaining, _model.stacks[i].count);
                _model.stacks[i].count -= take;
                remaining -= take;
                if (_model.stacks[i].count <= 0) _model.stacks.RemoveAt(i);
            }
            InventoryChanged?.Invoke();
            return true;
        }

        public int GetCount(string itemId)
        {
            int total = 0;
            foreach (var s in _model.stacks)
                if (s.itemId == itemId) total += s.count;
            return total;
        }

        public bool HasItem(string itemId, int count) => GetCount(itemId) >= count;

        public List<InventoryItemInstance> GetStacksByType(ItemType type)
        {
            var list = new List<InventoryItemInstance>();
            if (database == null) return list;
            foreach (var s in _model.stacks)
            {
                var data = database.GetItem(s.itemId);
                if (data != null && data.ItemType == type) list.Add(s);
            }
            return list;
        }

        // ---- Ingots ----

        public void AddIngot(IngotInstance ingot)
        {
            if (ingot == null) return;
            _model.ingots.Add(ingot);
            InventoryChanged?.Invoke();
        }

        public bool RemoveIngot(string uniqueId)
        {
            int removed = _model.ingots.RemoveAll(i => i.uniqueId == uniqueId);
            if (removed > 0) InventoryChanged?.Invoke();
            return removed > 0;
        }

        public IngotInstance GetIngot(string uniqueId) => _model.ingots.Find(i => i.uniqueId == uniqueId);

        // ---- Weapons ----

        public void AddWeapon(WeaponInstance weapon)
        {
            if (weapon == null) return;
            _model.weapons.Add(weapon);
            InventoryChanged?.Invoke();
        }

        public bool RemoveWeapon(string uniqueId)
        {
            int removed = _model.weapons.RemoveAll(w => w.uniqueId == uniqueId);
            if (removed > 0) InventoryChanged?.Invoke();
            return removed > 0;
        }

        // ---- Money ----

        public void AddMoney(int amount)
        {
            _model.money = Mathf.Max(0, _model.money + amount);
            InventoryChanged?.Invoke();
        }

        // ---- Persistence ----

        public InventoryModel Export() => _model;

        public void LoadFrom(InventoryModel model)
        {
            _model = model ?? new InventoryModel();
            _model.stacks ??= new List<InventoryItemInstance>();
            _model.ingots ??= new List<IngotInstance>();
            _model.weapons ??= new List<WeaponInstance>();
            InventoryChanged?.Invoke();
        }

        public void RaiseChanged() => InventoryChanged?.Invoke();
    }
}
