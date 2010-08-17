using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Text;
using System.Windows.Forms;

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

        public string Execute(string regexString, string webData, string[] names)
        {
            result = regexString;
            RegexTextbox.Text = result;
            FillPageData(webData);
            insertComboBox.Items.Clear();
            foreach (string s in names)
                insertComboBox.Items.Add(new RegexPart(s, @"(?<" + s + @">[^@@@]*)"));
            insertComboBox.Items.Add(new RegexPart("skip to", @"(?:(?!@@).)*"));
            insertComboBox.Items.Add(new RegexPart("skip to single char", @"[^@@@]*"));
            insertComboBox.Items.Add(new RegexPart("optional", @"(?:@@@@)?"));
            insertComboBox.SelectedIndex = 0;
            fields = names;
            testData = webData;
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


        private void FillPageData(string webData)
        {
            webData = webData.Replace(@"\", @"\\").Replace(@"{", @"\{").Replace(@"}", @"\}").Replace("\n", "\\par\n").Replace("\r", String.Empty);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"{\rtf1\ansi\deff0{\fonttbl{\f0\fnil\fcharset0 Microsoft Sans Serif;}}");
            sb.AppendLine(@"{\colortbl ;");
            sb.AppendLine(@"\red177\green19\blue128;");
            sb.AppendLine(@"\red100\green128\blue0;");
            sb.AppendLine(@"\red255\green65\blue0;");
            sb.AppendLine(@"\red58\green110\blue165;");
            sb.AppendLine(@"\red0\green128\blue0;");
            sb.AppendLine(@"}");
            sb.Append(@"\viewkind4\uc1\pard\cf0\lang1043\f0\fs17 ");
            int i = -1;
            int j = 0;
            do
            {
                i = webData.IndexOf('<', j);
                if (i != -1)
                {
                    if (i != j)
                        append2(sb, webData.Substring(j, i - j));
                    j = webData.IndexOf('>', i + 1);
                    if (j >= 0)
                    {
                        int k = webData.LastIndexOf('<', j - 1);
                        if (k != i)
                        {
                            append2(sb, webData.Substring(i, k - i));
                            i = k;
                        }
                        append(sb, webData.Substring(i, j - i + 1));
                        j++;
                    }
                }
            } while (i >= 0 && (i + 1) < webData.Length);
            if (i == -1)
                append2(sb, webData.Substring(j, webData.Length - j));
            sb.Append('}');
            richTextBox1.Rtf = sb.ToString();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            result = RegexTextbox.Text;
            Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void button4_Click(object sender, EventArgs e)
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


        private void button5_Click(object sender, EventArgs e)
        {
            string strToInsert = ((RegexPart)insertComboBox.SelectedItem).Value;
            int p = strToInsert.IndexOf(@"@@@@");
            if (p >= 0)
            {
                strToInsert = strToInsert.Substring(0, p) + RegexTextbox.SelectedText + strToInsert.Substring(p + 4);
                p = -1; // prevent further processing
            }
            bool replaceWithLast = false;
            if (p < 0)
            {
                p = strToInsert.IndexOf(@"@@@");
                replaceWithLast = p >= 0;
                if (p < 0) p = strToInsert.IndexOf(@"@@");
            }
            int nextInd = RegexTextbox.SelectionStart + RegexTextbox.SelectionLength;
            string nextChar = String.Empty;
            if (nextInd < RegexTextbox.Text.Length)
                nextChar = new String(RegexTextbox.Text[nextInd], 1);

            int insPos = RegexTextbox.SelectionStart;
            RegexTextbox.Text = RegexTextbox.Text.Substring(0, insPos) + strToInsert +
                RegexTextbox.Text.Substring(insPos + RegexTextbox.SelectionLength);
            if (replaceWithLast)
            {
                insPos += p;
                RegexTextbox.Text = RegexTextbox.Text.Substring(0, insPos) + nextChar +
                    RegexTextbox.Text.Substring(insPos + 3);
                RegexTextbox.SelectionStart = insPos;
                RegexTextbox.SelectionLength = 0;
            }
            else
                if (p >= 0)
                {
                    RegexTextbox.SelectionStart = insPos + p;
                    RegexTextbox.SelectionLength = 2;
                }
                else
                {
                    RegexTextbox.SelectionStart = insPos;
                    RegexTextbox.SelectionLength = 0;
                }
            RegexTextbox.Focus();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            treeView1.BeginUpdate();
            treeView1.Nodes.Clear();

            try
            {
                Regex test = new Regex(RegexTextbox.Text, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
                Match m = test.Match(testData);
                while (m.Success)
                {
                    TreeNode node = treeView1.Nodes.Add(treeView1.Nodes.Count.ToString());
                    foreach (String field in fields)
                    {
                        node.Nodes.Add(field + " " + m.Groups[field].Value);
                        node.Expand();
                    }
                    m = m.NextMatch();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            treeView1.EndUpdate();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            //string txt = Clipboard.GetText();
            string txt = richTextBox1.SelectedText;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < txt.Length; i++)
            {
                switch (txt[i])
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
                        sb.Append('\\'); sb.Append(txt[i]); break;
                    case ' ':
                    case '\t':
                    case '\r':
                    case '\n':
                        {
                            int j = i + 1;
                            while (j < txt.Length && (txt[j] == ' ' || txt[j] == '\t' || txt[j] == '\r' || txt[j] == '\n'))
                                j++;
                            if (j - i > 1 || txt[i] == '\n') sb.Append(@"\s*");
                            else
                                sb.Append(@"\s");
                            i = j - 1;
                            break;
                        }
                    default: sb.Append(txt[i]); break;
                }
            }
            RegexTextbox.Text = RegexTextbox.Text.Substring(0, RegexTextbox.SelectionStart) + sb.ToString() +
                RegexTextbox.Text.Substring(RegexTextbox.SelectionStart + RegexTextbox.SelectionLength);

        }

        private void findTextBox_TextChanged(object sender, EventArgs e)
        {
            richTextBox1.SelectionStart = 0;
            findButton.Text = "Find";
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
