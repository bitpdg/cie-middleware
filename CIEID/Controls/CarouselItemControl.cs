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

        private const int IMAGE_WIDTH = 120;
        private const int IMAGE_HEIGHT = 70;

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
            container = new FlowLayoutPanel();
            container.WrapContents = false;
            container.AutoSize = true;
            container.AutoSizeMode = AutoSizeMode.GrowOnly;
            container.FlowDirection = FlowDirection.TopDown;
            container.Anchor = (AnchorStyles.Top);

            image = new PictureBox
            {
                Image = Properties.Resources.cie,
                SizeMode = PictureBoxSizeMode.StretchImage,
                Size = new System.Drawing.Size(IMAGE_WIDTH, IMAGE_HEIGHT)
            };

            container.Controls.Add(image);

            cardNumberLabel = new Label
            {
                Text = Resources.card_number_label,
                Size = new System.Drawing.Size(120, 20),
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 10, 0)
            };

            container.Controls.Add(cardNumberLabel);

            cardNumberValueLabel = new Label
            {
                Size = new System.Drawing.Size(120, 20),
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Font = new System.Drawing.Font(Font.Name, 10f),
                Padding = new Padding(10, 0, 10, 0)
            };

            container.Controls.Add(cardNumberValueLabel);

            cardOwnerLabel = new Label
            {
                Text = Resources.owner_label,
                Size = new System.Drawing.Size(120, 20),
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 10, 0)
            };

            container.Controls.Add(cardOwnerLabel);

            cardOwnerValueLabel = new Label
            {
                Size = new System.Drawing.Size(120, 20),
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Font = new System.Drawing.Font(Font.Name, 10f),
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
