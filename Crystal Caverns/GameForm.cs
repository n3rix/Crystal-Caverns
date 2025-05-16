using System;
using System.Drawing;
using System.Windows.Forms;
using Crystal_Caverns.Controller;
using Crystal_Caverns.Model;

namespace Crystal_Caverns.View
{
    public partial class GameForm : Form
    {
        // Контроллер игры
        private readonly GameController _controller;

        // UI-элементы
        private Label _scoreLabel;
        private Label _livesLabel;
        private Label _crystalsLabel;

        // Конструктор
        public GameForm()
        {
            InitializeComponent();

            // Дополнительная настройка формы
            Size = new Size(800, 600);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;
            BackColor = Color.DarkSlateBlue;
            Text = "Crystal Caverns";

            // Создаем контроллер
            _controller = new GameController();

            // Добавляем обработчики событий для ввода
            KeyDown += GameForm_KeyDown;
            KeyUp += GameForm_KeyUp;
        }

        // Обработчик события загрузки формы
        private void GameForm_Load(object sender, EventArgs e)
        {
            // Создаем UI элементы
            CreateUI();

            // Подписываемся на события GameManager
            SubscribeToEvents();

            // Запускаем игру
            _controller.StartGame(this);
        }

        // Создание UI элементов
        private void CreateUI()
        {
            // Метка для отображения счета
            _scoreLabel = new Label
            {
                Text = "Score: 0",
                Location = new Point(10, 10),
                AutoSize = true,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Arial", 14, FontStyle.Bold)
            };
            Controls.Add(_scoreLabel);

            // Метка для отображения жизней
            _livesLabel = new Label
            {
                Text = "Lives: 3",
                Location = new Point(10, 40),
                AutoSize = true,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Arial", 14, FontStyle.Bold)
            };
            Controls.Add(_livesLabel);

            // Метка для отображения кристаллов
            _crystalsLabel = new Label
            {
                Text = "Crystals: 0/0",
                Location = new Point(10, 70),
                AutoSize = true,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Arial", 14, FontStyle.Bold)
            };
            Controls.Add(_crystalsLabel);
        }

        // Подписка на события GameManager
        private void SubscribeToEvents()
        {
            var gameManager = GameManager.Instance;

            // Событие изменения состояния игры
            gameManager.GameStateChanged += GameManager_GameStateChanged;

            // Событие изменения счета
            gameManager.ScoreChanged += GameManager_ScoreChanged;

            // Событие изменения жизней
            gameManager.LivesChanged += GameManager_LivesChanged;

            // Событие изменения количества кристаллов
            gameManager.CollectiblesChanged += GameManager_CollectiblesChanged;
        }

        // Обработчики событий GameManager

        private void GameManager_GameStateChanged(object sender, GameStateEventArgs e)
        {
            // Если игра завершена, показываем экран окончания
            if (e.IsOver)
            {
                BeginInvoke(new Action(() =>
                {
                    _controller.StopGameLoop();
                    ShowGameOverScreen(e.IsVictory);
                }));
            }
        }

        private void GameManager_ScoreChanged(object sender, ScoreEventArgs e)
        {
            BeginInvoke(new Action(() =>
            {
                _scoreLabel.Text = $"Score: {e.Score}";
            }));
        }

        private void GameManager_LivesChanged(object sender, LivesEventArgs e)
        {
            BeginInvoke(new Action(() =>
            {
                _livesLabel.Text = $"Lives: {e.Lives}";
            }));
        }

        private void GameManager_CollectiblesChanged(object sender, CollectiblesEventArgs e)
        {
            BeginInvoke(new Action(() =>
            {
                _crystalsLabel.Text = $"Crystals: {e.Collected}/{e.Total}";
            }));
        }

        // Обработчики событий ввода

        private void GameForm_KeyDown(object sender, KeyEventArgs e)
        {
            _controller.HandleKeyDown(e.KeyCode);
        }

        private void GameForm_KeyUp(object sender, KeyEventArgs e)
        {
            _controller.HandleKeyUp(e.KeyCode);
        }

        // Показ экрана окончания игры
        private void ShowGameOverScreen(bool isVictory)
        {
            // Создаем панель-оверлей
            Panel overlay = new Panel
            {
                Size = ClientSize,
                Location = new Point(0, 0),
                BackColor = Color.FromArgb(150, 0, 0, 0)
            };
            Controls.Add(overlay);
            overlay.BringToFront();

            // Сообщение о результате
            Label messageLabel = new Label
            {
                Text = isVictory ? "Level Complete!" : "Game Over",
                AutoSize = false,
                Size = new Size(400, 50),
                Location = new Point((ClientSize.Width - 400) / 2, 200),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Arial", 24, FontStyle.Bold)
            };
            overlay.Controls.Add(messageLabel);

            // Кнопка перезапуска
            Button restartButton = new Button
            {
                Text = "Restart",
                Size = new Size(120, 40),
                Location = new Point((ClientSize.Width - 250) / 2, 320)
            };
            restartButton.Click += (s, e) =>
            {
                // Удаляем оверлей
                Controls.Remove(overlay);

                // Дополнительная пауза для гарантии, что все ресурсы освобождены
                Application.DoEvents();
                System.Threading.Thread.Sleep(50);

                // Производим полный рестарт
                _controller.RestartGame(this);

                // Дополнительно устанавливаем фокус
                Focus();

                // Принудительное обновление состояния окна
                Invalidate();
                Update();

                // Принудительное обновление фокуса с задержкой
                BeginInvoke(new Action(() =>
                {
                    System.Threading.Thread.Sleep(100);
                    Focus();
                }));
            };
            overlay.Controls.Add(restartButton);

            // Кнопка выхода в меню
            Button menuButton = new Button
            {
                Text = "Main Menu",
                Size = new Size(120, 40),
                Location = new Point((ClientSize.Width + 10) / 2, 320)
            };
            menuButton.Click += (s, e) =>
            {
                Close();
            };
            overlay.Controls.Add(menuButton);
        }

        // Освобождение ресурсов при закрытии формы
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);

            // Отписываемся от событий
            var gameManager = GameManager.Instance;
            gameManager.GameStateChanged -= GameManager_GameStateChanged;
            gameManager.ScoreChanged -= GameManager_ScoreChanged;
            gameManager.LivesChanged -= GameManager_LivesChanged;
            gameManager.CollectiblesChanged -= GameManager_CollectiblesChanged;

            // Освобождаем ресурсы контроллера
            _controller.Dispose();
        }
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            // Дополнительный сброс фокуса при показе формы
            Focus();

            // Гарантия активации формы
            if (!this.Focused)
            {
                this.Activate();
                this.Focus();
            }
        }
    }
}