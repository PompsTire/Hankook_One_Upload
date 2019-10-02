using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.Net.Mail;
using Renci.SshNet;

namespace Hankook_One_Upload
{
    class Program
    {
        static void Main(string[] args)
        {
            //SqlConnection connection = new SqlConnection("Data Source=gbsql01v2;Initial Catalog=Dealer_Programs;Persist Security Info=True;User ID=SQLDatabaseUser;Password=Pompstire12!");
            SqlConnection connection = new SqlConnection("Data Source=gbsql01v2;Initial Catalog=Dealer_Programs;Persist Security Info=True;User ID=sa;Password=4aCN4Ns");

            string logFileYear, logFileMonth, logFileDay, logFileHour, logFileMinute;

            logFileYear = DateTime.Today.Year.ToString();

            if (DateTime.Today.Month < 10)
            {
                logFileMonth = "0" + DateTime.Today.Month.ToString();
            }
            else
            {
                logFileMonth = DateTime.Today.Month.ToString();
            }

            if (DateTime.Today.Day < 10)
            {
                logFileDay = "0" + DateTime.Today.Day.ToString();
            }
            else
            {
                logFileDay = DateTime.Today.Day.ToString();
            }

            if (DateTime.Now.Hour < 10)
            {
                logFileHour = "0" + DateTime.Now.Hour.ToString();
            }
            else
            {
                logFileHour = DateTime.Now.Hour.ToString();
            }
            if (DateTime.Now.Minute < 10)
            {
                logFileMinute = "0" + DateTime.Now.Minute.ToString();
            }
            else
            {
                logFileMinute = DateTime.Now.Minute.ToString();
            }

            HankookPurchases(connection, logFileYear, logFileMonth, logFileDay, logFileHour, logFileMinute);

            System.Environment.Exit(1);
        }
        private static void Update_Hankook_One_Dealer(SqlConnection connection)
        {
            string myQuery;

            try
            {
                myQuery = @"Update Dealer_Programs.dbo.DealerProfile Set newDealer = 0 where cudealerprogram like ('Hankook One%') and newDealer = 1";
                SqlCommand cmd = new SqlCommand(myQuery, connection);
                cmd.ExecuteScalar();

                SqlCommand command = new SqlCommand("exec dbo.sp_Upload_HK_One_Purchases",connection);
                command.ExecuteScalar();

                Console.WriteLine("Status update {0}", "New Dealer status has been updated");
            }
            catch (Exception error)
            {
                string msg = "GBSQL01v2\nWhile updating Dealer Profile table " + error.Message;
                Email_Notification("HankookOne@pompstire.com", "Hankook One Update Error", msg);
            }
        }
        private static void Upload_Hankook_One_Purchases(SqlConnection connection,string filePath, string fileName)
        {
            string logFileName, host, username, password;
            //string downloadFileName;
            try
            {

                logFileName = filePath + fileName;
                host = "sftp.channel-fusion.com";
                username = "PompsTire";
                password = "TBDSQjQN";
                //downloadFileName = "ack_309354_" + logFileYear + logFileMonth + logFileDay + ".csv";
                //host = "fxserver.360incentives.com";
                //username = "HankookUSA_205545";
                //password = "~5SQwZq8TVU9";

                using (var client = new SftpClient(host, 22, username, password))
                {

                    client.Connect();

                    Console.WriteLine("Connected to {0}", host);

                    //client.ChangeDirectory("/in/");

                    //Console.WriteLine("Changed directory to {0}", workingdirectory);

                    using (var fileStream = new FileStream(logFileName, FileMode.Open))
                    {

                        //Console.WriteLine("Uploading {0} ({1:N0} bytes)", logFileName, fileStream.Length);

                        client.BufferSize = 4 * 1024; // bypass Payload error large files

                        client.UploadFile(fileStream, Path.GetFileName(logFileName));

                    }
                    Console.WriteLine("Status update {0}", logFileName + " has been uploaded");
                    FileInfo newFile = new FileInfo(filePath + "Hankook_One.csv");
                    if(newFile.Exists) { newFile.Delete(); }
                    FileInfo File = new FileInfo(filePath + fileName);
                    File.CopyTo(filePath + "Hankook_One.csv");
                    client.Disconnect();
                }
                Update_Hankook_One_Dealer(connection);
                //System.Environment.Exit(0);
                //Application.Exit();
            }
            catch (Exception error)
            {
                string msg = "GBSQL01v2\nWhile uploading HK One units file " + error.Message;
                Email_Notification("HankookOne@pompstire.com", "Hankook One Upload Error", msg);
            }
        }

        private static void HankookPurchases(SqlConnection connection, string logFileYear, string logFileMonth, string logFileDay, string logFileHour, string logFileMinute)
        {
            string myQuery, filePath, fileName,dataLine;
            try
            {
                //                myQuery = @"select SoldTo_Distributor_Number,(ShipTo_Distributor_Number * 1) as ShipTo_Distributor_Number,inv_num as Invoice_Number
                //,inv_date as Date_Of_Sale,rtrim(dp.cudealernum) as HK_ONE_Dealer_Number,replace(dp.cuname,',','') as Associate_Dealer_Name,rtrim(m.pdmfgprdno) as Material_Number
                //,Case when IsNumeric(m.Quantity) = 1 then convert(integer,m.Quantity) else 0 end as Quantity
                //from Openquery(MaddenCo_DTA577,'select ''205545'' as SoldTo_Distributor_Number
                //,sxcvstrcv as ShipTo_Distributor_Number
                //,cast(tihlnuminv as varchar(10)) as inv_num
                //,substring(tihhdteinv,5,2) || ''/'' || right(tihhdteinv,2) || ''/'' || left(tihhdteinv,4) as inv_date
                //,cast(tihlnumcst as varchar(12)) as tihlnumcst
                //,trim(cast(tihlprd as varchar(15))) as PdNumber
                //,trim(cast(pdmfgprdno as varchar(15))) as pdmfgprdno
                //,sum(tihlqty) as Quantity,To_Date(tihhdtechg,''YYYYMMDD'') as tihhdtechg
                //from dta577.tmihsl inner join dta577.tmihsh on tihlnuminv = tihhnuminv
                //inner join dta577.tmprod on tihlprd = pdnumber and tihlnumstr = pdstore
                //inner join dta577.tmsxcv on tihlnumstr = sxcvstrmc and sxcvnumvnd = ''41882''
                //where tihlqty <> 0 and tihhvoidyn <> ''Y'' and tihlcoddel <> ''D'' and tihlvndprd in(''060'',''061'') and tihlclsprd in(''04'',''08'')
                //--and left(tihlyrprin,4) = Year(Now())
                //--and right(tihlyrprin,2) = ''04''
                //and left(tihlyrprin,4) between Year(Now()) - 1 and Year(Now())
                //and right(tihlyrprin,2) between case when Month(Now()) = 1 then 12 else Month(Now()) - 1 end and Month(Now())
                //--and tihlnumcst in(''2037700'')
                //--and tihhdtechg between 20181210 and 20181216)
                //group by sxcvstrcv,tihlnuminv,tihhdteinv,tihhdtechg,tihlnumcst,tihlprd,pdmfgprdno') as m
                //inner join Dealer_Programs.dbo.DealerProfile as dp on m.tihlnumcst = dp.cunumber and dp.cudealerprogram in('Hankook One','Hankook One Secondary')
                //--inner join DealerPrograms.dbo.DealerInformation as di on dp.cudealernum = di.cudealernum
                //where dp.cudealernum is not null and dp.dealeractive = 1
                //and tihhdtechg between case when newDealer = 1 then cudealerstartdate else convert(date,DateAdd(day,-1,getdate())) end and getdate()
                //order by m.inv_num";

                myQuery = "EXEC Dealer_Programs.dbo.up_Hankook_DailyUploads ";

                filePath = "C:\\" + "Scheduled Tasks\\Hankook One Upload\\Documents\\";
                fileName = "205545-" + logFileYear.ToString() + logFileMonth.ToString() + logFileDay.ToString() + "-" + logFileHour.ToString() + logFileMinute.ToString() + ".csv";

                using (SqlCommand cmd = new SqlCommand(myQuery, connection))
                {
                    connection.Open();
                    try
                    {
                        StreamWriter outputFile;
                        outputFile = File.CreateText(filePath + fileName);
                        cmd.CommandTimeout = 0;
                        if (cmd.ExecuteScalar() != null)
                        {

                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                outputFile.WriteLine("SoldTo_Distributor_Number,ShipTo_Distributor_Number,Invoice_Number,Date_of_Sale,Hankook_ONE_Dealer_Number,Dealer_Name,Material_Number,Quantity");
                                while (reader.Read())
                                {
                                    dataLine = (reader["SoldTo_Distributor_Number"].ToString()
                                    + ',' + reader["ShipTo_Distributor_Number"].ToString()
                                    + ',' + reader["Invoice_Number"].ToString()
                                    + ',' + reader["Date_Of_Sale"].ToString()
                                        + ',' + reader["HK_ONE_Dealer_Number"].ToString()
                                        + ',' + reader["Associate_Dealer_Name"].ToString()
                                        + ',' + reader["Material_Number"].ToString()
                                        + ',' + reader["Quantity"].ToString());
                                    outputFile.WriteLine(dataLine);
                                    Console.WriteLine(dataLine);
                                }
                            }
                            outputFile.Close();
                            Console.WriteLine("Upload status {0}", fileName);
                            Upload_Hankook_One_Purchases(connection,filePath, fileName);
                        }
                        else
                        {
                            Email_Notification("HankookOne@pompstire.com", "Hankook One Notification: No Invoice Data - DNE", "No Invoice Data Found in Maddenco");
                        }
                        connection.Close();
                    }
                    catch (Exception ex)
                    {
                        string msg = "GBSQL01v2\nWhile writting Hankook One purchases file " + ex.Message;
                        Email_Notification("HankookOne@pompstire.com", "Hankook One Notification: Error", msg);
                    }
                }


            }
            catch (Exception ex)
            {
                string msg = "GBSQL01v2\nWhile Registering Purchases\n" + ex.Message;
                Email_Notification("HankookOne@pompstire.com", "Hankook One Upload Error", msg);
            }
        }
        private static void Email_Notification(string emailFrom, string emailSubject, string msg)
        {
            MailMessage Notification = new MailMessage();
            SmtpClient client = new SmtpClient();
            client.Port = 25;
            client.Host = "mail.pompstire.com";
            client.Timeout = 100000;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;
            client.Credentials = new System.Net.NetworkCredential("anonymous", "");
            Notification.From = new MailAddress(emailFrom);
            Notification.To.Add(new MailAddress("devhelp@pompstire.com"));  
            Notification.Subject = emailSubject;
            Notification.IsBodyHtml = true;
            Notification.Body = msg;
            client.Send(Notification);
        }
    }
}
