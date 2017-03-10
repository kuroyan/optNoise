using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using System.IO;

// Ver 0.96 ( 2017.03.10 )
//(1) 2nd項目に絞って表示させる項目をツールバーに組み入れた。
//

// Ver 0.95 ( 2017.03.07 )
//(1) csv write のdataGridviewの切り替えと書き出し
//

namespace optNoise
{
    public partial class Form1 : Form
    {
        MySQL mysql = new MySQL();

        private string dataPic1Text = "";
        private string fileNameTemp = "";
        private int[] g_id = new int[20000];

        private bool selectCheckFlag = false;
        private bool seleCancelFlag  = false;


        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            mysql.setServerIP(dbSrverIPTXT.Text.Trim());
            DoWriteBTN.Enabled = false;
            cancel2BTN.Enabled = false;

            dataGridView1.Visible = true;
            dataGridView2.Visible = false;
            msthRadio.Checked = true;
            selectCheckFlag   = false;
            seleCancelFlag = false;
            textBox2.Text = dbSrverIPTXT.Text;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string qcmd = "";
            string seiban = seibanTXT.Text.Trim();
            DoWriteBTN.Enabled = false;
            cancel2BTN.Enabled = false;

            dataGridView1.Columns.Clear();
            dataGridView2.Rows.Clear();
            textBox1.Text = "";

            dataPic1Text = textToDate(dateTimePicker1.Text);
            string srdate = dataPic1Text.Substring(0, 10);

            if (AlldispRadio.Checked == true)
            {
                dataGridView1.Visible = true;
                dataGridView2.Visible = false;

                if ((DATECHK.Checked == true) && (LOTCHK.Checked == false))
                {
                    qcmd = "select * from aoc.inspection_ccb_pcba_smth where StartTime like '" + srdate + "%' order by Id desc";
                }
                else if ((DATECHK.Checked == false) && (LOTCHK.Checked == true))
                {
                    qcmd = "select * from aoc.inspection_ccb_pcba_smth where seiban = '" + seiban + "' order by Id desc";
                }
                else if ((DATECHK.Checked == true) && (LOTCHK.Checked == true))
                {
                    qcmd = "select * from aoc.inspection_ccb_pcba_smth where StartTime like '" + srdate + "%' and seiban ='" + seiban + "' order by Id desc";
                }
                else
                {
                    if (MessageBox.Show("チェックが無い場合の全表示は時間がかかります", "確認", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation) == DialogResult.Cancel)
                    {
                        return;
                    }
                    qcmd = "select * from aoc.inspection_ccb_pcba_smth order by Id desc";
                }

                dataGridView1.DataSource = mysql.getTable(qcmd);
            }
            else //-- 主信号閾値での検索
            {
                search15BTN.Enabled = false;

                DoWriteBTN.Enabled = true;
                cancel2BTN.Enabled = true;

                msthAllDIsp( seiban,srdate );
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="showRowNUm"></param>
        /// <param name="dbuf"></param>
        /// <param name="id"></param>
        private void showDtatgridView1(int showRowNUm, ref string[] dbuf, int[] id  )
        {
            int j = 0;
            string qcmd = "";

            string[] r = new string[7];

            for (j = 0; j < showRowNUm; j++)
            {
                qcmd = "select Id, StartTime, seiban, pcb, ch, alljud, dMSTHperRSSI from aoc.inspection_ccb_pcba_smth";
                qcmd += " where pcb = '" + dbuf[j] + "' and alljud != 'NG' order by Id desc limit 4;";

                mysql.dbOpen();
                mysql.sqlCmd(qcmd);

                while (mysql.cReader.Read())
                {
                    r[0] = mysql.cReader[0].ToString();
                    r[1] = mysql.cReader[1].ToString();
                    r[2] = mysql.cReader[2].ToString();
                    r[3] = mysql.cReader[3].ToString();
                    r[4] = mysql.cReader[4].ToString();
                    r[5] = mysql.cReader[5].ToString();
                    r[6] = mysql.cReader[6].ToString();

                    int di = int.Parse(r[0]);


                    bool brnch = checkId(di);

                    if ( brnch )
                    {
                        dataGridView2.Rows.Add( true, r[0], r[1], r[2], r[3], r[4], r[5], r[6]);
                    }
                    else
                    {
                        dataGridView2.Rows.Add(false, r[0], r[1], r[2], r[3], r[4], r[5], r[6]);
                    }
                }
                mysql.dbClose();
            }
        }


        /// <summary>
        /// そのIdは選択されたものかを確認
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        private bool checkId(int ids)
        {
            bool res = false;

            for (int i = 0; i < g_id.Length; i++)
            {
                if (ids == g_id[i])
                {
                    res = true;
                    break;
                }
            }

            return res;
        }

        private void datepic1(object sender, EventArgs e)
        {
            dataPic1Text = textToDate(dateTimePicker1.Text);
        }

        /// <summary>
        /// datePickerの日付が付きや日で１桁であるとうまくDATEの型に変換できないので
        /// 月と日を2桁にする関数
        /// </summary>
        /// <param name="ptext"></param>
        /// <returns></returns>
        private string textToDate(string ptext)
        {
            string before = String.Empty;
            string after = String.Empty;

            ptext += "00:00:00";

            before = ptext.Replace("年", "-");
            before = before.Replace("月", "-");
            before = before.Replace("日", " ");

            if (before.Substring(7, 1) == "-")
            {
                //-- 月は2桁なので何もしない

                if (before.Substring(9, 1) == " ")
                {
                    after = before.Substring(0, 7) + "-0" + before.Substring(8, 1) + " 00:00:00";
                }
                else
                {
                    after = before;
                }
            }
            else
            {
                after = before.Substring(0, 4) + "-0" + before.Substring(5, 1) + before.Substring(6, 3) + " 00:00:00";

                if (after.Substring(9, 1) == " ")
                {
                    after = after.Substring(0, 7) + "-0" + after.Substring(8, 1) + " 00:00:00";
                }
            }
            return after;
        }

        private void button2_Click(object sender, EventArgs e)
        {

            if (dataGridView1.Visible == true)
            {
                int cnt = dataGridView1.Rows.Count;
                if (cnt < 1)
                {
                    return;
                }
                csvWriteByMysql();
            }

            if (dataGridView2.Visible == true)
            {
                int cnt = dataGridView2.Rows.Count;

                if (cnt < 1)
                {
                    return;
                }
                csvWriteByMysql2();
            }
        }

        /// <summary>
        /// MySQLの結果をCSV書き出しするための本体
        /// </summary>
        /// <param name="grd"></param>
        private void csvWriteByMysql()
        {
            // OpenFileDialog の新しいインスタンスを生成する (デザイナから追加している場合は必要ない)
            SaveFileDialog openFileDialog1 = new SaveFileDialog();

            // ダイアログのタイトルを設定する
            saveFileDialog1.Title = "ＣＳＶ書き出し";

            // ※下記の設定をしないと、一度選んだディレクトリを２度目も再度選択してくれる。
            // 初期表示するディレクトリを設定する

            saveFileDialog1.InitialDirectory = @"c:\\";

            // 初期表示するファイル名を設定する
            saveFileDialog1.FileName = fileNameTemp;

            // ファイルのフィルタを設定する
            saveFileDialog1.Filter = "ＣＳＶファイル|*.csv";

            // ダイアログを表示し、戻り値が [OK] の場合は、選択したファイルを表示する
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {

                using (StreamWriter writer = new StreamWriter(saveFileDialog1.FileName, false, Encoding.GetEncoding("shift_jis")))
                {
                    int rowCount = dataGridView1.Rows.Count;
                    int colCount = dataGridView1.ColumnCount;

                    // ユーザによる行追加が許可されている場合は、最後に新規入力用の
                    // 1行分を差し引く
                    if (dataGridView1.AllowUserToAddRows == true)
                    {
                        rowCount = rowCount - 1;
                    }

                    string sp = "";

                    // ヘッダー出力
                    foreach (DataGridViewColumn column in dataGridView1.Columns)
                    {
                        sp += column.HeaderText + ", ";
                        //column.HeaderText = String.Concat("Column ",
                        //column.Index.ToString());
                    }
                    writer.WriteLine(sp);

                    // 行
                    for (int i = 0; i < rowCount; i++)
                    {
                        // リストの初期化
                        List<String> strList = new List<String>();

                        // 列
                        for (int j = 0; j < dataGridView1.Columns.Count; j++)
                        {
                            strList.Add(dataGridView1[j, i].Value.ToString());
                        }
                        String[] strArray = strList.ToArray();  // 配列へ変換

                        // CSV 形式に変換
                        String strCsvData = String.Join(",", strArray);

                        writer.WriteLine(strCsvData);
                    }
                }
            }
        }



        /// <summary>
        /// MySQLの結果をCSV書き出しするための本体
        /// </summary>
        /// <param name="grd"></param>
        private void csvWriteByMysql2()
        {
            // OpenFileDialog の新しいインスタンスを生成する (デザイナから追加している場合は必要ない)
            SaveFileDialog openFileDialog1 = new SaveFileDialog();

            // ダイアログのタイトルを設定する
            saveFileDialog1.Title = "ＣＳＶ書き出し";

            // ※下記の設定をしないと、一度選んだディレクトリを２度目も再度選択してくれる。
            // 初期表示するディレクトリを設定する

            saveFileDialog1.InitialDirectory = @"c:\\";

            // 初期表示するファイル名を設定する
            saveFileDialog1.FileName = fileNameTemp;

            // ファイルのフィルタを設定する
            saveFileDialog1.Filter = "ＣＳＶファイル|*.csv";

            // ダイアログを表示し、戻り値が [OK] の場合は、選択したファイルを表示する
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {

                using (StreamWriter writer = new StreamWriter(saveFileDialog1.FileName, false, Encoding.GetEncoding("shift_jis")))
                {
                    int rowCount = dataGridView2.Rows.Count;
                    int colCount = dataGridView2.ColumnCount;

                    // ユーザによる行追加が許可されている場合は、最後に新規入力用の
                    // 1行分を差し引く
                    if (dataGridView2.AllowUserToAddRows == true)
                    {
                        rowCount = rowCount - 1;
                    }

                    string sp = "";

                    // ヘッダー出力
                    foreach (DataGridViewColumn column in dataGridView2.Columns)
                    {
                        sp += column.HeaderText + ", ";
                        //column.HeaderText = String.Concat("Column ",
                        //column.Index.ToString());
                    }
                    writer.WriteLine(sp);

                    // 行
                    for (int i = 0; i < rowCount; i++)
                    {
                        // リストの初期化
                        List<String> strList = new List<String>();

                        // 列
                        for (int j = 0; j < dataGridView2.Columns.Count; j++)
                        {
                            strList.Add(dataGridView2[j, i].Value.ToString());
                        }
                        String[] strArray = strList.ToArray();  // 配列へ変換

                        // CSV 形式に変換
                        String strCsvData = String.Join(",", strArray);

                        writer.WriteLine(strCsvData);
                    }
                }
            }
        }



        /// <summary>
        /// データグリッドのチェック状態を得る
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DoWriteBTN_Click(object sender, EventArgs e)
        {
            int i = 0;
            int j = 0;

            if (seleCancelFlag == false)
            {
                for (i = 0; i < g_id.Length; i++)
                {
                    g_id[i] = 0;
                }
                seleCancelFlag = true;
            }

            selectCheckFlag = true;

            int cnt = dataGridView2.Rows.Count;
            cnt--; //-- 最終行はnullであるから引く

            if (comboBox1.Text == "すべて表示")
            {
                string seiban = seibanTXT.Text.Trim();
                DoWriteBTN.Enabled = false;

                dataGridView2.Rows.Clear();
                textBox1.Text = "";

                dataPic1Text = textToDate(dateTimePicker1.Text);
                string srdate = dataPic1Text.Substring(0, 10);
                msthAllDIsp( seiban,  srdate );
            }

            if (comboBox1.Text == "選択行だけ表示")
            {
                for (i = 0; i < cnt; i++)
                {
                    // チェックが入っている場合
                    string h = dataGridView2[0, i].Value.ToString();

                    if (h == "True")
                    {
                        string ids = dataGridView2.Rows[i].Cells[1].Value.ToString();
                        g_id[j] = int.Parse(ids);
                        j++;
                    }
                }

                dataGridView2.Rows.Clear();

                string qcmd = "";

                string[] r = new string[7];

                for (i = 0; i < j; i++)
                {
                    qcmd = "select Id, StartTime, seiban, pcb, ch, alljud, dMSTHperRSSI from aoc.inspection_ccb_pcba_smth";
                    qcmd += " where Id = " + g_id[i] + ";";

                    mysql.dbOpen();
                    mysql.sqlCmd(qcmd);

                    while (mysql.cReader.Read())
                    {
                        r[0] = mysql.cReader[0].ToString();
                        r[1] = mysql.cReader[1].ToString();
                        r[2] = mysql.cReader[2].ToString();
                        r[3] = mysql.cReader[3].ToString();
                        r[4] = mysql.cReader[4].ToString();
                        r[5] = mysql.cReader[5].ToString();
                        r[6] = mysql.cReader[6].ToString();

                        dataGridView2.Rows.Add(true, r[0], r[1], r[2], r[3], r[4], r[5], r[6]);
                    }
                    mysql.dbClose();
                }
                selectCheckFlag = true;
            }
            else if (comboBox1.Text == "選択行を2次に反映する")
            {
                string msg = "";

                if (selectCheckFlag == false)
                {
                    msg = "「選択行だけ表示」を行って変更を確定してください";
                    MessageBox.Show(msg, "確認", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }

                int ct = dataGridView2.RowCount;

                if (ct == 1)
                {
                    msg = "反映すべき行がありません";
                    MessageBox.Show(msg, "確認", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    selectCheckFlag = false;
                    return;
                }

                msg = "本当に実行しますか";
                if (MessageBox.Show(msg, "確認", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation) == DialogResult.Cancel)
                {
                    return;
                }

                for ( i = 0; i < g_id.Length; i++)
                {
                    if (g_id[i] == 0) break;
                }

                mysql.Update2ndTest(g_id, i);

                for ( j = 0; j < i; j ++){g_id[j] = 0; }
                selectCheckFlag = false;
            }
            else if (comboBox1.Text == "2次反映を表示する")
            {
                string qcmd = "";
                string seiban = seibanTXT.Text.Trim();

                dataGridView1.Columns.Clear();
                dataGridView2.Rows.Clear();
                textBox1.Text = "";

                dataPic1Text = textToDate(dateTimePicker1.Text);
                string srdate = dataPic1Text.Substring(0, 10);

                string[] r = new string[7];

                dataGridView2.Rows.Clear();

                if ((DATECHK.Checked == true) && (LOTCHK.Checked == false))
                {
                    qcmd = "select Id, StartTime, seiban, pcb, ch, alljud, dMSTHperRSSI from aoc.inspection_ccb_pcba_smth ";
                    qcmd += " where alljud ='2nd' and StartTime like '" + srdate + "%' order by Id desc;";
                }
                else if ((DATECHK.Checked == false) && (LOTCHK.Checked == true))
                {
                    qcmd = "select Id, StartTime, seiban, pcb, ch, alljud, dMSTHperRSSI from aoc.inspection_ccb_pcba_smth ";
                    qcmd += " where alljud ='2nd' and seiban = '" + seiban +"' order by Id desc;";
                }
                else if ((DATECHK.Checked == true) && (LOTCHK.Checked == true))
                {
                    qcmd = "select Id, StartTime, seiban, pcb, ch, alljud, dMSTHperRSSI from aoc.inspection_ccb_pcba_smth ";
                    qcmd += " where alljud ='2nd' and StartTime like '" + srdate + "%' and seiban ='" + seiban + "' order by Id desc;";
                }
                else
                {
                    qcmd = "select Id, StartTime, seiban, pcb, ch, alljud, dMSTHperRSSI from aoc.inspection_ccb_pcba_smth where alljud ='2nd' order by Id desc;";
                }

                mysql.dbOpen();
                mysql.sqlCmd(qcmd);

                while (mysql.cReader.Read())
                {
                    r[0] = mysql.cReader[0].ToString();
                    r[1] = mysql.cReader[1].ToString();
                    r[2] = mysql.cReader[2].ToString();
                    r[3] = mysql.cReader[3].ToString();
                    r[4] = mysql.cReader[4].ToString();
                    r[5] = mysql.cReader[5].ToString();
                    r[6] = mysql.cReader[6].ToString();

                    dataGridView2.Rows.Add(true, r[0], r[1], r[2], r[3], r[4], r[5], r[6]);
                }
                mysql.dbClose();
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="seiban"></param>
        /// <param name="srdate"></param>
        private void msthAllDIsp( string seiban, string srdate)
        {
            string qcmd ="";
            dataGridView1.Visible = false;
            dataGridView2.Visible = true;


            DoWriteBTN.Enabled = true;
            int i = 0;

            string[] dbbufUnique = new string[10000];


            if ((DATECHK.Checked == true) && (LOTCHK.Checked == false))
            {
                //-- distinctでorder byは有効か？
                qcmd = "Select distinct pcb from aoc.inspection_ccb_pcba_smth ";
                qcmd += " where StartTime like '" + srdate + "%' and rssi3 = 0 and alljud !='NG' and test = 0 order by Id desc";

                mysql.dbOpen();
                mysql.sqlCmd(qcmd);

                while (mysql.cReader.Read())
                {
                    dbbufUnique[i] = mysql.cReader[0].ToString();
                    i++;
                }
                mysql.dbClose();

                textBox1.Text = i.ToString();

                showDtatgridView1(i, ref dbbufUnique, g_id );
            }
            else if ((DATECHK.Checked == false) && (LOTCHK.Checked == true))
            {
                //-- distinctでorder byは有効か？
                qcmd = "Select distinct pcb from aoc.inspection_ccb_pcba_smth ";
                qcmd += " where seiban = '" + seiban + "' and rssi3 = 0 and alljud !='NG' and test = 0 order by Id desc";

                mysql.dbOpen();
                mysql.sqlCmd(qcmd);

                while (mysql.cReader.Read())
                {
                    dbbufUnique[i] = mysql.cReader[0].ToString();
                    i++;
                }
                mysql.dbClose();

                textBox1.Text = i.ToString();
                showDtatgridView1(i, ref dbbufUnique, g_id);
            }
            else if ((DATECHK.Checked == true) && (LOTCHK.Checked == true))
            {
                //-- distinctでorder byは有効か？
                qcmd = "Select distinct pcb from aoc.inspection_ccb_pcba_smth ";
                qcmd += " where StartTime like '" + srdate + "%' and seiban = '" + seiban + "' and rssi3 = 0 and alljud !='NG' and test = 0 order by Id desc";

                mysql.dbOpen();
                mysql.sqlCmd(qcmd);

                while (mysql.cReader.Read())
                {
                    dbbufUnique[i] = mysql.cReader[0].ToString();
                    i++;
                }

                mysql.dbClose();
                textBox1.Text = i.ToString();
                showDtatgridView1(i, ref dbbufUnique, g_id);
            }
            else
            {

                if (MessageBox.Show("チェックが無い場合の全表示は時間がかかります", "確認", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation) == DialogResult.Cancel)
                {
                    return;
                }


                //-- distinctでorder byは有効か？
                qcmd = "Select distinct pcb from aoc.inspection_ccb_pcba_smth ";
                qcmd += " order by Id desc";

                mysql.dbOpen();
                mysql.sqlCmd(qcmd);

                while (mysql.cReader.Read())
                {
                    dbbufUnique[i] = mysql.cReader[0].ToString();
                    i++;
                }
                mysql.dbClose();
                textBox1.Text = i.ToString();
                showDtatgridView1(i, ref dbbufUnique, g_id);
            }
        }

        private void chgallItem(object sender, EventArgs e)
        {
            if (AlldispRadio.Checked == true)
            {
                search15BTN.Enabled = true;
                DoWriteBTN.Enabled  = false;
                cancel2BTN.Enabled  = false;
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            int i = 0;
            int j = 0;

            if ( seleCancelFlag == true)
            {
                for (i = 0; i < g_id.Length; i++)
                {
                    g_id[i] = 0;
                }
                selectCheckFlag = false;
                seleCancelFlag  = false;
            }

            int cnt = dataGridView2.Rows.Count;
            cnt--; //-- 最終行はnullであるから引く

            if (comboBox2.Text == "すべて表示")
            {
                string seiban = seibanTXT.Text.Trim();
                DoWriteBTN.Enabled = false;

                dataGridView2.Rows.Clear();
                textBox1.Text = "";

                dataPic1Text = textToDate(dateTimePicker1.Text);
                string srdate = dataPic1Text.Substring(0, 10);
                msthAllDIsp(seiban, srdate);
            }

            if (comboBox2.Text == "選択行だけ表示")
            {
                for (i = 0; i < cnt; i++)
                {
                    // チェックが入っている場合
                    string h = dataGridView2[0, i].Value.ToString();

                    if (h == "True")
                    {
                        string ids = dataGridView2.Rows[i].Cells[1].Value.ToString();
                        g_id[j] = int.Parse(ids);
                        j++;
                    }
                }

                dataGridView2.Rows.Clear();

                string qcmd = "";

                string[] r = new string[7];

                for (i = 0; i < j; i++)
                {
                    qcmd = "select Id, StartTime, seiban, pcb, ch, alljud, dMSTHperRSSI from aoc.inspection_ccb_pcba_smth";
                    qcmd += " where Id = " + g_id[i] + ";";

                    mysql.dbOpen();
                    mysql.sqlCmd(qcmd);

                    while (mysql.cReader.Read())
                    {
                        r[0] = mysql.cReader[0].ToString();
                        r[1] = mysql.cReader[1].ToString();
                        r[2] = mysql.cReader[2].ToString();
                        r[3] = mysql.cReader[3].ToString();
                        r[4] = mysql.cReader[4].ToString();
                        r[5] = mysql.cReader[5].ToString();
                        r[6] = mysql.cReader[6].ToString();

                        dataGridView2.Rows.Add(true, r[0], r[1], r[2], r[3], r[4], r[5], r[6]);
                    }
                    mysql.dbClose();
                }
                selectCheckFlag = true;
            }
            else if (comboBox2.Text == "選択行をNEXTにする")
            {

                string msg = "";

                if (selectCheckFlag == false)
                {
                    msg = "「選択行だけ表示」を行って変更を確定してください";
                    MessageBox.Show(msg, "確認", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }

                int ct = dataGridView2.RowCount;

                if (ct == 1)
                {
                    msg = "反映すべき行がありません";
                    MessageBox.Show(msg, "確認", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    selectCheckFlag = false;
                    return;
                }

                msg = "本当に実行しますか";
                if (MessageBox.Show(msg, "確認", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation) == DialogResult.Cancel)
                {
                    return;
                }

                for (i = 0; i < g_id.Length; i++)
                {
                    if (g_id[i] == 0) break;
                }

                mysql.Update2ndTestSetNext(g_id, i);

                for (j = 0; j < i; j++) { g_id[j] = 0; }
                selectCheckFlag = false;
            }
        }

        /// <summary>
        /// MySQLサーバーIPアドレスの切り替え
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click_2(object sender, EventArgs e)
        {
            mysql.setServerIP(dbSrverIPTXT.Text.Trim());
            textBox2.Text = dbSrverIPTXT.Text;
        }

        private void nd項目表示ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string qcmd = "";
            string seiban = seibanTXT.Text.Trim();
            DoWriteBTN.Enabled = false;
            cancel2BTN.Enabled = false;

            dataGridView1.Columns.Clear();
            dataGridView2.Rows.Clear();
            textBox1.Text = "";

            dataPic1Text = textToDate(dateTimePicker1.Text);
            string srdate = dataPic1Text.Substring(0, 10);

            dataGridView1.Visible = true;
            dataGridView2.Visible = false;

            qcmd = "select * from aoc.inspection_ccb_pcba_smth where alljud ='2nd' order by Id desc";

            dataGridView1.DataSource = mysql.getTable(qcmd);

        }

    }

}
