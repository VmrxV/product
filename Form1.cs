using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.ServiceProcess;
using System.Xml;
using System.Net;
using System.Data.SQLite;
using MySql.Data.MySqlClient;
using System.Collections.Generic;


namespace SWToR_RUS
{
    public partial class Form1 : Form
    {
        public string CurDir = AppDomain.CurrentDomain.BaseDirectory; //Путь к программе

        public string connStr_mysql = "server=" + "195.234.5.250" + //Адрес сервера (для локальной базы пишите "localhost")
                    ";user=" + "swtor" + //Имя пользователя
                    ";database=" + "swtor_ru" + //Имя базы данных
                    ";port=" + "3306" + //Порт для подключения
                    ";password=" + "KHUS86!JHksds" + //Пароль для подключения
                    ";default command timeout=18000;" +//Таймаут
                    ";pooling=false;";//Не храним соединения в пуле после закрытия приложения

        public Configuration Config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None); //Доступ к конфигурации

        public int gender = 1; //Пол персонажа

        public int launch_status = 0; //Статус лаунчера игры

        public string GamePath; //Путь к игре

        public int RusInstalled; //Установлен ли русификатор

        public int RusFontsInstalled; //Установлены ли шрифты

        public string heh = "";

        public string heh1 = "";

        public string arg = "";

        public string work = "";

        public int endtable;

        public int dbverasd;

        public int lastoffes;

        public int cou;

        public uint filescount;

        public string pathxml;

        public ulong vOut;

        public ulong vOut_old;

        public int keyxml;

        public string stjk;

        public uint hash_g;

        public string my_filename;

        public int is_run = 1;//Запускаем ли приложение, 1 -да,0-нет

        public IWebDriver driver;

        private readonly ManagementEventWatcher startWatch = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace"));

        private ProcessStartInfo startInfo = new ProcessStartInfo();

        public Form1()
        {
            InitializeComponent();
            App_Updater();//Обновление приложения, проверка новых версий
            if (is_run == 1)//Если нет обновлений запускаем приложение
            {
                Config_Work();//Работаем с конфигурационным файлом (проверка выставление отметок в интерфейсе)
                ManagementClass managementClass = new ManagementClass("Win32_Process");//Смотрим запущен ли лаучер игры
                foreach (ManagementObject instance in managementClass.GetInstances())
                {
                    if (instance["Name"].Equals("launcher.exe"))
                    {
                        launcher_status.Text = "Лаунчер SWToR запущен";
                        launcher_status.ForeColor = Color.Green;
                        launch_status = 1;
                        break;//Лаунчер найден - прерываем перебор запущенных процессов
                    }
                }
                if (launch_status == 0)//Если лаунчер не найден - сообщаем об этом
                {
                    launcher_status.Text = "Лаунчер SWToR не запущен";
                    launcher_status.ForeColor = Color.Red;
                }
                if (GamePath == "" || launch_status == 0)
                    Install_btn.Enabled = false;
                if (GamePath != "")//Если есть путь к расположению игры
                {
                    if (File.Exists(GamePath + "launcher.exe"))//Проверяем если игра в этой папке
                    {
                        GamePathTextBox.Text = GamePath;
                        if (GamePath.ToLower().IndexOf("steamapps") > 0)//Проверяем какая версия игры (Steam или нет)
                            steam_game.Checked = true;
                        else
                            steam_game.Checked = false;
                        if (Config.AppSettings.Settings["firstrun"].Value == "1")//Если это первый запуск программы
                        { del_btn.Enabled = false; }
                        else
                        {
                            string hash_in_config = Config.AppSettings.Settings["hash"].Value;//Считываем хэши из конфига и оригинального файла
                            if (hash_in_config != "")
                            {
                                string hash_original_file;
                                hash_original_file = CalculateMD5(GamePath + "\\Assets\\swtor_main_global_1.tor");
                                if (hash_in_config == hash_original_file)//Если хэши совпадают отключаем внопку Установки
                                {
                                    if (File.Exists(GamePath + "\\Assets\\swtor_en-us_global_1_tmp.tor") || File.Exists(GamePath + "\\Assets\\swtor_main_global_1_tmp.tor"))
                                        TryFix();//Видимо русификатор некорректно завершил работу, пытаемся вернуть оригинальные файлы на место
                                    if (File.Exists(GamePath + "\\Assets\\swtor_maln_gfx_assets_1.tor"))//Запоминаем если русификатор уже стоит
                                        RusInstalled = 1;
                                    Install_btn.Text = "Переустановить";
                                    del_btn.Enabled = true;
                                    Ins_font.Enabled = false;
                                }
                                else
                                    LogBox.AppendText("Hash не совпадает. Нажмите кнопку 'Переустановить'.\n");
                            }
                            if (File.Exists(GamePath + "\\Assets\\swtor_main_gfx_assets_1.tor_backup"))//Запоминаем если установлены только шрифты
                            {
                                RusFontsInstalled = 1;
                                Ins_font.Text = "Удалить шрифты";
                                del_btn.Enabled = false;
                            }
                        }
                        if (steam_game.Checked == true && launch_status == 1 && RusInstalled == 1)//Если версия Steam, Лаунчер запушен и русификатор установлен - подготавливаем файлы для запуска
                            Steam_Rename();
                    }
                }
                startWatch.EventArrived += StartWatch_EventArrived;//Начинаем следить за процессами, чтобы отловить игру или лаунчер
                startWatch.Start();
            }
        }

        public void StartWatch_EventArrived(object sender, EventArrivedEventArgs e) //Остлеживаем появление процессов, чтобы отловить игру или лаунчер
        {
            if (e.NewEvent.Properties["ProcessName"].Value.ToString() == "swtor.exe") //Отслеживаем запуск игры
            {
                startWatch.Stop();
                if (steam_game.Checked == false)//Если не steam версия на ходу подменяем файлы
                    Rus_for_orinal_game();//Подмена файлов для обычной версии с Bitraider'ом
            }
            if (e.NewEvent.Properties["ProcessName"].Value.ToString() == "launcher.exe")//Отслеживаем запуск лаунчера
            {
                Invoke((MethodInvoker)delegate
                {
                    launcher_status.Text = "Лаунчер SWToR запущен";
                    launcher_status.ForeColor = Color.Green;
                    if (steam_game.Checked == true && RusInstalled == 1)//Если версия Steam подготавливаем файлы для запуска
                        Steam_Rename();
                });
            }
        }

        public async void Rus_for_orinal_game()//Для обычной версии программа сама отслеживает запуск игры
        {
            ManagementClass managementClass = new ManagementClass("Win32_Process");
            foreach (ManagementObject instance in managementClass.GetInstances()) //Перебираем процессы в поисках игры
            {
                if (instance["Name"].Equals("swtor.exe")) //Собираем параметры и прекращаем процесс
                {
                    heh = instance["CommandLine"].ToString();
                    heh1 = instance["ExecutablePath"].ToString();
                    arg = heh.Substring(heh.LastIndexOf('"') + 2);
                    work = heh1.Remove(heh1.Length - 9);
                    instance.InvokeMethod("Terminate", null);
                    break;
                }
            }
            if (ServiceController.GetServices().FirstOrDefault((ServiceController s) => s.ServiceName == "BRSptStub") != null) //Отключаем Битрейдер
            {
                ServiceController serviceController = new ServiceController("BRSptStub");
                if (serviceController.Status.Equals(ServiceControllerStatus.Running))
                {
                    serviceController.Stop();
                    serviceController.WaitForStatus(ServiceControllerStatus.Stopped);
                }
            }
            foreach (ManagementObject instance2 in managementClass.GetInstances()) //Отключаем Битрейдер
            {
                if (instance2["Name"].Equals("brwc.exe"))
                {
                    instance2.InvokeMethod("Terminate", null);
                    break;
                }
            }
            foreach (ManagementObject instance3 in managementClass.GetInstances()) //Отключаем Битрейдер
            {
                if (instance3["Name"].Equals("BRSptSvc.exe"))
                {
                    instance3.InvokeMethod("Terminate", null);
                    break;
                }
            }
           

      

        private async void Install_btn_Click(object sender, EventArgs e) //Устанавливаем русификатор
        {
            Buttons_activity(0);
            await Install();
        }

        public async Task Install()//Установка русификатора
        {
            if (RusFontsInstalled == 1 && File.Exists(GamePath + "\\Assets\\swtor_main_gfx_assets_1.tor_backup") && File.Exists(GamePath + "\\Assets\\swtor_main_gfx_assets_1.tor"))
            {
                File.Delete(GamePath + "\\Assets\\swtor_main_gfx_assets_1.tor");
                File.Move(GamePath + "\\Assets\\swtor_main_gfx_assets_1.tor_backup", GamePath + "\\Assets\\swtor_main_gfx_assets_1.tor");
                Ins_font.Text = "Установить шрифт";
            }
           
            if (num == 0 || num2 == 0)
                LogBox.AppendText("Оригинальные файлы повреждены! Восспользуйтесь функцией проверки файлов игры.\n");
            else if (num == 2 || num2 == 2)
                LogBox.AppendText("Необходимо обновить игру перед установкой русификатора!\n");
            else
            {
                LogBox.AppendText(Properties.Resources.patchhosts);
                PatchHosts();//Патчим host файл, чтобы заблокировать отправку отчётов
                LogBox.AppendText(Properties.Resources.copyfiles);
                await CopyfilesAsync();//Копируем оригинальные файлы
                LogBox.AppendText(Properties.Resources.Done + "\n");
                LogBox.AppendText(Properties.Resources.Patch);
                Patch patch = new Patch();
                await Task.Run(delegate
                {
                    patch.ConnectDB();//Патчим файлы
                });
                LogBox.AppendText(Properties.Resources.Done + "\n");
                Config.AppSettings.Settings["firstrun"].Value = "0";
                Config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
                if (steam_game.Checked == true)
                    Steam_Rename();
                Install_btn.Text = "Переустановить";//Включаем элементы и переименовываем кнопку Установки
                RusInstalled = 1;
                Buttons_activity(1);
                LogBox.AppendText("Установка закончена.\n");
            }
        }

        private void PatchHosts()//Патчер host файла
        {
            string path = "C:\\Windows\\System32\\drivers\\etc\\hosts";
            if (File.Exists(path))
            {
                if (!File.ReadAllLines(path).Contains("# SWTOR crash send"))
                {
                    try
                    {
                        File.AppendAllLines(path, new string[6]
                        {
                            "\n",
                            "# SWTOR crash send",
                            "0.0.0.0 bugcatcher.swtor.com",
                            "0.0.0.0 crash.swtor.com",
                            "0.0.0.0 patcher-crash.swtor.com",
                            "\n"
                        });
                        LogBox.AppendText(Properties.Resources.Done + "\n");
                    }
                    catch (Exception)
                    {
                        LogBox.AppendText(Properties.Resources.hostsblock + "\n");
                    }
                }
                else
                    LogBox.AppendText(Properties.Resources.hostsalreadypatched + "\n");
            }
            else
                LogBox.AppendText(Properties.Resources.hostsnotfound + "\n");
        }

        public async Task CopyfilesAsync() //Копирование оригинальных файлов
        {
            ProgressBar1.Value = 0;
            ProgressBar1.Maximum = 10;
            await CopyFileAsync(GamePath + "\\Assets\\swtor_en-us_global_1.tor", GamePath + "\\Assets\\swtor_ru-wm_global_1.tor");
            ProgressBar1.Value += 1;
            await CopyFileAsync(GamePath + "\\Assets\\swtor_en-us_global_1.tor", GamePath + "\\Assets\\swtor_ru-ww_global_1.tor");
            ProgressBar1.Value += 2;
            await CopyFileAsync(GamePath + "\\Assets\\swtor_main_gfx_assets_1.tor", GamePath + "\\Assets\\swtor_maln_gfx_assets_1.tor");
            ProgressBar1.Value += 3;
            await CopyFileAsync(GamePath + "\\Assets\\swtor_main_global_1.tor", GamePath + "\\Assets\\swtor_maln_global_1.tor");
            string hash_original = CalculateMD5(GamePath + "\\Assets\\swtor_main_global_1.tor");
            ProgressBar1.Value += 4;
            Config.AppSettings.Settings["hash"].Value = hash_original;
            Config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        public async Task CopyFileAsync(string sourcePath, string destinationPath)//Ассинхронное копирование файлов
        {
            using (Stream source = File.OpenRead(sourcePath))
            {
                using (Stream destination = File.Create(destinationPath))
                {
                    await source.CopyToAsync(destination);
                }
            }
        }

       

        private void ChangePathButton_Click(object sender, EventArgs e)//Обработка изменения пути к игре
        {
            ChoosePath();
        }

        private void ChoosePath()//Обработка изменения пути к игре
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog
            {
                Description = "Укажите путь к SWToR"
            };
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                if (folderBrowserDialog.SelectedPath.ToLower().IndexOf("steamapps") > 0)//Проверяем путь к обычной версии или к Steam
                    steam_game.Checked = true;
                else if (steam_game.Checked == true)
                    steam_game.Checked = false;
                GamePath = folderBrowserDialog.SelectedPath;
                if (!GamePath.EndsWith("\\"))
                    GamePath += "\\";
                if (launch_status == 1)
                    TryFix();
                GamePathTextBox.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void ChooseSith_CheckedChanged(object sender, EventArgs e)//Переключатель варианта перевода Ситх\Сит
        {
            Config.AppSettings.Settings["sith"].Value = "0";
            Config.Save(ConfigurationSaveMode.Modified);
        }

        private void ChooseSit_CheckedChanged(object sender, EventArgs e)//Переключатель варианта перевода Ситх\Сит
        {
            Config.AppSettings.Settings["sith"].Value = "1";
            Config.Save(ConfigurationSaveMode.Modified);
        }

        private void ChooseMen_CheckedChanged(object sender, EventArgs e)//Переключатель мужской\женский персонаж
        {
            Config.AppSettings.Settings["gender"].Value = "1";
            Config.Save(ConfigurationSaveMode.Modified);
            gender = 1;
            if (steam_game.Checked == true && RusInstalled==1)
                Gender_steam_change("ru-wm");
        }

        private void ChooseWomen_CheckedChanged(object sender, EventArgs e)//Переключатель мужской\женский персонаж
        {
            Config.AppSettings.Settings["gender"].Value = "0";
            Config.Save(ConfigurationSaveMode.Modified);
            gender = 0;
            if (steam_game.Checked == true && RusInstalled == 1)
                Gender_steam_change("ru-ww");
        }

        public void Gender_steam_change(string first)//Переключатель мужской\женский персонаж для Steam версии игры
        {
            if (File.Exists(GamePath + "\\Assets\\swtor_en-us_global_1_tmp.tor"))
            {
                if (File.Exists(GamePath + "\\Assets\\swtor_ru-ww_global_1.tor"))
                    File.Move(GamePath + "\\Assets\\swtor_en-us_global_1.tor", GamePath + "\\Assets\\swtor_ru-wm_global_1.tor");
                else if (File.Exists(GamePath + "\\Assets\\swtor_ru-wm_global_1.tor"))
                    File.Move(GamePath + "\\Assets\\swtor_en-us_global_1.tor", GamePath + "\\Assets\\swtor_ru-ww_global_1.tor");
                File.Move(GamePath + "\\Assets\\swtor_"+ first + "_global_1.tor", GamePath + "\\Assets\\swtor_en-us_global_1.tor");
            }
        }

        private void GamePathTextBox_TextChanged(object sender, EventArgs e)//Проверяем изменение пути к игре
        {            
            string NewPath = GamePathTextBox.Text;
            if (!NewPath.EndsWith("\\"))
                NewPath += "\\";
            if (!File.Exists(NewPath + "launcher.exe")) //Если игры нет предлагаем заново выбрать путь
            {
                MessageBox.Show(Properties.Resources.WrongPath);
                ChoosePath();
                return;
            }
            Install_btn.Enabled = true;
            Ins_font.Enabled = true;
            Config.AppSettings.Settings["gamepath"].Value = NewPath;
            Config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
        
        private void Del_btn_Click(object sender, EventArgs e)//Удаляем русификатор
        {
            Buttons_activity(0);
            LogBox.AppendText(Properties.Resources.deletefiles);
            try
            {
                if (steam_game.Checked == true)
                    TryFix();
                if (File.Exists(GamePath + "\\Assets\\swtor_ru-wm_global_1.tor"))
                    File.Delete(GamePath + "\\Assets\\swtor_ru-wm_global_1.tor");
                if (File.Exists(GamePath + "\\Assets\\swtor_ru-ww_global_1.tor"))
                    File.Delete(GamePath + "\\Assets\\swtor_ru-ww_global_1.tor");
                if (File.Exists(GamePath + "\\Assets\\swtor_maln_gfx_assets_1.tor"))
                    File.Delete(GamePath + "\\Assets\\swtor_maln_gfx_assets_1.tor");
                if (File.Exists(GamePath + "\\Assets\\swtor_maln_global_1.tor"))
                    File.Delete(GamePath + "\\Assets\\swtor_maln_global_1.tor");
            }
            catch (Exception)
            {
            }
            LogBox.AppendText(Properties.Resources.Done + "\n");
            LogBox.AppendText(Properties.Resources.deletefromhosts);
            string path = "C:\\Windows\\System32\\drivers\\etc\\hosts";
            if (File.Exists(path))
            {
                try
                {
                    File.WriteAllLines(path, (from s in File.ReadLines(path)
                                              where s != "# SWTOR crash send"
                                              select s).ToList());
                    File.WriteAllLines(path, (from s in File.ReadLines(path)
                                              where s != "0.0.0.0 bugcatcher.swtor.com"
                                              select s).ToList());
                    File.WriteAllLines(path, (from s in File.ReadLines(path)
                                              where s != "0.0.0.0 crash.swtor.com"
                                              select s).ToList());
                    File.WriteAllLines(path, (from s in File.ReadLines(path)
                                              where s != "0.0.0.0 patcher-crash.swtor.com"
                                              select s).ToList());
                }
                catch (Exception)
                {
                }                
            }
            LogBox.AppendText(Properties.Resources.Done + "\n");
            LogBox.AppendText("Удаление закончено.\n");
            Install_btn.Text = "Установить";
            RusInstalled = 0;
            Buttons_activity(1);
        }
        private void Db_convertor_Click(object sender, EventArgs e)
        {
            int Deepl_First_time = 1;
            var outputElements = "";
            if (Deepl_First_time == 1)
            {
                Deepl_First_time = 0;
                FirefoxOptions options = new FirefoxOptions();
                options.AddArguments("--headless");
                driver = new FirefoxDriver(options)
                {
                    Url = "https://deepl.com/translator"
                };

                driver.FindElement(By.XPath("//*[@dl-test=\"translator-source-lang-btn\"]")).Click();
                Thread.Sleep(1000);
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                wait.Until(d => d.FindElements(By.XPath("//*[@dl-test=\"translator-source-lang-list\"]/div/button/div[@dl-test=\"translator-lang-option-en\"]")).Count > 0);
                Actions actions = new Actions(driver);
                actions.MoveToElement(driver.FindElement(By.XPath("//*[@dl-test=\"translator-source-lang-list\"]/div/button/div[@dl-test=\"translator-lang-option-en\"]")));
                actions.Perform();
                driver.FindElement(By.XPath("//*[@dl-test=\"translator-source-lang-list\"]/div/button/div[@dl-test=\"translator-lang-option-en\"]")).Click();

                driver.FindElement(By.XPath("//*[@dl-test=\"translator-target-lang-btn\"]")).Click();
                Thread.Sleep(1000);
                wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                wait.Until(d => d.FindElements(By.XPath("//*[@dl-test=\"translator-target-lang-list\"]/div/button/div[@dl-test=\"translator-lang-option-ru-RU\"]")).Count > 0);
                Actions actionss = new Actions(driver);
                actionss.MoveToElement(driver.FindElement(By.XPath("//*[@dl-test=\"translator-target-lang-list\"]/div/button/div[@dl-test=\"translator-lang-option-ru-RU\"]")));
                actionss.Perform();
                driver.FindElement(By.XPath("//*[@dl-test=\"translator-target-lang-list\"]/div/button/div[@dl-test=\"translator-lang-option-ru-RU\"]")).Click();
            }
            if (driver.FindElement(By.XPath("//*[@dl-test=\"translator-source-lang\"]/button/span/strong")).Text != "английского")
            {
                driver.FindElement(By.XPath("//*[@dl-test=\"translator-source-lang-btn\"]")).Click();
                Thread.Sleep(1000);
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                wait.Until(d => d.FindElements(By.XPath("//*[@dl-test=\"translator-source-lang-list\"]/div/button/div[@dl-test=\"translator-lang-option-en\"]")).Count > 0);
                Actions actions = new Actions(driver);
                actions.MoveToElement(driver.FindElement(By.XPath("//*[@dl-test=\"translator-source-lang-list\"]/div/button/div[@dl-test=\"translator-lang-option-en\"]")));
                actions.Perform();
                driver.FindElement(By.XPath("//*[@dl-test=\"translator-source-lang-list\"]/div/button/div[@dl-test=\"translator-lang-option-en\"]")).Click();

                driver.FindElement(By.XPath("//*[@dl-test=\"translator-target-lang-btn\"]")).Click();
                Thread.Sleep(1000);
                wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                wait.Until(d => d.FindElements(By.XPath("//*[@dl-test=\"translator-target-lang-list\"]/div/button/div[@dl-test=\"translator-lang-option-ru-RU\"]")).Count > 0);
                Actions actionss = new Actions(driver);
                actionss.MoveToElement(driver.FindElement(By.XPath("//*[@dl-test=\"translator-target-lang-list\"]/div/button/div[@dl-test=\"translator-lang-option-ru-RU\"]")));
                actionss.Perform();
                driver.FindElement(By.XPath("//*[@dl-test=\"translator-target-lang-list\"]/div/button/div[@dl-test=\"translator-lang-option-ru-RU\"]")).Click();
            }
            Thread.Sleep(500);

                Deepl_First_time = 1;
                Thread.Sleep(10000);
                driver.Close();
                driver.Quit();
            }
            Console.WriteLine(outputElements);
            driver.Close();
            driver.Quit();
        }

       
        private void Btn_info_Click(object sender, EventArgs e)//Окно Информация
        {
            if (ActiveForm.Height == 400)
            {
                Enabled = false;
                List<string> list_translators = new List<string>();
                using (SQLiteConnection sqlite_conn = new SQLiteConnection("Data Source=db\\translate.db3; Version = 3; New = True; Compress = True; "))
                {
                    using (SQLiteCommand sqlite_cmd = new SQLiteCommand(sqlite_conn))
                    {
                        sqlite_conn.Open();
                        string sqllite_select = "SELECT COUNT(DISTINCT text_en) FROM Translated";
                        sqlite_cmd.CommandText = sqllite_select;
                        float count_all_rows = Convert.ToInt32(sqlite_cmd.ExecuteScalar());
                        sqllite_select = "SELECT COUNT(DISTINCT text_en) from Translated WHERE translator_m!='Deepl'";
                        sqlite_cmd.CommandText = sqllite_select;
                        float count_trans_rows = Convert.ToInt32(sqlite_cmd.ExecuteScalar());
                        float percentag = (count_trans_rows / count_all_rows) * 100;
                        row_translated.Invoke((MethodInvoker)(() => row_translated.Text = Math.Round(percentag, 2) + "% (" + count_trans_rows + "/" + count_all_rows + ")"));//процентр перевода показываем
                        sqllite_select = "SELECT translator_m,translator_w FROM Translated GROUP by translator_m";
                        sqlite_cmd.CommandText = sqllite_select;
                        SQLiteDataReader r = sqlite_cmd.ExecuteReader();
                        while (r.Read())
                        {
                            if (!list_translators.Contains(WebUtility.HtmlDecode(r["translator_m"].ToString()), StringComparer.OrdinalIgnoreCase))
                                list_translators.Add(WebUtility.HtmlDecode(r["translator_m"].ToString()));
                            if (!list_translators.Contains(WebUtility.HtmlDecode(r["translator_w"].ToString()), StringComparer.OrdinalIgnoreCase))
                                list_translators.Add(WebUtility.HtmlDecode(r["translator_w"].ToString()));
                        }
                        r.Close();
                        sqlite_cmd.Dispose();
                        sqlite_conn.Close();
                    }
                }
               
        }

       
        private void Editor_btn_Click_1(object sender, EventArgs e)//Открываем окно редакора
        {
            this.Hide();
            Form2 form2 = new Form2();
            form2.Show();            
        }

        private void Dis_skills_CheckedChanged(object sender, EventArgs e)//Переключатель отключения перевода скилов
        {
            string js;
            if (Dis_skills.Checked)
            {
                js = "2";
                auto_translate.Checked = false;
                auto_translate.Enabled = false;
                changes.Checked = false;
                changes.Enabled = false;
            }
            else
            {
                js = "0";
                if (google_opt.Checked == true)
                    auto_translate.Enabled = true;
            }
            Config.AppSettings.Settings["skill"].Value = js;
            Config.Save(ConfigurationSaveMode.Modified);
        }

        private async void Upload_to_server_Click(object sender, EventArgs e)//Выгрузка переводов на сервер
        {
            Buttons_activity(0);
            LogBox.Invoke((MethodInvoker)(() => LogBox.AppendText("Начинаем выгрузку переводов на сервер...\n")));            
            ProgressBar1.Value = 0;            
            await Task.Run(() => Upload_to_server_method());
            Buttons_activity(1);
            LogBox.AppendText("Выгрузка закончена!\n");
        }

        public void Upload_to_server_method()//Выгрузка переводов на сервер
        {
            string key_import = "";
            string text_ru_m_import = "";
            string translator_m_import = "";
            string text_ru_w_import = "";
            string translator_w_import = "";
            string text_en_import = "";
            string sql_update = "";
            string sql_insert = "";
            int num_edited_rows = 0;
            DateTime time = DateTime.UtcNow;
            string format = "dd.MM.yyyy HH:mm:ss";
            string mysql_time_export = time.ToString(format);
            string[] allfiles = Directory.GetFiles("user_translation\\", "*.xml", SearchOption.TopDirectoryOnly);
            using (MySqlConnection conn = new MySqlConnection(connStr_mysql))
            {
                conn.Open();
                foreach (string filename in allfiles)
                {
                    LogBox.Invoke((MethodInvoker)(() => LogBox.AppendText("Работаем с файлом " + filename + "\n")));
                    ProgressBar1.Invoke((MethodInvoker)(() => ProgressBar1.Value = 0));
                    int lineCount = File.ReadLines(filename).Count();
                    ProgressBar1.Invoke((MethodInvoker)(() => ProgressBar1.Maximum = lineCount - 2));
                    XmlDocument xDoc1 = new XmlDocument();
                    xDoc1.Load(filename);
                    XmlElement xRoot1 = xDoc1.DocumentElement;
                    int jks = 1;
                    foreach (XmlNode childnode in xRoot1)
                    {
                        if (childnode.Name == "key")
                            key_import = childnode.InnerText;
                        if (childnode.Name == "text_en")
                            text_en_import = WebUtility.HtmlDecode(childnode.InnerText);
                        if (childnode.Name == "text_ru_m")
                        {
                            text_ru_m_import = WebUtility.HtmlDecode(childnode.InnerText);
                            translator_m_import = WebUtility.HtmlDecode(childnode.Attributes.GetNamedItem("transl").Value);
                        }
                        if (childnode.Name == "text_ru_w")
                        {
                            text_ru_w_import = WebUtility.HtmlDecode(childnode.InnerText);
                            translator_w_import = WebUtility.HtmlDecode(childnode.Attributes.GetNamedItem("transl").Value);
                        }
                        if (jks % 4 == 0)
                        {
                            if (text_ru_w_import != "")
                            {
                                if (translator_m_import != "Deepl" && translator_w_import != "Deepl")
                                {
                                    sql_update = "UPDATE Translated SET text_ru_m='" + WebUtility.HtmlEncode(text_ru_m_import) + "',translator_m='" + WebUtility.HtmlEncode(translator_m_import) + "',text_ru_w='" + WebUtility.HtmlEncode(text_ru_w_import) + "',translator_w='" + WebUtility.HtmlEncode(translator_w_import) + "',tr_datetime=STR_TO_DATE('" + mysql_time_export + "', '%d.%m.%Y %H:%i:%s') WHERE key_unic ='" + key_import + "' AND (translator_m='" + WebUtility.HtmlEncode(translator_m_import) + "' OR translator_w='" + WebUtility.HtmlEncode(translator_w_import) + "')";
                                    sql_insert = "INSERT INTO Translated(key_unic,text_en,text_ru_m,text_ru_w,translator_m,translator_w) VALUES ('" + key_import + "','" + WebUtility.HtmlEncode(text_en_import) + "','" + WebUtility.HtmlEncode(text_ru_m_import) + "','" + WebUtility.HtmlEncode(text_ru_w_import) + "','" + WebUtility.HtmlEncode(translator_m_import) + "','" + WebUtility.HtmlEncode(translator_w_import) + "')";
                                }
                                else if (translator_m_import != "Deepl")
                                {
                                    sql_update = "UPDATE Translated SET text_ru_m='" + WebUtility.HtmlEncode(text_ru_m_import) + "',translator_m='" + WebUtility.HtmlEncode(translator_m_import) + "',tr_datetime=STR_TO_DATE('" + mysql_time_export + "', '%d.%m.%Y %H:%i:%s') WHERE key_unic ='" + key_import + "' AND translator_m='" + WebUtility.HtmlEncode(translator_m_import) + "'";
                                    sql_insert = "INSERT INTO Translated(key_unic,text_en,text_ru_m,translator_m) VALUES ('" + key_import + "','" + WebUtility.HtmlEncode(text_en_import) + "','" + WebUtility.HtmlEncode(text_ru_m_import) + "','" + WebUtility.HtmlEncode(translator_m_import) + "')";
                                }
                                else if (translator_w_import != "Deepl")
                                {
                                    sql_update = "UPDATE Translated SET text_ru_w='" + WebUtility.HtmlEncode(text_ru_w_import) + "',translator_w='" + WebUtility.HtmlEncode(translator_w_import) + "',tr_datetime=STR_TO_DATE('" + mysql_time_export + "', '%d.%m.%Y %H:%i:%s') WHERE key_unic ='" + key_import + "' AND translator_w='" + WebUtility.HtmlEncode(translator_w_import) + "'";
                                    sql_insert = "INSERT INTO Translated(key_unic,text_en,text_ru_w,translator_w) VALUES ('" + key_import + "','" + WebUtility.HtmlEncode(text_en_import) + "','" + WebUtility.HtmlEncode(text_ru_w_import) + "','" + WebUtility.HtmlEncode(translator_w_import) + "')";
                                }
                            }
                            else if (text_ru_m_import != "")
                            {
                                if (translator_m_import != "Deepl")
                                {
                                    sql_update = "UPDATE Translated SET text_ru_m='" + WebUtility.HtmlEncode(text_ru_m_import) + "',translator_m='" + WebUtility.HtmlEncode(translator_m_import) + "',tr_datetime=STR_TO_DATE('" + mysql_time_export + "', '%d.%m.%Y %H:%i:%s') WHERE key_unic ='" + key_import + "' AND translator_m='" + WebUtility.HtmlEncode(translator_m_import) + "'";
                                    sql_insert = "INSERT INTO Translated(key_unic,text_en,text_ru_m,translator_m) VALUES ('" + key_import + "','" + WebUtility.HtmlEncode(text_en_import) + "','" + WebUtility.HtmlEncode(text_ru_m_import) + "','" + WebUtility.HtmlEncode(translator_m_import) + "')";
                                }
                            }
                            MySqlCommand update = new MySqlCommand(sql_update, conn);
                            int numRowsUpdated = update.ExecuteNonQuery();
                            update.Dispose();
                            if (numRowsUpdated == 0)
                            {
                                MySqlCommand insert = new MySqlCommand(sql_insert, conn);
                                insert.ExecuteNonQuery();
                                insert.Dispose();
                            }
                            num_edited_rows++;
                            ProgressBar1.Invoke((MethodInvoker)(() => ProgressBar1.Value += 1));
                        }
                        jks++;
                    }
                    if (!Directory.Exists("user_translation\\done"))
                        Directory.CreateDirectory("user_translation\\done");
                    string[] tokens0 = filename.Split(new char[] { '\\' });
                    Console.WriteLine("user_translation\\done\\" + tokens0.Last() + ".xml");
                    if (File.Exists("user_translation\\done\\" + tokens0.Last()))
                    {
                        DialogResult dialogResult = MessageBox.Show("Файл с таким именем уже существует. Вы уверены что хотите перенести новые переводы в него?", "Подтверждение", MessageBoxButtons.YesNo);
                        if (dialogResult == DialogResult.Yes)
                        {
                            var lines = File.ReadAllLines("user_translation\\done\\" + tokens0.Last());
                            var lines2 = File.ReadAllLines("user_translation\\" + tokens0.Last());
                            File.WriteAllLines("user_translation\\done\\" + tokens0.Last(), lines.Take(lines.Length - 1).ToArray(), encoding: Encoding.UTF8);
                            using (StreamWriter file_for_exam =
                                                                            new StreamWriter("user_translation\\done\\" + tokens0.Last(), true, encoding: Encoding.UTF8))
                            {
                                for (int jk = 1; jk <= lines2.Length-2; jk++)
                                    file_for_exam.WriteLine(lines2[jk]);
                                file_for_exam.WriteLine("</rezult>");
                            }
                            File.Delete("user_translation\\" + tokens0.Last());
                        }
                        else
                        {
                            string [] tokens1 = tokens0.Last().Split(new string[] { ".xm" }, StringSplitOptions.None);
                            File.Move(filename, "user_translation\\done\\" + tokens1[0] + "1.xml");
                        }
                    }
                    else
                    {
                        File.Move(filename, "user_translation\\done\\" + tokens0.Last());
                    }
                }
                conn.Close();
                ProgressBar1.Invoke((MethodInvoker)(() => ProgressBar1.Value = 0));
                LogBox.Invoke((MethodInvoker)(() => LogBox.AppendText("Выгрузка закончена. Выгружено " + num_edited_rows + " строк.\n")));
            }
        }

        private async void Upload_from_server_Click(object sender, EventArgs e) //Загрузка переводов с сервера
        {
            Buttons_activity(0);
            LogBox.Invoke((MethodInvoker)(() => LogBox.AppendText("Начинаем загрузку переводов с сервера...\n")));
            ProgressBar1.Value = 0;
            await Task.Run(() => Upload_from_server_method());
            Buttons_activity(1);
            LogBox.AppendText("Загрузка переводов закончена!\n");  
        }
        public void Upload_from_server_method()//Загрузка переводов с сервера
        {
            if (!File.Exists("db\\translate_backup.db3"))
                File.Copy("db\\translate.db3", "db\\translate_backup.db3");
            else
            {
                File.Delete("db\\translate_backup.db3");
                File.Copy("db\\translate.db3", "db\\translate_backup.db3");
            }
            if (!Directory.Exists("tmp"))
                Directory.CreateDirectory("tmp");
            if (File.Exists("tmp\\server_update.xml"))
                File.Delete("tmp\\server_update.xml");
            string sqllite_update = "";
            int num_edited_rows = 0;
            string mysql_time_export = "";
            DateTime time = DateTime.UtcNow;
            string format = "dd.MM.yyyy HH:mm:ss";
            mysql_time_export = time.ToString(format);
            string xml_name = mysql_time_export.Replace(":", "");
            int count_for_xml = 0;
            List<string> update_list = new List<string>();
            using (SQLiteConnection sqlite_conn = new SQLiteConnection("Data Source=db\\translate.db3; Version = 3; New = True; Compress = True; "))
            {
                sqlite_conn.Open();
                using (SQLiteCommand sqlite_cmd = new SQLiteCommand(sqlite_conn))
                {
                    List<string> add_list = new List<string>();
                    List<string> blocked_users = new List<string>();
                    ProgressBar1.Invoke((MethodInvoker)(() => ProgressBar1.Value = 0));
                    string sql = "";
                    string xml_text = "";
                    using (MySqlConnection conn = new MySqlConnection(connStr_mysql))
                    {
                        conn.Open();
                        if (Config.AppSettings.Settings["auth_translate"].Value == "1")//Если стоит отметка запрета загрузки заблокированных переводов, получаем список заблокированных авторов
                        {
                            sql = "SELECT name FROM users WHERE status=1";
                            MySqlCommand command = new MySqlCommand(sql, conn);
                            MySqlDataReader reader1 = command.ExecuteReader();
                            while (reader1.Read())
                            {
                                blocked_users.Add(reader1["name"].ToString());
                            }
                            reader1.Close();
                        }
                        sql = "SELECT key_unic,text_en,text_ru_m,text_ru_w,translator_m,translator_w FROM translated WHERE tr_datetime>STR_TO_DATE('" + Config.AppSettings.Settings["row_updated_from_server"].Value + "', '%d.%m.%Y %H:%i:%s')";
                        MySqlCommand command2 = new MySqlCommand(sql, conn)
                        {
                            CommandText = sql
                        };
                        MySqlDataReader reader = command2.ExecuteReader();
                        using (StreamWriter tmp_save = new StreamWriter("tmp\\server_update.xml", true, encoding: Encoding.UTF8))
                        {
                            tmp_save.WriteLine("<rezult>");
                        }
                        while (reader.Read())//перебираем все новые строки
                        {
                            string xml_text1 = "<key>" + WebUtility.HtmlEncode(reader["key_unic"].ToString()) + "</key><text_en>" + WebUtility.HtmlEncode(reader["text_en"].ToString()) + "</text_en><text_ru_m transl=\"" + WebUtility.HtmlEncode(reader["translator_m"].ToString()) + "\">" + WebUtility.HtmlEncode(reader["text_ru_m"].ToString()) + "</text_ru_m><text_ru_w transl=\"" + WebUtility.HtmlEncode(reader["translator_w"].ToString()) + "\">" + WebUtility.HtmlEncode(reader["text_ru_w"].ToString()) + "</text_ru_w>";
                            using (StreamWriter tmp_save = new StreamWriter("tmp\\server_update.xml", true, encoding: Encoding.UTF8))
                            {
                                tmp_save.WriteLine(xml_text1);
                            }
                        }
                        reader.Close();
                        conn.Close();
                    }
                    using (StreamWriter tmp_save = new StreamWriter("tmp\\server_update.xml", true, encoding: Encoding.UTF8))
                    {
                        tmp_save.WriteLine("</rezult>");
                    }
                    LogBox.Invoke((MethodInvoker)(() => LogBox.AppendText("Строки загружены с сервера...\n")));
                    LogBox.Invoke((MethodInvoker)(() => LogBox.AppendText("Подготавливаем строки...\n")));
                    string key_import = "";
                    string text_ru_m_import = "";
                    string translator_m_import = "";
                    string text_ru_w_import = "";
                    string translator_w_import = "";
                    string text_en_import = "";
                    XmlDocument xDoc1 = new XmlDocument();
                    xDoc1.Load("tmp\\server_update.xml");
                    XmlElement xRoot1 = xDoc1.DocumentElement;
                    int jks = 1;
                    int lineCount = File.ReadLines("tmp\\server_update.xml").Count();
                    ProgressBar1.Invoke((MethodInvoker)(() => ProgressBar1.Maximum = lineCount-2));
                    foreach (XmlNode childnode in xRoot1)
                    {
                        if (childnode.Name == "key")
                            key_import = childnode.InnerText;
                        if (childnode.Name == "text_en")
                            text_en_import = WebUtility.HtmlDecode(childnode.InnerText);
                        if (childnode.Name == "text_ru_m")
                        {
                            text_ru_m_import = WebUtility.HtmlDecode(childnode.InnerText);
                            translator_m_import = WebUtility.HtmlDecode(childnode.Attributes.GetNamedItem("transl").Value);
                        }
                        if (childnode.Name == "text_ru_w")
                        {
                            text_ru_w_import = WebUtility.HtmlDecode(childnode.InnerText);
                            translator_w_import = WebUtility.HtmlDecode(childnode.Attributes.GetNamedItem("transl").Value);
                        }
                        if (jks % 4 == 0)
                        {
                            sqllite_update = "";
                            if (text_ru_m_import != "" && text_ru_w_import != "")//Если в строке и М и Ж варианты перевода
                            {
                                if (Config.AppSettings.Settings["auth_translate"].Value == "1" || Config.AppSettings.Settings["translate_restrict"].Value == "1")
                                {
                                    string sql_select = "SELECT text_en, text_ru_m, text_ru_w, translator_m, translator_w FROM Translated WHERE key_unic='" + key_import + "'";
                                    sqlite_cmd.CommandText = sql_select;
                                    SQLiteDataReader reader1 = sqlite_cmd.ExecuteReader();
                                    while (reader1.Read()) //получили старых авторов этой строки перевода
                                    {
                                        xml_text = "";
                                        sqllite_update = "";
                                        if (Config.AppSettings.Settings["auth_translate"].Value == "1") //Если стоит отметка запрета загрузки заблокированных переводов,
                                        {
                                            if (blocked_users.Contains(reader1["translator_m"].ToString()) && reader1["translator_m"].ToString() != translator_m_import && blocked_users.Contains(reader1["translator_w"].ToString()) && reader1["translator_w"].ToString() != translator_w_import)
                                                xml_text = "<key>" + WebUtility.HtmlEncode(key_import) + "</key><text_en>" + WebUtility.HtmlEncode(reader1["text_en"].ToString()) + "</text_en><text_ru_m transl=\"" + WebUtility.HtmlEncode(translator_m_import) + "\">" + WebUtility.HtmlEncode(text_ru_m_import) + "</text_ru_m><text_ru_w  transl=\"" + WebUtility.HtmlEncode(translator_w_import) + "\">" + WebUtility.HtmlEncode(text_ru_w_import) + "</text_ru_w>";
                                            else if (blocked_users.Contains(reader1["translator_m"].ToString()) && reader1["translator_m"].ToString() != translator_m_import) //Если пользователь переводчик старого варианта строки М и есть новый переводчик
                                            {
                                                xml_text = "<key>" + WebUtility.HtmlEncode(key_import) + "</key><text_en>" + WebUtility.HtmlEncode(reader1["text_en"].ToString()) + "</text_en><text_ru_m transl=\"" + WebUtility.HtmlEncode(translator_m_import) + "\">" + WebUtility.HtmlEncode(text_ru_m_import) + "</text_ru_m><text_ru_w  transl=\"\"></text_ru_w>";
                                                sqllite_update = "UPDATE Translated SET text_ru_w='" + WebUtility.HtmlEncode(text_ru_w_import) + "',translator_w='" + WebUtility.HtmlEncode(translator_w_import) + "' WHERE key_unic ='" + WebUtility.HtmlEncode(key_import) + "';";
                                                    
                                            }
                                            else if (blocked_users.Contains(reader1["translator_w"].ToString()) && reader1["translator_w"].ToString() != translator_w_import) //Если пользователь переводчик старого варианта строки Ж и есть новый переводчик
                                            {
                                                xml_text = "<key>" + WebUtility.HtmlEncode(key_import) + "</key><text_en>" + WebUtility.HtmlEncode(reader1["text_en"].ToString()) + "</text_en><text_ru_m transl=\"\"></text_ru_m><text_ru_w  transl=\"" + WebUtility.HtmlEncode(translator_w_import) + "\">" + WebUtility.HtmlEncode(text_ru_w_import) + "</text_ru_w>";
                                                sqllite_update = "UPDATE Translated SET text_ru_m='" + WebUtility.HtmlEncode(text_ru_m_import) + "',translator_m='" + WebUtility.HtmlEncode(translator_m_import) + "' WHERE key_unic ='" + WebUtility.HtmlEncode(key_import) + "';";  
                                            }
                                            else if (!blocked_users.Contains(translator_m_import) && !blocked_users.Contains(translator_w_import))
                                                sqllite_update = "UPDATE Translated SET text_ru_m='" + WebUtility.HtmlEncode(text_ru_m_import) + "',translator_m='" + WebUtility.HtmlEncode(translator_m_import) + "',text_ru_w='" + WebUtility.HtmlEncode(text_ru_w_import) + "',translator_w='" + WebUtility.HtmlEncode(translator_w_import) + "' WHERE key_unic ='" + WebUtility.HtmlEncode(key_import) + "';";
                                        }
                                        else if (Config.AppSettings.Settings["translate_restrict"].Value == "1")//Если запрещёно редактирование перевода пользователя
                                        {
                                            if (reader1["translator_m"].ToString() == Config.AppSettings.Settings["author"].Value.ToString() && reader1["translator_w"].ToString() == Config.AppSettings.Settings["author"].Value.ToString() && translator_m_import != Config.AppSettings.Settings["author"].Value.ToString() && translator_w_import != Config.AppSettings.Settings["author"].Value.ToString())
                                                xml_text = "<key>" + WebUtility.HtmlEncode(key_import) + "</key><text_en>" + WebUtility.HtmlEncode(reader1["text_en"].ToString()) + "</text_en><text_ru_m transl=\"" + WebUtility.HtmlEncode(translator_m_import) + "\">" + WebUtility.HtmlEncode(text_ru_m_import) + "</text_ru_m><text_ru_w  transl=\"" + WebUtility.HtmlEncode(translator_w_import) + "\">" + WebUtility.HtmlEncode(text_ru_w_import) + "</text_ru_w>";
                                            else if (reader1["translator_m"].ToString() == Config.AppSettings.Settings["author"].Value.ToString() && translator_m_import != Config.AppSettings.Settings["author"].Value.ToString()) //Если пользователь переводчик старого варианта строки М и есть новый переводчик
                                            {
                                                xml_text = "<key>" + WebUtility.HtmlEncode(key_import) + "</key><text_en>" + WebUtility.HtmlEncode(reader1["text_en"].ToString()) + "</text_en><text_ru_m transl=\"" + WebUtility.HtmlEncode(translator_m_import) + "\">" + WebUtility.HtmlEncode(text_ru_m_import) + "</text_ru_m><text_ru_w  transl=\"\"></text_ru_w>";
                                                sqllite_update = "UPDATE Translated SET text_ru_w='" + WebUtility.HtmlEncode(text_ru_w_import) + "',translator_w='" + WebUtility.HtmlEncode(translator_w_import) + "' WHERE key_unic ='" + WebUtility.HtmlEncode(key_import) + "';";
                                            }
                                            else if (reader1["translator_w"].ToString() == Config.AppSettings.Settings["author"].Value.ToString() && translator_w_import != Config.AppSettings.Settings["author"].Value.ToString())//Если пользователь переводчик старого варианта строки Ж и есть новый переводчик
                                            {
                                                xml_text = "<key>" + WebUtility.HtmlEncode(key_import) + "</key><text_en>" + WebUtility.HtmlEncode(reader1["text_en"].ToString()) + "</text_en><text_ru_m transl=\"\"></text_ru_m><text_ru_w  transl=\"" + WebUtility.HtmlEncode(translator_w_import) + "\">" + WebUtility.HtmlEncode(text_ru_w_import) + "</text_ru_w>";
                                                sqllite_update = "UPDATE Translated SET text_ru_m='" + WebUtility.HtmlEncode(text_ru_m_import) + "',translator_m='" + WebUtility.HtmlEncode(translator_m_import) + "' WHERE key_unic ='" + WebUtility.HtmlEncode(key_import) + "';";
                                            }
                                            else //Если переводчик строки М и Ж тот же самый
                                                sqllite_update = "UPDATE Translated SET text_ru_m='" + WebUtility.HtmlEncode(text_ru_m_import) + "',translator_m='" + WebUtility.HtmlEncode(translator_m_import) + "',text_ru_w='" + WebUtility.HtmlEncode(text_ru_w_import) + "',translator_w='" + WebUtility.HtmlEncode(translator_w_import) + "' WHERE key_unic ='" + WebUtility.HtmlEncode(key_import) + "';";
                                        }
                                        else
                                            sqllite_update = "UPDATE Translated SET text_ru_m='" + WebUtility.HtmlEncode(text_ru_m_import) + "',translator_m='" + WebUtility.HtmlEncode(translator_m_import) + "',text_ru_w='" + WebUtility.HtmlEncode(text_ru_w_import) + "',translator_w='" + WebUtility.HtmlEncode(translator_w_import) + "' WHERE key_unic ='" + WebUtility.HtmlEncode(key_import) + "';";
                                    }
                                    reader1.Close();
                                }
                                else
                                    sqllite_update = "UPDATE Translated SET text_ru_m='" + WebUtility.HtmlEncode(text_ru_m_import) + "',translator_m='" + WebUtility.HtmlEncode(translator_m_import) + "',text_ru_w='" + WebUtility.HtmlEncode(text_ru_w_import) + "',translator_w='" + WebUtility.HtmlEncode(translator_w_import) + "' WHERE key_unic ='" + WebUtility.HtmlEncode(key_import) + "';";
                            }
                            else if (text_ru_m_import != "")//Если в строке только М вариант перевода
                            {
                                if (Config.AppSettings.Settings["auth_translate"].Value == "1" || Config.AppSettings.Settings["translate_restrict"].Value == "1")
                                {
                                    string sql_select = "SELECT text_en, text_ru_m, translator_m FROM Translated WHERE key_unic='" + key_import + "'";
                                    sqlite_cmd.CommandText = sql_select;
                                    SQLiteDataReader reader1 = sqlite_cmd.ExecuteReader();
                                    while (reader1.Read())//получили старого автора этой строки перевода
                                    {
                                        if (Config.AppSettings.Settings["auth_translate"].Value == "1")//Если стоит отметка запрета загрузки заблокированных переводов
                                        {
                                            if (reader1["translator_m"].ToString() != translator_m_import && blocked_users.Contains(reader1["translator_m"].ToString()))
                                                xml_text = "<key>" + WebUtility.HtmlEncode(key_import) + "</key><text_en>" + WebUtility.HtmlEncode(reader1["text_en"].ToString()) + "</text_en><text_ru_m transl=\"" + WebUtility.HtmlEncode(translator_m_import) + "\">" + WebUtility.HtmlEncode(text_ru_m_import) + "</text_ru_m><text_ru_w transl=\"" + WebUtility.HtmlEncode(translator_w_import) + "\">" + WebUtility.HtmlEncode(text_ru_w_import) + "</text_ru_w>";
                                            else if (!blocked_users.Contains(reader1["translator_m"].ToString()) || reader1["translator_m"].ToString()== translator_m_import)
                                                sqllite_update = "UPDATE Translated SET text_ru_m='" + WebUtility.HtmlEncode(text_ru_m_import) + "',translator_m='" + WebUtility.HtmlEncode(translator_m_import) + "' WHERE key_unic ='" + WebUtility.HtmlEncode(key_import) + "';";
                                        }
                                        else if (Config.AppSettings.Settings["translate_restrict"].Value == "1")// Если запрещёно редактирование перевода пользователя
                                        {
                                            if (reader1["translator_m"].ToString() == Config.AppSettings.Settings["author"].Value.ToString() && translator_m_import != Config.AppSettings.Settings["author"].Value.ToString())
                                                xml_text = "<key>" + WebUtility.HtmlEncode(key_import) + "</key><text_en>" + WebUtility.HtmlEncode(reader1["text_en"].ToString()) + "</text_en><text_ru_m transl=\"" + WebUtility.HtmlEncode(translator_m_import) + "\">" + WebUtility.HtmlEncode(text_ru_m_import) + "</text_ru_m><text_ru_w transl=\"" + WebUtility.HtmlEncode(translator_w_import) + "\">" + WebUtility.HtmlEncode(text_ru_w_import) + "</text_ru_w>";
                                            else
                                                sqllite_update = "UPDATE Translated SET text_ru_m='" + WebUtility.HtmlEncode(text_ru_m_import) + "',translator_m='" + WebUtility.HtmlEncode(translator_m_import) + "' WHERE key_unic ='" + WebUtility.HtmlEncode(key_import) + "';";
                                        }
                                        else
                                            sqllite_update = "UPDATE Translated SET text_ru_m='" + WebUtility.HtmlEncode(text_ru_m_import) + "',translator_m='" + WebUtility.HtmlEncode(translator_m_import) + "' WHERE key_unic ='" + WebUtility.HtmlEncode(key_import) + "';";
                                    }
                                    reader1.Close();
                                }
                                else
                                    sqllite_update = "UPDATE Translated SET text_ru_m='" + WebUtility.HtmlEncode(text_ru_m_import) + "',translator_m='" + WebUtility.HtmlEncode(translator_m_import) + "' WHERE key_unic ='" + WebUtility.HtmlEncode(key_import) + "';";
                            }
                                if (Config.AppSettings.Settings["auth_translate"].Value == "1" || Config.AppSettings.Settings["translate_restrict"].Value == "1")
                                {
                                    string sql_select = "SELECT text_en, text_ru_m, translator_m FROM Translated WHERE key_unic='" + key_import + "'";
                                    sqlite_cmd.CommandText = sql_select;
                                    SQLiteDataReader reader1 = sqlite_cmd.ExecuteReader();
                                    while (reader1.Read())//получили старого автора этой строки перевода
                                    {
                                        if (Config.AppSettings.Settings["auth_translate"].Value == "1") //Если стоит отметка запрета загрузки заблокированных переводов
                                        {
                                            if (reader1["translator_w"].ToString() != translator_w_import && blocked_users.Contains(translator_w_import))
                                                xml_text = "<key>" + WebUtility.HtmlEncode(key_import) + "</key><text_en>" + WebUtility.HtmlEncode(reader1["text_en"].ToString()) + "</text_en><text_ru_m transl=\"" + WebUtility.HtmlEncode(translator_m_import) + "\">" + WebUtility.HtmlEncode(text_ru_m_import) + "</text_ru_m><text_ru_w transl=\"" + WebUtility.HtmlEncode(translator_w_import) + "\">" + WebUtility.HtmlEncode(text_ru_w_import) + "</text_ru_w>";
                                            else if (!blocked_users.Contains(reader1["translator_w"].ToString()) || reader1["translator_w"].ToString() == translator_w_import)
                                                sqllite_update = "UPDATE Translated SET text_ru_w='" + WebUtility.HtmlEncode(text_ru_w_import) + "',translator_w='" + WebUtility.HtmlEncode(translator_w_import) + "' WHERE key_unic ='" + WebUtility.HtmlEncode(key_import) + "';";
                                        }
                                        else if (Config.AppSettings.Settings["translate_restrict"].Value == "1")// Если запрещёно редактирование перевода пользователя
                                        {
                                            if (reader1["translator_w"].ToString() == Config.AppSettings.Settings["author"].Value.ToString() && translator_w_import != Config.AppSettings.Settings["author"].Value.ToString())
                                                xml_text = "<key>" + WebUtility.HtmlEncode(key_import) + "</key><text_en>" + WebUtility.HtmlEncode(reader1["text_en"].ToString()) + "</text_en><text_ru_m transl=\"" + WebUtility.HtmlEncode(translator_m_import) + "\">" + WebUtility.HtmlEncode(text_ru_m_import) + "</text_ru_m><text_ru_w transl=\"" + WebUtility.HtmlEncode(translator_w_import) + "\">" + WebUtility.HtmlEncode(text_ru_w_import) + "</text_ru_w>";
                                            else
                                                sqllite_update = "UPDATE Translated SET text_ru_w='" + WebUtility.HtmlEncode(text_ru_w_import) + "',translator_w='" + WebUtility.HtmlEncode(translator_w_import) + "' WHERE key_unic ='" + WebUtility.HtmlEncode(key_import) + "';";
                                        }
                                        else
                                            sqllite_update = "UPDATE Translated SET text_ru_w='" + WebUtility.HtmlEncode(text_ru_w_import) + "',translator_w='" + WebUtility.HtmlEncode(translator_w_import) + "' WHERE key_unic ='" + WebUtility.HtmlEncode(key_import) + "';";
                                    }
                                    reader1.Close();
                                }
                                else
                                    sqllite_update = "UPDATE Translated SET text_ru_w='" + WebUtility.HtmlEncode(text_ru_w_import) + "',translator_w='" + WebUtility.HtmlEncode(translator_w_import) + "' WHERE key_unic ='" + WebUtility.HtmlEncode(key_import) + "';";
                            }
                            if (sqllite_update != "")
                            {
                                update_list.Add(sqllite_update);
                                add_list.Add(key_import);
                                num_edited_rows++;
                            }
                            if (xml_text != "")
                            {
                                if (!Directory.Exists("blocked_translations"))//Создаём папку для блокированных переводов
                                    Directory.CreateDirectory("blocked_translations");
                                count_for_xml++;
                                if (count_for_xml == 1)
                                {
                                    using (StreamWriter tmp_save = new StreamWriter("blocked_translations\\" + xml_name + ".xml", true, encoding: Encoding.UTF8))
                                    {
                                        tmp_save.WriteLine("<rezult>");
                                    }
                                }
                                using (StreamWriter tmp_save = new StreamWriter("blocked_translations\\" + xml_name + ".xml", true, encoding: Encoding.UTF8))
                                {
                                    tmp_save.WriteLine(xml_text);
                                }
                            }
                            ProgressBar1.Invoke((MethodInvoker)(() => ProgressBar1.Value += 1));
                        }
                        jks++;
                    }
                    ProgressBar1.Invoke((MethodInvoker)(() => ProgressBar1.Maximum = num_edited_rows));
                    ProgressBar1.Invoke((MethodInvoker)(() => ProgressBar1.Value = 0));
                    LogBox.Invoke((MethodInvoker)(() => LogBox.AppendText("Подготовка строк завершена...\n")));
                    LogBox.Invoke((MethodInvoker)(() => LogBox.AppendText("Обновляем локальную базу...\n")));
                    using (SQLiteTransaction transaction = sqlite_conn.BeginTransaction())
                    {
                        update_list.ForEach(delegate (String name)
                        {
                            sqlite_cmd.CommandText = name;
                            sqlite_cmd.ExecuteNonQuery();
                            ProgressBar1.Invoke((MethodInvoker)(() => ProgressBar1.Value += 1));
                        });
                        transaction.Commit();
                    }
                    File.Delete("tmp\\server_update.xml");
                    if (count_for_xml != 0)
                    {
                        using (StreamWriter tmp_save = new StreamWriter("blocked_translations\\" + xml_name + ".xml", true, encoding: Encoding.UTF8))
                        {
                            tmp_save.WriteLine("</rezult>");
                        }
                    }
                    string[] allrows = add_list.ToArray();
                    sqllite_update = string.Format("SELECT fileinfo FROM Translated WHERE key_unic in ({0}) GROUP BY fileinfo", string.Join(", ", allrows));
                    sqlite_cmd.CommandText = sqllite_update;
                    SQLiteDataReader r = sqlite_cmd.ExecuteReader();
                    LogBox.Invoke((MethodInvoker)(() => LogBox.AppendText("Внесены изменения в следующие файлы:\n")));
                    while (r.Read())
                        LogBox.Invoke((MethodInvoker)(() => LogBox.AppendText(r["fileinfo"].ToString() + "\n")));
                    r.Close();
                }
                sqlite_conn.Close();
            }
        }


        private static string GetError(int errNum)
        {
            foreach (ErrorClass er in ERROR_LIST)
            {
                if (er.num == errNum) return er.message;
            }
            return "Error: Unknown, " + errNum;
        }
        #endregion

        #endregion
    }
