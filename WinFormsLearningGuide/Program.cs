using System;
using System.IO;
using System.Windows.Forms;
using WinFormsLearningGuide.Forms;
using WinFormsLearningGuide.Services;

namespace WinFormsLearningGuide;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        try
        {
            var docs = new DocumentationService();
            docs.Load(Path.Combine(AppContext.BaseDirectory, "Data"));

            var user = new UserDataService();
            user.Load();

            Application.Run(new MainForm(docs, user));
        }
        catch (Exception ex)
        {
            MessageBox.Show("The application could not start:\n\n" + ex.Message,
                "WinForms Learning Guide", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
