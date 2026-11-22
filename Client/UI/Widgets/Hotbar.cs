namespace Client.UI.Widgets;

using System;
using System.Collections.Generic;
using System.Linq;
using global::Client.UI.Rendering;
using Raylib_cs;

public class Hotbar : WidgetContainer // Composite
{
    private int _nextSlotIndex = 1;

    public float SlotSize { get; }
    public float SlotGap { get; }
    public IReadOnlyList<ItemSlot> Slots => Children.OfType<ItemSlot>().ToList();
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
        var slot = new ItemSlot(new Rectangle(Bounds.X, Bounds.Y, SlotSize, SlotSize), Renderer, _nextSlotIndex++, itemName, assetId);
        Add(slot);
        return slot;
    }

    public override void Add(Widget child) // Add
    {
        ArgumentNullException.ThrowIfNull(child);
        if (child is not ItemSlot slot)
        {
            throw new ArgumentException("Hotbar only accepts ItemSlot children.", nameof(child));
        }

        int countBefore = Children.Count;
        base.Add(child);
        if (Children.Count == countBefore)
        {
            return;
        }

        Relayout();

        if (Children.Count == 1)
        {
            SelectSlot(0);
        }
        else
        {
            slot.IsSelected = false;
        }
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= Children.Count)
        {
            return;
        }

        SelectedIndex = index;
        for (int i = 0; i < Children.Count; i++)
        {
            if (Children[i] is ItemSlot slot)
            {
                slot.IsSelected = i == index;
            }
        }
    }

    public override bool Remove(Widget child) // Remove
    {
        if (!base.Remove(child))
        {
            return false;
        }

        if (child is ItemSlot removedSlot)
        {
            removedSlot.IsSelected = false;
        }

        Relayout();

        if (Children.Count == 0)
        {
            SelectedIndex = -1;
        }
        else
        {
            int selectedIdx = -1;
            for (int i = 0; i < Children.Count; i++)
            {
                if (Children[i] is ItemSlot slot && slot.IsSelected)
                {
                    selectedIdx = i;
                    break;
                }
            }

            if (selectedIdx >= 0)
            {
                SelectedIndex = selectedIdx;
            }
            else
            {
                SelectSlot(Math.Clamp(SelectedIndex, 0, Children.Count - 1));
            }
        }

        return true;
    }

    private void Relayout()
    {
        for (int i = 0; i < Children.Count; i++)
        {
            Children[i].Bounds = new Rectangle(Bounds.X + i * (SlotSize + SlotGap), Bounds.Y, SlotSize, SlotSize);
        }

        float totalWidth = Children.Count > 0 ? Children.Count * SlotSize + (Children.Count - 1) * SlotGap : 0.0f;
        Bounds = new Rectangle(Bounds.X, Bounds.Y, totalWidth, SlotSize);
    }
}
