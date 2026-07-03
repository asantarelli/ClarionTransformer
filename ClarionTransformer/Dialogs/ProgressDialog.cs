using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace ClarionTransformer.Dialogs
{
    /// <summary>
    /// Dialogo modal de progreso indeterminado con boton Cancelar.
    /// Se muestra mientras se espera la respuesta de la API.
    /// </summary>
    public class ProgressDialog : Form
    {
        private readonly CancellationTokenSource _cts;
        private Label  _lblStatus;
        private Button _btnCancel;

        public CancellationToken CancellationToken => _cts.Token;

        public ProgressDialog(string message)
        {
            _cts = new CancellationTokenSource();

            Text            = "ClarionTransformer";
            Size            = new Size(360, 130);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false; MinimizeBox = false;
            StartPosition   = FormStartPosition.CenterParent;
            ControlBox      = false;

            _lblStatus = new Label
            {
                Text      = message,
                Location  = new Point(16, 18),
                Size      = new Size(320, 40),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var bar = new ProgressBar
            {
                Style    = ProgressBarStyle.Marquee,
                Location = new Point(16, 56),
                Size     = new Size(320, 16)
            };

            _btnCancel = new Button
            {
                Text     = "Cancelar",
                Location = new Point(136, 80),
                Size     = new Size(80, 26)
            };
            _btnCancel.Click += (s, e) =>
            {
                _cts.Cancel();
                _lblStatus.Text = "Cancelando...";
                _btnCancel.Enabled = false;
            };

            Controls.AddRange(new Control[] { _lblStatus, bar, _btnCancel });
            CancelButton = _btnCancel;
        }

        public void SetStatus(string text)
        {
            if (InvokeRequired) Invoke(new Action(() => _lblStatus.Text = text));
            else _lblStatus.Text = text;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _cts.Dispose();
            base.Dispose(disposing);
        }
    }
}
