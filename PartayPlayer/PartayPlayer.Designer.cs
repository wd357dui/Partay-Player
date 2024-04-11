namespace PartayPlayer
{
    partial class PartayPlayer
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
            web = new Microsoft.Web.WebView2.WinForms.WebView2();
            ((System.ComponentModel.ISupportInitialize)web).BeginInit();
            SuspendLayout();
            // 
            // web
            // 
            web.AllowExternalDrop = true;
            web.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            web.CreationProperties = null;
            web.DefaultBackgroundColor = Color.White;
            web.Location = new Point(0, 0);
            web.Name = "web";
            web.Size = new Size(1280, 720);
            web.TabIndex = 0;
            web.ZoomFactor = 1D;
            // 
            // PartayPlayer
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(1280, 720);
            Controls.Add(web);
            Name = "PartayPlayer";
            Text = "Partay Player";
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)web).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Microsoft.Web.WebView2.WinForms.WebView2 web;
    }
}
