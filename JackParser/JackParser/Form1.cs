using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace JackParser
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            string settingFile = Properties.Settings.Default.TokenFile;
            settingFile = String.IsNullOrWhiteSpace(settingFile)?@"C:\JackParser\Tokens.xml":settingFile ;
            this.textBox1.Text = settingFile;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.TokenFile = this.textBox1.Text;
            Properties.Settings.Default.Save();           
            


            XmlDocument tokens = new XmlDocument();
            tokens.Load(this.textBox1.Text);
            string strXmlDoc = JackParser.GetCleanJackXmlStringFromTokens(tokens);
        }
    }
}
