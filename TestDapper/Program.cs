using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper.Contrib.Extensions;
using Dapper;

namespace TestDapper
{
    class Program
    {
        static void Main(string[] args)
        {

            /*
             *Pasos para configurar la prueba
             * 1- Corre el archivo DB.sql dentro de una base de datos con el nombre TestDB y luego setea el connectrion string TestConnStr con tus credenciales de SQL.
             * 2- Agrega la tabla y el procedure de la base de datos TestDB al modelo de EF
             * 3- Agrega la cantidad de iteraciones con las que quieres probar: variable iteraciones            * 
             *
             */

            int iteraciones = 10;

            string connStr = new TestDBEntities().Database.Connection.ConnectionString;
  

            string sql = "select * from testtable";

            var results = new List<Results>();

            //dapper
            var inicio = DateTime.Now;
            for (int i = 1; i <= iteraciones; i++)
            {
                using (var connection = new SqlConnection(connStr))
                {
                    connection.Open();
                    var orderDetails = connection.Query(sql + " where id=" + i).First();
                    string recordId = Convert.ToString(orderDetails.id);

                    //update a record
                    connection.ExecuteScalar("update testtable set column1 = '"+Guid.NewGuid().ToString()+"' where id=" + recordId);
                }
                Console.Clear();
                Console.WriteLine("Dapper:"+i);
            }
            results.Add(new Results() { TechName = "DAPPER", Seconds = (DateTime.Now - inicio).TotalSeconds });

            //ado
            inicio = DateTime.Now;
            for (int i = 1; i <= iteraciones; i++)
            {
                using (var connection = new SqlConnection(connStr))
                {
                    connection.Open();
                    var cmd = new SqlCommand(sql + " where id=" + i, connection);
                    var orderDetails = cmd.ExecuteReader();
                    orderDetails.Read();
                    var recordId = Convert.ToString(orderDetails["id"]);
                    orderDetails.Close();
                    cmd = new SqlCommand("update testtable set column1 = '" + Guid.NewGuid().ToString() + "' where id="+recordId, connection);
                    cmd.ExecuteScalar();
                }
                Console.Clear();
                Console.WriteLine("ADO:" + i);
            }
            results.Add(new Results() { TechName = "ADO.NET", Seconds = (DateTime.Now - inicio).TotalSeconds });

            //ado data adapter
            inicio = DateTime.Now;
            for (int i = 1; i <= iteraciones; i++)
            {
                var dt = new System.Data.DataTable();
                var da = new SqlDataAdapter(sql + " where id=" + i, connStr);
                da.Fill(dt);
                var recordId = Convert.ToString(dt.Rows[0]["id"]);
                dt = new System.Data.DataTable();
                da = new SqlDataAdapter("update testtable set column1 = '" + Guid.NewGuid().ToString() + "' where id="+recordId, connStr);
                da.Fill(dt);

                Console.Clear();
                Console.WriteLine("ADO DATA ADAPTER:" + i);
            }
            
            results.Add(new Results() { TechName = "ADO.NET DATA ADAPTER", Seconds = (DateTime.Now - inicio).TotalSeconds });

            //ef
            inicio = DateTime.Now;
            for (int i = 1; i <= iteraciones; i++)
            {
                using (var db = new TestDBEntities())
                {
                    var x = db.TestTables.Where(y => y.id == i).First();
                    var recordId =  x.id;
                    x.column1 = Guid.NewGuid().ToString();
                    db.SaveChanges();
                }

                Console.Clear();
                Console.WriteLine("EF:" + i);
            }
            results.Add(new Results() { TechName = "EF TABLE MODEL", Seconds = (DateTime.Now - inicio).TotalSeconds });

            //ef config
            inicio = DateTime.Now;           
            for (int i = 1; i <= iteraciones; i++)
            {
                using (var dbx = new TestDBEntities())
                {
                    dbx.Configuration.AutoDetectChangesEnabled = false;
                    dbx.Configuration.LazyLoadingEnabled = false;
                    var x = dbx.TestTables.Where(y => y.id == i).First();
                    var recordId = x.id;
                    x.column1 = Guid.NewGuid().ToString();
                    dbx.SaveChanges();
                }

                Console.Clear();
                Console.WriteLine("EF CONFIG:" + i);
            }           
            results.Add(new Results() { TechName = "EF TABLE MODEL WITH CONFIG", Seconds = (DateTime.Now - inicio).TotalSeconds });

            //ef proc + config
            inicio = DateTime.Now;
            for (int i = 1; i <= iteraciones; i++)
            {
                using (var dbx = new TestDBEntities())
                {
                    dbx.Configuration.AutoDetectChangesEnabled = false;
                    dbx.Configuration.LazyLoadingEnabled = false;
                    var x = dbx.sp_SelectRecord(i).First();
                    x.column1 = Guid.NewGuid().ToString();
                    dbx.SaveChanges();


                }

                Console.Clear();
                Console.WriteLine("EF CONFIG:" + i);
            }
            results.Add(new Results() { TechName = "EF SP WITH CONFIG", Seconds = (DateTime.Now - inicio).TotalSeconds });


            //Print results
            Console.Clear();
            foreach (var item in results.OrderBy(x=>x.Seconds))
            {
                Console.WriteLine("Tech:{0} {1}", item.TechName, item.Seconds);
            }


            Console.Read();
        }
    }

    public class Results
    {
        public string TechName { get; set; }
        public double Seconds { get; set; }
    }
}
