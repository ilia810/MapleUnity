"""Read-only PKG4 UI.nx metadata/image inspection. Run with Anaconda Python.

Example:
  python ui_nx_inspect.py StatusBar.img/base --out Logs/ui-assets --recursive
The optional export writes PNGs plus metadata.json; it never changes NX files.
"""
import argparse
import json
import pathlib
import struct


class Nx:
    def __init__(self, filename):
        self.data = pathlib.Path(filename).read_bytes()
        magic, self.node_count, self.node_offset, self.string_count, self.string_offset, self.bitmap_count, self.bitmap_offset, _, _ = struct.unpack_from('<4sIQIQIQIQ', self.data)
        if magic != b'PKG4':
            raise ValueError('Expected PKG4')

    def string(self, index):
        offset = struct.unpack_from('<Q', self.data, self.string_offset + index * 8)[0]
        length = struct.unpack_from('<H', self.data, offset)[0]
        return self.data[offset + 2:offset + 2 + length].decode('utf-8')

    def node(self, index):
        return struct.unpack_from('<IIHH8s', self.data, self.node_offset + index * 20)

    def children(self, index):
        _, start, count, _, _ = self.node(index)
        return {self.string(self.node(child)[0]): child for child in range(start, start + count)}

    def at(self, path):
        index = 0
        for name in path.strip('/').split('/'):
            if name:
                index = self.children(index).get(name)
                if index is None:
                    return None
        return index

    def describe(self, path):
        index = self.at(path)
        if index is None:
            return {'path': path, 'missing': True}
        _, _, _, kind, value = self.node(index)
        children = self.children(index)
        origin_node = children.get('origin')
        origin = struct.unpack('<ii', self.node(origin_node)[4]) if origin_node is not None else (0, 0)
        result = {'path': path, 'kind': kind, 'origin': origin, 'children': list(children)}
        if kind == 5:
            bitmap, width, height = struct.unpack('<IHH', value)
            result.update(bitmap=bitmap, width=width, height=height)
        elif kind == 1:
            result['value'] = struct.unpack('<q', value)[0]
        elif kind == 3:
            result['value'] = self.string(struct.unpack('<I', value[:4])[0])
        elif kind == 4:
            result['value'] = struct.unpack('<ii', value)
        return result

    def image(self, path):
        from PIL import Image
        import lz4.block
        entry = self.describe(path)
        if entry.get('kind') != 5:
            raise ValueError('Not a bitmap: ' + path)
        offset = struct.unpack_from('<Q', self.data, self.bitmap_offset + 8 * entry['bitmap'])[0]
        length = struct.unpack_from('<I', self.data, offset)[0]
        rgba = lz4.block.decompress(self.data[offset + 4:offset + 4 + length], uncompressed_size=entry['width'] * entry['height'] * 4)
        return Image.frombytes('RGBA', (entry['width'], entry['height']), rgba, 'raw', 'BGRA')

    def walk(self, path, recursive=False):
        yield path
        index = self.at(path)
        if index is None:
            return
        for child in self.children(index):
            next_path = path.rstrip('/') + '/' + child
            if recursive:
                yield from self.walk(next_path, recursive=True)
            else:
                yield next_path


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('paths', nargs='+')
    parser.add_argument('--nx', default='C:/HeavenClient/MapleStory-Client/nx/UI.nx')
    parser.add_argument('--out')
    parser.add_argument('--recursive', action='store_true')
    args = parser.parse_args()
    nx = Nx(args.nx)
    entries = [nx.describe(path) for root in args.paths for path in nx.walk(root, args.recursive)]
    if args.out:
        dest = pathlib.Path(args.out)
        dest.mkdir(parents=True, exist_ok=True)
        for entry in entries:
            if entry.get('kind') == 5:
                output = dest.joinpath(*entry['path'].split('/')).with_suffix('.png')
                output.parent.mkdir(parents=True, exist_ok=True)
                nx.image(entry['path']).save(output)
                entry['png'] = str(output.resolve())
        dest.joinpath('metadata.json').write_text(json.dumps(entries, indent=2), encoding='utf-8')
        print(f'Exported {sum(e.get("kind") == 5 for e in entries)} images to {dest}')
    else:
        print(json.dumps(entries, indent=2))
