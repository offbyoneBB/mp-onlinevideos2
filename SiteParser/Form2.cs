using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Net;
using System.Text.RegularExpressions;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using OnlineVideos;
using OnlineVideos.Sites;

namespace SiteParser
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        private string result;
        private string[] fields;
        private string testData;
        bool cleanupValues = false;

        private const int CP_NOCLOSE_BUTTON = 0x200;
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams myCp = base.CreateParams;
                myCp.ClassStyle = myCp.ClassStyle | CP_NOCLOSE_BUTTON; // disable the close button (x) on top right corner, so user must chose OK or CANCEL
                return myCp;
            }
        }

        public string Execute(string regexString, string url, string[] names, bool cleanupValues, bool forceUTF8, CookieContainer cc)
        {
            string webData = WebCache.Instance.GetWebData(url, forceUTF8: forceUTF8, cookies: cc);
            return Execute(regexString, webData, url, names, cleanupValues);
        }

        public string Execute(string regexString, string webData, string url, string[] names, bool cleanupValues)
        {
            this.cleanupValues = cleanupValues;
            result = regexString;
            FillPageData(result, regexRichText);
            FillPageData(webData, richTextBox1);
            insertComboBox.Items.Clear();
            foreach (string s in names)
                insertComboBox.Items.Add(new RegexPart(s, @"(?<" + s + @">[^@@@]*)"));
            insertComboBox.Items.Add(new RegexPart("skip to", @"(?:(?!@@).)*"));
            insertComboBox.Items.Add(new RegexPart("skip to single char", @"[^@@@]*"));
            insertComboBox.Items.Add(new RegexPart("optional", @"(?:@@@@)?"));
            insertComboBox.Items.Add(new RegexPart("match after", @"(?<=@@.*)"));
            insertComboBox.Items.Add(new RegexPart("match before", @"(?<!@@.*)"));
            insertComboBox.Items.Add(new RegexPart("match not", @"(?!@@)"));
            insertComboBox.SelectedIndex = 0;
            fields = names;
            testData = webData;
            webBrowser1.Visible = (url != null);
            if (url != null)
                webBrowser1.Url = new Uri(url);
            findButton.Focus();
            ShowDialog();
            return result;
        }

        private void append2(StringBuilder sb, string part)
        {
            bool inQuote = false;
            for (int i = 0; i < part.Length; i++)
            {
                if (part[i] == '"')
                {
                    if (inQuote) sb.Append(@"\cf0 ");
                    else
                        sb.Append(@"\cf5 ");
                    inQuote = !inQuote;
                }
                sb.Append(part[i]);
            }
        }

        private void append(StringBuilder sb, string part)
        {
            if (part.StartsWith(@"<!DOCTYPE", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append(@"\cf1 ");
                sb.Append(part);
                sb.Append(@"\cf0 ");
            }
            else
            {
                sb.Append(@"\cf2 ");
                bool inQuote = false;
                bool colSet = false;
                for (int i = 0; i < part.Length; i++)
                {
                    if (part[i] == '"')
                        inQuote = !inQuote;
                    else
                        if (!inQuote)
                        {
                            if (part[i] == '=')
                            {
                                sb.Append(@"\cf4 ");
                                colSet = true;
                            }
                            if (part[i] == ' ')
                            {
                                if (i < part.Length - 3) // prevent red dash at />
                                {
                                    sb.Append(@"\cf3 ");
                                    colSet = true;
                                }
                            }
                            if (part[i] == '>')
                            {
                                if (colSet)
                                    sb.Append(@"\cf2 ");
                            }
                        }
                    sb.Append(part[i]);

                }
                sb.Append(@"\cf0 ");
            }
        }


        private void FillPageData(string textData, RichTextBox textBox)
        {
            textBox.Text = textData;
            textData = textBox.Rtf;
            int q = textData.IndexOf('{', 1);
            textData = textData.Insert(q, @"{\colortbl ;\red177\green19\blue128;\red100\green128\blue0;\red255\green65\blue0;\red58\green110\blue165;\red0\green128\blue0;}");

            StringBuilder sb = new StringBuilder();
            int i = -1;
            int j = 0;
            do
            {
                i = j != -1 ? textData.IndexOf('<', j) : -1;
                if (i != -1)
                {
                    if (i != j)
                        append2(sb, textData.Substring(j, i - j));
                    j = textData.IndexOf('>', i + 1);
                    if (j >= 0)
                    {
                        int k = textData.LastIndexOf('<', j - 1);
                        if (k != i)
                        {
                            append2(sb, textData.Substring(i, k - i));
                            i = k;
                        }
                        append(sb, textData.Substring(i, j - i + 1));
                        j++;
                    }
                    else
                    {
                        j = i;
                        i = -1;
                    }
                }
            } while (i >= 0 && (i + 1) < textData.Length);
            if (i == -1)
                append2(sb, j == -1 ? textData : textData.Substring(j, textData.Length - j));
            textBox.Rtf = sb.ToString();
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            result = regexRichText.Text;
            Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void findButton_Click(object sender, EventArgs e)
        {
            int searchStartInd = richTextBox1.SelectionStart + 1;
            int i = richTextBox1.Text.IndexOf(findTextBox.Text, searchStartInd, StringComparison.OrdinalIgnoreCase);
            if (i != -1)
            {
                searchStartInd = i + 1;
                richTextBox1.Select(i, findTextBox.Text.Length);
                richTextBox1.ScrollToCaret();
                findButton.Text = "Next";
            }
            else
            {
                richTextBox1.Select(0, 0);
                searchStartInd = 0;
                findButton.Text = "Find";
            }
        }


        private void insertBbutton_Click(object sender, EventArgs e)
        {
            checkBoxOnlySelected.Checked = false;
            string strToInsert = ((RegexPart)insertComboBox.SelectedItem).Value;
            int p = strToInsert.IndexOf(@"@@@@");
            if (p >= 0)
            {
                strToInsert = strToInsert.Substring(0, p) + regexRichText.SelectedText + strToInsert.Substring(p + 4);
                p = -1; // prevent further processing
            }
            bool replaceWithLast = false;
            if (p < 0)
            {
                p = strToInsert.IndexOf(@"@@@");
                replaceWithLast = p >= 0;
                if (p < 0) p = strToInsert.IndexOf(@"@@");
            }
            int nextInd = regexRichText.SelectionStart + regexRichText.SelectionLength;
            string nextChar = String.Empty;
            string regexRichTextString = regexRichText.Text;
            if (nextInd < regexRichTextString.Length)
            {
                nextChar = new String(regexRichTextString[nextInd], 1);
                if (nextChar == "\\" && nextInd + 1 < regexRichTextString.Length)
                    nextChar += new String(regexRichTextString[nextInd + 1], 1);
            }

            int insPos = regexRichText.SelectionStart;
            if (strToInsert.StartsWith(@"(?<=") || strToInsert.StartsWith(@"(?<!"))
            {
                insPos = 0;
                regexRichText.SelectionLength = 0;
            }
            regexRichText.SelectionStart = insPos;
            regexRichText.SelectedText = strToInsert;
            regexRichText.SelectionStart = insPos;
            regexRichText.SelectionLength = strToInsert.Length;
            regexRichText.SelectionColor = Color.Black;
            if (replaceWithLast)
            {
                insPos += p;
                regexRichText.SelectionStart = insPos;
                regexRichText.SelectionLength = 3;
                regexRichText.SelectedText = nextChar;
                regexRichText.SelectionStart = insPos;
                regexRichText.SelectionLength = 0;
            }
            else
            {
                if (p >= 0)
                {
                    regexRichText.SelectionStart = insPos + p;
                    regexRichText.SelectionLength = 2;
                }
                else
                {
                    regexRichText.SelectionStart = insPos;
                    regexRichText.SelectionLength = 0;
                }
            }

            regexRichText.Focus();
        }

        private void populateTree(string regexText, bool showMessage)
        {
            if (String.IsNullOrEmpty(regexText))
            {
                treeView1.BeginUpdate();
                treeView1.Nodes.Clear();
                treeView1.EndUpdate();
                return;
            }

            treeView1.BeginUpdate();
            treeView1.Nodes.Clear();
            int charpos = richTextBox1.GetCharIndexFromPosition(new System.Drawing.Point(1, 1));
            bool highlightMatches = checkBoxHighlightMatches.Checked;
            try
            {
                Regex test = new Regex(regexText, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
                Match m;
                if (highlightMatches)
                {
                    LockWindowUpdate(richTextBox1.Handle);

                    richTextBox1.SelectAll();
                    richTextBox1.SelectionBackColor = richTextBox1.BackColor;
                    m = test.Match(richTextBox1.Text);
                }
                else
                    m = test.Match(testData);
                while (m.Success)
                {
                    if (highlightMatches)
                    {
                        richTextBox1.Select(m.Index, m.Length);
                        richTextBox1.SelectionBackColor = Color.White;
                    }
                    TreeNode node = treeView1.Nodes.Add(treeView1.Nodes.Count.ToString());
                    foreach (String field in fields)
                    {
                        string val = m.Groups[field].Value;
                        if (cleanupValues)
                            val = OnlineVideos.Helpers.StringUtils.PlainTextFromHtml(val);
                        node.Nodes.Add(field + " " + val);
                        node.Expand();
                    }
                    m = m.NextMatch();
                }
            }
            catch (Exception ex)
            {
                if (showMessage)
                    MessageBox.Show(ex.Message);
            }
            if (highlightMatches)
            {
                richTextBox1.Select(charpos, 0);
                richTextBox1.ScrollToCaret();

                LockWindowUpdate(IntPtr.Zero);
            }
            treeView1.EndUpdate();
        }

        private void testButton_Click(object sender, EventArgs e)
        {

            populateTree(regexRichText.Text, true);
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool LockWindowUpdate(IntPtr hWndLock);

        private void textToRegexButton_Click(object sender, EventArgs e)
        {
            checkBoxOnlySelected.Checked = false;
            try
            {
                LockWindowUpdate(regexRichText.Handle);
                int selStart = regexRichText.SelectionStart;
                int selEnd = selStart + richTextBox1.SelectionLength;
                string s = richTextBox1.SelectedRtf;
                s = Regex.Replace(s, @"\\highlight\d*\\", @"\");// remove background color
                regexRichText.SelectedRtf = s;
                for (int i = selStart; i < selEnd; )
                {
                    switch (regexRichText.Text[i])
                    {
                        case '(':
                        case ')':
                        case '[':
                        case '\\':
                        case '^':
                        case '$':
                        case '.':
                        case '|':
                        case '?':
                        case '*':
                        case '+':
                        case '#':
                            regexRichText.SelectionStart = i;
                            regexRichText.SelectionLength = 1;
                            regexRichText.SelectedText = @"\" + regexRichText.Text[i];
                            selEnd++;
                            i += 2;
                            break;
                        case ' ':
                        case '\t':
                        case '\r':
                        case '\n':
                            {
                                int j = i + 1;
                                string txt2 = regexRichText.Text;
                                while (j < txt2.Length && (txt2[j] == ' ' || txt2[j] == '\t' || txt2[j] == '\r' || txt2[j] == '\n'))
                                    j++;
                                regexRichText.SelectionStart = i;
                                regexRichText.SelectionLength = j - i;
                                string newText;
                                if (i == 0 || j >= txt2.Length)
                                    newText = String.Empty;
                                else
                                    if (j - i > 1 || txt2[i] == '\n')
                                        newText = @"\s*";
                                    else
                                        newText = @"\s";

                                regexRichText.SelectedText = newText;
                                regexRichText.SelectionStart = i;
                                regexRichText.SelectionLength = newText.Length;
                                regexRichText.SelectionColor = Color.Gray;
                                selEnd += newText.Length - (j - i);
                                i += newText.Length;
                                break;
                            }
                        default: { i++; break; }
                    }
                }
                regexRichText.SelectionLength = 0;
            }
            finally
            {
                LockWindowUpdate(IntPtr.Zero);
            }
        }

        private void findTextBox_TextChanged(object sender, EventArgs e)
        {
            richTextBox1.SelectionStart = 0;
            findButton.Text = "Find";
        }

        private void findTextBox_Enter(object sender, EventArgs e)
        {
            AcceptButton = findButton;
        }

        private void findTextBox_Leave(object sender, EventArgs e)
        {
            AcceptButton = null;
        }

        private void helpButton_Click(object sender, EventArgs e)
        {
            Process.Start(@"http://code.google.com/p/mp-onlinevideos2/wiki/CreateRegex");
        }

        private void regexRichText_SelectionChanged(object sender, EventArgs e)
        {
            if (checkBoxOnlySelected.Checked)
            {
                populateTree(regexRichText.SelectedText, false);
            }
        }

    }

    public class RegexPart
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public RegexPart(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public override string ToString()
        {
            return Name;
        }


    }
}
