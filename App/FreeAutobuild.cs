using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;

namespace HyruleCompanion;

internal static class FreeAutobuild
{
    const string Table = "RSDB/GameActorInfo.Product.121.rstbl.byml.zs";
    const string Mod = "Hyrule Companion Free Autobuild";
    static string Folder(string exe) => Path.Combine(Path.GetDirectoryName(Path.GetFullPath(exe))!, "mods", Mod);
    internal static bool Installed(string exe) => File.Exists(Path.Combine(Folder(exe), "romfs", Table));

    internal static void Set(string exe, bool enable)
    {
        string root = Path.GetDirectoryName(Path.GetFullPath(exe))!;
        string folder = Folder(exe);
        string target = Path.Combine(folder, "romfs", Table);
        string receipt = Path.Combine(folder, "companion.sha256");
        if (!enable)
        {
            if (!File.Exists(target)) return;
            if (!File.Exists(receipt) || Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(target))) != File.ReadAllText(receipt))
                throw new IOException("The mod file was edited externally. Manage it in the recomp mod menu.");
            File.Delete(target);
            File.Delete(receipt);
            return;
        }
        if (File.Exists(target)) return;
        var profile = RecompBuild.Detect(exe);
        if (profile?.Release is not ("0.4.1+6151ac4f" or "0.5.0+afae80c9"))
            throw new InvalidOperationException("Free Autobuild requires a supported recomp 0.4.1 or 0.5.0 build with TOTK 1.2.1.");
        string mods = Path.GetDirectoryName(folder)!;
        if (Directory.Exists(mods) && Directory.EnumerateFiles(mods, Path.GetFileName(Table), SearchOption.AllDirectories).Any())
            throw new IOException("Another mod supplies GameActorInfo. Merge the mods before enabling Free Autobuild.");
        using var index = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(root,"romfs","index.json")));
        using var archive = File.OpenRead(Path.Combine(root,"romfs","romfs.bin"));
        byte[] Read(string path, string hash)
        {
            var entry = index.RootElement.GetProperty("files").EnumerateArray().Single(e => e.GetProperty("path").GetString() == path);
            archive.Position = entry.GetProperty("offset").GetInt64();
            byte[] bytes = new byte[entry.GetProperty("size").GetInt32()];
            archive.ReadExactly(bytes);
            if (!Convert.ToHexString(SHA256.HashData(bytes)).Equals(hash,StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("This game asset differs from the supported TOTK 1.2.1 copy.");
            return bytes;
        }
        byte[] pack = Decode(Read("Pack/ZsDic.pack.zs","b3fe439759d4ea1786d1ab8c62b75d6f713ac65ebf35c7d734f2ddfcc7a931d3"), []);
        byte[] table = Decode(Read(Table,"88add60ff6a76df9665f5940c937177512a8bebc4bc6bd493f00fef4d19a6b5c"), pack[136..131208]);
        byte[] patched = ZeroReplacementCosts(table);
        byte[] output = new byte[(int)ZSTD_compressBound((nuint)patched.Length)];
        int size = Check(ZSTD_compress(output,(nuint)output.Length,patched,(nuint)patched.Length,9));
        Array.Resize(ref output,size);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        string temp = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllBytes(temp,output);
            File.WriteAllText(receipt,Convert.ToHexString(SHA256.HashData(output)));
            File.Move(temp,target);
        }
        finally { File.Delete(temp); }
    }

    // In this verified table key 0 is AutoBuilderReplaceConsumption. Missing keys
    // use the game's default cost. Copy each map and preserve its nested references.
    static byte[] ZeroReplacementCosts(byte[] table)
    {
        int root = BitConverter.ToInt32(table,12);
        int count = BitConverter.ToInt32(table,root) >> 8;
        int pointers = (root + 4 + count + 3) & ~3;
        using var result = new MemoryStream();
        result.Write(table);
        using var writer = new BinaryWriter(result);
        for (int i=0; i<count; i++)
        {
            int slot = pointers + i*4;
            int row = BitConverter.ToInt32(table,slot);
            int fields = BitConverter.ToInt32(table,row) >> 8;
            bool existing = (BitConverter.ToInt32(table,row+4) & 0xffffff) == 0;
            if (existing)
            {
                result.Position = row+8;
                writer.Write(0);
                continue;
            }
            int replacement = checked((int)result.Length);
            result.Position = slot;
            writer.Write(replacement);
            result.Position = replacement;
            writer.Write(((fields+1)<<8) | 0xc1);
            writer.Write(0xd1000000u);
            writer.Write(0);
            writer.Write(table,row+4,fields*8);
        }
        return result.ToArray();
    }

    static byte[] Decode(byte[] input, byte[] dictionary)
    {
        byte[] output = new byte[checked((int)ZSTD_getFrameContentSize(input,(nuint)input.Length))];
        nint ctx = ZSTD_createDCtx();
        if(ctx==0) throw new OutOfMemoryException();
        try
        {
            if(Check(ZSTD_decompress_usingDict(ctx,output,(nuint)output.Length,input,(nuint)input.Length,dictionary,(nuint)dictionary.Length)) != output.Length)
                throw new InvalidDataException("Unexpected game asset size.");
            return output;
        }
        finally { ZSTD_freeDCtx(ctx); }
    }
    static int Check(nuint result) => ZSTD_isError(result)==0 ? checked((int)result) : throw new InvalidDataException("Game asset compression failed.");
    [DllImport("libzstd")] static extern ulong ZSTD_getFrameContentSize(byte[] data,nuint size);
    [DllImport("libzstd")] static extern nint ZSTD_createDCtx();
    [DllImport("libzstd")] static extern nuint ZSTD_freeDCtx(nint ctx);
    [DllImport("libzstd")] static extern nuint ZSTD_decompress_usingDict(nint ctx,byte[] dst,nuint capacity,byte[] src,nuint size,byte[] dict,nuint dictSize);
    [DllImport("libzstd")] static extern nuint ZSTD_compressBound(nuint size);
    [DllImport("libzstd")] static extern nuint ZSTD_compress(byte[] dst,nuint capacity,byte[] src,nuint size,int level);
    [DllImport("libzstd")] static extern uint ZSTD_isError(nuint code);
}
