namespace KPI_measuring_software
{
    partial class Main_Window
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
            this.screenShowBox = new System.Windows.Forms.PictureBox();
            this.ScreenChooseNextButton = new System.Windows.Forms.Button();
            this.ChooseThisScreenButton = new System.Windows.Forms.Button();
            this.ErrorBox = new System.Windows.Forms.TextBox();
            this.ScreenShowPreviousButton = new System.Windows.Forms.Button();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.TestEnvironmentOptions = new System.Windows.Forms.ComboBox();
            this.ChooseEnvironmentLabel = new System.Windows.Forms.Label();
            this.ChooseEnvironmentOkButton = new System.Windows.Forms.Button();
            this.Options = new System.Windows.Forms.CheckedListBox();
            this.ChooseEnvironmentLanguageLabel = new System.Windows.Forms.Label();
            this.LanguageDropdown = new System.Windows.Forms.ComboBox();
            this.OptionsConfirmButton = new System.Windows.Forms.Button();
            this.OptionsIterationValueTextBox = new System.Windows.Forms.TextBox();
            this.STBIPLabel = new System.Windows.Forms.Label();
            this.TestButton = new System.Windows.Forms.Button();
            this.TakeScreenshotButton = new System.Windows.Forms.Button();
            this.STBIPInputBox = new System.Windows.Forms.TextBox();
            this.STBIPInputOkButton = new System.Windows.Forms.Button();
            this.ChooseCountryLabel = new System.Windows.Forms.Label();
            this.ChooseCountryOkButton = new System.Windows.Forms.Button();
            this.iterationValueLabel = new System.Windows.Forms.Label();
            this.cycleValueTextBox = new System.Windows.Forms.TextBox();
            this.cycleValueLabel = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.textBox3 = new System.Windows.Forms.TextBox();
            this.textBox4 = new System.Windows.Forms.TextBox();
            this.textBox5 = new System.Windows.Forms.TextBox();
            this.textBox6 = new System.Windows.Forms.TextBox();
            this.deviceDetailsConfirmButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.screenShowBox)).BeginInit();
            this.SuspendLayout();
            // 
            // screenShowBox
            // 
            this.screenShowBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.screenShowBox.Location = new System.Drawing.Point(12, 12);
            this.screenShowBox.Name = "screenShowBox";
            this.screenShowBox.Size = new System.Drawing.Size(1203, 427);
            this.screenShowBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.screenShowBox.TabIndex = 1;
            this.screenShowBox.TabStop = false;
            // 
            // ScreenChooseNextButton
            // 
            this.ScreenChooseNextButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.ScreenChooseNextButton.Location = new System.Drawing.Point(719, 473);
            this.ScreenChooseNextButton.Name = "ScreenChooseNextButton";
            this.ScreenChooseNextButton.Size = new System.Drawing.Size(149, 47);
            this.ScreenChooseNextButton.TabIndex = 2;
            this.ScreenChooseNextButton.Text = "Next";
            this.ScreenChooseNextButton.UseVisualStyleBackColor = true;
            this.ScreenChooseNextButton.Click += new System.EventHandler(this.ScreenChooseNextButton_Click);
            // 
            // ChooseThisScreenButton
            // 
            this.ChooseThisScreenButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.ChooseThisScreenButton.Location = new System.Drawing.Point(551, 473);
            this.ChooseThisScreenButton.Name = "ChooseThisScreenButton";
            this.ChooseThisScreenButton.Size = new System.Drawing.Size(147, 47);
            this.ChooseThisScreenButton.TabIndex = 3;
            this.ChooseThisScreenButton.Text = "Choose this screen";
            this.ChooseThisScreenButton.UseVisualStyleBackColor = true;
            this.ChooseThisScreenButton.Click += new System.EventHandler(this.ChooseThisScreenButton_Click);
            // 
            // ErrorBox
            // 
            this.ErrorBox.Location = new System.Drawing.Point(144, 52);
            this.ErrorBox.Multiline = true;
            this.ErrorBox.Name = "ErrorBox";
            this.ErrorBox.Size = new System.Drawing.Size(323, 100);
            this.ErrorBox.TabIndex = 4;
            // 
            // ScreenShowPreviousButton
            // 
            this.ScreenShowPreviousButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.ScreenShowPreviousButton.Location = new System.Drawing.Point(383, 473);
            this.ScreenShowPreviousButton.Name = "ScreenShowPreviousButton";
            this.ScreenShowPreviousButton.Size = new System.Drawing.Size(149, 47);
            this.ScreenShowPreviousButton.TabIndex = 5;
            this.ScreenShowPreviousButton.Text = "Previous";
            this.ScreenShowPreviousButton.UseVisualStyleBackColor = true;
            this.ScreenShowPreviousButton.Click += new System.EventHandler(this.ScreenShowPreviousButton_Click);
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(46, 12);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(114, 24);
            this.checkBox1.TabIndex = 6;
            this.checkBox1.Text = "Browser Test";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // TestEnvironmentOptions
            // 
            this.TestEnvironmentOptions.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.TestEnvironmentOptions.FormattingEnabled = true;
            this.TestEnvironmentOptions.Location = new System.Drawing.Point(539, 102);
            this.TestEnvironmentOptions.Name = "TestEnvironmentOptions";
            this.TestEnvironmentOptions.Size = new System.Drawing.Size(151, 28);
            this.TestEnvironmentOptions.TabIndex = 7;
            // 
            // ChooseEnvironmentLabel
            // 
            this.ChooseEnvironmentLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)));
            this.ChooseEnvironmentLabel.AutoSize = true;
            this.ChooseEnvironmentLabel.Location = new System.Drawing.Point(562, 55);
            this.ChooseEnvironmentLabel.Name = "ChooseEnvironmentLabel";
            this.ChooseEnvironmentLabel.Size = new System.Drawing.Size(107, 20);
            this.ChooseEnvironmentLabel.TabIndex = 8;
            this.ChooseEnvironmentLabel.Text = "Choose Device";
            // 
            // ChooseEnvironmentOkButton
            // 
            this.ChooseEnvironmentOkButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.ChooseEnvironmentOkButton.Location = new System.Drawing.Point(552, 473);
            this.ChooseEnvironmentOkButton.Name = "ChooseEnvironmentOkButton";
            this.ChooseEnvironmentOkButton.Size = new System.Drawing.Size(149, 47);
            this.ChooseEnvironmentOkButton.TabIndex = 9;
            this.ChooseEnvironmentOkButton.Text = "OK";
            this.ChooseEnvironmentOkButton.UseVisualStyleBackColor = true;
            this.ChooseEnvironmentOkButton.Click += new System.EventHandler(this.ChooseEnvironmentOkButton_Click);
            // 
            // Options
            // 
            this.Options.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.Options.CheckOnClick = true;
            this.Options.FormattingEnabled = true;
            this.Options.Location = new System.Drawing.Point(443, 146);
            this.Options.Name = "Options";
            this.Options.Size = new System.Drawing.Size(320, 180);
            this.Options.TabIndex = 10;
            // 
            // ChooseEnvironmentLanguageLabel
            // 
            this.ChooseEnvironmentLanguageLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)));
            this.ChooseEnvironmentLanguageLabel.AutoSize = true;
            this.ChooseEnvironmentLanguageLabel.Location = new System.Drawing.Point(562, 55);
            this.ChooseEnvironmentLanguageLabel.Name = "ChooseEnvironmentLanguageLabel";
            this.ChooseEnvironmentLanguageLabel.Size = new System.Drawing.Size(113, 20);
            this.ChooseEnvironmentLanguageLabel.TabIndex = 11;
            this.ChooseEnvironmentLanguageLabel.Text = "Choose Country";
            // 
            // LanguageDropdown
            // 
            this.LanguageDropdown.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.LanguageDropdown.FormattingEnabled = true;
            this.LanguageDropdown.Location = new System.Drawing.Point(539, 102);
            this.LanguageDropdown.Name = "LanguageDropdown";
            this.LanguageDropdown.Size = new System.Drawing.Size(151, 28);
            this.LanguageDropdown.TabIndex = 12;
            // 
            // OptionsConfirmButton
            // 
            this.OptionsConfirmButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.OptionsConfirmButton.Location = new System.Drawing.Point(552, 473);
            this.OptionsConfirmButton.Name = "OptionsConfirmButton";
            this.OptionsConfirmButton.Size = new System.Drawing.Size(149, 47);
            this.OptionsConfirmButton.TabIndex = 13;
            this.OptionsConfirmButton.Text = "OK";
            this.OptionsConfirmButton.UseVisualStyleBackColor = true;
            this.OptionsConfirmButton.Click += new System.EventHandler(this.OptionsConfirmButton_Click);
            // 
            // OptionsIterationValueTextBox
            // 
            this.OptionsIterationValueTextBox.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.OptionsIterationValueTextBox.Location = new System.Drawing.Point(893, 281);
            this.OptionsIterationValueTextBox.Name = "OptionsIterationValueTextBox";
            this.OptionsIterationValueTextBox.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.OptionsIterationValueTextBox.Size = new System.Drawing.Size(40, 27);
            this.OptionsIterationValueTextBox.TabIndex = 14;
            this.OptionsIterationValueTextBox.Text = "5";
            // 
            // STBIPLabel
            // 
            this.STBIPLabel.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.STBIPLabel.AutoSize = true;
            this.STBIPLabel.Location = new System.Drawing.Point(562, 55);
            this.STBIPLabel.Name = "STBIPLabel";
            this.STBIPLabel.Size = new System.Drawing.Size(88, 20);
            this.STBIPLabel.TabIndex = 15;
            this.STBIPLabel.Text = "Input STB IP";
            // 
            // TestButton
            // 
            this.TestButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.TestButton.Location = new System.Drawing.Point(1033, 473);
            this.TestButton.Name = "TestButton";
            this.TestButton.Size = new System.Drawing.Size(149, 47);
            this.TestButton.TabIndex = 16;
            this.TestButton.Text = "TestButton";
            this.TestButton.UseVisualStyleBackColor = true;
            this.TestButton.Click += new System.EventHandler(this.TestButton_Click);
            // 
            // TakeScreenshotButton
            // 
            this.TakeScreenshotButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.TakeScreenshotButton.Location = new System.Drawing.Point(1033, 407);
            this.TakeScreenshotButton.Name = "TakeScreenshotButton";
            this.TakeScreenshotButton.Size = new System.Drawing.Size(149, 60);
            this.TakeScreenshotButton.TabIndex = 17;
            this.TakeScreenshotButton.Text = "Screenshot To Templates";
            this.TakeScreenshotButton.UseVisualStyleBackColor = true;
            this.TakeScreenshotButton.Click += new System.EventHandler(this.TakeScreenshotButton_Click);
            // 
            // STBIPInputBox
            // 
            this.STBIPInputBox.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.STBIPInputBox.Location = new System.Drawing.Point(552, 103);
            this.STBIPInputBox.Name = "STBIPInputBox";
            this.STBIPInputBox.Size = new System.Drawing.Size(125, 27);
            this.STBIPInputBox.TabIndex = 18;
            // 
            // STBIPInputOkButton
            // 
            this.STBIPInputOkButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.STBIPInputOkButton.Location = new System.Drawing.Point(552, 473);
            this.STBIPInputOkButton.Name = "STBIPInputOkButton";
            this.STBIPInputOkButton.Size = new System.Drawing.Size(149, 47);
            this.STBIPInputOkButton.TabIndex = 19;
            this.STBIPInputOkButton.Text = "OK";
            this.STBIPInputOkButton.UseVisualStyleBackColor = true;
            this.STBIPInputOkButton.Click += new System.EventHandler(this.STBIPInputOkButton_Click);
            // 
            // ChooseCountryLabel
            // 
            this.ChooseCountryLabel.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.ChooseCountryLabel.AutoSize = true;
            this.ChooseCountryLabel.Location = new System.Drawing.Point(562, 55);
            this.ChooseCountryLabel.Name = "ChooseCountryLabel";
            this.ChooseCountryLabel.Size = new System.Drawing.Size(113, 20);
            this.ChooseCountryLabel.TabIndex = 20;
            this.ChooseCountryLabel.Text = "Choose Country";
            // 
            // ChooseCountryOkButton
            // 
            this.ChooseCountryOkButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.ChooseCountryOkButton.Location = new System.Drawing.Point(552, 473);
            this.ChooseCountryOkButton.Name = "ChooseCountryOkButton";
            this.ChooseCountryOkButton.Size = new System.Drawing.Size(149, 47);
            this.ChooseCountryOkButton.TabIndex = 21;
            this.ChooseCountryOkButton.Text = "OK";
            this.ChooseCountryOkButton.UseVisualStyleBackColor = true;
            this.ChooseCountryOkButton.Click += new System.EventHandler(this.ChooseCountryOkButton_Click);
            // 
            // iterationValueLabel
            // 
            this.iterationValueLabel.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.iterationValueLabel.AutoSize = true;
            this.iterationValueLabel.Location = new System.Drawing.Point(783, 247);
            this.iterationValueLabel.Name = "iterationValueLabel";
            this.iterationValueLabel.Size = new System.Drawing.Size(271, 20);
            this.iterationValueLabel.TabIndex = 22;
            this.iterationValueLabel.Text = "Required amount of iterations per cycle";
            // 
            // cycleValueTextBox
            // 
            this.cycleValueTextBox.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.cycleValueTextBox.Location = new System.Drawing.Point(893, 206);
            this.cycleValueTextBox.Name = "cycleValueTextBox";
            this.cycleValueTextBox.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.cycleValueTextBox.Size = new System.Drawing.Size(40, 27);
            this.cycleValueTextBox.TabIndex = 23;
            this.cycleValueTextBox.Text = "5";
            // 
            // cycleValueLabel
            // 
            this.cycleValueLabel.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.cycleValueLabel.AutoSize = true;
            this.cycleValueLabel.Location = new System.Drawing.Point(824, 171);
            this.cycleValueLabel.Name = "cycleValueLabel";
            this.cycleValueLabel.Size = new System.Drawing.Size(185, 20);
            this.cycleValueLabel.TabIndex = 24;
            this.cycleValueLabel.Text = "Required amount of cycles";
            // 
            // label1
            // 
            this.label1.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(419, 106);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(196, 20);
            this.label1.TabIndex = 25;
            this.label1.Text = "Identification Of Connection";
            // 
            // label2
            // 
            this.label2.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(419, 146);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(89, 20);
            this.label2.TabIndex = 26;
            this.label2.Text = "Device Type";
            // 
            // label3
            // 
            this.label3.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(419, 186);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(73, 20);
            this.label3.TabIndex = 27;
            this.label3.Text = "Device ID";
            // 
            // label4
            // 
            this.label4.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(419, 224);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(80, 20);
            this.label4.TabIndex = 28;
            this.label4.Text = "OS Version";
            // 
            // label5
            // 
            this.label5.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(419, 262);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(121, 20);
            this.label5.TabIndex = 29;
            this.label5.Text = "Firmware version";
            // 
            // label6
            // 
            this.label6.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(419, 306);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(96, 20);
            this.label6.TabIndex = 30;
            this.label6.Text = "MAC address";
            // 
            // textBox1
            // 
            this.textBox1.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.textBox1.Location = new System.Drawing.Point(638, 102);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(125, 27);
            this.textBox1.TabIndex = 31;
            // 
            // textBox2
            // 
            this.textBox2.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.textBox2.Location = new System.Drawing.Point(638, 146);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(125, 27);
            this.textBox2.TabIndex = 32;
            // 
            // textBox3
            // 
            this.textBox3.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.textBox3.Location = new System.Drawing.Point(638, 186);
            this.textBox3.Name = "textBox3";
            this.textBox3.Size = new System.Drawing.Size(125, 27);
            this.textBox3.TabIndex = 33;
            // 
            // textBox4
            // 
            this.textBox4.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.textBox4.Location = new System.Drawing.Point(638, 224);
            this.textBox4.Name = "textBox4";
            this.textBox4.Size = new System.Drawing.Size(125, 27);
            this.textBox4.TabIndex = 34;
            // 
            // textBox5
            // 
            this.textBox5.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.textBox5.Location = new System.Drawing.Point(638, 262);
            this.textBox5.Name = "textBox5";
            this.textBox5.Size = new System.Drawing.Size(125, 27);
            this.textBox5.TabIndex = 35;
            // 
            // textBox6
            // 
            this.textBox6.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.textBox6.Location = new System.Drawing.Point(638, 299);
            this.textBox6.Name = "textBox6";
            this.textBox6.Size = new System.Drawing.Size(125, 27);
            this.textBox6.TabIndex = 36;
            // 
            // deviceDetailsConfirmButton
            // 
            this.deviceDetailsConfirmButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.deviceDetailsConfirmButton.Location = new System.Drawing.Point(552, 473);
            this.deviceDetailsConfirmButton.Name = "deviceDetailsConfirmButton";
            this.deviceDetailsConfirmButton.Size = new System.Drawing.Size(149, 47);
            this.deviceDetailsConfirmButton.TabIndex = 37;
            this.deviceDetailsConfirmButton.Text = "OK";
            this.deviceDetailsConfirmButton.UseVisualStyleBackColor = true;
            this.deviceDetailsConfirmButton.Click += new System.EventHandler(this.deviceDetailsConfirmButton_Click);
            // 
            // Main_Window
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1227, 548);
            this.Controls.Add(this.deviceDetailsConfirmButton);
            this.Controls.Add(this.textBox6);
            this.Controls.Add(this.textBox5);
            this.Controls.Add(this.textBox4);
            this.Controls.Add(this.textBox3);
            this.Controls.Add(this.textBox2);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cycleValueLabel);
            this.Controls.Add(this.cycleValueTextBox);
            this.Controls.Add(this.iterationValueLabel);
            this.Controls.Add(this.ChooseCountryOkButton);
            this.Controls.Add(this.ChooseCountryLabel);
            this.Controls.Add(this.STBIPInputOkButton);
            this.Controls.Add(this.STBIPInputBox);
            this.Controls.Add(this.TakeScreenshotButton);
            this.Controls.Add(this.TestButton);
            this.Controls.Add(this.STBIPLabel);
            this.Controls.Add(this.OptionsIterationValueTextBox);
            this.Controls.Add(this.OptionsConfirmButton);
            this.Controls.Add(this.LanguageDropdown);
            this.Controls.Add(this.ChooseEnvironmentLanguageLabel);
            this.Controls.Add(this.Options);
            this.Controls.Add(this.ChooseEnvironmentOkButton);
            this.Controls.Add(this.ChooseEnvironmentLabel);
            this.Controls.Add(this.TestEnvironmentOptions);
            this.Controls.Add(this.checkBox1);
            this.Controls.Add(this.ScreenShowPreviousButton);
            this.Controls.Add(this.ErrorBox);
            this.Controls.Add(this.ChooseThisScreenButton);
            this.Controls.Add(this.ScreenChooseNextButton);
            this.Controls.Add(this.screenShowBox);
            this.Name = "Main_Window";
            this.Text = "KPI measuring software";
            ((System.ComponentModel.ISupportInitialize)(this.screenShowBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private PictureBox screenShowBox;
        private Button ScreenChooseNextButton;
        private Button ChooseThisScreenButton;
        private TextBox ErrorBox;
        private Button ScreenShowPreviousButton;
        private CheckBox checkBox1;
        private ComboBox TestEnvironmentOptions;
        private Label ChooseEnvironmentLabel;
        private Button ChooseEnvironmentOkButton;
        private CheckedListBox Options;
        private Label ChooseEnvironmentLanguageLabel;
        private ComboBox LanguageDropdown;
        private Button OptionsConfirmButton;
        private TextBox OptionsIterationValueTextBox;
        private Label STBIPLabel;
        private Button TestButton;
        private Button TakeScreenshotButton;
        private TextBox STBIPInputBox;
        private Button STBIPInputOkButton;
        private Label ChooseCountryLabel;
        private Button ChooseCountryOkButton;
        private Label iterationValueLabel;
        private TextBox cycleValueTextBox;
        private Label cycleValueLabel;
        private Label label1;
        private Label label2;
        private Label label3;
        private Label label4;
        private Label label5;
        private Label label6;
        private TextBox textBox1;
        private TextBox textBox2;
        private TextBox textBox3;
        private TextBox textBox4;
        private TextBox textBox5;
        private TextBox textBox6;
        private Button deviceDetailsConfirmButton;
    }
}