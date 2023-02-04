namespace KPI_measuring_software
{
    partial class main_window
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
            this.OptionsRepeatValueTextBox = new System.Windows.Forms.TextBox();
            this.STBIPLabel = new System.Windows.Forms.Label();
            this.TestButton = new System.Windows.Forms.Button();
            this.TakeScreenshotButton = new System.Windows.Forms.Button();
            this.STBIPInputBox = new System.Windows.Forms.TextBox();
            this.STBIPInputOkButton = new System.Windows.Forms.Button();
            this.ChooseCountryLabel = new System.Windows.Forms.Label();
            this.ChooseCountryOkButton = new System.Windows.Forms.Button();
            this.repeatValueLabel = new System.Windows.Forms.Label();
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
            this.ChooseEnvironmentLabel.Location = new System.Drawing.Point(528, 55);
            this.ChooseEnvironmentLabel.Name = "ChooseEnvironmentLabel";
            this.ChooseEnvironmentLabel.Size = new System.Drawing.Size(173, 20);
            this.ChooseEnvironmentLabel.TabIndex = 8;
            this.ChooseEnvironmentLabel.Text = "Choose test environment";
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
            this.Options.Location = new System.Drawing.Point(539, 146);
            this.Options.Name = "Options";
            this.Options.Size = new System.Drawing.Size(224, 180);
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
            // OptionsRepeatValueTextBox
            // 
            this.OptionsRepeatValueTextBox.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.OptionsRepeatValueTextBox.Location = new System.Drawing.Point(893, 240);
            this.OptionsRepeatValueTextBox.Name = "OptionsRepeatValueTextBox";
            this.OptionsRepeatValueTextBox.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.OptionsRepeatValueTextBox.Size = new System.Drawing.Size(40, 27);
            this.OptionsRepeatValueTextBox.TabIndex = 14;
            this.OptionsRepeatValueTextBox.Text = "5";
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
            this.STBIPInputBox.Location = new System.Drawing.Point(550, 215);
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
            // repeatValueLabel
            // 
            this.repeatValueLabel.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.repeatValueLabel.AutoSize = true;
            this.repeatValueLabel.Location = new System.Drawing.Point(791, 206);
            this.repeatValueLabel.Name = "repeatValueLabel";
            this.repeatValueLabel.Size = new System.Drawing.Size(242, 20);
            this.repeatValueLabel.TabIndex = 22;
            this.repeatValueLabel.Text = "Required amount of measurements";
            // 
            // main_window
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1227, 548);
            this.Controls.Add(this.repeatValueLabel);
            this.Controls.Add(this.ChooseCountryOkButton);
            this.Controls.Add(this.ChooseCountryLabel);
            this.Controls.Add(this.STBIPInputOkButton);
            this.Controls.Add(this.STBIPInputBox);
            this.Controls.Add(this.TakeScreenshotButton);
            this.Controls.Add(this.TestButton);
            this.Controls.Add(this.STBIPLabel);
            this.Controls.Add(this.OptionsRepeatValueTextBox);
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
            this.Name = "main_window";
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
        private TextBox OptionsRepeatValueTextBox;
        private Label STBIPLabel;
        private Button TestButton;
        private Button TakeScreenshotButton;
        private TextBox STBIPInputBox;
        private Button STBIPInputOkButton;
        private Label ChooseCountryLabel;
        private Button ChooseCountryOkButton;
        private Label repeatValueLabel;
    }
}