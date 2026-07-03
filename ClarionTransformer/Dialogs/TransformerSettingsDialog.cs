using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ClarionTransformer.Services;

namespace ClarionTransformer.Dialogs
{
    public class TransformerSettingsDialog : Form
    {
        private TransformerSettings _settings;
        private TransformerProfile  _current;
        private bool _initializing = true;

        private ComboBox _cboProfiles;
        private Button   _btnNewProfile, _btnDelProfile, _btnRenameProfile;
        private TextBox   _txtApiKey, _txtAiModel, _txtProtocolFile, _txtAiExtra;
        private CheckBox  _chkBackup, _chkComments, _chkReindent;
        private NumericUpDown _numIndentSpaces;

        public TransformerSettingsDialog()
        {
            _settings = TransformerProfileService.Load();
            _current  = TransformerProfileService.GetActiveProfile();
            BuildUI();
            _initializing = true;
            RefreshProfileList();
            LoadValues();
            _initializing = false;
        }

        private void BuildUI()
        {
            Text            = "ClarionTransformer - Configuracion";
            Size            = new Size(560, 560);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            StartPosition   = FormStartPosition.CenterParent;

            // ── Profile bar ────────────────────────────────────────────────────
            var profilePanel = new Panel { Dock = DockStyle.Top, Height = 38, Padding = new Padding(4) };
            profilePanel.Controls.Add(new Label { Text = "Perfil:", Location = new Point(6, 10), AutoSize = true });
            _cboProfiles = new ComboBox { Location = new Point(50, 6), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboProfiles.SelectedIndexChanged += (s, e) => OnProfileSelected();

            _btnNewProfile    = MakeSmallBtn("Nuevo",     new Point(258, 6)); _btnNewProfile.Click    += OnNewProfile;
            _btnRenameProfile = MakeSmallBtn("Renombrar", new Point(318, 6)); _btnRenameProfile.Click += OnRenameProfile;
            _btnDelProfile    = MakeSmallBtn("Eliminar",  new Point(400, 6)); _btnDelProfile.Click    += OnDeleteProfile;

            profilePanel.Controls.AddRange(new Control[] { _cboProfiles, _btnNewProfile, _btnRenameProfile, _btnDelProfile });

            // ── Content panel ──────────────────────────────────────────────────
            var content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 8, 12, 8) };
            int y = 12;

            AddSectionLabel(content, "Conexion con la API de Anthropic (global)", ref y);

            content.Controls.Add(new Label { Text = "API Key (sk-ant-...):", Location = new Point(12, y + 2), AutoSize = true });
            _txtApiKey = new TextBox { Location = new Point(200, y), Width = 310, PasswordChar = '*' };
            content.Controls.Add(_txtApiKey);
            y += 28;

            content.Controls.Add(new Label { Text = "Modelo:", Location = new Point(12, y + 2), AutoSize = true });
            _txtAiModel = new TextBox { Location = new Point(200, y), Width = 310 };
            content.Controls.Add(_txtAiModel);
            y += 22;

            var lblModels = new Label
            {
                Text      = "Recomendado: claude-sonnet-4-6   Rapido/economico: claude-haiku-4-5-20251001",
                Location  = new Point(200, y), Width = 310, Height = 18,
                Font      = new Font(Font, FontStyle.Italic), ForeColor = Color.Gray
            };
            content.Controls.Add(lblModels);
            y += 28;

            AddSectionLabel(content, "Archivo de protocolo (por perfil)", ref y);

            content.Controls.Add(new Label { Text = "Ruta del archivo:", Location = new Point(12, y + 2), AutoSize = true });
            _txtProtocolFile = new TextBox { Location = new Point(200, y), Width = 240 };
            content.Controls.Add(_txtProtocolFile);

            var btnBrowse = new Button { Text = "...", Location = new Point(445, y - 1), Width = 28, Height = 22 };
            btnBrowse.Click += OnBrowseProtocol;
            content.Controls.Add(btnBrowse);

            var btnEdit = new Button { Text = "Editar", Location = new Point(477, y - 1), Width = 55, Height = 22 };
            btnEdit.Click += OnEditProtocol;
            content.Controls.Add(btnEdit);
            y += 22;

            var lblProto = new Label
            {
                Text      = "Si no se indica, usa Protocolo_ClarionTransformer.md en %APPDATA%\\ClarionAssistant\\",
                Location  = new Point(200, y), Width = 330, Height = 28,
                Font      = new Font(Font, FontStyle.Italic), ForeColor = Color.Gray
            };
            content.Controls.Add(lblProto);
            y += 34;

            AddSectionLabel(content, "Backup (por perfil)", ref y);

            _chkBackup = new CheckBox
            {
                Text     = "Guardar codigo original y transformado (Old/New) antes de aplicar",
                Location = new Point(12, y),
                AutoSize = true
            };
            content.Controls.Add(_chkBackup);
            y += 18;

            var lblBackup = new Label
            {
                Text      = "Se guardan en _ClarionTransformerBackups\\ dentro de la solucion Clarion abierta.",
                Location  = new Point(30, y), Width = 480, Height = 16,
                Font      = new Font(Font, FontStyle.Italic), ForeColor = Color.Gray
            };
            content.Controls.Add(lblBackup);
            y += 26;

            _chkComments = new CheckBox
            {
                Text     = "Agregar comentarios explicativos en bloques complejos",
                Location = new Point(12, y),
                AutoSize = true
            };
            content.Controls.Add(_chkComments);
            y += 18;

            var lblComments = new Label
            {
                Text      = "Claude agrega comentarios solo donde el funcionamiento no sea obvio, sin tocar el resto.",
                Location  = new Point(30, y), Width = 480, Height = 16,
                Font      = new Font(Font, FontStyle.Italic), ForeColor = Color.Gray
            };
            content.Controls.Add(lblComments);
            y += 26;

            _chkReindent = new CheckBox
            {
                Text     = "Reindentar automaticamente el bloque transformado",
                Location = new Point(12, y),
                AutoSize = true
            };
            content.Controls.Add(_chkReindent);

            content.Controls.Add(new Label { Text = "Espacios por nivel:", Location = new Point(330, y + 2), AutoSize = true });
            _numIndentSpaces = new NumericUpDown
            {
                Location = new Point(455, y), Width = 50,
                Minimum  = 1, Maximum = 8, Value = 4
            };
            content.Controls.Add(_numIndentSpaces);
            y += 18;

            var lblReindent = new Label
            {
                Text      = "Claude corrige la indentacion Clarion (IF/LOOP/CASE/END, etc.) de todo el bloque devuelto.",
                Location  = new Point(30, y), Width = 480, Height = 16,
                Font      = new Font(Font, FontStyle.Italic), ForeColor = Color.Gray
            };
            content.Controls.Add(lblReindent);
            y += 26;

            AddSectionLabel(content, "Instrucciones adicionales para Claude (por perfil)", ref y);

            _txtAiExtra = new TextBox
            {
                Location      = new Point(12, y),
                Size          = new Size(515, 80),
                Multiline     = true,
                ScrollBars    = ScrollBars.Vertical,
                WordWrap      = true,
                AcceptsReturn = true
            };
            content.Controls.Add(_txtAiExtra);

            // ── Button bar ──────────────────────────────────────────────────────
            var btnPanel = new Panel { Dock = DockStyle.Bottom, Height = 40 };

            var btnOk = new Button { Text = "Guardar", DialogResult = DialogResult.OK };
            btnOk.Size = new Size(90, 26); btnOk.Location = new Point(358, 7);
            btnOk.Click += (s, e) => SaveValues();

            var btnCancel = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel };
            btnCancel.Size = new Size(90, 26); btnCancel.Location = new Point(454, 7);

            var pathLabel = new Label
            {
                Text         = "Config: " + TransformerProfileService.SettingsPath,
                Dock         = DockStyle.Bottom,
                Height       = 18,
                Font         = new Font("Courier New", 7f),
                ForeColor    = Color.Gray,
                AutoEllipsis = true
            };

            btnPanel.Controls.AddRange(new Control[] { btnOk, btnCancel });

            Controls.Add(content);
            Controls.Add(profilePanel);
            Controls.Add(pathLabel);
            Controls.Add(btnPanel);
            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        private Button MakeSmallBtn(string text, Point loc)
            => new Button { Text = text, Size = new Size(74, 26), Location = loc };

        private void AddSectionLabel(Panel page, string text, ref int y)
        {
            var lbl = new Label
            {
                Text      = "-- " + text + " --",
                Location  = new Point(12, y),
                AutoSize  = true,
                Font      = new Font(Font, FontStyle.Bold),
                ForeColor = Color.DarkBlue
            };
            page.Controls.Add(lbl);
            y += 22;
        }

        // ── Profile management ────────────────────────────────────────────────

        private void RefreshProfileList()
        {
            _cboProfiles.Items.Clear();
            foreach (var p in _settings.Profiles)
                _cboProfiles.Items.Add(p.ProfileName);
            int idx = _cboProfiles.Items.IndexOf(_settings.ActiveProfile);
            _cboProfiles.SelectedIndex = idx >= 0 ? idx : 0;
        }

        private void OnProfileSelected()
        {
            if (_initializing || _cboProfiles.SelectedIndex < 0) return;
            ReadFormInto(_current);
            string name = _cboProfiles.SelectedItem.ToString();
            _current = _settings.Profiles.FirstOrDefault(p => p.ProfileName == name) ?? _settings.Profiles[0];
            _settings.ActiveProfile = _current.ProfileName;
            LoadValues();
        }

        private void OnNewProfile(object s, EventArgs e)
        {
            string name = PromptString("Nombre del nuevo perfil:", "Nuevo perfil");
            if (string.IsNullOrWhiteSpace(name)) return;
            if (_settings.Profiles.Any(p => p.ProfileName == name))
            { MessageBox.Show("Ya existe un perfil con ese nombre."); return; }
            var newP = new TransformerProfile
            {
                ProfileName    = name,
                AiProtocolFile = _current.AiProtocolFile,
                CreateBackup   = _current.CreateBackup,
                AddComments    = _current.AddComments,
                Reindent       = _current.Reindent,
                IndentSpaces   = _current.IndentSpaces
            };
            _settings.Profiles.Add(newP);
            _settings.ActiveProfile = name;
            _current = newP;
            RefreshProfileList();
        }

        private void OnRenameProfile(object s, EventArgs e)
        {
            string name = PromptString("Nuevo nombre:", "Renombrar perfil", _current.ProfileName);
            if (string.IsNullOrWhiteSpace(name) || name == _current.ProfileName) return;
            if (_settings.Profiles.Any(p => p.ProfileName == name))
            { MessageBox.Show("Ya existe un perfil con ese nombre."); return; }
            _current.ProfileName = name;
            _settings.ActiveProfile = name;
            RefreshProfileList();
        }

        private void OnDeleteProfile(object s, EventArgs e)
        {
            if (_settings.Profiles.Count <= 1)
            { MessageBox.Show("No se puede eliminar el unico perfil."); return; }
            if (MessageBox.Show("Eliminar perfil \"" + _current.ProfileName + "\"?",
                "Confirmar", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            _settings.Profiles.Remove(_current);
            _settings.ActiveProfile = _settings.Profiles[0].ProfileName;
            _current = _settings.Profiles[0];
            RefreshProfileList();
            LoadValues();
        }

        // ── Protocol file ─────────────────────────────────────────────────────

        private void OnBrowseProtocol(object s, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title  = "Seleccionar archivo de protocolo";
                dlg.Filter = "Markdown (*.md)|*.md|Texto (*.txt)|*.txt|Todos (*.*)|*.*";
                if (!string.IsNullOrWhiteSpace(_txtProtocolFile.Text))
                    try { dlg.InitialDirectory = System.IO.Path.GetDirectoryName(_txtProtocolFile.Text); } catch { }
                if (dlg.ShowDialog() == DialogResult.OK)
                    _txtProtocolFile.Text = dlg.FileName;
            }
        }

        private void OnEditProtocol(object s, EventArgs e)
        {
            string path = _txtProtocolFile.Text.Trim();
            if (string.IsNullOrWhiteSpace(path))
            {
                // Ofrecer abrir el predeterminado
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                path = System.IO.Path.Combine(appData, "ClarionAssistant", "Protocolo_ClarionTransformer.md");
                if (!System.IO.File.Exists(path))
                { MessageBox.Show("No hay archivo de protocolo configurado ni existe el predeterminado.\n\n" + path, "Editar protocolo", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            }
            if (!System.IO.File.Exists(path))
            { MessageBox.Show("El archivo no existe:\n" + path, "Editar protocolo", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            try { System.Diagnostics.Process.Start(path); }
            catch (Exception ex) { MessageBox.Show("No se pudo abrir el archivo:\n" + ex.Message, "Editar protocolo", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        // ── Load / Save ───────────────────────────────────────────────────────

        private void LoadValues()
        {
            _txtApiKey.Text       = _settings.AnthropicApiKey ?? "";
            _txtAiModel.Text      = _settings.AiModel ?? "claude-sonnet-4-6";
            _txtProtocolFile.Text = _current.AiProtocolFile ?? "";
            _txtAiExtra.Text      = _current.AiExtraInstructions ?? "";
            _chkBackup.Checked      = _current.CreateBackup;
            _chkComments.Checked    = _current.AddComments;
            _chkReindent.Checked    = _current.Reindent;
            _numIndentSpaces.Value  = Math.Max(_numIndentSpaces.Minimum, Math.Min(_numIndentSpaces.Maximum, _current.IndentSpaces));
        }

        private void ReadFormInto(TransformerProfile p)
        {
            _settings.AnthropicApiKey = _txtApiKey.Text.Trim();
            _settings.AiModel         = _txtAiModel.Text.Trim();
            p.AiProtocolFile          = _txtProtocolFile.Text.Trim();
            p.AiExtraInstructions     = _txtAiExtra.Text;
            p.CreateBackup            = _chkBackup.Checked;
            p.Reindent                = _chkReindent.Checked;
            p.IndentSpaces            = (int)_numIndentSpaces.Value;
            p.AddComments             = _chkComments.Checked;
        }

        private void SaveValues()
        {
            ReadFormInto(_current);
            TransformerProfileService.Save(_settings);
            TransformerProfileService.Invalidate();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static string PromptString(string prompt, string title, string defaultVal = "")
        {
            var f = new Form { Text = title, Size = new Size(340, 120),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent, MaximizeBox = false };
            f.Controls.Add(new Label { Text = prompt, Location = new Point(10, 10), AutoSize = true });
            var tb = new TextBox { Location = new Point(10, 30), Width = 300, Text = defaultVal };
            var ok = new Button { Text = "OK",       Location = new Point(140, 58), DialogResult = DialogResult.OK };
            var ca = new Button { Text = "Cancelar", Location = new Point(228, 58), DialogResult = DialogResult.Cancel };
            f.Controls.AddRange(new Control[] { tb, ok, ca });
            f.AcceptButton = ok; f.CancelButton = ca;
            return f.ShowDialog() == DialogResult.OK ? tb.Text.Trim() : null;
        }
    }
}
