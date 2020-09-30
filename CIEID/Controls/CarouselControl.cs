using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CIEID.Controls
{
    class CarouselControl : FlowLayoutPanel {

        private const int DOT_SIZE = 10;

        public class ButtonsEventArgs : EventArgs
        {
            public bool IsRightButton { get; set; }
            public bool IsEnabled { get; set; }
            public bool IsVisible { get; set; }

            public ButtonsEventArgs(bool isRightButton, bool isEnabled) : this(isRightButton, isEnabled, true)
            { }

            public ButtonsEventArgs(bool isRightButton, bool isEnabled, bool isVisible)
            {
                this.IsRightButton = isRightButton;
                this.IsEnabled = isEnabled;
                this.IsVisible = isVisible;
            }
        }

        private EventHandler<ButtonsEventArgs> onButtonsChanged;

        public event EventHandler<ButtonsEventArgs> ButtonsChanged
        {
            add
            {
                onButtonsChanged += value;
            }
            remove
            {
                onButtonsChanged -= value;
            }
        }

        public CircularViewList Items {
            get
            {
                return circularList;
            }
            set
            {
                if (value == null)
                    return;

                circularList = new CircularViewList();

                activeItemIndex = 1;

                if (value.Count == 1)
                {
                    circularList.Add(new EmptyItemControl());
                    circularList.Add(value.First());
                    circularList.Add(new EmptyItemControl());
                }
                else if (value.Count == 2)
                {
                    circularList.Add(new EmptyItemControl());
                    circularList.AddRange(value);

                    activeItemIndex = 0;
                }
                else
                {
                    circularList.AddRange(value);
                }

                UpdateButtons();
            }
        }

        public int CarouselItemsCount
        {
            get
            {
                return Items.Count(x => x is CarouselItemControl);
            }
        }

        private CircularViewList circularList;
        private FlowLayoutPanel cardsContainer;
        private TableLayoutPanel dotsContainer;
        private FlowLayoutPanel dotsGroup;

        private int activeItemIndex { get; set; }
        private bool dotsCreated = false;

        public void LoadData(CieCollection cieCollection)
        {
            var controls = from model in cieCollection.MyDictionary.Values
                           select new CarouselItemControl(model)
                           {
                               Size = new System.Drawing.Size(BaseItemControl.IMAGE_WIDTH, BaseItemControl.IMAGE_HEIGHT),
                           };

            Items = new CircularViewList(controls);
        }

        public new void PerformLayout()
        {
            this.FlowDirection = FlowDirection.TopDown;
            this.WrapContents = true;
            this.AutoSize = true;


            SuspendLayout();

            this.cardsContainer = new FlowLayoutPanel();
            this.cardsContainer.FlowDirection = FlowDirection.LeftToRight;
            this.cardsContainer.WrapContents = false;
            this.cardsContainer.AutoSize = true;
            this.cardsContainer.MaximumSize = new System.Drawing.Size(this.Width, 210);
            this.cardsContainer.Anchor = (AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom);

            this.Controls.Add(this.cardsContainer);

            this.dotsContainer = new TableLayoutPanel();
            this.dotsContainer.RowCount = 1;
            this.dotsContainer.ColumnCount = 1;
            this.dotsContainer.GrowStyle = TableLayoutPanelGrowStyle.AddColumns;
            this.dotsContainer.AutoSize = true;
            this.dotsContainer.Anchor = (AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom);

            this.Controls.Add(this.dotsContainer);

            this.dotsGroup = new FlowLayoutPanel();
            this.dotsGroup.FlowDirection = FlowDirection.LeftToRight;
            this.dotsGroup.AutoSize = true;
            this.dotsGroup.WrapContents = true;
            this.dotsGroup.Anchor = AnchorStyles.None;

            this.dotsContainer.Controls.Add(this.dotsGroup);

            ResumeLayout(false);
        }

        private void UpdateButtons()
        {
            if (CarouselItemsCount > 2)
            {
                onButtonsChanged?.Invoke(this, new ButtonsEventArgs(false, true));
                onButtonsChanged?.Invoke(this, new ButtonsEventArgs(true, true));
            } else
            if (CarouselItemsCount == 2) {
                onButtonsChanged?.Invoke(this, new ButtonsEventArgs(false, circularList.LastOrDefault() is EmptyItemControl));
                onButtonsChanged?.Invoke(this, new ButtonsEventArgs(true, circularList.FirstOrDefault() is EmptyItemControl));
            } else
            {
                onButtonsChanged?.Invoke(this, new ButtonsEventArgs(false, false, false));
                onButtonsChanged?.Invoke(this, new ButtonsEventArgs(true, false, false));
            }
        }

        public void ShiftLeft(int steps = 1)
        {
            while (steps-- > 0)
            {
                circularList.ShiftLeft();

                activeItemIndex++;

                if (activeItemIndex >= CarouselItemsCount)
                    activeItemIndex = 0;
            }

            UpdateLayout();
            UpdateButtons();
            UpdateDots();
        }

        public void ShiftRight(int steps = 1)
        {
            while (steps-- > 0)
            {
                circularList.ShiftRight();

                activeItemIndex--;

                if (activeItemIndex < 0)
                    activeItemIndex = CarouselItemsCount - 1;
            }

            UpdateLayout();
            UpdateButtons();
            UpdateDots();
        }

        private void UpdateDots()
        {
            var radioButton = (RadioButton)this.dotsGroup.Controls[activeItemIndex];

            radioButton.CheckedChanged -= dot_CheckedChanged;
            radioButton.Checked = true;
            radioButton.CheckedChanged += dot_CheckedChanged;
        }

        public void CreateDots()
        {
            this.dotsGroup.Controls.Clear();

            if (Items != null && CarouselItemsCount > 1)
            {
                for (int i = 0; i < CarouselItemsCount; i++)
                {
                    var dot = new CustomRadioButton
                    {
                        Name = String.Format("Dot#{0}", i),
                        Size = new System.Drawing.Size(DOT_SIZE, DOT_SIZE),
                        Anchor = (AnchorStyles.None),
                        Checked = activeItemIndex == i
                    };

                    dot.CheckedChanged += dot_CheckedChanged;
                    
                    this.dotsGroup.Controls.Add(dot);
                }
            }

            dotsCreated = true;
        }

        private void dot_CheckedChanged(object sender, EventArgs e)
        {
            var radioButton = (RadioButton)sender;

            if (radioButton.Checked)
            {
                var index = this.dotsGroup.Controls.GetChildIndex(radioButton);
                var diff = Math.Abs(activeItemIndex - index);
                Console.WriteLine("on checked #{0}", index);

                if (activeItemIndex < index)
                {
                    ShiftLeft(diff);
                } else if (activeItemIndex > index)
                {
                    ShiftRight(diff);
                }

                activeItemIndex = index;
            }
        }

        public void UpdateLayout()
        {
            if (circularList != null && circularList.Count > 0)
            {
                this.cardsContainer.Controls.Clear();
                var itemWidth = (this.Width / 3);
                var index = 0;

                foreach (var item in circularList.Take(3))
                {
                    item.Location = new System.Drawing.Point(itemWidth * index, 0);
                    item.MaximumSize = new System.Drawing.Size(itemWidth, this.Height);

                    if (1 == index)
                    {
                        item.UpdateChildrenSizeFactor(-0.35f);
                    } else
                    {
                        item.UpdateChildrenSizeFactor(0.2f);
                    }

                    this.cardsContainer.Controls.Add(item);

                    index++;
                }
            }

            if (!dotsCreated)
                CreateDots();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ResumeLayout(false);
        }
    }
}