using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Windows.Forms;
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace FTP_client_program
{
    public partial class Form1 : Form
    {

        // Создание пунктов меню для listView
        ToolStripMenuItem downloadFileItem = new ToolStripMenuItem("Скачать файл");
        ToolStripMenuItem deleteFileItem = new ToolStripMenuItem("Удалить файл");
        ToolStripMenuItem deleteFolderItem = new ToolStripMenuItem("Удалить папку");
        ToolStripMenuItem createFolderItem = new ToolStripMenuItem("Создать папку");

        /// <summary>
        /// Вход в окно формочки.
        /// </summary>
        public Form1()
        {
            InitializeComponent();
            //Выключаем кнопки, пока не войдём на сервер
            buttonPreviousDirectory.Enabled = false;
            button1.Enabled = false;

            //Обрабатываем события нажатий
            listView1.MouseDoubleClick += listView1_MouseDoubleClick;
            listView1.MouseDown += listView1_MouseDown;
            
            // Привязка обработчиков событий к пунктам меню
            downloadFileItem.Click += DownloadFileToolStripMenuItem_Click;
            deleteFileItem.Click += DeleteFileToolStripMenuItem_Click;
            deleteFolderItem.Click += DeleteDirToolStripMenuItem_Click;
            createFolderItem.Click += CreateFolderToolStripMenuItem_Click;
        }

        /// <summary>
        /// Событие нажатия на кнопку "Создать папку".
        /// </summary>
        private void CreateFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Создание нового диалогового окна
            using (var form = new Form())
            {
                // Настройка параметров окна
                form.Text = "Создать новую папку";
                form.StartPosition = FormStartPosition.CenterParent;

                // Создание текстового поля для ввода названия папки
                var textBox = new TextBox();
                textBox.Dock = DockStyle.Top;
                textBox.Margin = new Padding(10);
                textBox.Text = "New_folder";
                form.Controls.Add(textBox);

                // Создание кнопки "Создать"
                var createButton = new Button();
                createButton.Text = "Создать";
                createButton.Dock = DockStyle.Top;
                createButton.Margin = new Padding(10);
                createButton.Click += (sender2, e2) =>
                {
                    string folderName = textBox.Text.Trim();
                    if (!string.IsNullOrEmpty(folderName))
                    {
                        // Вызов метода для создания новой папки
                        CreateDirectory(currentServerPathLabel.Text + "/" + folderName);
                        // Закрытие диалогового окна
                        form.Close();
                    }
                    else
                    {
                        MessageBox.Show("Пожалуйста, введите название папки.");
                    }
                };
                form.Controls.Add(createButton);

                // Отображение диалогового окна
                form.ShowDialog();
            }
        }

        /// <summary>
        /// Событие нажатия на кнопку "Скачать файл".
        /// </summary>
        private void DownloadFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();
            string folderBrowserlocal = "";
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                folderBrowserlocal = folderBrowserDialog1.SelectedPath;
                string fullFilePath = "";
                ListViewItem selectedItem = listView1.FocusedItem;
                if (selectedItem != null)
                {
                    string itemName = selectedItem.Text;
                    fullFilePath = currentServerPathLabel.Text + "/" + itemName;
                    string remoteFileName = itemName;
                    DownloadFile(remoteFileName, fullFilePath, folderBrowserlocal);
                }
                else MessageBox.Show("Путь к файлу недействителен");
            }
        }

        /// <summary>
        /// Событие нажатия на кнопку "Удалить файл".
        /// </summary>
        private void DeleteFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string fullFilePath = "";
            ListViewItem selectedItem = listView1.FocusedItem;
            if (selectedItem != null)
            {
                string itemName = selectedItem.Text;
                fullFilePath = currentServerPathLabel.Text + "/" + itemName;

                if (!string.IsNullOrEmpty(fullFilePath))
                {
                    // Разделяем путь к файлу на директорию и имя файла
                    string directoryPath = Path.GetDirectoryName(fullFilePath);
                    string fileName = Path.GetFileName(fullFilePath);

                    // Удаляем файл с сервера FTP
                    DeleteFileFromFTP(fileName, directoryPath);
                }
                else
                {
                    MessageBox.Show("Путь к файлу недействителен или не указан.");
                }
            }
        }

        /// <summary>
        /// Событие нажатия на кнопку "Удалить папку".
        /// </summary>
        private void DeleteDirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string fullFolderPath = "";
            ListViewItem selectedItem = listView1.FocusedItem;
            if (selectedItem != null)
            {
                string itemName = selectedItem.Text;
                fullFolderPath = currentServerPathLabel.Text + "/" + itemName;
                DeleteDirectory(fullFolderPath);
            } 
        }

        /// <summary>
        /// Событие нажатия на кнопку "Выбрать и загрузить файл на сервер".
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            string localFilePath;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                localFilePath = openFileDialog1.FileName;
                string fileName = Path.GetFileName(localFilePath);
                UploadFile(fileName, localFilePath, currentServerPathLabel.Text);
            }
            else MessageBox.Show("Выберите файл на локальном устройстве.");
        }

        /// <summary>
        /// Событие нажатия два раза ЛКМ на элемент listView.
        /// </summary>
        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListViewItem selectedItem = listView1.FocusedItem;
            if (selectedItem != null)
            {
                string itemName = selectedItem.Text;
                if (!itemName.Contains("."))
                {
                    currentServerPathLabel.Text += "/" + itemName;
                    // Это папка, поэтому обновляем содержимое listBox1
                    ListDirectoriesAndFiles(currentServerPathLabel.Text);
                }
            }
        }

        /// <summary>
        /// Событие нажатия ПКМ на элемент listView.
        /// </summary>
        private void listView1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // Проверяем, было ли щелчок в пределах элемента ListView
                ListViewItem clickedItem = listView1.GetItemAt(e.X, e.Y);
                if (clickedItem != null)
                {
                    // Проверяем, является ли элемент папкой
                    bool isDirectory = !clickedItem.Text.Contains(".");

                    // Создаем контекстное меню для элемента ListView
                    ContextMenuStrip contextMenu = new ContextMenuStrip();
                    if (isDirectory)
                    {              
                        contextMenu.Items.Add(deleteFolderItem);
                    }
                    else
                    {
                        contextMenu.Items.Add(downloadFileItem);
                        contextMenu.Items.Add(deleteFileItem);
                    }

                    // Показываем контекстное меню рядом с курсором мыши
                    contextMenu.Show(Cursor.Position);
                }
                else
                {
                    // Создаем контекстное меню для пустой области ListView
                    ContextMenuStrip contextMenu = new ContextMenuStrip();
                    contextMenu.Items.Add(createFolderItem);

                    // Показываем контекстное меню рядом с курсором мыши
                    contextMenu.Show(Cursor.Position);
                }
            }
        }

        /// <summary>
        /// Событие нажатия на кнопку возврата в предыдую директори (<-).
        /// </summary>
        private void buttonPreviousDirectory_Click(object sender, EventArgs e)
        {
            // Получаем текущий путь из textBox4
            string currentPath = currentServerPathLabel.Text;

            // Проверяем, не является ли текущий путь пустой строкой или символом /
            if (!string.IsNullOrEmpty(currentPath) && currentPath != "/")
            {
                // Находим индекс последнего символа /
                int lastIndex = currentPath.LastIndexOf('/');

                // Если есть символ / в пути, то удаляем его и все, что после него
                if (lastIndex >= 0)
                {
                    currentPath = currentPath.Substring(0, lastIndex);

                    // Обновляем textBox4 с новым путем
                    currentServerPathLabel.Text = currentPath;

                    // Затем вызываем метод ListDirectoriesAndFiles с новым путем
                    ListDirectoriesAndFiles(currentPath);
                }
            }
        }

        /// <summary>
        /// Метод для отображения текущей директории сервера.
        /// </summary>
        private void ListDirectoriesAndFiles(string directoryPath)
        {
            try
            {
                // Создание ImageList и добавление в него изображений
                ImageList imageList = new ImageList();
                imageList.ImageSize = new Size(48, 48); // Устанавливаем размер иконок в пикселях
                imageList.Images.Add(Properties.Resources.Image1); // добавление иконки папки
                imageList.Images.Add(Properties.Resources.Image3); // добавление иконки файла

                // Установка ImageList для ListView
                listView1.LargeImageList = imageList;
                listView1.View = View.LargeIcon;

                // Установка размера шрифта текста в ListView
                listView1.Font = new Font("Arial", 12); // Устанавливаем шрифт Arial размером 12 пунктов

                if (string.IsNullOrEmpty(directoryPath)) directoryPath = "/"; // Root directory
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create($"ftp://{ftpServer_textBox.Text}/{directoryPath}");
                request.Credentials = new NetworkCredential(username_textBox.Text, password_textBox.Text);
                request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;

                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                using (Stream responseStream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(responseStream))
                {
                    listView1.Items.Clear(); // Очищаем предыдущий список

                    List<ListViewItem> directories = new List<ListViewItem>();
                    List<ListViewItem> files = new List<ListViewItem>();

                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        string[] tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        if (tokens.Length == 0)
                            continue; // Пропускаем пустые строки

                        // Получаем имя файла или папки из последнего токена
                        string name = tokens[tokens.Length - 1];

                        // Пропускаем элементы "." и ".."
                        if (name == "." || name == "..")
                            continue;

                        // Проверяем, является ли элемент папкой
                        bool isDirectory = !name.Contains(".");

                        // Создаем новый объект ListViewItem
                        ListViewItem item = new ListViewItem();
                        item.Text = name; // Устанавливаем текст элемента

                        // Устанавливаем индекс изображения для элемента
                        int imageIndex = isDirectory ? 0 : 1; // Выбираем индекс картинки в ImageList
                        item.ImageIndex = imageIndex;
                        if (isDirectory)
                        {
                            directories.Add(item);
                        }
                        else
                        {
                            files.Add(item);
                        }
                    }
                    // Добавляем сначала папки, а затем файлы
                    listView1.Items.AddRange(directories.ToArray());
                    listView1.Items.AddRange(files.ToArray());
                }
            }
            catch (WebException ex)
            {
                // Обработка ошибки с сервера
                MessageBox.Show($"Ошибка при запросе к серверу FTP: {ex.Message}");
            }
        }

        /// <summary>
        /// Метод для создания директории на сервере.
        /// </summary>
        private void CreateDirectory(string directoryPath)
        {
            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create($"ftp://{ftpServer_textBox.Text}/{directoryPath}");
                request.Credentials = new NetworkCredential(username_textBox.Text, password_textBox.Text);
                request.Method = WebRequestMethods.Ftp.MakeDirectory;

                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    ListDirectoriesAndFiles(currentServerPathLabel.Text);
                }
            }
            catch (WebException ex)
            {
                MessageBox.Show($"Ошибка в создании папки: {ex.Message}");
            }
        }

        /// <summary>
        /// Метод для удаления пустой директории на сервере.
        /// </summary>
        private void DeleteDirectory(string directoryPath)
        {
            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create($"ftp://{ftpServer_textBox.Text}/{directoryPath}");
                request.Credentials = new NetworkCredential(username_textBox.Text, password_textBox.Text);
                request.Method = WebRequestMethods.Ftp.RemoveDirectory;

                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    ListDirectoriesAndFiles(currentServerPathLabel.Text);
                    MessageBox.Show($"Папка '{directoryPath}' успешно удалена");
                }
            }
            catch
            {
                MessageBox.Show($"Ошибка с удалением папки, скорее всего она не пустая.");
            }
        }

        /// <summary>
        /// Метод для загрузки файла на сервер.
        /// </summary>
        private void UploadFile(string fileName, string localFilePath, string remoteDirectoryPath)
        {
            // Создание запроса для загрузки файла на FTP-сервер
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create($"ftp://{ftpServer_textBox.Text}/{remoteDirectoryPath}/{fileName}");

            // Установка учетных данных для доступа к FTP-серверу
            request.Credentials = new NetworkCredential(username_textBox.Text, password_textBox.Text);

            // Установка метода запроса как "UploadFile", что означает загрузку файла на сервер
            request.Method = WebRequestMethods.Ftp.UploadFile;

            // Открытие локального файла для чтения
            using (Stream fileStream = File.OpenRead(localFilePath))
            // Получение потока для записи на FTP-сервер
            using (Stream ftpStream = request.GetRequestStream())
            {
                byte[] buffer = new byte[10240]; // Создание буфера для чтения файла кусками
                int read;
                // Чтение файла и запись его содержимого в поток FTP-сервера
                while ((read = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ftpStream.Write(buffer, 0, read);
                }
            }

            // Обновление списка файлов и папок на сервере после загрузки файла
            ListDirectoriesAndFiles(currentServerPathLabel.Text);

            // Вывод сообщения об успешной загрузке файла
            MessageBox.Show($"Файл {fileName} успешно загружен с {localFilePath}.");
        }

        /// <summary>
        /// Метод для скачивания файла с сервера.
        /// </summary>
        private void DownloadFile(string fileName, string remoteFileDirName, string localDirectory)
        {
            // Формирование пути к локальному файлу
            string localFilePath = Path.Combine(localDirectory, fileName);

            // Создание запроса для загрузки файла с FTP-сервера
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create($"ftp://{ftpServer_textBox.Text}/{remoteFileDirName}");

            // Установка учетных данных для доступа к FTP-серверу
            request.Credentials = new NetworkCredential(username_textBox.Text, password_textBox.Text);

            // Установка метода запроса как "DownloadFile", что означает загрузку файла с сервера
            request.Method = WebRequestMethods.Ftp.DownloadFile;

            // Получение потока ответа от FTP-сервера
            using (Stream ftpStream = request.GetResponse().GetResponseStream())
            // Создание локального файла для записи данных
            using (Stream fileStream = File.Create(localFilePath))
            {
                byte[] buffer = new byte[10240]; // Создание буфера для чтения данных
                int read;
                // Чтение данных из потока FTP-сервера и запись их в локальный файл
                while ((read = ftpStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    fileStream.Write(buffer, 0, read);
                }
            }

            // Обновление списка файлов и папок на сервере после загрузки файла
            ListDirectoriesAndFiles(currentServerPathLabel.Text);

            // Вывод сообщения об успешной загрузке файла
            MessageBox.Show($"Файл '{fileName}' успешно загружен в '{remoteFileDirName}'.");
        }

        /// <summary>
        /// Метод для удаления файла с сервера.
        /// </summary>
        private void DeleteFileFromFTP(string fileName, string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath))
                directoryPath = "/"; // Root directory

            // Создаем URI для удаления файла
            Uri uri = new Uri($"ftp://{ftpServer_textBox.Text}/{directoryPath}/{fileName}");

            // Создаем запрос на удаление файла
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(uri);
            request.Credentials = new NetworkCredential(username_textBox.Text, password_textBox.Text);
            request.Method = WebRequestMethods.Ftp.DeleteFile;

            // Отправляем запрос на удаление файла
            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse()) { }
            ListDirectoriesAndFiles(currentServerPathLabel.Text);
            MessageBox.Show($"Файл '{fileName}' успешно удалён с '{directoryPath}'.");
        }

        /// <summary>
        /// Событие нажатия на кнопку "Подключиться".
        /// </summary>
        private void start_button_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(ftpServer_textBox.Text) &&
                !string.IsNullOrEmpty(username_textBox.Text) &&
                !string.IsNullOrEmpty(password_textBox.Text))
            {
                currentServerPathLabel.Text = "";
                ListDirectoriesAndFiles(currentServerPathLabel.Text);
                buttonPreviousDirectory.Enabled = true;
                button1.Enabled = true;
                MessageBox.Show($"Вы успешно подключились к хосту {ftpServer_textBox.Text}");
            }
            else
            {
                MessageBox.Show("Вы не ввели данные во все необходимые поля.");
            }
        }
    }
}