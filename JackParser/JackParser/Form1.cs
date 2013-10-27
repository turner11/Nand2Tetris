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
            this.textBox1.Text = @"C:\JackParser\Tokens.xml";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            XmlDocument tokens = new XmlDocument();
            tokens.Load(this.textBox1.Text);
            string strXmlDoc = JackParser.GetJackXmlStringFromTokens(tokens);
        }
    }
}
