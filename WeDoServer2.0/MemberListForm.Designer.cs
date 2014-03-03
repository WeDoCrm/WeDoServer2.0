namespace WDMsgServer
{
    partial class MemberListForm
    {
        /// <summary>
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 디자이너에서 생성한 코드

        /// <summary>
        /// 디자이너 지원에 필요한 메서드입니다.
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마십시오.
        /// </summary>
        private void InitializeComponent()
        {
            this.ListTable = new System.Windows.Forms.ListView();
            this.id = new System.Windows.Forms.ColumnHeader();
            this.conName = new System.Windows.Forms.ColumnHeader();
            this.groupName = new System.Windows.Forms.ColumnHeader();
            this.memStat = new System.Windows.Forms.ColumnHeader();
            this.connTime = new System.Windows.Forms.ColumnHeader();
            this.SuspendLayout();
            // 
            // ListTable
            // 
            this.ListTable.Activation = System.Windows.Forms.ItemActivation.OneClick;
            this.ListTable.AllowColumnReorder = true;
            this.ListTable.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.ListTable.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.id,
            this.conName,
            this.groupName,
            this.memStat,
            this.connTime});
            this.ListTable.GridLines = true;
            this.ListTable.Location = new System.Drawing.Point(0, 0);
            this.ListTable.Name = "ListTable";
            this.ListTable.Size = new System.Drawing.Size(404, 404);
            this.ListTable.Sorting = System.Windows.Forms.SortOrder.Descending;
            this.ListTable.TabIndex = 5;
            this.ListTable.UseCompatibleStateImageBehavior = false;
            this.ListTable.View = System.Windows.Forms.View.Details;
            // 
            // id
            // 
            this.id.Text = "아이디";
            this.id.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.id.Width = 87;
            // 
            // conName
            // 
            this.conName.Text = "이름";
            this.conName.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.conName.Width = 65;
            // 
            // groupName
            // 
            this.groupName.Text = "소속";
            this.groupName.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.groupName.Width = 64;
            // 
            // memStat
            // 
            this.memStat.Text = "현재상태";
            this.memStat.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.memStat.Width = 65;
            // 
            // connTime
            // 
            this.connTime.Text = "접속시각";
            this.connTime.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.connTime.Width = 119;
            // 
            // MemberListForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(405, 407);
            this.Controls.Add(this.ListTable);
            this.Name = "MemberListForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "전체 사용자 현황";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MemberListForm_FormClosing);
            this.ResumeLayout(false);

        }

        #endregion

        public System.Windows.Forms.ListView ListTable;
        private System.Windows.Forms.ColumnHeader conName;
        private System.Windows.Forms.ColumnHeader memStat;
        private System.Windows.Forms.ColumnHeader groupName;
        private System.Windows.Forms.ColumnHeader connTime;
        private System.Windows.Forms.ColumnHeader id;
    }
}