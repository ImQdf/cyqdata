using System;
using System.Collections.Generic;
using System.Text;
using CYQ.Data.Tool;
using System.Threading;
using System.IO;


namespace CYQ.Data
{
    /// <summary>
    /// ���ݿ����Ͳ�����
    /// </summary>
    internal class DalCreate
    {
        //private const string SqlClient = "System.Data.SqlClient";
        //private const string OleDb = "System.Data.OleDb";
        //private const string OracleClient = "System.Data.OracleClient";
        //private const string SQLiteClient = "System.Data.SQLite";
        //private const string MySqlClient = "MySql.Data.MySqlClient";
        //private const string SybaseClient = "Sybase.Data.AseClient";
        //private const string PostgreClient = "System.Data.NpgSqlClient";
        //private const string TxtClient = "CYQ.Data.TxtClient";
        //private const string XmlClient = "CYQ.Data.XmlClient";
        //private const string XHtmlClient = "CYQ.Data.XHtmlClient";

        /// <summary>
        /// �򵥹�����Factory Method��
        /// </summary>
        /// <param name="dbConn"></param>
        /// <returns></returns>
        public static DbBase CreateDal(string connNameOrString)
        {
            //ABCConn
            DbBase db = GetDbBaseBy(ConnObject.Create(connNameOrString));

            if (db.connObject.Master.ConnName != connNameOrString && connNameOrString.EndsWith("Conn"))//��Ҫ�л����á�
            {
                //Conn  A��
                //BConn  xxx �Ҳ���ʱ����Ĭ�Ͽ⡣
                DbResetResult result = db.ChangeDatabase(connNameOrString.Substring(0, connNameOrString.Length - 4));
                if (result == DbResetResult.Yes) // д�뻺��
                {
                    db.connObject.SaveToCache(connNameOrString);
                }
            }
            return db;

        }

        private static DbBase GetDbBaseBy(ConnObject co)
        {
            DalType dalType = co.Master.ConnDalType;
            //License.Check(providerName);//���ģ����Ȩ��⡣
            switch (dalType)
            {
                case DalType.MsSql:
                    return new MsSqlDal(co);
                case DalType.Access:
                    return new OleDbDal(co);
                case DalType.Oracle:
                    return new OracleDal(co);
                case DalType.SQLite:
                    return new SQLiteDal(co);
                case DalType.MySql:
                    return new MySQLDal(co);
                case DalType.Sybase:
                    return new SybaseDal(co);
                case DalType.PostgreSQL:
                    return new PostgreDal(co);
                case DalType.Txt:
                case DalType.Xml:
                    return new NoSqlDal(co);
            }
            return (DbBase)Error.Throw(string.Format("GetHelper:{0} No Be Support Now!", dalType.ToString()));
        }

        public static DalType GetDalTypeByReaderName(string typeName)
        {
            switch (typeName.Replace("DataReader", "").ToLower())
            {
                case "oracle":
                    return DalType.Oracle;
                case "sql":
                    return DalType.MsSql;
                case "sqlite":
                    return DalType.SQLite;
                case "oledb":
                    return DalType.Access;
                case "mysql":
                    return DalType.MySql;
                case "odbc":
                case "ase":
                    return DalType.Sybase;
                case "PgSql":
                case "Npgsql":
                    return DalType.PostgreSQL;
                default:
                    return DalType.None;

            }
        }

    }


}