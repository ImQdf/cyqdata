using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace CYQ.Data
{
    /// <summary>
    /// ���ݿ�������ʵ��
    /// </summary>
    internal partial class ConnBean
    {
        private ConnBean()
        {

        }
        /// <summary>
        /// ��Ӧ��ConnectionString��Name
        /// </summary>
        public string ConnName = string.Empty;
        /// <summary>
        /// ���ӵ�״̬�Ƿ�������
        /// </summary>
        public bool IsOK = true;
        /// <summary>
        /// �Ƿ�ӿ�
        /// </summary>
        public bool IsSlave = false;
        /// <summary>
        /// ���Ӵ���ʱ���쳣��Ϣ��
        /// </summary>
        internal string ErrorMsg = string.Empty;
        /// <summary>
        /// ���ݿ������ַ���
        /// </summary>
        public string ConnString = string.Empty;
        /// <summary>
        /// ���ݿ�����
        /// </summary>
        public DalType ConnDalType;
        /// <summary>
        /// ���ݿ�汾��Ϣ
        /// </summary>
        public string Version;
        public ConnBean Clone()
        {
            ConnBean cb = new ConnBean();
            cb.ConnName = this.ConnName;
            cb.ConnString = this.ConnString;
            cb.ConnDalType = this.ConnDalType;
            cb.IsOK = this.IsOK;
            return cb;
        }
        public bool TryTestConn()
        {
            //err = string.Empty;
            if (!string.IsNullOrEmpty(ConnName))
            {
                DbBase helper = DalCreate.CreateDal(ConnName);
                try
                {

                    helper.Con.Open();
                    Version = helper.Con.ServerVersion;
                    if (string.IsNullOrEmpty(Version)) { Version = helper.dalType.ToString(); }
                    helper.Con.Close();
                    IsOK = true;
                    ErrorMsg = string.Empty;
                }
                catch (Exception er)
                {
                    ErrorMsg = er.Message;
                    IsOK = false;
                }
                finally
                {
                    helper.Dispose();
                }
            }
            else
            {
                IsOK = false;
            }
            return IsOK;
        }
    }
    internal partial class ConnBean
    {
        /// <summary>
        /// ����һ��ʵ����
        /// </summary>
        /// <param name="dbConn"></param>
        /// <returns></returns>
        public static ConnBean Create(string connNameOrString)
        {
            string connString = string.Format(AppConfig.GetConn(connNameOrString), AppConfig.WebRootPath);
            if (string.IsNullOrEmpty(connString))
            {
                return null;
            }
            ConnBean cb = new ConnBean();
            cb.ConnName = connNameOrString;
            cb.ConnDalType = GetDalTypeByConnString(connString);
            cb.ConnString = RemoveConnProvider(cb.ConnDalType, connString);

            return cb;
        }
        /// <summary>
        /// ȥ�� �����е� provider=xxxx;
        /// </summary>
        public static string RemoveConnProvider(DalType dal, string connString)
        {
            if (dal != DalType.Access)
            {
                string conn = connString.ToLower();
                int index = conn.IndexOf("provider");
                if (index > -1 && index < connString.Length - 5 && (connString[index + 8] == '=' || connString[index + 9] == '='))
                {
                    int end = conn.IndexOf(';', index);
                    if (end > index)
                    {
                        connString = connString.Remove(index, end - index + 1);
                    }
                }
            }
            return connString;
        }
        public static DalType GetDalTypeByConnString(string connString)
        {
            connString = connString.ToLower().Replace(" ", "");//ȥ���ո�

            #region �ȴ��������жϹ����
            if (connString.Contains("txtpath="))
            {
                return DalType.Txt;
            }
            if (connString.Contains("xmlpath="))
            {
                return DalType.Xml;
            }
            if (connString.Contains("initialcatalog=") || connString.Contains(",1433;"))
            {
                return DalType.MsSql;
            }
            if (connString.Contains("microsoft.jet.oledb.4.0") || connString.Contains("microsoft.ace.oledb") || connString.Contains(".mdb"))
            {
                return DalType.Access;
            }
            if (connString.Contains("provider=msdaora") || connString.Contains("provider=oraoledb.oracle")
                || connString.Contains("description=") || connString.Contains("fororacle"))
            {
                return DalType.Oracle;
            }
            if (connString.Contains("failifmissing=") || (connString.StartsWith("datasource=") && (connString.EndsWith(".db") || connString.EndsWith(".db3"))))
            {
                return DalType.SQLite;
            }
            if (connString.Contains("convertzerodatetime") || connString.Contains("port=3306") || (connString.Contains("host=") && connString.Contains("port=") && connString.Contains("database=")))
            {
                return DalType.MySql;
            }
            if (connString.Contains("provider=ase") || connString.Contains("port=5000") || (connString.Contains("datasource=") && connString.Contains("port=") && connString.Contains("database=")))
            {
                return DalType.Sybase;
            }
            if (connString.Contains("port=5432"))
            {
                return DalType.PostgreSQL;
            }

            #endregion

            //postgre��mssql���������һ����Ϊpostgre
            if (File.Exists(AppConfig.AssemblyPath + "Npgsql.dll"))
            {
                return DalType.PostgreSQL;
            }

            return DalType.MsSql;

        }

    }
}
