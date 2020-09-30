using CIEID.Properties;
using System;
using System.Windows.Forms;

namespace CIEID.Controls
{
    class CarouselItemControl : BaseItemControl
    {
        private PictureBox image;
        private Label cardNumberLabel;
        private Label cardNumberValueLabel;
        private Label cardOwnerLabel;
        private Label cardOwnerValueLabel;
        private FlowLayoutPanel container;

        private CieModel cieModel;

        public CarouselItemControl(CieModel cieModel)
        {
            this.cieModel = cieModel;
            this.ColumnCount = 1;
            this.RowCount = 1;

            CreateLayout();
            UpdateLayout(this.cieModel);
        }

        private void CreateLayout()
        {
            AutoSize = true;
            Anchor = (AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom);
            this.Margin = new Padding(0);

            container = new FlowLayoutPanel();
            container.WrapContents = false;
            container.AutoSize = true;
            container.AutoSizeMode = AutoSizeMode.GrowOnly;
            container.MaximumSize = new System.Drawing.Size(this.Width, 1000);
            container.FlowDirection = FlowDirection.TopDown;
            container.Anchor = (AnchorStyles.Left | AnchorStyles.Right);
            container.Margin = new Padding(0);

            image = new PictureBox
            {
                Image = Properties.Resources.cie,
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new System.Drawing.Size(IMAGE_WIDTH, IMAGE_HEIGHT),
                Anchor = (AnchorStyles.Left | AnchorStyles.Right)
            };

            container.Controls.Add(image);

            cardNumberLabel = new Label
            {
                Text = Resources.card_number_label,
                Size = new System.Drawing.Size(120, 20),
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Font = new System.Drawing.Font(Font.Name, 9f, System.Drawing.FontStyle.Bold),
                Padding = new Padding(10, 0, 10, 0)
            };

            container.Controls.Add(cardNumberLabel);

            cardNumberValueLabel = new Label
            {
                Size = new System.Drawing.Size(120, 20),
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Font = new System.Drawing.Font(Font.Name, 8f),
                Padding = new Padding(10, 0, 10, 0)
            };

            container.Controls.Add(cardNumberValueLabel);

            cardOwnerLabel = new Label
            {
                Text = Resources.owner_label,
                Size = new System.Drawing.Size(120, 20),
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Font = new System.Drawing.Font(Font.Name, 9f, System.Drawing.FontStyle.Bold),
                Padding = new Padding(10, 0, 10, 0)
            };

            container.Controls.Add(cardOwnerLabel);

            cardOwnerValueLabel = new Label
            {
                Size = new System.Drawing.Size(120, 20),
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Font = new System.Drawing.Font(Font.Name, 8f),
                Padding = new Padding(10, 0, 10, 0)
            };

            container.Controls.Add(cardOwnerValueLabel);

            this.Controls.Add(container);
        }

        public void UpdateLayout(CieModel cieModel)
        {
            cardNumberValueLabel.Text = cieModel.SerialNumber;
            cardOwnerValueLabel.Text = cieModel.Owner;
        }

        override public void UpdateChildrenSizeFactor(float factor)
        {
            foreach (Control control in this.container.Controls)
            {
                if (control is Label)
                {
                    var font = ((Label)control).Font;

                    font = new System.Drawing.Font(font.Name, font.Size - (font.Size * factor));
                }
                else
                {
                    var currentSize = control.Size;
                    control.Size = new System.Drawing.Size
                    {
                        Width = (int)(IMAGE_WIDTH - (IMAGE_WIDTH * factor)),
                        Height = (int)(IMAGE_HEIGHT - (IMAGE_HEIGHT * factor))
                    };
                }
            }
        }
    }
}
