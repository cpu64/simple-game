namespace Client.UI.Widgets;

using System;
using System.Collections.Generic;
using global::Client.UI.Rendering;
using Raylib_cs;

public class Hotbar : WidgetContainer
{
    private readonly List<ItemSlot> _slots = new();
    private int _nextSlotIndex = 1;

    public float SlotSize { get; }
    public float SlotGap { get; }
    public IReadOnlyList<ItemSlot> Slots => _slots;
    public int SelectedIndex { get; private set; } = -1;

    public Hotbar(Rectangle bounds, IRenderer renderer, float slotSize = 48.0f, float slotGap = 8.0f)
        : base(bounds, renderer)
    {
        if (!(slotSize > 0.0f))
        {
            throw new ArgumentOutOfRangeException(nameof(slotSize), "Slot size must be positive.");
        }

        if (!(slotGap >= 0.0f))
        {
            throw new ArgumentOutOfRangeException(nameof(slotGap), "Slot gap cannot be negative.");
        }

        SlotSize = slotSize;
        SlotGap = slotGap;
    }

    public ItemSlot AddSlot(string itemName, string assetId)
    {
        int index = _slots.Count;
        float x = Bounds.X + index * (SlotSize + SlotGap);
        Rectangle slotBounds = new(x, Bounds.Y, SlotSize, SlotSize);
        var slot = new ItemSlot(slotBounds, Renderer, _nextSlotIndex++, itemName, assetId);

        _slots.Add(slot);
        Add(slot);
        UpdateBounds();

        if (_slots.Count == 1)
        {
            SelectSlot(0);
        }

        return slot;
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= _slots.Count)
        {
            return;
        }

        SelectedIndex = index;
        for (int i = 0; i < _slots.Count; i++)
        {
            _slots[i].IsSelected = i == index;
        }
    }

    public override bool Remove(Widget child)
    {
        if (!base.Remove(child))
        {
            return false;
        }

        if (child is ItemSlot slot && _slots.Remove(slot))
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                _slots[i].Bounds = new Rectangle(Bounds.X + i * (SlotSize + SlotGap), Bounds.Y, SlotSize, SlotSize);
            }

            UpdateBounds();

            if (_slots.Count == 0)
            {
                SelectedIndex = -1;
            }
            else
            {
                int selectedIdx = _slots.FindIndex(s => s.IsSelected);
                if (selectedIdx >= 0)
                {
                    SelectedIndex = selectedIdx;
                }
                else
                {
                    SelectSlot(Math.Clamp(SelectedIndex, 0, _slots.Count - 1));
                }
            }
        }

        return true;
    }

    private void UpdateBounds()
    {
        float totalWidth = _slots.Count > 0 ? _slots.Count * SlotSize + (_slots.Count - 1) * SlotGap : 0.0f;
        Bounds = new Rectangle(Bounds.X, Bounds.Y, totalWidth, SlotSize);
    }
}
