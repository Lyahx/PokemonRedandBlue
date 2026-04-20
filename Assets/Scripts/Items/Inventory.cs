using System;
using System.Collections.Generic;

namespace PokeRed.Items
{
    [Serializable]
    public class InventorySlot
    {
        public ItemData item;
        public int count;
    }

    [Serializable]
    public class Inventory
    {
        public List<InventorySlot> slots = new();

        public int CountOf(ItemData item)
        {
            foreach (var s in slots) if (s.item == item) return s.count;
            return 0;
        }

        public void Add(ItemData item, int count = 1)
        {
            foreach (var s in slots)
                if (s.item == item) { s.count += count; return; }
            slots.Add(new InventorySlot { item = item, count = count });
        }

        public bool Consume(ItemData item, int count = 1)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].item != item) continue;
                if (slots[i].count < count) return false;
                slots[i].count -= count;
                if (slots[i].count <= 0) slots.RemoveAt(i);
                return true;
            }
            return false;
        }
    }
}
