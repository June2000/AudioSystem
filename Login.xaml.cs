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
    /// Login.xaml 的交互逻辑
    /// </summary>
    public partial class Login : Window
    {
        public Login()
        {
            InitializeComponent();

        }
       
        public static string AccessConnStr = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=MyLocalData.mdb;Jet OLEDB:Database Password=CH123321";

        private string MyUser = "";
        private string MyPassWord = "";
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OleDbConnection olecon = new OleDbConnection(AccessConnStr);
                olecon.Open();
                string selstr = @"select * from uinfo where ID=1 ";
                OleDbCommand olecmd = new OleDbCommand(selstr, olecon);
                OleDbDataReader olereader = olecmd.ExecuteReader();     //执行查询
                if (olereader.Read())
                {
                    MyUser = olereader["myuser"].ToString();
                    MyPassWord = olereader["mypassword"].ToString();
                }
                olereader.Close();
                olecon.Close();

                if (MyUser == UserName.Text && MyPassWord == UserPass.Password)
                {
                    this.DialogResult = true;
                }
                else
                {
                   // this.DialogResult = false;
                    MessageBox.Show("账户或密码错误，请重新输入！");
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("错误" + ex.Message);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
           
            this.Close();
        }
    }
}
