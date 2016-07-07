using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using CommandLine.Text;

namespace csql
{
    class Program
    {
        private const string INIFILE = "csql.ini";
        private static string connectionString = string.Empty;
        private static Options options = new Options();
        private static int databaseIndex = -1;
        private static List<string> databases = new List<string>();
        private static List<string> comments = new List<string>();
        private static bool updateIniFile = false;

        public static string ConnectionString
        {
            get
            {
                return connectionString;
            }
        }
        static void Main(string[] args)
        {
            if (!CommandLine.Parser.Default.ParseArguments(args, options))
            {
                Environment.Exit(CommandLine.Parser.DefaultExitCodeFail);
            }

            extractSqlCommand(args);

            if (options.Help)
            {
                showHelp();
                return;
            }

            if (!loadIniFile())
            {
                Environment.Exit(1);
            }

            if (options.ShowList)
            {
                Console.WriteLine("csql version 0.2");

                showList();
                return;
            }

            string sql = extractSqlCommand(args);

            if (sql.Trim().Length == 0)
            {
                Console.WriteLine("No sql command given");
                return;
            }

            processSQL(sql);

            if (updateIniFile)
            {
                writeIniFile();
            }
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

            Console.WriteLine("csql v 0.2. Connected to {0}", formatDbString(ConnectionString));

            if (options.Verbose)
            {
                Console.WriteLine("csql v 0.2. Connection string: {0}", ConnectionString);
            }

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
                    var rowText = (table.Rows.Count == 1) ? " row" : " rows";
                    Console.WriteLine("\nTable " + tableCount.ToString() + ": " + table.Rows.Count.ToString() + rowText);
                }
                else
                {
                    var rowText = (table.Rows.Count == 1) ? " row" : " rows";
                    Console.WriteLine("\n" + table.Rows.Count.ToString() + rowText);
                }

                if (options.Column)
                {
                    outputInColumnFormat(table);
                }
                else
                {
                    outputInLineFormat(table);
                }

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
            //First find the maximum width of each column of data
            List<int> maxColumnWidth = new List<int>(table.Columns.Count);

            foreach (DataColumn col in table.Columns)
            {
                maxColumnWidth.Add(col.ColumnName.Length);
            }

            int index = 0;
            foreach (DataRow row in table.Rows)
            {
                index = 0;
                foreach (object column in row.ItemArray)
                {
                    var colLength = column.ToString().Length;
                    if (colLength > maxColumnWidth[index])
                    {
                        maxColumnWidth[index] = colLength;
                    }
                    index++;
                }
            }

            StringBuilder sbSeparator = new StringBuilder();
            int lineWidth = 0;
            foreach (var value in maxColumnWidth)
            {
                lineWidth += value + 1;
                sbSeparator.Append("+" + "".PadLeft(value, '-'));
            }

            //Now write out the actual data
            sbSeparator.Append("+");
            string separator = sbSeparator.ToString();

            Console.Write(separator + "\n");
            index = 0;
            foreach (DataColumn col in table.Columns)
            {
                Console.Write(formatColumn(col.ColumnName, maxColumnWidth[index]));
                index++;
            }

            Console.Write("|\n");
            Console.Write(separator);

            foreach (DataRow row in table.Rows)
            {
                Console.Write("\n");
                index = 0;
                foreach (object column in row.ItemArray)
                {
                    Console.Write(formatColumn(column.ToString(), maxColumnWidth[index]));
                    index++;
                }
                Console.Write("|");
            }
            Console.Write("\n");
            Console.Write(separator);
        }

        private static string formatColumn(string text, int width)
        {
            return "|" + text.PadRight(width, ' ');
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

        private static bool loadIniFile()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;

            using (StreamReader reader = new StreamReader(path + @"\" + INIFILE))
            {
                #region Retrieve the database-to-use number
                var line = reader.ReadLine();

                if (line.Trim().Length == 0)
                {
                    Console.WriteLine("Invalid ini file. First line should be the number of the database connection to use");
                    return false;
                }

                try
                {
                    databaseIndex = System.Convert.ToInt32(line);
                }
                catch (Exception)
                {
                    Console.WriteLine("Invalid ini file. First line should be the number of the database connection to use");
                    return false;
                }
                #endregion

                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Trim().Length > 0)
                    {
                        if (line.Substring(0, 1) == "#")
                        {
                            comments.Add(line);
                        }
                        else
                        {
                            databases.Add(line);
                        }
                    }
                }
            }

            if (options.SelectedDatabase >= 0)
            {
                databaseIndex = options.SelectedDatabase;
                updateIniFile = true;
            }
            if (databaseIndex >= databases.Count)
            {
                Console.WriteLine("Invalid database selected. Available databases:");
                showList();
                return false;
            }
            else
            {
                connectionString = databases[databaseIndex];
            }
            return true;

        }

        private static string extractSqlCommand(string[] args)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var arg in args)
            {
                if (arg.Substring(0, 1) != "-")
                {
                    sb.Append(arg + " ");
                }
            }

            return sb.ToString();
        }

        private static void showHelp()
        {
            HelpText ht = new HelpText("csql version 0.2");
            ht.AddOptions(options);
            Console.WriteLine(ht.ToString());

        }

        private static void showList()
        {
            Console.WriteLine("");

            int counter = 0;
            foreach (var db in databases)
            {
                Console.WriteLine(string.Format("{0}: {1}", counter, formatDbString(db)));
                counter++;
            }
            Console.WriteLine("");
        }

        private static string formatDbString(string db)
        {
            int start = db.IndexOf("Source=") + 7;
            int end = db.IndexOf(";", start);
            string server = db.Substring(start, end - start);

            start = db.IndexOf("Catalog=") + 8;
            end = db.IndexOf(";", start);
            string database = db.Substring(start, end - start);

            return server + "." + database;
        }

        private static void writeIniFile()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;

            using (StreamWriter writer = new StreamWriter(path + @"\" + INIFILE))
            {
                writer.WriteLine(databaseIndex.ToString());
                foreach (var db in databases)
                {
                    writer.WriteLine(db);
                }
                foreach (var c in comments)
                {
                    writer.WriteLine(c);
                }
            }
        }
    }
}
