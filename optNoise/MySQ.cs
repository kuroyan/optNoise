using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MySql.Data.MySqlClient;  //-- .net 4.0に対応している　.net4.5 は否です。

namespace optNoise
{
    class MySQL
    {
        private MySqlConnection conn;
        private MySqlCommand cmd;
        private bool romdBFlag = false;
        public MySqlDataReader cReader;

        //-- DB name はaoc まで入れないこと
        private const string dbname = "inspection_ccb_pcba_smth";

        static string ipadrs;
        private string stConnectionString = string.Empty;
        
        enum DBFIELD
        {
            Id,
            seiban, 
            pcb,
            ch,
            stime,
            opname,
            ver,
            alljud,
            rssi1,
            rxlos,
            msth,
            jdrssi,
            rssi2,
            temp,
            dmsth,
            pwmw,
            dMSTHperRSSI,
            ber,
            jdmsth,
            jdval,
            jdrssi1val,
            toolNo,
            test,
            rssi3,
            eye10,
            eye1g,
            eyecloser,
            RNPI1m,
            OMA,
            lightSN,
            fel1Cnt,
            fel2Cnt
        };


        public MySQL()
        {
        }

        public void setServerIP(string adrs)
        {
            ipadrs = adrs;
        }

        public void dbOpen()
        {
            //-- connect server
            string connstr = "";

            if (ipadrs == "127.0.0.1")
            {
                connstr = "userid=root; password=hoge; database = AOC; Host=" + ipadrs;
            }
            else
            {
                connstr = "userid=AocAdmin; password=aocAdmin; database = AOC; Host=" + ipadrs;
            }
                
                conn = new MySqlConnection(connstr);
            cmd = conn.CreateCommand();

            try
            {
                conn.Open();
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("connection failed\n" + ex.Message);
            }
        }

        /// <summary>
        /// close DataBase
        /// </summary>
        public void dbClose()
        {
            conn.Close();
            //objConn.Dispose();
        }


        /// <summary>
        /// Excecute Query　
        /// </summary>
        /// <param name="cmdstr"></param>
        public bool sqlCmd(string cmdstr)
        {
            bool ret = false;

            try
            {
                cmd.CommandText = cmdstr;
                cmd.Connection = conn;
                cReader = cmd.ExecuteReader();
            }
            catch (MySqlException ex)
            {
                if (romdBFlag == true)
                {
                    MessageBox.Show("データベースが存在します。\r\n管理ツール（MySQL Workbench)で削除してからの作業です。\r\n管理者にお願いしてください。");
                }
                else
                {
                    MessageBox.Show("Query Fail: " + ex.Message);
                }
                ret = true;
            }
            return ret;
        }

        /// <summary>
        /// Excecute Query　
        /// </summary>
        /// <param name="cmdstr"></param>
        public int sqlCmdCount(string cmdstr)
        {
            string ret = "0";

            dbOpen();
            try
            {
                cmd.CommandText = cmdstr;
                cmd.Connection = conn;
                ret = cmd.ExecuteScalar().ToString();
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Query ExecuteScalar Fail: " + ex.Message);
            }
            dbClose();

            return int.Parse(ret);
        }


        
        /// <summary>
        /// フェルール使用残数を取得
        /// </summary>
        /// <param name="pcb"></param>
        /// <returns></returns>
        public int getfelUseCnt(string pcb)
        {
            dbOpen();
            string qcmd = "select fel2Cnt from aoc.inspection_ccb_pcba_smth  where rssi3 !=0 order by Id desc limit 1;";
            string count = "";
            sqlCmd(qcmd);

            while (cReader.Read())
            {
                count = cReader[0].ToString();
            }

            dbClose();

            return int.Parse( count);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="upid"></param>
        public void Update2ndTest(int[] upid, int upItemNum )
        {

            for (int i = 0; i < upItemNum; i++)
            {
                dbOpen();
                string qcmd = "update aoc.inspection_ccb_pcba_smth  set alljud = '2nd' where Id=" + upid[i] + ";";
                sqlCmd(qcmd);
                dbClose();
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="upid"></param>
        public void Update2ndTestSetNext(int[] upid, int upItemNum)
        {

            for (int i = 0; i < upItemNum; i++)
            {
                dbOpen();
                string qcmd = "update aoc.inspection_ccb_pcba_smth  set alljud = 'NEXT' where Id=" + upid[i] + ";";
                sqlCmd(qcmd);
                dbClose();
            }
        }

        
        /// <summary>
        /// NEXTチャンネル情報を得る
        /// </summary>
        /// <param name="?"></param>
        /// <param name="?"></param>
        public bool getNextCh( ref bool[] nextCh, string pcb )
        {
            int j = 0;
            int[] next = new int[4]{0,0,0,0};
            string qcmd = "";
            string res = "";
            string[] jd = new string[4] { "", "", "", "" };

            dbOpen();
            qcmd = "select Id from aoc.inspection_ccb_pcba_smth  where pcb ='" + pcb + "' and alljud != 'NG'  order by Id desc limit 4;";
            int count = 0;
            sqlCmd(qcmd);

            while (cReader.Read())
            {
                res = cReader[0].ToString();
                count ++;
            }

            dbClose();

            if (count != 4)
            {
                return true;
            }
            
            
            //dbOpen();
            //qcmd = "select ch, alljud from aoc.inspection_ccb_pcba_smth  where pcb ='" + pcb + "' and ( alljud = 'NEXT' or alljud ='G' ) order by Id desc limit 4;";
            //sqlCmd(qcmd);

            //while (cReader.Read())
            //{
            //    res = cReader[0].ToString();
            //    next[i] = int.Parse(res);
            //    jd[i] = cReader[1].ToString();
            //    i++;
            //}

            ////-- カウントされたNEXTチャンネルに対応した参照配列に True を入れる
            for (j = 0; j < 4; j++)
            {
                nextCh[j] = true; //-- NEXT も G もすべて2次選別を実施する。

                //pg = next[j];
                //pg--;
                //if (jd[j] == "NEXT")
                //{
                //    nextCh[pg] = true;
                //}
                //else
                //{
                //    nextCh[pg] = false;
                //}
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ch"></param>
        /// <param name="pcb"></param>
        /// <param name="?"></param>
        public void getRecentPCBch(
                  int ch,
                  string pcb,
                  string[] dBufTmp )
        {
            ch++; //--　ch1=0なので調整
            dbOpen();
            string qcmd = "select * from aoc.inspection_ccb_pcba_smth  where ch = " + ch + " and pcb ='" + pcb + "' order by Id desc limit 1;";
            sqlCmd(qcmd);

            while (cReader.Read())
            {
                dBufTmp[(int)DBFIELD.seiban] = cReader[(int)DBFIELD.seiban].ToString();
                dBufTmp[(int)DBFIELD.seiban] = cReader[(int)DBFIELD.seiban].ToString();
                dBufTmp[(int)DBFIELD.pcb]    = cReader[(int)DBFIELD.pcb].ToString();
                dBufTmp[(int)DBFIELD.ch]     = cReader[(int)DBFIELD.ch].ToString();
                dBufTmp[(int)DBFIELD.stime]  = cReader[(int)DBFIELD.stime].ToString();
                dBufTmp[(int)DBFIELD.opname] = cReader[(int)DBFIELD.opname].ToString();
                dBufTmp[(int)DBFIELD.ver]    = cReader[(int)DBFIELD.ver].ToString();
                dBufTmp[(int)DBFIELD.alljud] = cReader[(int)DBFIELD.alljud].ToString();
                dBufTmp[(int)DBFIELD.rssi1]  = cReader[(int)DBFIELD.rssi1].ToString();
                dBufTmp[(int)DBFIELD.rxlos]  = cReader[(int)DBFIELD.rxlos].ToString();
                dBufTmp[(int)DBFIELD.msth]   = cReader[(int)DBFIELD.msth].ToString();
                dBufTmp[(int)DBFIELD.jdrssi] = cReader[(int)DBFIELD.jdrssi].ToString();
                dBufTmp[(int)DBFIELD.rssi2]  = cReader[(int)DBFIELD.rssi2].ToString();
                dBufTmp[(int)DBFIELD.temp]   = cReader[(int)DBFIELD.temp].ToString();
                dBufTmp[(int)DBFIELD.dmsth]  = cReader[(int)DBFIELD.dmsth].ToString();
                dBufTmp[(int)DBFIELD.pwmw]   = cReader[(int)DBFIELD.pwmw].ToString();
                dBufTmp[(int)DBFIELD.dMSTHperRSSI] = cReader[(int)DBFIELD.dMSTHperRSSI].ToString();
                dBufTmp[(int)DBFIELD.ber]    = cReader[(int)DBFIELD.ber].ToString();
                dBufTmp[(int)DBFIELD.jdmsth] = cReader[(int)DBFIELD.jdmsth].ToString();
                dBufTmp[(int)DBFIELD.jdval]  = cReader[(int)DBFIELD.jdval].ToString();
                dBufTmp[(int)DBFIELD.jdrssi1val] = cReader[(int)DBFIELD.jdrssi1val].ToString();
                dBufTmp[(int)DBFIELD.toolNo] = cReader[(int)DBFIELD.toolNo].ToString();
                dBufTmp[(int)DBFIELD.test]   = cReader[(int)DBFIELD.test].ToString();
                dBufTmp[(int)DBFIELD.rssi3]  = cReader[(int)DBFIELD.rssi3].ToString();
                dBufTmp[(int)DBFIELD.eye10]  = cReader[(int)DBFIELD.eye10].ToString();
                dBufTmp[(int)DBFIELD.eye1g]  = cReader[(int)DBFIELD.eye1g].ToString();
                dBufTmp[(int)DBFIELD.eyecloser] = cReader[(int)DBFIELD.eyecloser].ToString();
                dBufTmp[(int)DBFIELD.RNPI1m]  = cReader[(int)DBFIELD.RNPI1m].ToString();
                dBufTmp[(int)DBFIELD.OMA]     = cReader[(int)DBFIELD.OMA].ToString();
                dBufTmp[(int)DBFIELD.lightSN] = cReader[(int)DBFIELD.lightSN].ToString();
                dBufTmp[(int)DBFIELD.fel1Cnt] = cReader[(int)DBFIELD.fel1Cnt].ToString();
                dBufTmp[(int)DBFIELD.fel2Cnt] = cReader[(int)DBFIELD.fel2Cnt].ToString();
            }

            dbClose();
        }



        /// <summary>
        /// Insert to Database
        /// </summary>
        /// <summary>
        /// Insert to Database
        /// </summary>
        public void insertDatabase(int ch, string[,] buf)
        {
            int c = ch;
            string qcmd = "INSERT INTO " + dbname + " (";
            qcmd += "Id,";
            qcmd += "seiban,";      //--1
            qcmd += "pcb,";         //--2
            qcmd += "ch,";          //--3
            qcmd += "StartTime,";   //--4
            qcmd += "ver,";         //--5   
            qcmd += "opname,";      //--6
            qcmd += "alljud,";      //--7    
            qcmd += "rssi1,";       //--8
            qcmd += "rxlos,";       //--9
            qcmd += "msth,";       //--10
            qcmd += "jdrssi,";      //--11
            qcmd += "rssi2,";       //--12 (消光比の予定)
            qcmd += "temp,";       //--13
            qcmd += "dmsth,";       //--14
            qcmd += "pwmw,";        //--15
            qcmd += "dMSTHperRSSI,";//--16
            qcmd += "ber,";         //--17
            qcmd += "jdmsth,";      //--18
            qcmd += "jdval,";       //--19
            qcmd += "jdrssi1val,";  //--20
            qcmd += "toolNo,";      //--21
            qcmd += "test,";
            qcmd += "rssi3,";
            qcmd += "eye10,";
            qcmd += "eye1g,";
            qcmd += "eyecloser,";
            qcmd += "RNPI1m,";
            qcmd += "OMA,";
            qcmd += "lightSN,";
            qcmd += "fel2Cnt)";      

            qcmd += " VALUES( NULL,'";
            qcmd += buf[c, (int)DBFIELD.seiban] + "','"; 
            qcmd += buf[c, (int)DBFIELD.pcb] + "',";
            qcmd += buf[c, (int)DBFIELD.ch]     + ",'";
            qcmd += buf[c, (int)DBFIELD.stime]  + "','";
            qcmd += buf[c, (int)DBFIELD.ver]    + "','";
            qcmd += buf[c, (int)DBFIELD.opname] + "','";
            qcmd += buf[c, (int)DBFIELD.alljud] + "',";
            qcmd += buf[c, (int)DBFIELD.rssi1]  + ",";
            qcmd += buf[c, (int)DBFIELD.rxlos]  + ",";
            qcmd += buf[c, (int)DBFIELD.msth]  + ",'";
            qcmd += buf[c, (int)DBFIELD.jdrssi] + "',";
            qcmd += buf[c, (int)DBFIELD.rssi2]  + ",";
            qcmd += buf[c, (int)DBFIELD.temp]   + ",";
            qcmd += buf[c, (int)DBFIELD.dmsth]  + ",";
            qcmd += buf[c, (int)DBFIELD.pwmw]   + ",";
            qcmd += buf[c, (int)DBFIELD.dMSTHperRSSI] + ",";
            qcmd += buf[c, (int)DBFIELD.ber]    + ",'";
            qcmd += buf[c, (int)DBFIELD.jdmsth] + "',";
            qcmd += buf[c, (int)DBFIELD.jdval]  + ",";
            qcmd += buf[c, (int)DBFIELD.jdrssi1val] + ",";
            qcmd += buf[c, (int)DBFIELD.toolNo] + ",";
            qcmd += buf[c, (int)DBFIELD.test] + ",";

            qcmd += buf[c, (int)DBFIELD.rssi3] + ",";
            qcmd += buf[c, (int)DBFIELD.eye10] + ",";
            qcmd += buf[c, (int)DBFIELD.eye1g] + ",";
            qcmd += buf[c, (int)DBFIELD.eyecloser] + ",";
            qcmd += buf[c, (int)DBFIELD.RNPI1m] + ",";
            qcmd += buf[c, (int)DBFIELD.OMA] + ","; 
            qcmd += buf[c, (int)DBFIELD.lightSN] + ",";
            qcmd += buf[c, (int)DBFIELD.fel2Cnt] + ");";
            
            dbOpen();
            sqlCmd(qcmd);
            dbClose();
        }


        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="ds"></param>
        public void UpdateTable(int Id, double erate, double ercnt, string dbErr)
        {
            dbOpen();
            string qcmd = "";

            if (dbErr == "G")
            {
                qcmd = "UPDATE aoc.inspection_ccb_pcba_smth  " + "SET  BER = " + erate + ", ErrCnt =" + ercnt + ", judgeBer = 'G' ";
            }
            else
            {
                qcmd = "UPDATE aoc.inspection_ccb_pcba_smth  " + "SET  BER = " + erate + ", ErrCnt =" + ercnt + " , judgeAll = 'NG' , judgeBer ='NG' ";
            }
            qcmd += " WHERE Id=" + Id + ";";
            sqlCmd(qcmd);
            dbClose();
        }

        /// <summary>
        ///アダプタですべて得る
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public DataTable getTable(string qcmd)
        {
            DataTable dt = new DataTable();

            dbOpen();
            dt.Clear();
            MySqlDataAdapter da = new MySqlDataAdapter(qcmd, conn);
            da.Fill(dt);
            dbClose();
            return dt;
        }

        /// <summary>
        /// 不要になったチャンネル１のエビデンス用データを削除する
        /// </summary>
        /// <param name="Sno"></param>
        /// <param name="end"></param>
        public void deleteEvidenceCH1(string pcb)
        {
            string qcmd = "DELETE from aoc.inspection_ccb_pcba_smth  WHERE ch = 1 and rssi1 = -999 and pcb = '" + pcb + "';";
            dbOpen();
            sqlCmd(qcmd);
            dbClose();
        }

    }
}


