using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using System.Data.SqlClient;

namespace csql
{
    class Program
    {
        private const string INIFILE = "csql.ini";
        private static string connectionString = string.Empty;

        public static string ConnectionString
        {
            get
            {
                return connectionString;
            }
        }
        static void Main(string[] args)
        {

            string path = @"c:\tools";
            using (StreamReader reader = new StreamReader(path + @"\" + INIFILE))
            {
                connectionString = reader.ReadLine();

                if (ConnectionString == String.Empty)
                {
                    Console.WriteLine("No connection string found");
                    return;
                }
            }

            if (args.Length == 0)
            {
                Console.WriteLine("No sql command given");
                return;
            }

            string sql = string.Empty;

            if (args.Length == 2 && args[0] == "-i")
            {
                sql = readSqlFromFile(args[1]);
            }
            else
            {
                sql = args[0];
            }

            processSQL(sql);
        }

        private static string readSqlFromFile(string filename)
        {
            if (!File.Exists(filename))
            {
                return string.Empty;
            }

            return File.ReadAllText(filename);
        }

        static void processSQL(string command)
        {
            SqlConnection connection = new SqlConnection(ConnectionString);
            try
            {
                connection.Open();
            }
            catch (SqlException ex)
            {
                Console.WriteLine("Error connecting to database using string: " + ConnectionString);
                return;
            }

            Console.WriteLine("csql v 0.1. Connection string: {0}", ConnectionString);

            SqlCommand cmd = new System.Data.SqlClient.SqlCommand(command, connection);
            cmd.CommandType = CommandType.Text;
            SqlDataAdapter adapter = new SqlDataAdapter(cmd);
            DataSet returnDs = new DataSet();

            try
            {
                adapter.Fill(returnDs);
            }
            catch (SqlException ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return;
            }
            int tableCount = 1;

            foreach (DataTable table in returnDs.Tables)
            {
                if (returnDs.Tables.Count > 1)
                {
                    Console.WriteLine("Table " + tableCount.ToString());
                    Console.WriteLine("-------");
                }

                //outputInLineFormat(table);
                outputInColumnFormat(table);

                Console.WriteLine(" ");

                tableCount++;
            }
            if (returnDs.Tables.Count == 1)
            {
                Console.WriteLine("\nFinished. 1 table returned.");
            }
            else
            {
                Console.WriteLine("\nFinished. {0} tables returned.", returnDs.Tables.Count);
            }
        }

        /// <summary>
        /// Display the contents of the table in Line oriented mode
        /// </summary>
        /// <param name="table"></param>
        private static void outputInLineFormat(DataTable table)
        {

            foreach (DataColumn col in table.Columns)
            {
                Console.Write(col.ColumnName + " | ");
            }
            Console.Write("\n");

            foreach (DataRow row in table.Rows)
            {
                Console.Write("\n");
                foreach (object column in row.ItemArray)
                {
                    Console.Write(column.ToString() + " | ");
                }
            }
        }

        /// <summary>
        /// Display the contents of the table in Line oriented mode
        /// </summary>
        /// <param name="table"></param>
        private static void outputInColumnFormat(DataTable table)
        {
            string[] columnNames = new string[table.Columns.Count];

            int index = 0;
            int longestNameLength = 0;

            foreach (DataColumn col in table.Columns)
            {
                columnNames[index] = col.ColumnName;
                if (col.ColumnName.Length > longestNameLength) { longestNameLength = col.ColumnName.Length; }
                index++;
            }

            foreach (DataRow row in table.Rows)
            {
                index = 0;
                Console.Write("\n");
                foreach (object column in row.ItemArray)
                {
                    string colName = columnNames[index];
                    colName = colName.PadRight(longestNameLength);
                    Console.Write(colName + ": ");
                    Console.Write(column.ToString() + "\n");
                    index++;
                }
            }
        }

    }
}
