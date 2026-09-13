import json
import os
import tkinter as tk


GRID_X = 40
GRID_Y = 30
TILE_SIZE = 20
OUTPUT_FILE = "world.json"


class MapEditor:
    def __init__(self, root):
        self.root = root
        self.root.title("40x40 Map Editor")

        # Set of (x, y) coordinates containing blocks
        self.blocks = set()

        # Keeps track of tiles already affected during the current drag.
        self.dragged_tiles = set()

        self.canvas = tk.Canvas(
            root,
            width=GRID_X * TILE_SIZE,
            height=GRID_Y * TILE_SIZE,
            bg="white",
            highlightthickness=0
        )
        self.canvas.pack()

        self.draw_grid()
        self.load()

        # Left mouse = place
        self.canvas.bind("<Button-1>", self.start_place)
        self.canvas.bind("<B1-Motion>", self.drag_place)

        # Right mouse = remove
        self.canvas.bind("<Button-3>", self.start_remove)
        self.canvas.bind("<B3-Motion>", self.drag_remove)

        # Clear drag state when mouse buttons are released
        self.canvas.bind("<ButtonRelease-1>", self.end_drag)
        self.canvas.bind("<ButtonRelease-3>", self.end_drag)

        # S = save
        self.root.bind("<s>", self.save)
        self.root.bind("<S>", self.save)

        info = tk.Label(
            root,
            text="Left click/drag: Place   |   Right click/drag: Remove   |   S: Save"
        )
        info.pack(pady=5)

    def draw_grid(self):
        """Draw the grid."""
        for y in range(GRID_Y):
            for x in range(GRID_X):
                x1 = x * TILE_SIZE
                y1 = y * TILE_SIZE
                x2 = x1 + TILE_SIZE
                y2 = y1 + TILE_SIZE

                self.canvas.create_rectangle(
                    x1,
                    y1,
                    x2,
                    y2,
                    fill="white",
                    outline="#cccccc",
                    tags=f"tile_{x}_{y}"
                )

    def load(self):
        """Load blocks from OUTPUT_FILE if it exists."""
        if not os.path.exists(OUTPUT_FILE):
            print(f"{OUTPUT_FILE} does not exist. Starting with an empty map.")
            return

        try:
            with open(OUTPUT_FILE, "r", encoding="utf-8") as file:
                data = json.load(file)

            for block in data.get("Blocks", []):
                x = block.get("X")
                y = block.get("Y")

                if (
                    isinstance(x, int)
                    and isinstance(y, int)
                    and 0 <= x < GRID_X
                    and 0 <= y < GRID_Y
                ):
                    self.blocks.add((x, y))

            # Update the visual grid.
            for x, y in self.blocks:
                self.set_tile_color(x, y, "#4a90e2")

            print(f"Loaded {len(self.blocks)} blocks from {OUTPUT_FILE}")

        except (json.JSONDecodeError, OSError) as e:
            print(f"Could not load {OUTPUT_FILE}: {e}")

    def get_tile(self, event):
        """Convert mouse coordinates to a grid coordinate."""
        x = event.x // TILE_SIZE
        y = event.y // TILE_SIZE

        if not (0 <= x < GRID_X and 0 <= y < GRID_Y):
            return None

        return x, y

    def set_tile_color(self, x, y, color):
        """Change the visual color of a tile."""
        self.canvas.itemconfig(
            f"tile_{x}_{y}",
            fill=color
        )

    def place(self, x, y):
        """Place a block at x,y."""
        if (x, y) not in self.blocks:
            self.blocks.add((x, y))
            self.set_tile_color(x, y, "#4a90e2")

    def remove(self, x, y):
        """Remove a block at x,y."""
        if (x, y) in self.blocks:
            self.blocks.remove((x, y))
            self.set_tile_color(x, y, "white")

    def start_place(self, event):
        """Start a left-click placement operation."""
        self.dragged_tiles.clear()

        tile = self.get_tile(event)
        if tile is None:
            return

        x, y = tile
        self.dragged_tiles.add(tile)
        self.place(x, y)

    def drag_place(self, event):
        """Place blocks while dragging with the left mouse button."""
        tile = self.get_tile(event)
        if tile is None or tile in self.dragged_tiles:
            return

        self.dragged_tiles.add(tile)

        x, y = tile
        self.place(x, y)

    def start_remove(self, event):
        """Start a right-click removal operation."""
        self.dragged_tiles.clear()

        tile = self.get_tile(event)
        if tile is None:
            return

        x, y = tile
        self.dragged_tiles.add(tile)
        self.remove(x, y)

    def drag_remove(self, event):
        """Remove blocks while dragging with the right mouse button."""
        tile = self.get_tile(event)
        if tile is None or tile in self.dragged_tiles:
            return

        self.dragged_tiles.add(tile)

        x, y = tile
        self.remove(x, y)

    def end_drag(self, event):
        """Reset the list of tiles affected by the current drag."""
        self.dragged_tiles.clear()

    def save(self, event=None):
        """Save the current map to world_new.json."""
        sorted_blocks = sorted(
            self.blocks,
            key=lambda pos: (pos[1], pos[0])
        )

        data = {
            "Blocks": [
                {
                    "X": x,
                    "Y": y,
                    "Type": 0
                }
                for x, y in sorted_blocks
            ]
        }

        with open(OUTPUT_FILE, "w", encoding="utf-8") as file:
            json.dump(data, file, indent=2)

        print(f"Saved {len(sorted_blocks)} blocks to {OUTPUT_FILE}")


if __name__ == "__main__":
    root = tk.Tk()
    editor = MapEditor(root)
    root.mainloop()
