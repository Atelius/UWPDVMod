using UWPDVMod.Core;

namespace UWPDVMod.App;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        // Relaunched elevated to finish a file operation the unelevated instance couldn't do.
        // Args: --elevated <action> <profileId> <gameFolder> [restore]
        if (args.Length >= 4 && args[0] == "--elevated")
        {
            RunElevated(args);
            return;
        }

        Application.Run(new MainForm());
    }

    private static void RunElevated(string[] args)
    {
        string action = args[1], profileId = args[2], folder = args[3];
        var profile = ProfileRegistry.ById(profileId);
        if (profile == null)
        {
            MessageBox.Show($"Unknown profile '{profileId}'.", "UWPDVMod", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        try
        {
            var installer = new ModInstaller(profile, folder);
            switch (action)
            {
                case "install": installer.Install(restoreBackupIni: args.Length > 4 && args[4] == "restore"); break;
                case "uninstall": installer.Uninstall(); break;
                case "enable": installer.Enable(); break;
                case "disable": installer.Disable(); break;
            }
            MessageBox.Show($"'{action}' completed successfully (elevated).", "UWPDVMod",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Elevated '{action}' failed:\n{ex.Message}", "UWPDVMod",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
