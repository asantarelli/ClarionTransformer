using System;
using System.Windows.Forms;
using ICSharpCode.Core;
using ClarionTransformer.Dialogs;

namespace ClarionTransformer.Commands
{
    public class TransformerSettingsCommand : AbstractMenuCommand
    {
        public override void Run()
        {
            try
            {
                using (var dlg = new TransformerSettingsDialog())
                    dlg.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al abrir configuracion: " + ex.Message,
                    "ClarionTransformer", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
