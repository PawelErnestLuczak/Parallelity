using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Numerics;
using System.Reflection;
using System.IO;
using Parallelity.Logs;
using Parallelity.Tasks;
using Parallelity.Converters;
using Parallelity.OperatingSystem;

namespace Parallelity.Windows.Forms
{
    public partial class MainForm : Form
    {
        private List<dynamic> Tasks;
        private dynamic SelectedTask;

        public MainForm()
        {
            InitializeComponent();

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

            Tasks = new List<dynamic>();
            SelectedTask = null;

            toolStripComboBox1.Items.AddRange(ParallelPlatformExtension.Platforms.Select(platform => platform.DisplayName()).ToArray());
            toolStripComboBox1.SelectedIndex = ParallelPlatformExtension.Platforms.IndexOf(ParallelPlatform.PlatformMPI);

            dataGridView1.DataSource = LogController.logs;
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
            }
            catch (Exception exc)
            {
                toolStripStatusLabel1.Visible = false;
                MessageBox.Show(exc.Message, "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void zamknijToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
