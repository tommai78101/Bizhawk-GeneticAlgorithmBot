namespace GeneticAlgorithmBot {
	partial class NeatMappingRow {
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.inputLabel = new System.Windows.Forms.Label();
			this.outputSelectionBox = new System.Windows.Forms.ComboBox();
			this.SuspendLayout();
			// 
			// inputLabel
			// 
			this.inputLabel.Location = new System.Drawing.Point(4, 5);
			this.inputLabel.Name = "inputLabel";
			this.inputLabel.Size = new System.Drawing.Size(95, 13);
			this.inputLabel.TabIndex = 0;
			this.inputLabel.Text = "MyButton";
			// 
			// outputSelectionBox
			// 
			this.outputSelectionBox.FormattingEnabled = true;
			this.outputSelectionBox.Location = new System.Drawing.Point(103, 1);
			this.outputSelectionBox.Name = "outputSelectionBox";
			this.outputSelectionBox.Size = new System.Drawing.Size(95, 21);
			this.outputSelectionBox.TabIndex = 1;
			// 
			// NeatMappingRow
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.outputSelectionBox);
			this.Controls.Add(this.inputLabel);
			this.Name = "NeatMappingRow";
			this.Size = new System.Drawing.Size(200, 23);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Label inputLabel;
		private System.Windows.Forms.ComboBox outputSelectionBox;
	}
}
