"""Extrae los archivos de un ejecutable .NET single-file (DOTNET_BUNDLE).

Uso: python3 extraer_bundle.py <ejecutable> <carpeta_salida>
"""
import os
import struct
import sys
import zlib

# Firma que el SDK coloca justo después del offset de la cabecera del bundle.
SIGNATURE = bytes.fromhex(
    "8b1202b96a612038727b930214d7a03213f5b9e6efae3318ee3b2dce24b36aae"
)
TYPES = {0: "unknown", 1: "assembly", 2: "native", 3: "deps.json",
         4: "runtimeconfig.json", 5: "symbols"}


class Reader:
    def __init__(self, data, pos):
        self.data, self.pos = data, pos

    def take(self, fmt):
        vals = struct.unpack_from(fmt, self.data, self.pos)
        self.pos += struct.calcsize(fmt)
        return vals[0] if len(vals) == 1 else vals

    def string(self):
        # Longitud codificada en 7 bits (formato BinaryWriter de .NET).
        length, shift = 0, 0
        while True:
            b = self.data[self.pos]
            self.pos += 1
            length |= (b & 0x7F) << shift
            if b < 0x80:
                break
            shift += 7
        s = self.data[self.pos:self.pos + length].decode("utf-8")
        self.pos += length
        return s


def main(exe, out):
    data = open(exe, "rb").read()
    sig = data.find(SIGNATURE)
    if sig < 8:
        sys.exit("No se encontró la firma de bundle .NET")
    header_offset = struct.unpack_from("<q", data, sig - 8)[0]
    if header_offset == 0:
        sys.exit("El ejecutable no lleva bundle (offset 0)")

    r = Reader(data, header_offset)
    major, minor, count = r.take("<IIi")
    bundle_id = r.string()
    if major >= 2:
        r.take("<qqqqQ")  # deps.json, runtimeconfig.json y flags
    print(f"Bundle v{major}.{minor}, id {bundle_id}, {count} archivos")

    for _ in range(count):
        offset, size = r.take("<qq")
        compressed = r.take("<q") if major >= 6 else 0
        kind = r.take("<B")
        path = r.string()
        blob = data[offset:offset + (compressed or size)]
        if compressed:
            blob = zlib.decompress(blob, -15)
        dest = os.path.join(out, path)
        os.makedirs(os.path.dirname(dest) or out, exist_ok=True)
        with open(dest, "wb") as f:
            f.write(blob)
        print(f"  {TYPES.get(kind, kind):18} {len(blob):>10}  {path}")


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
