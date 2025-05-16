using System;
using System.Drawing;
using System.Windows.Forms;

namespace Crystal_Caverns.View
{
    public partial class MainMenuForm : Form
    {
        public MainMenuForm()
        {
            InitializeComponent();
            SetupUI();
        }

        private void SetupUI()
        {
            // Настройка формы
            Text = "Crystal Caverns";
            Size = new Size(800, 600);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.DarkBlue;

            // Добавление заголовка
            Label titleLabel = new Label
            {
                Text = "Crystal Caverns",
                AutoSize = false,
                Size = new Size(500, 80),
                Location = new Point((ClientSize.Width - 500) / 2, 100),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Arial", 36, FontStyle.Bold)
            };
            Controls.Add(titleLabel);

            // Кнопка Start Game
            Button startButton = new Button
            {
                Text = "Start Game",
                Size = new Size(200, 50),
                Location = new Point((ClientSize.Width - 200) / 2, 250),
                Font = new Font("Arial", 16)
            };
            startButton.Click += StartButton_Click;
            Controls.Add(startButton);

            // Кнопка Help
            Button helpButton = new Button
            {
                Text = "Help",
                Size = new Size(200, 50),
                Location = new Point((ClientSize.Width - 200) / 2, 320),
                Font = new Font("Arial", 16)
            };
            helpButton.Click += HelpButton_Click;
            Controls.Add(helpButton);

            // Кнопка Exit
            Button exitButton = new Button
            {
                Text = "Exit",
                Size = new Size(200, 50),
                Location = new Point((ClientSize.Width - 200) / 2, 390),
                Font = new Font("Arial", 16)
            };
            exitButton.Click += ExitButton_Click;
            Controls.Add(exitButton);
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            // Скрываем главное меню
            Hide();

            // Создаем и показываем игровую форму
            using (var gameForm = new GameForm())
            {
                gameForm.ShowDialog();
            }

            // Показываем меню снова после закрытия игры
            Show();
        }

        private void HelpButton_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "Controls:\n" +
                "- Arrow Keys or WASD: Move left/right and jump\n" +
                "- Space: Jump\n\n" +
                "Objective:\n" +
                "Collect all crystals and reach the exit door.\n" +
                "Avoid enemies and don't fall off platforms!\n\n" +
                "Tips:\n" +
                "- Collecting all crystals will open the exit door.\n" +
                "- Some platforms will move, use them to reach higher areas.\n" +
                "- Blue crystals give you the ability to double jump.",
                "How to Play",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private void ExitButton_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void MainMenuForm_Load(object sender, EventArgs e)
        {

        }
    }
}