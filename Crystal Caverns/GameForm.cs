using System;
using System.Drawing;
using System.Windows.Forms;
using Crystal_Caverns.Controller;
using Crystal_Caverns.Model;

namespace Crystal_Caverns.View
{
    public partial class GameForm : Form
    {
        private readonly GameController _controller;

        private Label _scoreLabel;
        private Label _livesLabel;
        private Label _crystalsLabel;

        public GameForm()
        {
            InitializeComponent();

            Size = new Size(800, 600);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;
            BackColor = Color.DarkSlateBlue;
            Text = "Crystal Caverns";

            _controller = new GameController();

            KeyDown += GameForm_KeyDown;
            KeyUp += GameForm_KeyUp;
        }

        private void GameForm_Load(object sender, EventArgs e)
        {
            CreateUI();

            SubscribeToEvents();

            _controller.StartGame(this);
        }

        private void CreateUI()
        {
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

        private void SubscribeToEvents()
        {
            var gameManager = GameManager.Instance;

            gameManager.GameStateChanged += GameManager_GameStateChanged;

            gameManager.ScoreChanged += GameManager_ScoreChanged;

            gameManager.LivesChanged += GameManager_LivesChanged;

            gameManager.CollectiblesChanged += GameManager_CollectiblesChanged;
        }


        private void GameManager_GameStateChanged(object sender, GameStateEventArgs e)
        {
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


        private void GameForm_KeyDown(object sender, KeyEventArgs e)
        {
            _controller.HandleKeyDown(e.KeyCode);
        }

        private void GameForm_KeyUp(object sender, KeyEventArgs e)
        {
            _controller.HandleKeyUp(e.KeyCode);
        }

        private void ShowGameOverScreen(bool isVictory)
        {
            Panel overlay = new Panel
            {
                Size = ClientSize,
                Location = new Point(0, 0),
                BackColor = Color.FromArgb(150, 0, 0, 0)
            };
            Controls.Add(overlay);
            overlay.BringToFront();

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

            Button restartButton = new Button
            {
                Text = "Restart",
                Size = new Size(120, 40),
                Location = new Point((ClientSize.Width - 250) / 2, 320)
            };
            restartButton.Click += (s, e) =>
            {
                Controls.Remove(overlay);

                Application.DoEvents();
                System.Threading.Thread.Sleep(50);

                _controller.RestartGame(this);

                Focus();

                Invalidate();
                Update();

                BeginInvoke(new Action(() =>
{
    System.Threading.Thread.Sleep(100);
    Focus();
}));
            };
            overlay.Controls.Add(restartButton);

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

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);

            var gameManager = GameManager.Instance;
            gameManager.GameStateChanged -= GameManager_GameStateChanged;
            gameManager.ScoreChanged -= GameManager_ScoreChanged;
            gameManager.LivesChanged -= GameManager_LivesChanged;
            gameManager.CollectiblesChanged -= GameManager_CollectiblesChanged;

            _controller.Dispose();
        }
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            Focus();

            if (!this.Focused)
            {
                this.Activate();
                this.Focus();
            }
        }
    }
}