/*
 * CIE ID, l'applicazione per gestire la CIE
 * Author: Ugo Chirico - http://www.ugochirico.com
 * Data: 10/04/2019 
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using Newtonsoft.Json;
using CIEID.Controls;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;

namespace CIEID
{
    public partial class MainForm : Form
    {
        public const int CKR_OK = 0x00000000;
        public const int CKR_CANCEL = 0x00000001;
        public const int CKR_TOKEN_NOT_PRESENT = 0x000000E0;
        public const int CKR_TOKEN_NOT_RECOGNIZED = 0x000000E1;
        public const int CKR_DEVICE_ERROR = 0x00000030;
        public const int CKR_GENERAL_ERROR = 0x00000005;
        public const int CKR_PIN_INCORRECT = 0x000000A0;
        public const int CKR_PIN_INVALID = 0x000000A1;
        public const int CKR_PIN_LEN_RANGE = 0x000000A2;
        public const int CARD_ALREADY_ENABLED = 0x000000F0;
        public const int CARD_PAN_MISMATCH = 0x000000F1;

        /* CKR_PIN_EXPIRED and CKR_PIN_LOCKED are new for v2.0 */
        public const int CKR_PIN_EXPIRED = 0x000000A3;
        public const int CKR_PIN_LOCKED = 0x000000A4;

        public const int ENROLLED = 1;
        public const int NOT_ENROLLED = 0;

        private PdfPreview pdfPreview = null;
        private enum opSelectedState
        {
            NO_OP = 0,
            FIRMA_PADES = 1,
            FIRMA_CADES = 2
        }
        private int signFontSize = 60;

        private opSelectedState signOp = opSelectedState.NO_OP;

        delegate long ProgressCallback(int progress, string message);
        delegate long CompletedCallback(string pan, string name, string ef_seriale);
        delegate long SignCompletedCallback(int retValue);

        [DllImport("ciepki.dll")]
        static extern int VerificaCIEAbilitata(string pan);

        [DllImport("ciepki.dll")]
        static extern int DisabilitaCIE(string pan);

        [DllImport("ciepki.dll", CallingConvention = CallingConvention.StdCall)]
        static extern int isCIEEnrolled(StringBuilder pan);

        [DllImport("ciepki.dll", CallingConvention = CallingConvention.StdCall)]
        static extern int AbbinaCIE(string szPAN, string szPIN, int[] attempts, ProgressCallback progressCallBack, CompletedCallback completedCallBack);

        [DllImport("ciepki.dll", CallingConvention = CallingConvention.StdCall)]
        static extern int ChangePIN(string szPIN, string szNewPIN, int[] attempts, ProgressCallback progressCallBack);

        [DllImport("ciepki.dll", CallingConvention = CallingConvention.StdCall)]
        static extern int UnlockPIN(string szPUK, string szNewPIN, int[] attempts, ProgressCallback progressCallBack);

        [DllImport("ciepki.dll", CallingConvention = CallingConvention.StdCall)]
        static extern int firmaConCIE(string inFilePath, string type, string pin, string pan, int page, float x, float y, float w, float h, string imagePathFile, string outFilePath, ProgressCallback progressCallBack, SignCompletedCallback signCompletedCallBack);


        private CieCollection cieColl;

        internal CieCollection CieColl { get => cieColl; set => cieColl = value; }

        private CarouselControl carouselControl;

        public MainForm(string arg)
        {
            InitializeComponent();

            //for (int i = 1; i < 9; i++)
            //{
            //    TextBox txtField = (TextBox)FindControlByTag(Controls, "" + i);

            //    txtField.Paint += new System.Windows.Forms.PaintEventHandler(this.TextBox_Paint);

            //    txtField.BorderStyle = BorderStyle.None;
            //}

            if ("unlock".Equals(arg))
            {
                selectUnlock();
            }
            else if ("changepin".Equals(arg))
            {
                selectChangePIN();
            }
            else
            {
                selectHome();
            }
        }

        void TextBox_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics, ((Control)sender).DisplayRectangle, Color.LightGray, ButtonBorderStyle.Solid);
        }

        long ProgressAbbina(int progress, string message)
        {
            this.Invoke((MethodInvoker)delegate
            {
                progressBar.Value = progress;
                labelProgressMessage.Text = message;
            });

            return 0;
        }

        long ProgressFirma(int progress, string message)
        {
            this.Invoke((MethodInvoker)delegate
            {
                progressFirmaPina.Value = progress;
            });

            return 0;
        }

        private string getSignImagePath(string efSeriale)
        {
            string appdataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return string.Format("{0}\\IPZS\\{1}_default.png", appdataPath, efSeriale);
        }

        long CompletedFirma(int retValue)
        {
            this.Invoke((MethodInvoker)delegate
            {
                Console.WriteLine("111111111111111111 {0}", retValue);
                if (retValue != 0)
                {
                    Console.WriteLine("22222222");
                    lblFirmaSuccess.Text = "Si è verificato un errore";
                    //TODO cambiare immagine con X rossa
                }

                progressFirmaPina.Hide();
                lblFirmaPin.Hide();
                lblCartaFirmaPin.Hide();
                btnAnullaFirmaPin.Hide();
                btnFirma.Hide();
                lblFirmaSuccess.Show();
                lblFirmaSuccess.Update();
                pbFirmaPin.Show();
                pbFirmaPin.Update();
                btnConcludi.Show();
                btnConcludi.Update();
            });

            return 0;
        }

        long CompletedAbbina(string pan, string name, string efSeriale)
        {

            string defaultSignImagePath = getSignImagePath(efSeriale);
            CieColl.addCie(pan, new CieModel(efSeriale, name, pan));

            Properties.Settings.Default.cieList = JsonConvert.SerializeObject(CieColl.MyDictionary);
            Properties.Settings.Default.Save();

            Console.WriteLine("Immagine firma salvata in: {0}", defaultSignImagePath);

            TextInfo nameInfo = new CultureInfo("it-IT", false).TextInfo;
            DrawText(nameInfo.ToTitleCase(name.ToLower()), Color.Black, defaultSignImagePath);

            Console.WriteLine("Cie Abbinate dopo aggiunta: " + Properties.Settings.Default.cieList);
            
            return 0;
        }

        long ProgressCambioPIN(int progress, string message)
        {
            this.Invoke((MethodInvoker)delegate
            {
                progressBarCambioPIN.Value = progress;
                labelProgressCambioPIN.Text = message;
            });

            return 0;
        }

        long ProgressSbloccaPIN(int progress, string message)
        {
            this.Invoke((MethodInvoker)delegate
            {
                progressBarUnlock.Value = progress;
                labelProgressUnlock.Text = message;
            });

            return 0;
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar >= '0' && e.KeyChar <= '9')
            {
                TextBox textBox = (TextBox)sender;

                int tag = Int16.Parse((String)textBox.Tag);

                if (tag < 8)
                {
                    Control nextTextBox = FindControlByTag(this.Controls, "" + (tag + 1));
                    nextTextBox.Focus();
                }
            }
            else if (e.KeyChar == 8) // backspace
            {
                TextBox textBox = (TextBox)sender;

                int tag = Int16.Parse((String)textBox.Tag);


                if (tag > 1)
                {
                    Control previousTextBox = FindControlByTag(this.Controls, "" + (tag - 1));
                    previousTextBox.Focus();

                }
            }
            else if (e.KeyChar == 13) // enter
            {
                TextBox textBox = (TextBox)sender;

                int tag = Int16.Parse((String)textBox.Tag);

                if (tag == 8)
                    buttonAbbina_Click(sender, e);
            }
        }

        private void textBoxSignPin_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar >= '0' && e.KeyChar <= '9')
            {
                TextBox textBox = (TextBox)sender;

                int tag = Int16.Parse((String)textBox.Tag);

                if (tag < 12)
                {
                    Control nextTextBox = FindControlByTag(this.Controls, "" + (tag + 1));
                    nextTextBox.Focus();
                }
                else
                {
                    btnFirma.Enabled = true;
                }
            }
            else if (e.KeyChar == 8) // backspace
            {
                TextBox textBox = (TextBox)sender;

                int tag = Int16.Parse((String)textBox.Tag);


                if (tag > 9)
                {
                    Control previousTextBox = FindControlByTag(this.Controls, "" + (tag - 1));
                    previousTextBox.Focus();

                }
            }
            else if (e.KeyChar == 13) // enter
            {
                TextBox textBox = (TextBox)sender;

                int tag = Int16.Parse((String)textBox.Tag);

                if (tag == 12)
                {
                    btnFirma_Click(sender, e);

                }

            }
        }

        private Control FindControlByTag(Control.ControlCollection controls, object tag)
        {
            Control control = null;
            foreach (Control c in controls)
            {
                if (tag.Equals(c.Tag))
                {
                    control = c;
                }
                else
                {
                    if (c.HasChildren)
                        control = FindControlByTag(c.Controls, tag); //Recursively check all children controls as well; ie groupboxes or tabpages
                }

                if (control != null)
                    return control;
            }

            return null;
        }


        private void configureHomeButtons(CieCollection cieColl)
        {


            if (cieColl.MyDictionary.Count > 1)
            {
                int size_x = tableLayoutPanel1.Size.Width;
                int size_y = tableLayoutPanel1.Size.Height;
                int remaining_space = (size_x - (3 * (buttonDeleteCIE.Width)))/4;


                buttonRemoveAll.Location = new Point(remaining_space, tableLayoutPanel1.Location.Y + size_y + 10);
                buttonDeleteCIE.Location = new Point(remaining_space + buttonRemoveAll.Width + buttonRemoveAll.Location.X, buttonRemoveAll.Location.Y);
                buttonAggiungi.Location = new Point(remaining_space + buttonRemoveAll.Width + buttonDeleteCIE.Location.X, buttonRemoveAll.Location.Y);
                buttonRemoveAll.Visible = true;
            }
            else
            {
                int size_x = tableLayoutPanel1.Size.Width;
                int size_y = tableLayoutPanel1.Size.Height;
                int remaining_space = (size_x - (2 * (buttonDeleteCIE.Width)))/3;

                buttonRemoveAll.Visible = false;
                buttonDeleteCIE.Location = new Point(remaining_space, tableLayoutPanel1.Location.Y + size_y + 10);
                buttonAggiungi.Location = new Point(2* remaining_space + buttonDeleteCIE.Width, buttonDeleteCIE.Location.Y);
            }

            if (CieColl.MyDictionary.Count >= 1)
            {
                int size_x = tabPage1.Size.Width;
                int size_y = tabPage1.Size.Height;
                int remaining_space = (size_x - 2*((buttonAbbina.Width))) / 3;

                int height = size_y - buttonAbbina.Height - 30;

                buttonAnnulla.Location = new System.Drawing.Point(remaining_space, height); 
                buttonAbbina.Location = new System.Drawing.Point(2 * remaining_space + buttonAnnulla.Width, buttonAnnulla.Location.Y);
                buttonAnnulla.Visible = true;
            }
            else
            {
                int size_x = tabPage1.Size.Width;
                int size_y = tabPage1.Size.Height;
                int remaining_space = (size_x - ((buttonAbbina.Width))) / 2;
                int height = size_y - buttonAbbina.Height - 30;

                buttonAbbina.Location = new System.Drawing.Point(remaining_space, height);
                buttonAnnulla.Visible = false;
            }
        }

        private void buttonHome_Click(object sender, EventArgs e)
        {
            selectHome();
        }

        private void changeHomeObjects()
        {

            label5.Text = "Firma Elettronica";
            label2.Text = "Seleziona la CIE da usare";

            buttonDeleteCIE.Visible = false;
            buttonRemoveAll.Visible = false;
            buttonAggiungi.Visible = false;
            btnSigSelectCie.Visible = true;

            tabControlMain.SelectedIndex = 1;
        }

        private void createImages(CieCollection CieColl)
        {


            /*
            string defaultSignImagePath = getSignImagePath(efSeriale);
            CieColl.addCie(pan, new CieModel(efSeriale, name, pan));

            Properties.Settings.Default.cieList = JsonConvert.SerializeObject(CieColl.MyDictionary);
            Properties.Settings.Default.Save();

            Console.WriteLine("Immagine firma salvata in: {0}", defaultSignImagePath);

            TextInfo nameInfo = new CultureInfo("it-IT", false).TextInfo;
            DrawText(nameInfo.ToTitleCase(name.ToLower()), Color.Black, defaultSignImagePath);
            */
        }

        private void selectHome()
        {

            buttonDeleteCIE.Visible = true;
            buttonRemoveAll.Visible = true;
            buttonAggiungi.Visible = true;
            btnSigSelectCie.Visible = false;

            label5.Text = "CIE ID";
            label2.Text = "Carta d'Identità Elettronica abbinata correttamente";

            CieColl = new CieCollection(Properties.Settings.Default.cieList);
            
            if (!Properties.Settings.Default.cardHolder.Equals(""))
            {
                CieColl.addCie(Properties.Settings.Default.serialNumber, new CieModel(Properties.Settings.Default.efSeriale, Properties.Settings.Default.cardHolder, Properties.Settings.Default.serialNumber));

                Properties.Settings.Default.serialNumber = "";
                Properties.Settings.Default.cardHolder = "";
                Properties.Settings.Default.efSeriale = "";
                Properties.Settings.Default.Save(); 
            }


            configureHomeButtons(cieColl);

            Console.WriteLine("Lista CIE abbinate: " + Properties.Settings.Default.cieList);

            if (CieColl.MyDictionary.Count == 0)
            {
                tabControlMain.SelectedIndex = 0;
            }else
            {
                if (carouselControl == null)
                {
                    carouselControl = new CarouselControl(tableLayoutPanelCarousel, dotsGroup);
                    carouselControl.ButtonsChanged += carouselControl_ButtonsChanged;
                }

                carouselControl.LoadData(CieColl);
                carouselControl.UpdateLayout();

                tabControlMain.SelectedIndex = 1;
            }

            buttonHome.BackColor = Color.LightGray;
            buttonChangePIN.BackColor = Color.Transparent;
            buttonUnlock.BackColor = Color.Transparent;
            buttonTutorial.BackColor = Color.Transparent;
            buttonInfo.BackColor = Color.Transparent;
            buttonHelp.BackColor = Color.Transparent;
            buttonFirma.BackColor = Color.Transparent;
    }

        private PrivateFontCollection loadCustomFont()
        {
            //Create your private font collection object.
            PrivateFontCollection pfc = new PrivateFontCollection();

            //Select your font from the resources.
            //My font here is "Digireu.ttf"
            int fontLength = Properties.Resources.Allura_Regular.Length;

            // create a buffer to read in to
            byte[] fontdata = Properties.Resources.Allura_Regular;

            // create an unsafe memory block for the font data
            System.IntPtr data = Marshal.AllocCoTaskMem(fontLength);

            // copy the bytes to the unsafe memory block
            Marshal.Copy(fontdata, 0, data, fontLength);

            // pass the font to the font collection
            pfc.AddMemoryFont(data, fontLength);

            return pfc;
        }

        private void DrawText(String text, Color textColor, String path)
        {
            PrivateFontCollection pfc = loadCustomFont();

            Font font = new Font(pfc.Families[0], signFontSize);

            //first, create a dummy bitmap just to get a graphics object
            Image img = new Bitmap(1, 1);
            Graphics drawing = Graphics.FromImage(img);
            //measure the string to see how big the image needs to be
            SizeF textSize = drawing.MeasureString(text, font);

            //set the stringformat flags to rtl
            StringFormat sf = new StringFormat();
            //uncomment the next line for right to left languages
            //sf.FormatFlags = StringFormatFlags.DirectionRightToLeft;
            sf.Trimming = StringTrimming.Word;
            //free up the dummy image and old graphics object
            img.Dispose();
            drawing.Dispose();

            //create a new image of the right size
            img = new Bitmap((int)textSize.Width, (int)textSize.Height);

            drawing = Graphics.FromImage(img);
            //Adjust for high quality
            drawing.CompositingQuality = CompositingQuality.HighQuality;
            drawing.InterpolationMode = InterpolationMode.HighQualityBilinear;
            drawing.PixelOffsetMode = PixelOffsetMode.HighQuality;
            drawing.SmoothingMode = SmoothingMode.HighQuality;
            drawing.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            //paint the background
            drawing.Clear(Color.White);

            //create a brush for the text
            Brush textBrush = new SolidBrush(textColor);

            drawing.DrawString(text, font, textBrush, new RectangleF(0, 0, textSize.Width, textSize.Height), sf);

            drawing.Save();

            textBrush.Dispose();
            drawing.Dispose();
            img.Save(path, ImageFormat.Png);
            img.Dispose();

        }

        private void selectAbbinaProgress()
        {
            tabControlMain.SelectedIndex = 2;
            progressBar.Value = 0;
            progressBar.Maximum = 100;
        }

        private void buttonAbbina_Click(object sender, EventArgs e)
        {
            string pin = "";

            int i;
            for (i = 1; i < 9; i++)
            {
                TextBox txtField = (TextBox)FindControlByTag(Controls, "" + i);

                pin += txtField.Text;
            }

            if (pin.Length != 8)
            {
                MessageBox.Show("Il PIN deve essere composto da 8 numeri", "PIN non corretto", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            char c = pin[0];

            i = 1;
            for (i = 1; i < pin.Length && (c >= '0' && c <= '9'); i++)
            {
                c = pin[i];
            }

            if (i < pin.Length || !(c >= '0' && c <= '9'))
            {
                MessageBox.Show("Il PIN deve essere composto da 8 numeri", "PIN non corretto", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            for (i = 1; i < 9; i++)
            {
                TextBox txtField = (TextBox)FindControlByTag(Controls, "" + i);
                txtField.Text = "";
            }

            ((Control)sender).Enabled = false;

            tabControlMain.SelectedIndex = 2;

            ThreadStart processTaskThread = delegate { abbina(sender, pin); };

            new Thread(processTaskThread).Start();

        }

        private void abbina(object sender, string pin)
        {
            try
            {
                int[] attempts = new int[1];


                int ret = AbbinaCIE(null, pin, attempts, new ProgressCallback(ProgressAbbina), new CompletedCallback(CompletedAbbina));

                this.Invoke((MethodInvoker)delegate
                {
                    ((Control)sender).Enabled = true;
                    switch (ret)
                    {
                        case CKR_TOKEN_NOT_RECOGNIZED:
                            MessageBox.Show("CIE non presente sul lettore", "Abilitazione CIE", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                            selectHome();
                            break;

                        case CKR_TOKEN_NOT_PRESENT:
                            MessageBox.Show("CIE non presente sul lettore", "Abilitazione CIE", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                            selectHome();
                            break;

                        case CKR_PIN_INCORRECT:
                            MessageBox.Show(String.Format("Il PIN digitato è errato. rimangono {0} tentativi", attempts[0]), "PIN non corretto", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                            selectHome();
                            break;

                        case CKR_PIN_LOCKED:
                            MessageBox.Show("Munisciti del codice PUK e utilizza la funzione di sblocco carta per abilitarla", "Carta bloccata", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                            selectHome();
                            break;

                        case CKR_GENERAL_ERROR:
                            MessageBox.Show("Errore inaspettato durante la comunicazione con la smart card", "Errore inaspettato", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                            selectHome();
                            break;

                        case CKR_OK:
                            MessageBox.Show("L'abilitazione della CIE è avvenuta con successo", "CIE abilitata", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            selectHome();
                            break;
                        case CARD_ALREADY_ENABLED:
                            MessageBox.Show("Carta già abilitata", "Carta già abilitata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            selectHome();
                            break;
                    }
                });
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void buttonDeleteCIE_Click(object sender, EventArgs e)
        {
            var model = carouselControl.ActiveCieModel;

            if (MessageBox.Show(
                    String.Format("Stai rimuovendo la Carta di Identità di {0} dal sistema, per utilizzarla nuovamente dovrai ripetere l'abbinamento.", model.Owner), 
                    "Disabilita CIE", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.Cancel)
                return;
            
            int ret = DisabilitaCIE(model.Pan);

            switch (ret)
            {
                case CKR_OK:
                    MessageBox.Show("CIE disabilitata con successo", "CIE disabilitata", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    CieColl.removeCie(model.Pan);
                    Properties.Settings.Default.cieList = JsonConvert.SerializeObject(CieColl.MyDictionary);
                    Properties.Settings.Default.Save();

                    Console.WriteLine("Cie Rimanenti: " + Properties.Settings.Default.cieList);


                    if (System.IO.File.Exists(getSignImagePath(model.SerialNumber)))
                    {
                        System.IO.File.Delete(getSignImagePath(model.SerialNumber));
                    }

                    selectHome();

                    break;

                case CKR_TOKEN_NOT_PRESENT:
                    MessageBox.Show("CIE non presente sul lettore", "Disabilitazione CIE", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    break;

                default:
                    MessageBox.Show("Impossibile disabilitare la CIE", "CIE non disabilitata", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    break;
            }
        }

        private void buttonCambiaPIN_Click(object sender, EventArgs e)
        {
            string pin = textBoxPIN.Text;
            string newpin = textBoxNewPIN.Text;
            string newpin2 = textBoxNewPIN2.Text;

            int i;

            if (pin.Length != 8)
            {
                MessageBox.Show("Il PIN deve essere composto da 8 numeri", "PIN non corretto", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            if (newpin.Length != 8)
            {
                MessageBox.Show("Il PIN deve essere composto da 8 numeri", "PIN non corretto", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }


            char c = pin[0];

            i = 1;
            for (i = 1; i < pin.Length && (c >= '0' && c <= '9'); i++)
            {
                c = pin[i];
            }

            if (i < pin.Length || !(c >= '0' && c <= '9'))
            {
                MessageBox.Show("Il PIN deve essere composto da 8 numeri", "PIN non corretto", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            c = newpin[0];

            i = 1;
            for (i = 1; i < newpin.Length && (c >= '0' && c <= '9'); i++)
            {
                c = newpin[i];
            }

            if (i < newpin.Length || !(c >= '0' && c <= '9'))
            {
                MessageBox.Show("Il PIN deve essere composto da 8 numeri", "PIN non corretto", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            if (!newpin.Equals(newpin2))
            {
                MessageBox.Show("I PIN non corrispondono", "PIN non corrispondenti", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            c = newpin[0];
            char lastchar = c;

            for (i = 1; i < newpin.Length && c == lastchar; i++)
            {
                lastchar = c;
                c = newpin[i];
            }

            if (c == lastchar)
            {
                MessageBox.Show("Il nuovo PIN non deve essere composto da cifre uguali", "PIN non valido", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            c = newpin[0];
            lastchar = (char)((int)c - 1);

            for (i = 1; i < newpin.Length && c == lastchar + 1; i++)
            {
                lastchar = c;
                c = newpin[i];
            }

            if (c == lastchar + 1)
            {
                MessageBox.Show("Il nuovo PIN non deve essere composto da cifre consecutive", "PIN non valido", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            c = newpin[0];
            lastchar = (char)(c + 1);

            for (i = 1; i < newpin.Length && c == lastchar - 1; i++)
            {
                lastchar = c;
                c = newpin[i];
            }

            if (c == lastchar - 1)
            {
                MessageBox.Show("Il nuovo PIN non deve essere composto da cifre consecutive", "PIN non valido", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            textBoxPIN.Text = "";
            textBoxNewPIN2.Text = "";
            textBoxNewPIN.Text = "";

            ((Control)sender).Enabled = false;

            tabControlMain.SelectedIndex = 4;

            ThreadStart processTaskThread = delegate { cambiaPIN(sender, pin, newpin); };

            new Thread(processTaskThread).Start();
        }

        private void cambiaPIN(object sender, string pin, string newpin)
        {
            int[] attempts = new int[1];

            int ret = ChangePIN(pin, newpin, attempts, ProgressCambioPIN);

            this.Invoke((MethodInvoker)delegate
            {
                ((Control)sender).Enabled = true;

                switch (ret)
                {
                    case CKR_TOKEN_NOT_RECOGNIZED:
                        MessageBox.Show("CIE non presente sul lettore", "Cambio PIN", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        selectHome();
                        //[self showHomeFirstPage];
                        break;

                    case CKR_TOKEN_NOT_PRESENT:
                        MessageBox.Show("CIE non presente sul lettore", "Cambio PIN", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        selectHome();
                        break;

                    case CKR_PIN_INCORRECT:
                        MessageBox.Show(String.Format("Il PIN digitato è errato. rimangono {0} tentativi", attempts[0]), "PIN non corretto", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        selectHome();
                        break;

                    case CKR_PIN_LOCKED:
                        MessageBox.Show("Munisciti del codice PUK e utilizza la funzione di sblocco carta per abilitarla", "Carta bloccata", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        selectHome();
                        break;

                    case CKR_GENERAL_ERROR:
                        MessageBox.Show("Errore inaspettato durante la comunicazione con la smart card", "Errore inaspettato", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        selectHome();
                        break;

                    case CKR_OK:
                        MessageBox.Show("Il PIN è stato modificato con successo", "Operazione completata", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        selectHome();
                        new PINNotice().ShowDialog();
                        break;
                }
            });                      
        }

        private void sbloccaPIN(object sender, string puk, string newpin)
        {
            int[] attempts = new int[1];

            long ret = UnlockPIN(puk, newpin, attempts, ProgressSbloccaPIN);

            this.Invoke((MethodInvoker)delegate
            {
                ((Control)sender).Enabled = true;

                switch (ret)
                {
                    case CKR_TOKEN_NOT_RECOGNIZED:
                        MessageBox.Show("CIE non presente sul lettore", "Sblocca CIE", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        selectHome();
                        //[self showHomeFirstPage];
                        break;

                    case CKR_TOKEN_NOT_PRESENT:
                        MessageBox.Show("CIE non presente sul lettore", "Sblocca CIE", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        selectHome();
                        break;

                    case CKR_PIN_INCORRECT:
                        MessageBox.Show(String.Format("Il PUK digitato è errato. rimangono {0} tentativi", attempts[0]), "PIN non corretto", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        selectHome();
                        break;

                    case CKR_PIN_LOCKED:
                        MessageBox.Show("PUK bloccato. La tua CIE deve essere sostutuita", "Carta bloccata", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        selectHome();
                        break;

                    case CKR_GENERAL_ERROR:
                        MessageBox.Show("Errore inaspettato durante la comunicazione con la smart card", "Errore inaspettato", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        selectHome();
                        break;
                    case CKR_DEVICE_ERROR:
                        MessageBox.Show("Errore inaspettato durante la comunicazione con la smart card", "Errore inaspettato", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        selectHome();
                        break;
                    case CKR_OK:
                        MessageBox.Show("La CIE è stata sbloccata con successo", "Operazione completata", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        selectHome();
                        new PINNotice().ShowDialog();
                        break;
                }
            });
        }

        private void buttonChangePIN_Click(object sender, EventArgs e)
        {
            selectChangePIN();
        }

        private void selectChangePIN()
        { 
            tabControlMain.SelectedIndex = 3;

            buttonHome.BackColor = Color.Transparent;
            buttonChangePIN.BackColor = Color.LightGray;
            buttonUnlock.BackColor = Color.Transparent;
            buttonTutorial.BackColor = Color.Transparent;
            buttonInfo.BackColor = Color.Transparent;
            buttonHelp.BackColor = Color.Transparent;
            buttonFirma.BackColor = Color.Transparent;

        }

        private void buttonUnlock_Click(object sender, EventArgs e)
        {
            selectUnlock();
        }

        private void selectUnlock()
        {
            tabControlMain.SelectedIndex = 5;

            buttonHome.BackColor = Color.Transparent;
            buttonChangePIN.BackColor = Color.Transparent;
            buttonUnlock.BackColor = Color.LightGray;
            buttonTutorial.BackColor = Color.Transparent;
            buttonInfo.BackColor = Color.Transparent;
            buttonHelp.BackColor = Color.Transparent;
            buttonFirma.BackColor = Color.Transparent;
        }

        private void buttonUnlockPIN_Click(object sender, EventArgs e)
        {
            string puk = textBoxPUK.Text;
            string newpin = textBoxUnlockPIN.Text;
            string newpin2 = textBoxUnlockPIN2.Text;

            int i;

            if (puk.Length != 8)
            {
                MessageBox.Show("Il PUK deve essere composto da 8 numeri", "PUK non corretto", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            if (newpin.Length != 8)
            {
                MessageBox.Show("Il PIN deve essere composto da 8 numeri", "PIN non corretto", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }


            char c = puk[0];

            i = 1;
            for (i = 1; i < puk.Length && (c >= '0' && c <= '9'); i++)
            {
                c = puk[i];
            }

            if (i < puk.Length || !(c >= '0' && c <= '9'))
            {
                MessageBox.Show("Il PUK deve essere composto da 8 numeri", "PUK non corretto", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            c = newpin[0];

            i = 1;
            for (i = 1; i < newpin.Length && (c >= '0' && c <= '9'); i++)
            {
                c = newpin[i];
            }

            if (i < newpin.Length || !(c >= '0' && c <= '9'))
            {
                MessageBox.Show("Il PIN deve essere composto da 8 numeri", "PIN non corretto", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            if (!newpin.Equals(newpin2))
            {
                MessageBox.Show("I PIN non corrispondono", "PIN non corrispondenti", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            c = newpin[0];
            char lastchar = c;

            for (i = 1; i < newpin.Length && c == lastchar; i++)
            {
                lastchar = c;
                c = newpin[i];
            }

            if (c == lastchar)
            {
                MessageBox.Show("Il nuovo PIN non deve essere composto da cifre uguali", "PIN non valido", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            c = newpin[0];
            lastchar = (char)((int)c - 1);

            for (i = 1; i < newpin.Length && c == lastchar + 1; i++)
            {
                lastchar = c;
                c = newpin[i];
            }

            if (c == lastchar + 1)
            {
                MessageBox.Show("Il nuovo PIN non deve essere composto da cifre consecutive", "PIN non valido", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            c = newpin[0];
            lastchar = (char)(c + 1);

            for (i = 1; i < newpin.Length && c == lastchar - 1; i++)
            {
                lastchar = c;
                c = newpin[i];
            }

            if (c == lastchar - 1)
            {
                MessageBox.Show("Il nuovo PIN non deve essere composto da cifre consecutive", "PIN non valido", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            textBoxPUK.Text = "";
            textBoxUnlockPIN.Text = "";
            textBoxUnlockPIN2.Text = "";

            ((Control)sender).Enabled = false;

            tabControlMain.SelectedIndex = 6;

            ThreadStart processTaskThread = delegate { sbloccaPIN(sender, puk, newpin); };

            new Thread(processTaskThread).Start();
        }

        private void label26_Click(object sender, EventArgs e)
        {

        }

        private void label22_Click(object sender, EventArgs e)
        {

        }

        private void buttonTutorial_Click(object sender, EventArgs e)
        {
            tabControlMain.SelectedIndex = 7;

            buttonHome.BackColor = Color.Transparent;
            buttonChangePIN.BackColor = Color.Transparent;
            buttonUnlock.BackColor = Color.Transparent;
            buttonTutorial.BackColor = Color.LightGray;
            buttonInfo.BackColor = Color.Transparent;
            buttonHelp.BackColor = Color.Transparent;
            buttonFirma.BackColor = Color.Transparent;
            webBrowserTutorial.Navigate("https://idserver.servizicie.interno.gov.it/idp/tutorial_win.jsp");
        }

        private void buttonHelp_Click(object sender, EventArgs e)
        {
            tabControlMain.SelectedIndex = 8;

            buttonHome.BackColor = Color.Transparent;
            buttonChangePIN.BackColor = Color.Transparent;
            buttonUnlock.BackColor = Color.Transparent;
            buttonTutorial.BackColor = Color.Transparent;
            buttonInfo.BackColor = Color.Transparent;
            buttonHelp.BackColor = Color.LightGray;
            buttonFirma.BackColor = Color.Transparent;

            webBrowserHelp.Navigate("https://idserver.servizicie.interno.gov.it/idp/aiuto.jsp");        
        }

        private void buttonInfo_Click(object sender, EventArgs e)
        {
            tabControlMain.SelectedIndex = 9;

            buttonHome.BackColor = Color.Transparent;
            buttonChangePIN.BackColor = Color.Transparent;
            buttonUnlock.BackColor = Color.Transparent;
            buttonTutorial.BackColor = Color.Transparent;
            buttonInfo.BackColor = Color.LightGray;
            buttonHelp.BackColor = Color.Transparent;
            buttonFirma.BackColor = Color.Transparent;

            webBrowserInfo.Navigate("https://idserver.servizicie.interno.gov.it/idp/privacy.jsp");
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void textBoxPIN_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13) // enter
            {
                buttonCambiaPIN_Click(sender, e);
            }
        }

        private void textBoxPUK_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13) // enter
            {
                buttonUnlockPIN_Click(sender, e);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            tabControlMain.SelectedIndex = 0;
        }

        private void buttonRemoveAll_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show( String.Format("Rimuovere tutte le Carte di Identità attualmente abbinate?"),
                "Disabilita tutte le CIE", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.Cancel)
                return;

            String[] arrayCIE = new String[CieColl.MyDictionary.Count];

            for(int i = 0; i< arrayCIE.Count(); i++)
            {
                arrayCIE[i] = CieColl.MyDictionary.ElementAt(i).Key;
            }
            
            for (int i = 0; i< arrayCIE.Count(); i++)   
            {

                int ret = DisabilitaCIE(arrayCIE[i]);

                if (ret != CKR_OK)
                {
                    var cieModel = CieColl.MyDictionary.ElementAt(i).Value;
                    MessageBox.Show("Impossibile disabilitare la CIE numero " + cieModel.SerialNumber, "CIE non disabilitata", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    Properties.Settings.Default.cieList = JsonConvert.SerializeObject(CieColl.MyDictionary);
                    Properties.Settings.Default.Save();
                    selectHome();
                    return;
                }

                CieColl.removeCie(arrayCIE[i]);
                Console.WriteLine("CIE con chiave " + arrayCIE[i] + " disabilitata");
            }

            Properties.Settings.Default.cieList = JsonConvert.SerializeObject(CieColl.MyDictionary);
            Properties.Settings.Default.Save();

            Console.WriteLine("Cie Rimanenti: " + Properties.Settings.Default.cieList);

            MessageBox.Show("CIE disabilitate con successo", "CIE disabilitate", MessageBoxButtons.OK, MessageBoxIcon.Information);
            selectHome();
        }

        private void buttonLeft_Click(object sender, EventArgs e)
        {
            carouselControl.ShiftRight();
        }

        private void buttonRight_Click(object sender, EventArgs e)
        {
            carouselControl.ShiftLeft();
        }

        private void carouselControl_ButtonsChanged(object sender, Controls.CarouselControl.ButtonsEventArgs e)
        {
            if (e.IsRightButton)
            {
                buttonRight.Enabled = e.IsEnabled;
                toggleButtonVisibility(buttonRight, e.IsVisible);
            }
            else
            {
                buttonLeft.Enabled = e.IsEnabled;
                toggleButtonVisibility(buttonLeft, e.IsVisible);
            }
        }

        private void toggleButtonVisibility(Button button, bool show)
        {
            if (show)
            {
                button.Show();
            }
            else
            {
                button.Hide();
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
        

        private void labelOwnerValue1_Click(object sender, EventArgs e)
        {

        }

        private void labelOwnerValue0_Click(object sender, EventArgs e)
        {

        }

        private void buttonAnnulla_Click(object sender, EventArgs e)
        {
            selectHome();
        }

        private void tabPage6_Click(object sender, EventArgs e)
        {

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            Panel panel = (Panel)sender;
            float width = (float)5.0;
            Pen pen = new Pen(SystemColors.ControlDark, width);
            pen.DashStyle = DashStyle.Dash;
            e.Graphics.DrawLine(pen, 0, 0, 0, panel.Height - 0);
            e.Graphics.DrawLine(pen, 0, 0, panel.Width - 0, 0);
            e.Graphics.DrawLine(pen, panel.Width - 1, panel.Height - 1, 0, panel.Height - 1);
            e.Graphics.DrawLine(pen, panel.Width - 1, panel.Height - 1, panel.Width - 1, 0);

        }


        private void buttonFirma_Click(object sender, EventArgs e)
        {
            buttonHome.BackColor = Color.Transparent;
            buttonChangePIN.BackColor = Color.Transparent;
            buttonUnlock.BackColor = Color.Transparent;
            buttonTutorial.BackColor = Color.Transparent;
            buttonInfo.BackColor = Color.Transparent;
            buttonHelp.BackColor = Color.Transparent;
            buttonFirma.BackColor = Color.LightGray;

            signOp = opSelectedState.NO_OP;

            changeHomeObjects();           
        }

        private void lbPeronalizza_Click(object sender, EventArgs e)
        {
            Console.WriteLine("Carta selezionata: {0}", labelCardNumberValue1.Text);
            string signImagePath = getSignImagePath(labelCardNumberValue1.Text);
            Console.WriteLine("SignImagePath: {0}", signImagePath);

            if (pnFirmaGrafica.Controls.Count > 0 && pnFirmaGrafica.Controls[0] != null)
            {
                pnFirmaGrafica.Controls[0].Dispose();

                Console.WriteLine("PictureBox disposing");
            }

            PictureBox signPicture = new PictureBox();
            signPicture.BackColor = Color.Transparent;
            signPicture.Width = pnFirmaGrafica.Width;
            signPicture.Height = pnFirmaGrafica.Height;

            Image image;
            using (Stream stream = File.OpenRead(signImagePath))
            {
                image = System.Drawing.Image.FromStream(stream);
            }

            Bitmap signImage = new Bitmap(image, signPicture.Width, signPicture.Height);
            signImage.MakeTransparent();
            signPicture.Image = (Image)signImage.Clone();

            signPicture.Update();
            pnFirmaGrafica.Controls.Add(signPicture);

            tabControlMain.SelectedIndex = 15;

        }

        private void lbPeronalizza_MouseEnter(object sender, EventArgs e)
        { 
            lbPeronalizza.Font = new Font(lbPeronalizza.Font, FontStyle.Underline);
        }
        private void lbPeronalizza_MouseLeave(object sender, EventArgs e)
        {
            lbPeronalizza.Font = new Font(lbPeronalizza.Font, FontStyle.Regular);
        }

        void panelChooseDoc_dragEnter(object sender, DragEventArgs e)
        {
            Console.WriteLine("Panel_DragEnter");
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }


        private void goToSelectSignOp(string file_name)
        {
            lblPath.Text = file_name;
            tabControlMain.SelectedIndex = 11;

        }

        void panelChooseDoc_dragDrop(object sender, DragEventArgs e)
        {
            Console.WriteLine("Panel_DropEnter");

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            Console.WriteLine("File drop: {0}", files[0]);
            goToSelectSignOp(files[0]);
        }

        private void selectDocument_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();

            //deleteTmpFiles();
            //page_index = 1;

            if (openFile.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string file_name = openFile.FileName;
                Console.WriteLine("Selected file: {0}", file_name);

                goToSelectSignOp(file_name);
            }
        }

        private void pnFirmaOp_MouseClick(object sender, EventArgs e)
        {

            lblPath2.Text = lblPath.Text;

            signOp = opSelectedState.NO_OP;
            btnSignProsegui.Enabled = false;

            //TODO immagine PADES grigia
            lblPadesTitle.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            lblPadesExp.ForeColor = System.Drawing.SystemColors.ControlDarkDark;

            //TODO immagine CADES grigia
            lblCadesTitle.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            lblCadesExp.ForeColor = System.Drawing.SystemColors.ControlDarkDark;

            cbFirmaGrafica.Checked = false;


            tabControlMain.SelectedIndex = 12;
        }

        private void pnFirmaOp_MouseEnter(object sender, EventArgs e)
        {
            lblFirmaOp.ForeColor = System.Drawing.SystemColors.Highlight;
        }

        private void pnFirmaOp_MouseLeave(object sender, EventArgs e)
        {
            lblFirmaOp.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
        }

        private void pnVerificaOp_MouseClick(object sender, EventArgs e)
        {
            
            lblVerificaPath.Text = lblPath.Text;
            SignerInfo sInfo = new SignerInfo(lblVerificaPath.Text, pnSignerInfo);
            int n_sott = sInfo.verify();

            Console.WriteLine(lblVerificaPath.Text);

            lblSottoscrittori.Text = string.Format("Numero di sottoscrittori: {0}", n_sott);
            lblSottoscrittori.Update();

            pnVerifica.Visible = true;

            tabControlMain.SelectedIndex = 16;


        }

        private void pnVerificaOp_MouseEnter(object sender, EventArgs e)
        {
            lblVerificaOp.ForeColor = System.Drawing.SystemColors.Highlight;
        }

        private void pnVerificaOp_MouseLeave(object sender, EventArgs e)
        {
            lblVerificaOp.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
        }


        private void btnAnnullaOp_Click(object sender, EventArgs e)
        {
            tabControlMain.SelectedIndex = 10;
        }

        private void cbFirmaGrafica_CheckedChanged(object sender, EventArgs e)
        {
            if (lblPath2.Text.EndsWith(".pdf"))
            {
                if (cbFirmaGrafica.Checked == true)
                {
                    cbFirmaGrafica.ForeColor = System.Drawing.SystemColors.Highlight;
                }
                else
                {
                    cbFirmaGrafica.ForeColor = System.Drawing.SystemColors.GrayText;
                }
            }

        }

        private void btnSignAnnulla_Click(object sender, EventArgs e)
        {

            signOp = opSelectedState.NO_OP;
            btnSignProsegui.Enabled = false;

            //TODO immagine PADES grigia
            lblPadesTitle.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            lblPadesExp.ForeColor = System.Drawing.SystemColors.ControlDarkDark;

            //TODO immagine CADES grigia
            lblCadesTitle.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            lblCadesExp.ForeColor = System.Drawing.SystemColors.ControlDarkDark;

            cbFirmaGrafica.Checked = false;

            tabControlMain.SelectedIndex = 11;

        }

        private void panelChoosePades_MouseEnter(object sender, EventArgs e)
        {
            panelChoosePades.BorderStyle = BorderStyle.FixedSingle;
        }

        private void panelChoosePades_MouseLeave(object sender, EventArgs e)
        {
            panelChoosePades.BorderStyle = BorderStyle.None; 
        }

        private void panelChooseCades_MouseEnter(object sender, EventArgs e)
        {
            panelChooseCades.BorderStyle = BorderStyle.FixedSingle;
        }

        private void panelChooseCades_MouseLeave(object sender, EventArgs e)
        {
            panelChooseCades.BorderStyle = BorderStyle.None;
        }

        private void panelChoosePades_MouseClick(object sender, EventArgs e)
        {
            if(lblPath2.Text.EndsWith(".pdf"))
            {
                //TODO immagine PADES colorata
                lblPadesTitle.ForeColor = Color.Red;
                lblPadesExp.ForeColor = Color.Black;

                lblCadesTitle.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
                lblCadesExp.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
                //TODO immagine CADES grigia

                signOp = opSelectedState.FIRMA_PADES;
                btnSignProsegui.Enabled = true;
            }


        }

        private void panelChooseCades_MouseClick(object sender, EventArgs e)
        {
            //TODO immagine CADES colorata
            lblCadesTitle.ForeColor = System.Drawing.SystemColors.Highlight;
            lblCadesExp.ForeColor = Color.Black;

            lblPadesTitle.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            lblPadesExp.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            cbFirmaGrafica.Checked = false;
            //TODO immagine PADES grigia

            signOp = opSelectedState.FIRMA_CADES;
            btnSignProsegui.Enabled = true;

        }

        private void btnSignProsegui_Click(object sender, EventArgs e)
        {

            if((cbFirmaGrafica.Checked == true) && (signOp == opSelectedState.FIRMA_PADES))
            {
                string signImagePath = getSignImagePath(labelCardNumberValue1.Text);

                if (!System.IO.File.Exists(signImagePath))
                {
                    TextInfo nameInfo = new CultureInfo("it-IT", false).TextInfo;
                    string name = CieColl.MyDictionary.ElementAt(0).Value.Owner;
                    DrawText(nameInfo.ToTitleCase(name.ToLower()), Color.Black, signImagePath);
                }

                lblPath3.Text = lblPath2.Text;

                btnUp.Enabled = true;
                btnDown.Enabled = true;

                if (pdfPreview != null)
                {
                    pdfPreview.pdfPreviewRemoveObjects();
                }

                pdfPreview = new PdfPreview(panePreview, lblPath3.Text, signImagePath);
                if (pdfPreview.getPdfPages() <= 1)
                {
                    btnUp.Enabled = false;
                    btnDown.Enabled = false;
                }
                tabControlMain.SelectedIndex = 13;
            }
            else
            {
                lblPath4.Text = lblPath2.Text;
                tabControlMain.SelectedIndex = 14;
            }
            

        }

        private void btnUp_Click(object sender, EventArgs e)
        {
            pdfPreview.pageUp();
        }

        private void btnDown_Click(object sender, EventArgs e)
        {
            pdfPreview.pageDown();
        }

        private void btnAnullaFirmaPin_Click(object sender, EventArgs e)
        {
            for (int i = 9; i < 13; i++)
            {
                TextBox txtField = (TextBox)FindControlByTag(Controls, "" + i);

                txtField.Text = "";
            }
            tabControlMain.SelectedIndex = 12;

        }

        private void btnConcludi_Click(object sender, EventArgs e)
        {
            lblFirmaPin.Text = "Inserisci le ultime 4 cifre del PIN";
            lblFirmaPin.TextAlign = ContentAlignment.MiddleLeft;
            lblFirmaPin.Show();

            for (int i = 9; i < 13; i++)
            {
                TextBox txtField = (TextBox)FindControlByTag(Controls, "" + i);

                txtField.Text = "";
                txtField.Show();
            }

            btnAnullaFirmaPin.Show();
            btnAnullaFirmaPin.Enabled = true;
            btnFirma.Show();
            btnFirma.Enabled = false;
            btnConcludi.Hide();

            lblCartaFirmaPin.Show();
            pbFirmaPin.Hide();
            lblFirmaSuccess.Hide();

            tabControlMain.SelectedIndex = 10;
        }

        private void goToSignPin(string pdfPath)
        {
            lblPath4.Text = pdfPath;
            tabControlMain.SelectedIndex = 14;
        }

        private void btnProseguiPreview_Click(object sender, EventArgs e)
        {
            goToSignPin(lblPath3.Text);
        }

        private void btnFirma_Click(object sender, EventArgs e)
        {
            string pin = "";

            for (int i = 9; i < 13; i++)
            {
                TextBox txtField = (TextBox)FindControlByTag(Controls, "" + i);

                pin += txtField.Text;
            }

            if (pin.Length != 4)
            {
                MessageBox.Show("Inserire le ultime 4 cifre del PIN", "PIN non corretto", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            //Chiedere all'utente dove salvare il file firmato
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            if(signOp == opSelectedState.FIRMA_PADES)
            {
                saveFileDialog1.Filter = "File (*.pdf) | *.pdf";
            }
            else
            {
                saveFileDialog1.Filter = "File (*.p7m) | *.p7m";
            }
            string sfdname = saveFileDialog1.FileName;
            string pathToSaveFile = "";
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                pathToSaveFile = Path.GetFullPath(saveFileDialog1.FileName);
                Console.WriteLine("Path save file: {0}", pathToSaveFile);
            }
            else
            {
                return;
            }

            //Nascondere le textbox contenente il pin (svuotandole prima)
            for (int i = 9; i < 13; i++)
            {
                TextBox txtField = (TextBox)FindControlByTag(Controls, "" + i);

                txtField.Hide();
            }

            //disabilitare i pulsanti
            btnAnullaFirmaPin.Enabled = false;
            btnFirma.Enabled = false;

            //mostrare progress al posto delle textbox
            progressFirmaPina.Value = 0;
            progressFirmaPina.Show();
            lblFirmaPin.TextAlign = ContentAlignment.MiddleCenter;
            lblFirmaPin.Text = "Firma in corso...";
            lblFirmaPin.Update();
            
            //invocare metodo firma in un altro thread


            var model = carouselControl.ActiveCieModel;
            if (cbFirmaGrafica.Checked && (signOp == opSelectedState.FIRMA_PADES))
            {
                Console.WriteLine("Pades con grafica");
                Dictionary<string, float> signImageInfo = pdfPreview.getSignImageInfos();
                firmaConCIE(lblPath4.Text, "pdf", pin, model.Pan, (int)signImageInfo["pageNumber"], signImageInfo["x"], signImageInfo["y"], signImageInfo["w"], signImageInfo["h"],
                    pdfPreview.getSignImagePath(), pathToSaveFile, new ProgressCallback(ProgressFirma), new SignCompletedCallback(CompletedFirma));

            }
            else if (signOp == opSelectedState.FIRMA_PADES)
            {
                Console.WriteLine("Pades senza grafica");

                firmaConCIE(lblPath4.Text, "pdf", pin, model.Pan, 0, 0.0f, 0.0f, 0.0f, 0.0f, null, pathToSaveFile, new ProgressCallback(ProgressFirma), new SignCompletedCallback(CompletedFirma));
            }
            else if (signOp == opSelectedState.FIRMA_CADES)
            {
                Console.WriteLine("Cades");

                firmaConCIE(lblPath4.Text, "p7m", pin, model.Pan, 0, 0.0f, 0.0f, 0.0f, 0.0f, null, pathToSaveFile, new ProgressCallback(ProgressFirma), new SignCompletedCallback(CompletedFirma));
            }
                
        }

        private void btnPersonalizzaAnnulla_Click(object sender, EventArgs e)
        {
            tabControlMain.SelectedIndex = 10;
        }

        private void btnPersonalizzaSelect_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();

            openFile.Filter = "File (*.png) | *.png";

            if (openFile.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string file_name = openFile.FileName;
                Console.WriteLine("PNG Selected file: {0}", file_name);


                var model = carouselControl.ActiveCieModel;

                File.Copy(file_name, getSignImagePath(model.SerialNumber), true);
                CieColl.MyDictionary[model.Pan].isCustomSign = true;
                Properties.Settings.Default.cieList = JsonConvert.SerializeObject(CieColl.MyDictionary);
                Properties.Settings.Default.Save();

                Image image;
                using (Stream stream = File.OpenRead(getSignImagePath(model.SerialNumber)))
                {
                    image = System.Drawing.Image.FromStream(stream);
                }

                PictureBox signPicture = (PictureBox)pnFirmaGrafica.Controls[0];
                Bitmap signImage = new Bitmap(image, signPicture.Width, signPicture.Height);
                signImage.MakeTransparent();
                signPicture.Image = signImage;
                signPicture.Update();

                lblPersonalizzaPreambolo.Text = "Una tua firma grafica personalizzata è già stata caricata. Vuoi aggiornarla?";
                lblPersonalizzaPreambolo.Update();

            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            tabControlMain.SelectedIndex = 10;
        }

        private void btnSigSelectCie_Click(object sender, EventArgs e)
        {

            var model = carouselControl.ActiveCieModel;

            if(model.isCustomSign)
            {
                lbPeronalizza.Text = "Aggiorna";
                label29.Text = "Firma personalizzata correttamente";
            }

            tabControlMain.SelectedIndex = 10;


        }
    }
        //long ret = VerificaCIEAbilitata();

        //            switch (ret)
        //            {
        //                case CKR_DEVICE_ERROR:
        //                    break;

        //                case CKR_TOKEN_NOT_PRESENT:
        //                    break;

        //                case CKR_GENERAL_ERROR:
        //                    break;

        //                case ENROLLED:
        //                    break;

        //                case NOT_ENROLLED:
        //                    abbina();
        //                    break;

        //            }
 }
