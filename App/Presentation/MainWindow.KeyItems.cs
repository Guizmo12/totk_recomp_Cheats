using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace HyruleCompanion;

public partial class MainWindow
{
    string? keyItemSave;
    byte[]? keyItemOriginal;
    bool EditingSeeds => KeyItemChoice.SelectedIndex == 1;

    void ChooseKeyItemSave(object sender, RoutedEventArgs e)
    {
        try
        {
            string root = Path.Combine(Path.GetDirectoryName(settings.GamePath)!, "profile", "save");
            var latest = Directory.Exists(root)
                ? Directory.EnumerateFiles(root, "progress.sav", SearchOption.AllDirectories)
                    .OrderByDescending(File.GetLastWriteTime).FirstOrDefault()
                : null;
            var dialog = new OpenFileDialog
            {
                Title = "Choose the TOTK save slot to edit",
                FileName = "progress.sav",
                Filter = "TOTK progress save (progress.sav)|progress.sav",
                InitialDirectory = latest is null ? root : Path.GetDirectoryName(latest)
            };
            if (dialog.ShowDialog(this) != true) return;
            var bytes = File.ReadAllBytes(dialog.FileName);
            KeyItemAmounts.Read(bytes, EditingSeeds);
            keyItemSave = dialog.FileName;
            keyItemOriginal = bytes;
            RefreshKeyItemAmount();
        }
        catch (Exception ex) { ShowNotice(ex.Message); }
    }

    void KeyItemChoiceChanged(object sender, SelectionChangedEventArgs e)
    {
        if (keyItemOriginal is not null) RefreshKeyItemAmount();
    }

    void RefreshKeyItemAmount()
    {
        if (keyItemOriginal is null || keyItemSave is null) return;
        int current = KeyItemAmounts.Read(keyItemOriginal, EditingSeeds);
        KeyItemAmount.Text = current.ToString(CultureInfo.InvariantCulture);
        KeyItemStatus.Text = $"{Path.GetFileName(Path.GetDirectoryName(keyItemSave))} • current amount {current}";
        KeyItemStatus.ToolTip = keyItemSave;
        ApplyKeyItemAmount.IsEnabled = true;
    }

    void ApplyKeyItemAmountClick(object sender, RoutedEventArgs e)
    {
        if (keyItemOriginal is null || keyItemSave is null) return;
        if (!int.TryParse(KeyItemAmount.Text, NumberStyles.None, CultureInfo.InvariantCulture, out int amount) ||
            amount is < 0 or > KeyItemAmounts.Maximum)
        {
            ShowNotice("Enter a whole number from 0 to 999.");
            return;
        }
        string? temp = null;
        try
        {
            RequireTotkClosed();
            var latest = File.ReadAllBytes(keyItemSave);
            if (!latest.AsSpan().SequenceEqual(keyItemOriginal))
            {
                KeyItemAmounts.Read(latest, EditingSeeds);
                keyItemOriginal = latest;
                RefreshKeyItemAmount();
                ShowNotice("This save changed since you selected it. Review the refreshed amount and try again.");
                return;
            }
            var updated = (byte[])latest.Clone();
            KeyItemAmounts.Set(updated, EditingSeeds, amount);
            if (updated.AsSpan().SequenceEqual(latest)) return;

            var backupFolder = Path.Combine(AppContext.BaseDirectory, "backups", "inventory");
            Directory.CreateDirectory(backupFolder);
            var backup = Path.Combine(backupFolder, $"{Path.GetFileName(Path.GetDirectoryName(keyItemSave))}-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.sav");
            File.WriteAllBytes(backup, latest);
            temp = keyItemSave + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllBytes(temp, updated);
            RequireTotkClosed();
            if (!File.ReadAllBytes(keyItemSave).AsSpan().SequenceEqual(latest))
                throw new IOException("This save changed during editing. Choose the slot again.");
            File.Replace(temp, keyItemSave, null);
            keyItemOriginal = updated;
            RefreshKeyItemAmount();
            ShowNotice("Saved the inventory amount. Load this slot in TOTK. The original is in backups/inventory.");
        }
        catch (Exception ex) { ShowNotice(ex.Message); }
        finally
        {
            if (temp is not null)
                try { File.Delete(temp); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }

    void RequireTotkClosed()
    {
        var gameName = Path.GetFileNameWithoutExtension(settings.GamePath);
        if (IsRunning("totk") || (gameName is not null && IsRunning(gameName)))
            throw new InvalidOperationException("Close TOTK before applying a save edit.");
    }

    static bool IsRunning(string name)
    {
        var processes = Process.GetProcessesByName(name);
        foreach (var process in processes) process.Dispose();
        return processes.Length > 0;
    }
}
