using System.Buffers.Binary;
using System.IO;

namespace HyruleCompanion;

internal enum KeyItem { CrystallizedCharge, KorokSeeds, LightOfBlessing }

internal static class KeyItemAmounts
{
    internal const int Maximum = 999;
    const int Size = 2307656;
    const int TableEnd = 0x3c080;
    const int Slots = 200;
    const int NameSize = 64;

    static ReadOnlySpan<byte> Actor(KeyItem item) => item switch
    {
        KeyItem.CrystallizedCharge => "Energy_Material_01"u8,
        KeyItem.KorokSeeds => "Obj_KorokNuts"u8,
        KeyItem.LightOfBlessing => "Obj_DungeonClearSeal"u8,
        _ => throw new ArgumentOutOfRangeException(nameof(item))
    };
    static int ReadInt(byte[] data, int offset) => BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset, 4));
    static void WriteInt(byte[] data, int offset, int value) => BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(offset, 4), value);

    static (int Names, int Counts, int Item, int Free) Locate(byte[] data, KeyItem keyItem)
    {
        if (data.Length != Size || ReadInt(data, 0) != 0x01020304 ||
            ReadInt(data, 4) != 0x0047e0f4 || ReadInt(data, 8) != TableEnd + 8 ||
            ReadInt(data, TableEnd) != unchecked((int)0xa3db7114))
            throw new InvalidDataException("Choose a TOTK 1.2.1 progress.sav file.");

        int FindArray(int hash, int stride)
        {
            for (int i = 0x28; i < TableEnd; i += 8)
            {
                if (ReadInt(data, i) != hash) continue;
                int offset = ReadInt(data, i + 4);
                if (offset >= TableEnd + 8 && offset <= Size - 4 - Slots * stride &&
                    (offset & 3) == 0 && ReadInt(data, offset) == Slots)
                    return offset + 4;
                break;
            }
            throw new InvalidDataException("The save has an unexpected key-item inventory layout.");
        }

        int names = FindArray(0x22c6530a, NameSize);
        int counts = FindArray(0x60fac288, sizeof(int));
        if (names < counts + Slots * sizeof(int) && counts < names + Slots * NameSize)
            throw new InvalidDataException("The save has overlapping inventory data.");

        int item = -1, free = -1;
        for (int i = 0; i < Slots; i++)
        {
            var name = data.AsSpan(names + i * NameSize, NameSize);
            int end = name.IndexOf((byte)0);
            if (end < 0) throw new InvalidDataException("The save has an invalid key-item name.");
            int count = ReadInt(data, counts + i * sizeof(int));
            if (end == 0 && count == -1 && free < 0) free = i;
            if (!name[..end].SequenceEqual(Actor(keyItem))) continue;
            if (item >= 0 || count < 0)
                throw new InvalidDataException("The save has an invalid stack for this item.");
            item = i;
        }
        return (names, counts, item, free);
    }

    internal static int Read(byte[] data, KeyItem keyItem)
    {
        var (_, counts, item, _) = Locate(data, keyItem);
        return item < 0 ? 0 : ReadInt(data, counts + item * sizeof(int));
    }

    internal static void Set(byte[] data, KeyItem keyItem, int amount)
    {
        if (amount is < 0 or > Maximum) throw new ArgumentOutOfRangeException(nameof(amount));
        var (names, counts, item, free) = Locate(data, keyItem);
        if (item < 0 && amount == 0) return;
        if (item < 0)
        {
            if (free < 0) throw new InvalidDataException("The key-item pouch is full.");
            item = free;
            var name = data.AsSpan(names + item * NameSize, NameSize);
            name.Clear();
            Actor(keyItem).CopyTo(name);
        }
        if (amount == 0)
        {
            int tail = Slots - item - 1;
            Array.Copy(data, names + (item + 1) * NameSize, data, names + item * NameSize, tail * NameSize);
            Array.Copy(data, counts + (item + 1) * sizeof(int), data, counts + item * sizeof(int), tail * sizeof(int));
            data.AsSpan(names + (Slots - 1) * NameSize, NameSize).Clear();
            WriteInt(data, counts + (Slots - 1) * sizeof(int), -1);
            return;
        }
        WriteInt(data, counts + item * sizeof(int), amount);
    }
}
