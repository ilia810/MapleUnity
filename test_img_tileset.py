#!/usr/bin/env python3
"""Test if .img tileset exists in NX data"""

import os
import sys

# Add paths for Unity Python modules if needed
nx_path = r"C:\Users\me\MapleUnity\Assets\NX"

print("=== Checking for .img tileset ===")
print(f"Looking in: {nx_path}")

# Check Map.nx structure
map_nx_path = os.path.join(nx_path, "Map.nx")
if os.path.exists(map_nx_path):
    print(f"Map.nx exists at: {map_nx_path}")
    print(f"File size: {os.path.getsize(map_nx_path):,} bytes")
else:
    print("Map.nx not found!")

# Check for extracted tile data
tile_path = os.path.join(nx_path, "Map", "Tile")
if os.path.exists(tile_path):
    print(f"\nTile directory exists at: {tile_path}")
    
    # List all items in Tile directory
    items = os.listdir(tile_path)
    print(f"Found {len(items)} items in Tile directory")
    
    # Check specifically for .img
    img_path = os.path.join(tile_path, ".img")
    if os.path.exists(img_path):
        print("\nFOUND .img tileset directory!")
        if os.path.isdir(img_path):
            variants = os.listdir(img_path)
            print(f"Number of variants: {len(variants)}")
            print("Variants:", variants[:10])  # Show first 10
    else:
        print("\n.img tileset NOT FOUND in extracted data")
        
    # Show first 20 tilesets
    print("\nFirst 20 tilesets:")
    for item in sorted(items)[:20]:
        item_path = os.path.join(tile_path, item)
        if os.path.isdir(item_path):
            print(f"  - {item}/")
        else:
            print(f"  - {item}")
else:
    print("Tile directory not found in extracted NX data")

# Alternative check - look for cache or other storage
cache_path = os.path.join(os.path.dirname(nx_path), "NXCache")
if os.path.exists(cache_path):
    print(f"\nNXCache exists at: {cache_path}")