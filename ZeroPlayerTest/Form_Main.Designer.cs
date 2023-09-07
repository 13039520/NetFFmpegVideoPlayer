namespace ZeroPlayerTest
{
    partial class Form_Main
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            panel1 = new Panel();
            textBox_Msg = new TextBox();
            panel_CtrlBox = new Panel();
            textBox_PlayTime = new TextBox();
            button_FastForward = new Button();
            button_FastBackward = new Button();
            button_Stop = new Button();
            button_Pause = new Button();
            button_Play = new Button();
            panel2 = new Panel();
            pictureBox1 = new PictureBox();
            panel1.SuspendLayout();
            panel_CtrlBox.SuspendLayout();
            panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Controls.Add(textBox_Msg);
            panel1.Controls.Add(panel_CtrlBox);
            panel1.Dock = DockStyle.Right;
            panel1.Location = new Point(797, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(299, 579);
            panel1.TabIndex = 0;
            // 
            // textBox_Msg
            // 
            textBox_Msg.Dock = DockStyle.Fill;
            textBox_Msg.Location = new Point(0, 100);
            textBox_Msg.Multiline = true;
            textBox_Msg.Name = "textBox_Msg";
            textBox_Msg.ReadOnly = true;
            textBox_Msg.ScrollBars = ScrollBars.Vertical;
            textBox_Msg.Size = new Size(299, 479);
            textBox_Msg.TabIndex = 0;
            // 
            // panel_CtrlBox
            // 
            panel_CtrlBox.Controls.Add(textBox_PlayTime);
            panel_CtrlBox.Controls.Add(button_FastForward);
            panel_CtrlBox.Controls.Add(button_FastBackward);
            panel_CtrlBox.Controls.Add(button_Stop);
            panel_CtrlBox.Controls.Add(button_Pause);
            panel_CtrlBox.Controls.Add(button_Play);
            panel_CtrlBox.Dock = DockStyle.Top;
            panel_CtrlBox.Location = new Point(0, 0);
            panel_CtrlBox.Name = "panel_CtrlBox";
            panel_CtrlBox.Size = new Size(299, 100);
            panel_CtrlBox.TabIndex = 1;
            // 
            // textBox_PlayTime
            // 
            textBox_PlayTime.Location = new Point(11, 72);
            textBox_PlayTime.Name = "textBox_PlayTime";
            textBox_PlayTime.ReadOnly = true;
            textBox_PlayTime.Size = new Size(258, 23);
            textBox_PlayTime.TabIndex = 5;
            // 
            // button_FastForward
            // 
            button_FastForward.Location = new Point(142, 43);
            button_FastForward.Name = "button_FastForward";
            button_FastForward.Size = new Size(127, 23);
            button_FastForward.TabIndex = 4;
            button_FastForward.Text = "FastForward(10s)";
            button_FastForward.UseVisualStyleBackColor = true;
            // 
            // button_FastBackward
            // 
            button_FastBackward.Location = new Point(9, 43);
            button_FastBackward.Name = "button_FastBackward";
            button_FastBackward.Size = new Size(127, 23);
            button_FastBackward.TabIndex = 3;
            button_FastBackward.Text = "FastBackward(10s)";
            button_FastBackward.UseVisualStyleBackColor = true;
            // 
            // button_Stop
            // 
            button_Stop.Location = new Point(171, 12);
            button_Stop.Name = "button_Stop";
            button_Stop.Size = new Size(75, 23);
            button_Stop.TabIndex = 2;
            button_Stop.Text = "Stop";
            button_Stop.UseVisualStyleBackColor = true;
            // 
            // button_Pause
            // 
            button_Pause.Location = new Point(90, 12);
            button_Pause.Name = "button_Pause";
            button_Pause.Size = new Size(75, 23);
            button_Pause.TabIndex = 1;
            button_Pause.Text = "Pause";
            button_Pause.UseVisualStyleBackColor = true;
            // 
            // button_Play
            // 
            button_Play.Location = new Point(9, 12);
            button_Play.Name = "button_Play";
            button_Play.Size = new Size(75, 23);
            button_Play.TabIndex = 0;
            button_Play.Text = "Play";
            button_Play.UseVisualStyleBackColor = true;
            // 
            // panel2
            // 
            panel2.Controls.Add(pictureBox1);
            panel2.Dock = DockStyle.Fill;
            panel2.Location = new Point(0, 0);
            panel2.Name = "panel2";
            panel2.Size = new Size(797, 579);
            panel2.TabIndex = 1;
            // 
            // pictureBox1
            // 
            pictureBox1.BackColor = SystemColors.ActiveCaptionText;
            pictureBox1.BackgroundImageLayout = ImageLayout.Center;
            pictureBox1.Dock = DockStyle.Fill;
            pictureBox1.Location = new Point(0, 0);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(797, 579);
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            // 
            // Form_Main
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1096, 579);
            Controls.Add(panel2);
            Controls.Add(panel1);
            Name = "Form_Main";
            Text = "Form1";
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            panel_CtrlBox.ResumeLayout(false);
            panel_CtrlBox.PerformLayout();
            panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel panel1;
        private TextBox textBox_Msg;
        private Panel panel2;
        private PictureBox pictureBox1;
        private Panel panel_CtrlBox;
        private Button button_Play;
        private Button button_Stop;
        private Button button_Pause;
        private Button button_FastForward;
        private Button button_FastBackward;
        private TextBox textBox_PlayTime;
    }
}