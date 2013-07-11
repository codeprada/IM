namespace IM___Client
{
    partial class WindowMessenger
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WindowMessenger));
            this.mainOutputTxtBox = new System.Windows.Forms.TextBox();
            this.msgTextBox = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // mainOutputTxtBox
            // 
            this.mainOutputTxtBox.BackColor = System.Drawing.Color.White;
            this.mainOutputTxtBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.mainOutputTxtBox.Location = new System.Drawing.Point(13, 13);
            this.mainOutputTxtBox.Multiline = true;
            this.mainOutputTxtBox.Name = "mainOutputTxtBox";
            this.mainOutputTxtBox.ReadOnly = true;
            this.mainOutputTxtBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.mainOutputTxtBox.Size = new System.Drawing.Size(348, 193);
            this.mainOutputTxtBox.TabIndex = 1;
            // 
            // msgTextBox
            // 
            this.msgTextBox.Location = new System.Drawing.Point(12, 212);
            this.msgTextBox.Name = "msgTextBox";
            this.msgTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.msgTextBox.Size = new System.Drawing.Size(265, 20);
            this.msgTextBox.TabIndex = 0;
            this.msgTextBox.WordWrap = false;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(284, 212);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(77, 20);
            this.button1.TabIndex = 2;
            this.button1.Text = "Send";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // WindowMessenger
            // 
            this.AcceptButton = this.button1;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(373, 244);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.msgTextBox);
            this.Controls.Add(this.mainOutputTxtBox);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "WindowMessenger";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.WindowMessenger_FormClosed);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox mainOutputTxtBox;
        private System.Windows.Forms.TextBox msgTextBox;
        private System.Windows.Forms.Button button1;
    }
}