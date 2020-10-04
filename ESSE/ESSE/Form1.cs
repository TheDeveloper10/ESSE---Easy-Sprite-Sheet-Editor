using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ESSE
{
    public partial class ESSE_Form : Form
    {
        private uint deallocs = 0;

        public ESSE_Form()
        {
            InitializeComponent();
        }

        #region All Special Methods
        /// <summary>
        /// This method is called when the image deallocations are many and the memory isn't cleaned up
        /// </summary>
        private void GarbageCollect(int neededDeallocs = 5)
        {
            if (deallocs > neededDeallocs)
            {
                GC.Collect();
                deallocs = 0;
            }
        }

        private void ImageDispose(Image img)
        {
            img.Dispose();
            ++deallocs;
            GarbageCollect();
        }

        private void ImageDispose(params Image[] imgs)
        {
            deallocs += (uint)imgs.Length;
            for (int i = 0; i < imgs.Length; ++i)
                imgs[i].Dispose();
            GarbageCollect();
        }

        #region Item Moving
        private enum MoveDirection { Up = 0, Down = 1 };

        /// <summary>
        /// Moves an item up/down in the hierarchy(Hierarchy_CLB)
        /// </summary>
        private void MoveHierarchyItem(MoveDirection dir)
        {
            int selectedIndx = Hierarchy_CLB.SelectedIndex, dirI = 0;
            if (dir == MoveDirection.Up)
            {
                if (selectedIndx == -1 || selectedIndx == 0)
                    return;
                dirI = -1;
            }
            else
            {
                if (selectedIndx == -1 || selectedIndx == Hierarchy_CLB.Items.Count - 1)
                    return;
                dirI = 1;
            }

            object a = Hierarchy_CLB.Items[selectedIndx + dirI];
            bool b = Hierarchy_CLB.GetItemChecked(selectedIndx + dirI);

            Hierarchy_CLB.Items[selectedIndx + dirI] = Hierarchy_CLB.Items[selectedIndx];
            Hierarchy_CLB.SetItemChecked(selectedIndx + dirI, Hierarchy_CLB.GetItemChecked(selectedIndx));
            Hierarchy_CLB.Items[selectedIndx] = a;
            Hierarchy_CLB.SetItemChecked(selectedIndx, b);

            Hierarchy_CLB.ClearSelected();
            Hierarchy_CLB.SelectedIndex = selectedIndx + dirI;
        }

        /// <summary>
        /// Moves an item in listBox1 up/down
        /// </summary>
        private void MoveLBItem(MoveDirection dir)
        {
            int selectedIndx = listBox1.SelectedIndex, dirI = 0;
            if (dir == MoveDirection.Up)
            {
                if (selectedIndx == -1 || selectedIndx == 0)
                    return;
                dirI = -1;
            }
            else
            {
                if (selectedIndx == -1 || selectedIndx == paths.Count - 1)
                    return;
                dirI = 1;
            }

            object a = listBox1.Items[selectedIndx + dirI];
            listBox1.Items[selectedIndx + dirI] = listBox1.Items[selectedIndx];
            listBox1.Items[selectedIndx] = a;

            string a2 = paths[selectedIndx + dirI];
            paths[selectedIndx + dirI] = paths[selectedIndx];
            paths[selectedIndx] = a2;

            listBox1.ClearSelected();
            listBox1.SelectedIndex = selectedIndx + dirI;
        }
        #endregion
        
        /// <summary>
        /// Crops an image from another image based on coords
        /// </summary>
        private Bitmap CropImg(Bitmap bmp, int startX, int startY, int width, int height)
        {
            // the only methods that are allowed to call this one
            // are based to never need to check everything in here
            // so this is why there's no ifs

            Bitmap target = new Bitmap(width, height, bmp.PixelFormat);

            using (Graphics g = Graphics.FromImage(target))
            {
                g.DrawImage(bmp, new Rectangle(0, 0, width, height),
                                 new Rectangle(startX, startY, width, height),
                                 GraphicsUnit.Pixel);
            }

            return target;
        }

        /// <summary>
        /// Updates label13 or the label that tells you the information about how many files are loaded
        /// </summary>
        private void UpdateLoadedFilesLabel()
        {
            label13.Text = "Loaded: " + paths.Count;
        }

        /// <summary>
        /// Updates all columns, rows and resolution in the output group box
        /// </summary>
        private void ColAndRowNumChangeAndLabelChange()
        {
            int c = listBox1.Items.Count;
            if(c == 0)
            {
                label13.Text = "Loaded: 0";
                return;
            }
            int a = (int)Math.Sqrt(c);
            int b = c / a;

            NoCols_NUD.Value = (int)a;
            NoRows_NUD.Value = (int)b;

            label13.Text = "Loaded: " + c;
        }

        /// <summary>
        /// Updates the columns, rows and resolution in the output group box
        /// </summary>
        private void UpdateOutputColsRowsAndRes()
        {
            ColAndRowNumChangeAndLabelChange();
            
            Tuple<int, int> res = GetProcessedResolutionBasedOnHierarchy();
            ExportWidth_NUD.Value = res.Item1 * NoCols_NUD.Value;
            ExportHeight_NUD.Value = res.Item2 * NoRows_NUD.Value;
        }
        
        /// <summary>
        /// Loads all images to the listBox and in the list paths.
        /// If both paths and listBox have values in them the
        /// new values get added.
        /// </summary>
        private void LoadImages(string[] files)
        {
            paths.AddRange(files);
            for (int i = 0; i < files.Length; ++i)
                listBox1.Items.Add(Path.GetFileName(files[i]));
            UpdateLoadedFilesLabel();
            ColAndRowNumChangeAndLabelChange();
        }

        /// <summary>
        /// Sets image to preview picture box
        /// </summary>
        private void SetPreview(Image img, bool backup = false)
        {
            if (Preview_PicBox.Image != null)
                ImageDispose(Preview_PicBox.Image);

            if (backup)
                UpdateBackupImage(img);
            Preview_PicBox.Image = img;
            
            UpdatePreviewResoltuionLabel();
        }

        private Image backupImg = null;
        /// <summary>
        /// Backs up the passed image by creating a copy of it
        /// </summary>
        private void UpdateBackupImage(Image img)
        {
            if (backupImg != null)
                ImageDispose(backupImg);

            if (img != null)
                backupImg = (Image)img.Clone();
        }

        /// <summary>
        /// Updates the resolution label(label4) in Preview group box
        /// </summary>
        private void UpdatePreviewResoltuionLabel()
        {
            if (Preview_PicBox.Image != null)
                label4.Text = Preview_PicBox.Image.Width + "x" + Preview_PicBox.Image.Height;
            else
                label4.Text = "No preview image available!";
        }

        /// <summary>
        /// Sets backup image to preview
        /// </summary>
        private void SetBackupImageToPreview()
        {
            if (backupImg != null)
                SetPreview((Image)backupImg.Clone(), false);
            else
                SetPreview(null, false);
        }

        #region Some Image Processing
        /// <summary>
        /// Process an image based on the hierarcy
        /// </summary>
        private Bitmap ProcessImage(Image input)
        {
            if (!PP_CB.Checked)
            {
                UpdateOutputColsRowsAndRes();
                if (input != null)
                    return new Bitmap(input);
                return null;
            }
            if (input == null)
                return null;

            Bitmap result = new Bitmap(input);

            // this way it's easier to add a new processing effect
            for (int i = 0; i < Hierarchy_CLB.Items.Count; ++i)
            {
                if (!Hierarchy_CLB.GetItemChecked(i))
                    continue;

                switch (Hierarchy_CLB.Items[i].ToString())
                {
                    case "Crop":
                        {
                            if (CWidth_NUD.Value == 0 || CHeight_NUD.Value == 0)
                                continue;

                            Bitmap c = CropImg(result,
                                                (int)StartX_NUD.Value, (int)StartY_NUD.Value,
                                                (int)CWidth_NUD.Value, (int)CHeight_NUD.Value);
                            result.Dispose();
                            ++deallocs;
                            result = c;

                            break;
                        }
                    case "Resize":
                        {
                            if (Width_NumUpDown.Value == 0 || Height_NumUpDown.Value == 0)
                                continue;

                            Bitmap r = new Bitmap(result,
                                                    (int)Width_NumUpDown.Value, (int)Height_NumUpDown.Value);
                            result.Dispose();
                            ++deallocs;
                            result = r;

                            break;
                        }
                }
            }
            GarbageCollect();
            
            ExportWidth_NUD.Value = NoCols_NUD.Value * result.Width;
            ExportHeight_NUD.Value = NoRows_NUD.Value * result.Height;

            return result;
        }

        /// <summary>
        /// Gets processed image resolution based on hierarchy
        /// </summary>
        private Tuple<int, int> GetProcessedResolutionBasedOnHierarchy()
        {
            Tuple<int, int> res = null;

            for (int i = 0; i < Hierarchy_CLB.Items.Count; ++i)
            {
                if (!Hierarchy_CLB.GetItemChecked(i))
                    continue;

                switch (Hierarchy_CLB.Items[i].ToString())
                {
                    case "Crop":
                        {
                            if (CWidth_NUD.Value == 0 || CHeight_NUD.Value == 0)
                                continue;

                            res = new Tuple<int, int>((int)CWidth_NUD.Value, (int)CHeight_NUD.Value);
                            break;
                        }
                    case "Resize":
                        {
                            if (Width_NumUpDown.Value == 0 || Height_NumUpDown.Value == 0)
                                continue;

                            res = new Tuple<int, int>((int)Width_NumUpDown.Value, (int)Height_NumUpDown.Value);
                            break;
                        }
                }
            }

            if(res == null)
            {
                if(backupImg != null)
                    res = new Tuple<int, int>(backupImg.Width, backupImg.Height);
                else
                {
                    if(paths.Count != 0)
                    {
                        Image img = Image.FromFile(paths[0]);
                        res = new Tuple<int, int>(img.Width, img.Height);
                        ImageDispose(img);
                    }
                    else
                        res = new Tuple<int, int>(0, 0);
                }
            }
            return res;
        }
        #endregion

        /// <summary>
        /// Returns all selected indices in listBox1
        /// </summary>
        private int[] GetSelectedListBoxIndices()
        {
            ListBox.SelectedIndexCollection selectedIndicesCol = listBox1.SelectedIndices;
            
            int[] selectedIndices = new int[selectedIndicesCol.Count];
            for (int i = 0; i < selectedIndices.Length; ++i)
                selectedIndices[i] = selectedIndicesCol[i];
            Array.Sort(selectedIndices); // sorts array in asc order
            Array.Reverse(selectedIndices); // the array is in desc order
            selectedIndicesCol.Clear();
            return selectedIndices;
        }

        /// <summary>
        /// Removes an item from the listBox1
        /// </summary>
        private void RemoveSelectedInListBox()
        {
            int[] selectedIndices = GetSelectedListBoxIndices();
            if (selectedIndices.Length == 0)
                return;

            for (int i = 0; i < selectedIndices.Length; ++i)
            {
                listBox1.Items.RemoveAt(selectedIndices[i]);
                paths.RemoveAt(selectedIndices[i]);
            }
            if (Array.IndexOf(selectedIndices, loadedIndex) != -1)
            {
                loadedIndex = -1;
                SetPreview(null, true);
            }

            ColAndRowNumChangeAndLabelChange();
            if (listBox1.Items.Count == 0)
                return;

            int si = selectedIndices[selectedIndices.Length - 1] - 1;
            if (si > listBox1.Items.Count)
                si = listBox1.Items.Count;
            if (si < 0)
                si = 0;
            listBox1.SelectedIndex = si;
        }
        #endregion

        #region Events
        private void Form1_Load(object sender, EventArgs e)
        {
            // setting all tabs to true
            for (int i = 0; i < Hierarchy_CLB.Items.Count; ++i)
                Hierarchy_CLB.SetItemChecked(i, true);
        }

        private List<string> paths = new List<string>();
        private void LoadImgsBtn_Click(object sender, EventArgs e)
        {
            using(OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image Files(*.bmp;*.jpg;*.jpeg;*.png;)|*.bmp;*.jpg;*.jpeg;*.png;";
                ofd.Title = "Image Browser";
                ofd.Multiselect = true;
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        LoadImages(ofd.FileNames);
                    }
                    catch(Exception exc) {
                        MessageBox.Show(exc.Message, "Error occurred!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }
        }

        private int loadedIndex = -1;
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex == -1)
            {
                SetPreview(null);
                return;
            }

            loadedIndex = listBox1.SelectedIndex;
            UpdateBackupImage(Image.FromFile(paths[loadedIndex]));
            SetPreview(ProcessImage(backupImg));
        }
        
        private void Remove_Selected_Click(object sender, EventArgs e)
        {
            RemoveSelectedInListBox();
            listBox1.Focus();
        }

        private void ScalePreviewImageDown_CB_CheckedChanged(object sender, EventArgs e)
        {
            Preview_PicBox.SizeMode = ScalePreviewImageDown_CB.Checked ? PictureBoxSizeMode.StretchImage : PictureBoxSizeMode.Normal;
        }

        // Post_Processing_Check_Box_Checked_Change
        private void PP_CB_CheckedChanged(object sender, EventArgs e)
        {
            if (!PP_CB.Checked)
                SetBackupImageToPreview();
            else if (backupImg != null)
                SetPreview(ProcessImage(backupImg));
            else
                SetPreview(null);
        }

        #region Input Image Processing Property Change
        private void InputSizeChangeNUD(object sender, EventArgs e)
        {
            SetPreview(ProcessImage(backupImg));
        }

        private void CropChangeNUD(object sender, EventArgs e)
        {
            SetPreview(ProcessImage(backupImg));
        }

        private void Hierarchy_CLB_SelectedValueChanged(object sender, EventArgs e)
        {
            SetPreview(ProcessImage(backupImg));
        }
        #endregion

        #region Moving Items Up/Down
        private void Move_UP_H_Click(object sender, EventArgs e)
        {
            MoveHierarchyItem(MoveDirection.Up);
        }

        private void Move_DOWN_H_Click(object sender, EventArgs e)
        {
            MoveHierarchyItem(MoveDirection.Down);
        }

        private void LB_Move_Up_Click(object sender, EventArgs e)
        {
            MoveLBItem(MoveDirection.Up);
        }

        private void LB_Move_Down_Click(object sender, EventArgs e)
        {
            MoveLBItem(MoveDirection.Down);
        }
        #endregion

        #region Exporting
        private void ProcessAndExport_Btn_Click(object sender, EventArgs e)
        {
            int cols = (int)NoCols_NUD.Value, rows = (int)NoRows_NUD.Value;
            if (cols * rows != listBox1.Items.Count)
            {
                int r = (int)(listBox1.Items.Count - cols * rows);

                if (r > 0 && MessageBox.Show("The export won't include the last " +
                                r +
                                " images!\r\nAre you sure you want to continue?", "Warning!",
                                MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
                    == DialogResult.No)
                    return;
                else if(r < 0)
                {
                    MessageBox.Show("Please decrease the number of rows/columns so\r\nit matches(or is less than) the number of images(" + listBox1.Items.Count + ")!", "Error occurred!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            if (listBox1.Items.Count == 0)
            {
                MessageBox.Show("No images are loaded!", "Warning!",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Tuple<int, int> processedRes = GetProcessedResolutionBasedOnHierarchy();
            int defWidth = processedRes.Item1 * (int)NoCols_NUD.Value, defHeight = processedRes.Item2 * (int)NoRows_NUD.Value;
            int finalWidth = (int)ExportWidth_NUD.Value, finalHeight = (int)ExportHeight_NUD.Value;

            if (finalWidth == 0)
                finalWidth = defWidth;
            if (finalHeight == 0)
                finalHeight = defHeight;
            Bitmap export = new Bitmap(finalWidth, finalHeight);

            float widthCoeff = (float)finalWidth / (float)defWidth, 
                heightCoeff = (float)finalHeight / (float)defHeight;

            if (export == null)
                return;

            using (Graphics g = Graphics.FromImage(export))
            {
                float i = 0f, j = 0f;
                int item = 0;
                for (; i < rows; ++i)
                {
                    for (j = 0f; j < cols; ++j)
                    {
                        Image loaded = Image.FromFile(paths[item]);
                        Image processed = ProcessImage(loaded);
                        Bitmap drawn = new Bitmap(processed, 
                            (int)(processed.Width * widthCoeff), (int)(processed.Height * heightCoeff));

                        g.DrawImage(drawn, (j / (float)cols) * export.Width, (i / (float)rows) * export.Height);

                        ImageDispose(loaded, processed, drawn);
                        ++item;
                    }
                }
            }
            
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "Image Save";
                sfd.Filter = "PNG|*.png|JPG|*.jpg|BMP|*.bmp";
                if(sfd.ShowDialog() == DialogResult.OK)
                {
                    export.Save(sfd.FileName);
                }
            }
            ImageDispose(export);
        }
        #endregion

        #region Follow Me Group Box (Socials)
        private void Socials_OnMouseEnter(object sender, EventArgs e)
        {
            Control c = (Control)sender;
            c.Size = new Size(55, 55);
            c.Cursor = Cursors.Hand;
        }

        private void Socials_OnMouseLeave(object sender, EventArgs e)
        {
            Control c = (Control)sender;
            c.Size = new Size(51, 51);
            c.Cursor = Cursors.Arrow;
        }

        private void Socials_OnMouseClick(object sender, MouseEventArgs e)
        {
            Control c = (Control)sender;
            switch (c.Name)
            {
                case "YT_PicBox":
                    System.Diagnostics.Process.Start("https://www.youtube.com/channel/UCwO0k5dccZrTW6-GmJsiFrg");
                    break;
                case "IG_PicBox":
                    System.Diagnostics.Process.Start("https://www.instagram.com/thedeveloper10/");
                    break;
                case "T_PicBox":
                    System.Diagnostics.Process.Start("https://twitter.com/the_developer10");
                    break;
                case "FCB_PicBox":
                    System.Diagnostics.Process.Start("https://www.facebook.com/VicTor-372230180173180");
                    break;
                case "LIN_PicBox":
                    System.Diagnostics.Process.Start("https://www.linkedin.com/company/65346254");
                    break;
                case "GH_PicBox":
                    System.Diagnostics.Process.Start("https://www.linkedin.com/company/65346254");
                    break;
                case "W_PicBox":
                    System.Diagnostics.Process.Start("https://thedevelopers.tech/");
                    break;
            }
        }
        #endregion
        #endregion
    }
}
