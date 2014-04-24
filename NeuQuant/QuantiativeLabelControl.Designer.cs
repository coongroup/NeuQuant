namespace NeuQuant
{
    partial class QuantiativeLabelControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.panel1 = new System.Windows.Forms.Panel();
            this.SecondaryLabel = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.Label = new System.Windows.Forms.ComboBox();
            this.LabelType = new System.Windows.Forms.ComboBox();
            this.removeButton = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.NominalMass = new System.Windows.Forms.ComboBox();
            this.NameTextBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.SystemColors.ControlLight;
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.SecondaryLabel);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.Label);
            this.panel1.Controls.Add(this.LabelType);
            this.panel1.Controls.Add(this.removeButton);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.NominalMass);
            this.panel1.Controls.Add(this.NameTextBox);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(502, 55);
            this.panel1.TabIndex = 0;
            // 
            // SecondaryLabel
            // 
            this.SecondaryLabel.FormattingEnabled = true;
            this.SecondaryLabel.Location = new System.Drawing.Point(253, 28);
            this.SecondaryLabel.Name = "SecondaryLabel";
            this.SecondaryLabel.Size = new System.Drawing.Size(188, 21);
            this.SecondaryLabel.TabIndex = 9;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(5, 29);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(47, 16);
            this.label3.TabIndex = 8;
            this.label3.Text = "Label";
            // 
            // Label
            // 
            this.Label.FormattingEnabled = true;
            this.Label.Location = new System.Drawing.Point(58, 28);
            this.Label.Name = "Label";
            this.Label.Size = new System.Drawing.Size(188, 21);
            this.Label.TabIndex = 7;
            // 
            // LabelType
            // 
            this.LabelType.FormattingEnabled = true;
            this.LabelType.Items.AddRange(new object[] {
            "Amino Acid",
            "Two Amino Acids",
            "Chemical Label"});
            this.LabelType.Location = new System.Drawing.Point(363, 2);
            this.LabelType.Name = "LabelType";
            this.LabelType.Size = new System.Drawing.Size(112, 21);
            this.LabelType.TabIndex = 6;
            this.LabelType.SelectedIndexChanged += new System.EventHandler(this.comboBox2_SelectedIndexChanged);
            // 
            // removeButton
            // 
            this.removeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.removeButton.BackColor = System.Drawing.SystemColors.ControlLight;
            this.removeButton.FlatAppearance.BorderSize = 0;
            this.removeButton.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.ControlLight;
            this.removeButton.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.ControlLight;
            this.removeButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.removeButton.Image = global::NeuQuant.Properties.Resources.close16;
            this.removeButton.Location = new System.Drawing.Point(481, 2);
            this.removeButton.MaximumSize = new System.Drawing.Size(16, 16);
            this.removeButton.Name = "removeButton";
            this.removeButton.Size = new System.Drawing.Size(16, 16);
            this.removeButton.TabIndex = 5;
            this.removeButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.removeButton.UseVisualStyleBackColor = false;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(250, 6);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(44, 16);
            this.label2.TabIndex = 4;
            this.label2.Text = "Type";
            // 
            // NominalMass
            // 
            this.NominalMass.FormattingEnabled = true;
            this.NominalMass.Location = new System.Drawing.Point(300, 2);
            this.NominalMass.Name = "NominalMass";
            this.NominalMass.Size = new System.Drawing.Size(57, 21);
            this.NominalMass.TabIndex = 3;
            this.NominalMass.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
            // 
            // NameTextBox
            // 
            this.NameTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.NameTextBox.Location = new System.Drawing.Point(58, 2);
            this.NameTextBox.Name = "NameTextBox";
            this.NameTextBox.Size = new System.Drawing.Size(186, 20);
            this.NameTextBox.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(3, 6);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(49, 16);
            this.label1.TabIndex = 0;
            this.label1.Text = "Name";
            // 
            // QuantiativeLabelControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panel1);
            this.Name = "QuantiativeLabelControl";
            this.Size = new System.Drawing.Size(502, 55);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        public System.Windows.Forms.Button removeButton;
        private System.Windows.Forms.Label label3;
        public System.Windows.Forms.TextBox NameTextBox;
        public System.Windows.Forms.ComboBox NominalMass;
        public System.Windows.Forms.ComboBox LabelType;
        public System.Windows.Forms.ComboBox Label;
        public System.Windows.Forms.ComboBox SecondaryLabel;
    }
}
