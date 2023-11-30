using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Data.OleDb;


namespace AudioSystem
{
    /// <summary>
    /// 修改密码.xaml 的交互逻辑
    /// </summary>
    public partial class 修改密码 : Window
    {
        public 修改密码()
        {
            InitializeComponent();
        }
        public static string AccessConnStr = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=MyLocalData.mdb;Jet OLEDB:Database Password=CH123321";
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            string user = "";
            string password = "";
            if (UserName.Text == "" || UserPass1.Password == "" || UserPass2.Password == "")
            {
                MessageBox.Show("账户和密码不能为空！","错误");
                return;
            }
            else if (UserPass1.Password != UserPass2.Password)
            {
                MessageBox.Show("输入密码与确认密码不一致！", "错误");
                return;
            }
            else
            {
                user = UserName.Text;
                password = UserPass1.Password;

                try
                {
                    OleDbConnection olecon = new OleDbConnection(AccessConnStr);
                    olecon.Open();
                    string Updsql = "UPDATE UINFO SET myuser='" + UserName.Text.ToString() + "',mypassword='" + UserPass1.Password.ToString() + "' WHERE [ID]=1";
    
                    OleDbCommand Updcmd = new OleDbCommand(Updsql, olecon);
                    Updcmd.ExecuteNonQuery();
                    olecon.Close();
 

                    MessageBox.Show("账户和密码修改成功！");
                    this.Close();

                }
                catch (Exception ex)
                {
                    MessageBox.Show("错误" + ex.Message);
                }
            }

            
        }
    }
}
