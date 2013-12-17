using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Numerics;
using System.Reflection;
using System.IO;
using Parallelity.Logs;
using Parallelity.Tasks;
using Parallelity.Converters;
using Parallelity.OperatingSystem;
using Parallelity.Facebook;

namespace Parallelity.Windows.Forms
{
    public partial class MainForm : Form, IMessageFilter
    {
        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);
        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(Point pt);
        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);

        private const int HotKeyModifierCtrl = 2;
        private const int HotKeyIdCopy = 1;

        private List<dynamic> Tasks;
        private dynamic SelectedTask;

        public MainForm()
        {
            InitializeComponent();

            Application.AddMessageFilter(this);

            if (!RegistryValidator.IsRegistryCompatible())
            {
                DialogResult result = MessageBox.Show(
                    "Wykryto niekorzystne ustawienia systemu. Do poprawnego działania aplikacji konieczne jest, aby limit czasu wykonywania jądra karty graficznej był wyłączony.\r\n\r\nCzy chcesz wprowadzić poprawkę do rejestru?",
                    "Komunikat",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1);

                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    RegistryValidator.FixRegistry();
                    MessageBox.Show("Zmiany zostały pomyślnie zapisane w rejestrze. Aby kontynuować, konieczny jest restart systemu.",
                        "Sukces",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }

            RegisterHotKey(this.Handle, HotKeyIdCopy, HotKeyModifierCtrl, (int)'C');

            Tasks = new List<dynamic>();
            SelectedTask = null;

            toolStripComboBox1.Items.AddRange(ParallelPlatformExtension.Platforms.Select(platform => platform.DisplayName()).ToArray());
            toolStripComboBox1.SelectedIndex = ParallelPlatformExtension.Platforms.IndexOf(ParallelPlatform.PlatformMPI);

            dataGridView1.DataSource = LogController.logs;

            pictureBox1.MouseWheel += new MouseEventHandler(pictureBox1_MouseWheel);
        }

        private TreeNode loadLibrary(String libraryPath)
        {
            Assembly dllAssembly = Assembly.LoadFile(libraryPath);
            Type[] dllTypes = dllAssembly.GetTypes();
            Type[] taskTypes = dllTypes.Where(type => !type.IsGenericTypeDefinition && type.IsSubclassOf(typeof(ParallelTimeTask))).ToArray();

            if (taskTypes.Length > 0)
                LogController.add(LogType.Verbose, "Wczytano plik " + libraryPath);
            else
                throw new FileLoadException("Plik nie zawiera klas z zadaniami.");

            Tasks = taskTypes.Select(type => Activator.CreateInstance(type)).ToList();
            Tasks.ForEach(task =>
            {
                ((ParallelTimeTask)task).CheckpointTriggered += (timeTask, checkpoint) =>
                {
                    LogController.add(LogType.Information, checkpoint.DisplayName().ToCapital() + " dla zadania '" + timeTask.GetType().Name + "'.");
                };
            });

            TreeNode root = new TreeNode(dllAssembly.GetName().Name);
            root.Nodes.AddRange(Tasks.Select(obj => new TreeNode(obj.GetType().Name)).ToArray());
            treeView1.Nodes.Add(root);

            treeView1.AfterSelect += (s, a) =>
            {
                if (a.Node.Parent == null)
                    return;

                int count = 0;

                foreach (TreeNode node in treeView1.Nodes)
                    if (node != a.Node.Parent)
                        count += node.Nodes.Count;
                    else
                        break;

                int offset = count + a.Node.Index;

                SelectedTask = Tasks[offset];

                propertyGrid1.SelectedObject = SelectedTask.Params;
                pictureBox1.Image = SelectedTask.Result;
            };

            return root;
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    TreeNode libraryRoot = loadLibrary(openFileDialog1.FileName);

                    treeView1.ExpandAll();
                    treeView1.SelectedNode = libraryRoot.FirstNode;
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.Message, "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    toolStripButton4_Click(sender, e);
                }
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            if (SelectedTask == null)
                return;

            toolStripStatusLabel1.Text = "Przetwarzanie...";
            toolStripStatusLabel1.Visible = true;
            Application.DoEvents();

            try
            {
                pictureBox1.Image = SelectedTask.Run(ParallelPlatformExtension.Parse((String)toolStripComboBox1.SelectedItem));
                toolStripStatusLabel1.Text = "Czas przetwarzania: " + SelectedTask[ParallelExecutionCheckpointType.CheckpointPlatformDeinit].ToString();

                zapiszJakoToolStripMenuItem.Enabled = true;
                udostępnijToolStripMenuItem.Enabled = true;
            }
            catch (Exception exc)
            {
                toolStripStatusLabel1.Visible = false;

                Exception leaf;
                for (leaf = exc; leaf.InnerException != null; leaf = leaf.InnerException) ;
                MessageBox.Show(leaf.Message, "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void zamknijToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0312 && m.WParam.ToInt32() == HotKeyIdCopy)
            {
                if (pictureBox1.Image != null)
                {
                    Clipboard.SetImage(pictureBox1.Image);
                    System.Media.SystemSounds.Beep.Play();
                }
            }

            base.WndProc(ref m);
        }

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == 0x20a)
            {
                Point pos = new Point(m.LParam.ToInt32() & 0xffff, m.LParam.ToInt32() >> 16);
                IntPtr hWnd = WindowFromPoint(pos);

                if (hWnd != IntPtr.Zero && hWnd != m.HWnd && Control.FromHandle(hWnd) != null)
                {
                    SendMessage(hWnd, m.Msg, m.WParam, m.LParam);
                    return true;
                }
            }

            return false;
        }

        private void zapiszJakoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                switch (saveFileDialog1.FilterIndex)
                {
                    case 1: pictureBox1.Image.Save(saveFileDialog1.FileName, ImageFormat.Bmp); break;
                    case 2: pictureBox1.Image.Save(saveFileDialog1.FileName, ImageFormat.Png); break;
                    case 3: pictureBox1.Image.Save(saveFileDialog1.FileName, ImageFormat.Jpeg); break;
                    case 4: pictureBox1.Image.Save(saveFileDialog1.FileName, ImageFormat.Gif); break;
                }
            }
        }

        private void udostępnijToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (FacebookManager.PublishPhoto(pictureBox1.Image, "Obraz wygenerowany za pomocą aplikacji Parallelity."))
            {
                DialogResult result = MessageBox.Show(
                    "Obraz został udostępniony na Facebooku.",
                    "Komunikat",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information,
                    MessageBoxDefaultButton.Button1);
            }
        }

        private void pictureBox1_MouseWheel(object sender, MouseEventArgs e)
        {
            if (propertyGrid1.SelectedObject == null)
                return;

            PropertyInfo property = propertyGrid1.SelectedObject.GetType().GetProperty("Scale");

            if (property == null)
                return;
            
            float scale = (float)property.GetValue(propertyGrid1.SelectedObject, null);
            float newScale = scale * (e.Delta > 0 ? 1.1f : 0.9f);
            property.SetValue(propertyGrid1.SelectedObject, newScale, null);

            propertyGrid1.SelectedObject = propertyGrid1.SelectedObject;

            toolStripButton1_Click(sender, e);
        }
    }
}
